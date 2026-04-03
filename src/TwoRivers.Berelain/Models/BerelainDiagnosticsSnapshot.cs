namespace TwoRivers.Berelain.Models;

public sealed record BerelainDiagnosticsSnapshot(
    DateTimeOffset GeneratedUtc,
    int SelectedIconCount,
    int RuntimeAllowlistCount,
    int UsedAssignmentsTotal,
    int UsedCanonicalAssignments,
    int UsedLegacyAssignments,
    int DistinctUsedCanonicalCount,
    int UsedCanonicalInAllowlistCount,
    int UsedCanonicalOutsideAllowlistCount,
    int UnusedSelectedIconCount,
    IReadOnlyList<string> UsedCanonicalOutsideAllowlistKeys,
    IReadOnlyList<string> TopUsedCanonicalKeys);
