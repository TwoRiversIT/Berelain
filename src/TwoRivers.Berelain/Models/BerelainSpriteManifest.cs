namespace TwoRivers.Berelain.Models;

public sealed record BerelainSpriteManifest(
    DateTimeOffset GeneratedUtc,
    string SettingsSignature,
    string SpriteFilePath,
    int IconCount,
    IReadOnlyList<string> SpriteIds);
