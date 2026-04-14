using System;
using System.Text.RegularExpressions;

namespace TwoRivers.Berelain.Models;

public sealed record BerelainIconKey(string Family, string Style, string Name)
{
    private static readonly Regex SegmentRegex = new("^[a-z0-9-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryParse(string? value, out BerelainIconKey? iconKey)
    {
        iconKey = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Trim().Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        var family = NormalizeSegment(parts[0]);
        var style = NormalizeSegment(parts[1]);
        var name = NormalizeSegment(parts[2]);

        if (!IsValidSegment(family) || !IsValidSegment(style) || !IsValidSegment(name))
        {
            return false;
        }

        iconKey = new BerelainIconKey(family, style, name);
        return true;
    }

    public static bool TryResolveCssClasses(string? value, out string cssClasses)
    {
        cssClasses = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (TryParse(value, out var parsed) && parsed != null)
        {
            cssClasses = parsed.ToCssClasses();
            return true;
        }

        // Backward compatibility: preserve existing free-text class values.
        cssClasses = value.Trim();
        return true;
    }

    public string ToCanonicalKey() => $"{Family}/{Style}/{Name}";

    public string ToCssClasses()
    {
        var familyClass = Family switch
        {
            "classic" => "fa-classic",
            "duotone" => "fa-duotone",
            "sharp" => "fa-sharp",
            "sharp-duotone" => "fa-sharp-duotone",
            "brands" => "fa-brands",
            _ => $"fa-{Family}"
        };

        var styleClass = Family == "brands"
            ? string.Empty
            : $" fa-{Style}";

        return $"{familyClass}{styleClass} fa-{Name}";
    }

    private static bool IsValidSegment(string value) => SegmentRegex.IsMatch(value);

    private static string NormalizeSegment(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized.StartsWith("fa-", StringComparison.Ordinal) ? normalized[3..] : normalized;
    }
}
