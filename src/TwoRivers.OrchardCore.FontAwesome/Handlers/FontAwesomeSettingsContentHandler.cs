using TwoRivers.Berelain.Services;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;

namespace TwoRivers.Berelain.Handlers;

public sealed class BerelainSettingsContentHandler : ContentHandlerBase
{
    private const string BerelainSettingsType = "BerelainSettings";

    private readonly IBerelainMetadataService _metadataService;
    private readonly IEnumerable<IBerelainSvgService> _svgServices;
    private readonly ILogger<BerelainSettingsContentHandler> _logger;

    public BerelainSettingsContentHandler(
        IBerelainMetadataService metadataService,
        IEnumerable<IBerelainSvgService> svgServices,
        ILogger<BerelainSettingsContentHandler> logger)
    {
        _metadataService = metadataService;
        _svgServices = svgServices;
        _logger = logger;
    }

    public override async Task PublishedAsync(PublishContentContext context)
    {
        await RebuildManifestIfNeededAsync(context.ContentItem);
    }

    private async Task RebuildManifestIfNeededAsync(ContentItem? contentItem)
    {
        if (!string.Equals(contentItem?.ContentType, BerelainSettingsType, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            var manifest = await _metadataService.BuildCuratedManifestAsync();
            await _metadataService.GetRuntimeAllowlistAsync();
            await _metadataService.GenerateSubsetCssAsync();

            var svgService = _svgServices.FirstOrDefault();
            if (svgService is not null && svgService.IsSvgModeAvailable())
            {
                await svgService.BuildSpriteAsync(manifest.Icons);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to rebuild the curated icon manifest after settings update.");
        }
    }
}
