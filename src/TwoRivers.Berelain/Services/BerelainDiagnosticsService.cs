using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Records;
using TwoRivers.Berelain.Fields;
using TwoRivers.Berelain.Models;
using YesSql;

namespace TwoRivers.Berelain.Services;

public sealed class BerelainDiagnosticsService : IBerelainDiagnosticsService
{
    private readonly ISession _session;
    private readonly IContentManager _contentManager;
    private readonly IContentDefinitionManager _contentDefinitionManager;
    private readonly IBerelainMetadataService _metadataService;
    private readonly ILogger<BerelainDiagnosticsService> _logger;

    public BerelainDiagnosticsService(
        ISession session,
        IContentManager contentManager,
        IContentDefinitionManager contentDefinitionManager,
        IBerelainMetadataService metadataService,
        ILogger<BerelainDiagnosticsService> logger)
    {
        _session = session;
        _contentManager = contentManager;
        _contentDefinitionManager = contentDefinitionManager;
        _metadataService = metadataService;
        _logger = logger;
    }

    public async Task<BerelainDiagnosticsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var runtimeAllowlist = await _metadataService.GetRuntimeAllowlistAsync(cancellationToken);
        var manifest = await _metadataService.BuildCuratedManifestAsync(cancellationToken);

        // Discover all content types that have a BerelainIconField.
        var iconFieldLocations = await DiscoverIconFieldLocationsAsync();

        var iconAssignments = new List<string>();

        foreach (var location in iconFieldLocations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var contentItemIds = await _session.QueryIndex<ContentItemIndex>(index =>
                    index.Published && index.ContentType == location.ContentType)
                .ListAsync();

            foreach (var index in contentItemIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var contentItem = await _contentManager.GetAsync(index.ContentItemId, VersionOptions.Published);
                var iconKey = ExtractIconKey(contentItem, location.PartName, location.FieldName);
                if (!string.IsNullOrWhiteSpace(iconKey))
                {
                    iconAssignments.Add(iconKey.Trim());
                }
            }
        }

        var usedAssignmentsTotal = iconAssignments.Count;
        var usedCanonicalAssignments = 0;
        var usedLegacyAssignments = 0;

        var canonicalUsageCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var assignment in iconAssignments)
        {
            if (BerelainIconKey.TryParse(assignment, out var parsed) && parsed is not null)
            {
                usedCanonicalAssignments++;
                var key = parsed.ToCanonicalKey();
                if (!canonicalUsageCounts.TryAdd(key, 1))
                {
                    canonicalUsageCounts[key]++;
                }
            }
            else
            {
                usedLegacyAssignments++;
            }
        }

        var distinctUsedCanonicalKeys = canonicalUsageCounts.Keys.ToHashSet(StringComparer.Ordinal);
        var usedCanonicalInAllowlist = distinctUsedCanonicalKeys.Where(runtimeAllowlist.Contains).ToList();
        var usedCanonicalOutsideAllowlist = distinctUsedCanonicalKeys
            .Where(key => !runtimeAllowlist.Contains(key))
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToList();

        var topUsedCanonicalKeys = canonicalUsageCounts
            .OrderByDescending(static kv => kv.Value)
            .ThenBy(static kv => kv.Key, StringComparer.Ordinal)
            .Take(20)
            .Select(static kv => kv.Key)
            .ToList();

        var snapshot = new BerelainDiagnosticsSnapshot(
            DateTimeOffset.UtcNow,
            manifest.IconCount,
            runtimeAllowlist.Count,
            usedAssignmentsTotal,
            usedCanonicalAssignments,
            usedLegacyAssignments,
            distinctUsedCanonicalKeys.Count,
            usedCanonicalInAllowlist.Count,
            usedCanonicalOutsideAllowlist.Count,
            Math.Max(0, runtimeAllowlist.Count - usedCanonicalInAllowlist.Count),
            usedCanonicalOutsideAllowlist,
            topUsedCanonicalKeys);

        _logger.LogInformation(
            "Berelain diagnostics: selected={Selected}, runtimeAllowlist={Allowlist}, usedAssignments={Assignments}, usedCanonicalDistinct={CanonicalDistinct}, outsideAllowlist={OutsideAllowlist}.",
            snapshot.SelectedIconCount,
            snapshot.RuntimeAllowlistCount,
            snapshot.UsedAssignmentsTotal,
            snapshot.DistinctUsedCanonicalCount,
            snapshot.UsedCanonicalOutsideAllowlistCount);

        return snapshot;
    }

    private async Task<List<IconFieldLocation>> DiscoverIconFieldLocationsAsync()
    {
        var locations = new List<IconFieldLocation>();
        var fieldTypeName = nameof(BerelainIconField);

        foreach (var typeDefinition in await _contentDefinitionManager.ListTypeDefinitionsAsync())
        {
            foreach (var partDefinition in typeDefinition.Parts)
            {
                var partDef = partDefinition.PartDefinition;
                foreach (var fieldDefinition in partDef.Fields)
                {
                    if (string.Equals(fieldDefinition.FieldDefinition.Name, fieldTypeName, StringComparison.Ordinal))
                    {
                        locations.Add(new IconFieldLocation(
                            typeDefinition.Name,
                            partDef.Name,
                            fieldDefinition.Name));
                    }
                }
            }
        }

        return locations;
    }

    private static string? ExtractIconKey(ContentItem? contentItem, string partName, string fieldName)
    {
        var partElement = contentItem?.Content?[partName];
        var fieldElement = partElement?[fieldName];
        return (string?)fieldElement?.IconKey;
    }

    private sealed record IconFieldLocation(string ContentType, string PartName, string FieldName);
}
