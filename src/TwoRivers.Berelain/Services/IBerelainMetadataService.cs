using TwoRivers.Berelain.Models;

namespace TwoRivers.Berelain.Services;

public interface IBerelainMetadataService
{
    Task<IReadOnlyList<BerelainCatalogItem>> GetNormalizedCatalogAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BerelainCatalogItem>> GetAllowedIconsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetRuntimeAllowlistAsync(CancellationToken cancellationToken = default);

    Task<BerelainCuratedManifest> BuildCuratedManifestAsync(CancellationToken cancellationToken = default);

    Task GenerateSubsetCssAsync(CancellationToken cancellationToken = default);
}
