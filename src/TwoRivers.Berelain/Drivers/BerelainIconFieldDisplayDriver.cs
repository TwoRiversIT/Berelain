using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using TwoRivers.Berelain.Fields;
using TwoRivers.Berelain.Models;
using TwoRivers.Berelain.ViewModels;

namespace TwoRivers.Berelain.Drivers;

public sealed class BerelainIconFieldDisplayDriver : ContentFieldDisplayDriver<BerelainIconField>
{
    private static readonly string[] AdminCssCandidates =
    [
        "lib/fontawesome/css/all.min.css",
        "lib/fontawesome-pro/css/all.min.css",
        "lib/fontawesome-pro-plus-7.2.0-web/css/all.min.css",
        "lib/fontawesome/css/fontawesome.min.css"
    ];

    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly IActionContextAccessor _actionContextAccessor;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public BerelainIconFieldDisplayDriver(
        IUrlHelperFactory urlHelperFactory,
        IActionContextAccessor actionContextAccessor,
        IWebHostEnvironment webHostEnvironment)
    {
        _urlHelperFactory = urlHelperFactory;
        _actionContextAccessor = actionContextAccessor;
        _webHostEnvironment = webHostEnvironment;
    }

    public override IDisplayResult Display(BerelainIconField field, BuildFieldDisplayContext context)
    {
        return Initialize<BerelainIconFieldDisplayViewModel>(
            GetDisplayShapeType(context),
            model =>
            {
                model.IconKey = field.IconKey;
                model.CssClasses = BuildDisplayCssClasses(field);
            })
            .Location("Detail", "Content")
            .Location("Summary", "Content");
    }

    public override IDisplayResult Edit(BerelainIconField field, BuildFieldEditorContext context)
    {
        var settings = context.PartFieldDefinition.GetSettings<BerelainIconFieldSettings>();

        return Initialize<BerelainIconFieldEditViewModel>(
            GetEditorShapeType(context),
            model =>
            {
                model.IconKey = field.IconKey;
                model.Size = field.Size;
                model.Color = field.Color;
                model.AllowSizeSelection = settings.AllowSizeSelection;
                model.AllowColorSelection = settings.AllowColorSelection;

                var actionContext = _actionContextAccessor.ActionContext;
                if (actionContext is not null)
                {
                    var urlHelper = _urlHelperFactory.GetUrlHelper(actionContext);
                    model.CuratedIconsApiUrl = urlHelper.Action(
                        "CuratedIcons",
                        "BerelainAdmin",
                        new { area = "TwoRivers.Berelain" }) ?? "/berelain/admin/curated-icons";
                    model.SubsetCssUrl = urlHelper.Content("~/lib/fontawesome/curated-icons.min.css");

                    var cssPath = ResolveAdminCssPath();
                    if (!string.IsNullOrEmpty(cssPath))
                    {
                        model.IconLibraryCssUrl = urlHelper.Content($"~/{cssPath}");
                    }
                }
            })
            .Location("Content");
    }

    public override async Task<IDisplayResult> UpdateAsync(BerelainIconField field, UpdateFieldEditorContext context)
    {
        var settings = context.PartFieldDefinition.GetSettings<BerelainIconFieldSettings>();
        var viewModel = new BerelainIconFieldEditViewModel();
        await context.Updater.TryUpdateModelAsync(viewModel, Prefix);

        field.IconKey = viewModel.IconKey?.Trim();
        field.Size = NormalizeAppearanceValue(settings.AllowSizeSelection, viewModel.Size);
        field.Color = NormalizeAppearanceValue(settings.AllowColorSelection, viewModel.Color);

        return Edit(field, context);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds the full CSS class string for front-end rendering, combining the icon
    /// identity classes with any stored size modifier and color class.
    /// </summary>
    private static string BuildDisplayCssClasses(BerelainIconField field)
    {
        if (!BerelainIconKey.TryParse(field.IconKey, out var parsed) || parsed is null)
        {
            return string.Empty;
        }

        var parts = new List<string>(3) { parsed.ToCssClasses() };

        if (!string.IsNullOrEmpty(field.Size))
        {
            parts.Add(field.Size);
        }

        if (!string.IsNullOrEmpty(field.Color))
        {
            parts.Add(field.Color);
        }

        return string.Join(' ', parts);
    }

    /// <summary>
    /// Returns the trimmed value when the corresponding field setting is enabled and
    /// the value is non-empty; otherwise returns null to clear any stored value.
    /// </summary>
    private static string? NormalizeAppearanceValue(bool settingEnabled, string? value)
    {
        if (!settingEnabled)
        {
            return null;
        }

        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private string ResolveAdminCssPath()
    {
        var provider = _webHostEnvironment.WebRootFileProvider;
        foreach (var candidate in AdminCssCandidates)
        {
            if (provider.GetFileInfo(candidate).Exists)
            {
                return candidate;
            }
        }

        return string.Empty;
    }
}
