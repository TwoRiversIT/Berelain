using OrchardCore.ContentManagement;

namespace TwoRivers.Berelain.Fields;

public class BerelainIconField : ContentField
{
    public string? IconKey { get; set; }

    /// <summary>Font Awesome size modifier class, e.g. "fa-lg". Null means inherit.</summary>
    public string? Size { get; set; }

    /// <summary>Bootstrap text-color class, e.g. "text-primary". Null means inherit.</summary>
    public string? Color { get; set; }
}
