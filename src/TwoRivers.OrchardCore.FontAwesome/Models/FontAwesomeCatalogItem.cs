namespace TwoRivers.Berelain.Models;

public sealed record BerelainCatalogItem(
    string Key,
    string Family,
    string Style,
    string Name,
    string Label,
    string Unicode,
    IReadOnlyList<string> SearchTerms,
    bool IsFree);
