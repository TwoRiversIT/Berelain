namespace TwoRivers.Berelain.Models;

public sealed record BerelainCuratedManifest(
    DateTimeOffset GeneratedUtc,
    string MetadataSource,
    string SettingsSignature,
    int IconCount,
    IReadOnlyList<BerelainCatalogItem> Icons);
