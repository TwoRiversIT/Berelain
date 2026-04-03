using TwoRivers.Berelain.Models;

namespace TwoRivers.Berelain.Services;

public interface IBerelainDiagnosticsService
{
    Task<BerelainDiagnosticsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
