namespace TwoRivers.Berelain.Models;

/// <summary>
/// Per-field-definition settings that control which appearance properties are
/// exposed in the content editor. Defaults to false for both flags so that
/// existing field definitions render without size or color controls until
/// explicitly opted in.
/// </summary>
public class BerelainIconFieldSettings
{
    /// <summary>
    /// When true, the field editor shows a size dropdown (fa-2xs through fa-2xl).
    /// When false, the <see cref="TwoRivers.Berelain.Fields.BerelainIconField.Size"/>
    /// value is cleared on save.
    /// </summary>
    public bool AllowSizeSelection { get; set; }

    /// <summary>
    /// When true, the field editor shows a Bootstrap semantic text-color dropdown.
    /// When false, the <see cref="TwoRivers.Berelain.Fields.BerelainIconField.Color"/>
    /// value is cleared on save.
    /// </summary>
    public bool AllowColorSelection { get; set; }
}
