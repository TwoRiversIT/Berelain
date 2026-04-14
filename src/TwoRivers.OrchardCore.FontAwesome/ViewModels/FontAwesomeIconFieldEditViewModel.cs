namespace TwoRivers.Berelain.ViewModels;

public class BerelainIconFieldEditViewModel
{
    // ── Field values ──────────────────────────────────────────────────────────

    public string? IconKey { get; set; }

    /// <summary>Font Awesome size modifier class selected by the editor, e.g. "fa-lg".</summary>
    public string? Size { get; set; }

    /// <summary>Bootstrap text-color class selected by the editor, e.g. "text-primary".</summary>
    public string? Color { get; set; }

    // ── Field definition settings (read-only in the edit view) ────────────────

    public bool AllowSizeSelection { get; set; }
    public bool AllowColorSelection { get; set; }

    // ── Infrastructure URLs ───────────────────────────────────────────────────

    public string CuratedIconsApiUrl { get; set; } = string.Empty;
    public string IconLibraryCssUrl { get; set; } = string.Empty;
    public string SubsetCssUrl { get; set; } = string.Empty;
}
