using OrchardCore.ContentManagement;

namespace TwoRivers.Berelain.Models;

public readonly record struct BerelainAllowlist(
    bool AllowClassic,
    bool AllowDuotone,
    bool AllowSharp,
    bool AllowSharpDuotone,
    bool AllowBrands,
    bool AllowSolid,
    bool AllowRegular,
    bool AllowLight,
    bool AllowThin)
{
    public static BerelainAllowlist FromContentItem(ContentItem? settingsItem)
    {
        dynamic? part = settingsItem?.Content?.BerelainSettings;

        return new BerelainAllowlist(
            ReadBool(part?.AllowClassic?.Value),
            ReadBool(part?.AllowDuotone?.Value),
            ReadBool(part?.AllowSharp?.Value),
            ReadBool(part?.AllowSharpDuotone?.Value),
            ReadBool(part?.AllowBrands?.Value),
            ReadBool(part?.AllowSolid?.Value),
            ReadBool(part?.AllowRegular?.Value),
            ReadBool(part?.AllowLight?.Value),
            ReadBool(part?.AllowThin?.Value));
    }

    public bool IsAllowed(string family, string style)
    {
        if (!IsFamilyAllowed(family))
        {
            return false;
        }

        if (string.Equals(family, "brands", StringComparison.Ordinal))
        {
            return true;
        }

        return style switch
        {
            "solid" => AllowSolid,
            "regular" => AllowRegular,
            "light" => AllowLight,
            "thin" => AllowThin,
            _ => false
        };
    }

    public string ToSignature()
    {
        return string.Join('|',
            AllowClassic,
            AllowDuotone,
            AllowSharp,
            AllowSharpDuotone,
            AllowBrands,
            AllowSolid,
            AllowRegular,
            AllowLight,
            AllowThin);
    }

    private bool IsFamilyAllowed(string family)
    {
        return family switch
        {
            "classic" => AllowClassic,
            "duotone" => AllowDuotone,
            "sharp" => AllowSharp,
            "sharp-duotone" => AllowSharpDuotone,
            "brands" => AllowBrands,
            _ => false
        };
    }

    private static bool ReadBool(object? value)
    {
        return value as bool? ?? true;
    }
}
