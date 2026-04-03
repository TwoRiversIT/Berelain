using TwoRivers.Berelain.Models;

namespace TwoRivers.Berelain.Services;

/// <summary>
/// Builds and serves an SVG sprite from the allowed Font Awesome icon set.
/// This service is only registered when the SvgSprites feature is enabled.
/// </summary>
public interface IBerelainSvgService
{
    /// <summary>
    /// Returns true if the SVG source files are present and the service can build a sprite.
    /// </summary>
    bool IsSvgModeAvailable();

    /// <summary>
    /// Builds a curated SVG sprite from the supplied icon list and writes it to App_Data.
    /// </summary>
    Task BuildSpriteAsync(IReadOnlyList<BerelainCatalogItem> icons, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the sprite manifest, or null if the sprite has not been built yet.
    /// </summary>
    Task<BerelainSpriteManifest?> GetSpriteManifestAsync(CancellationToken cancellationToken = default);
}
