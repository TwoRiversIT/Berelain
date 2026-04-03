using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using TwoRivers.Berelain.Fields;
using TwoRivers.Berelain.Models;

namespace TwoRivers.Berelain.Drivers;

/// <summary>
/// Drives the Settings tab rendered in the content type editor when a
/// <see cref="BerelainIconField"/> definition is being configured.
/// </summary>
public sealed class BerelainIconFieldSettingsDisplayDriver
    : ContentPartFieldDefinitionDisplayDriver<BerelainIconField>
{
    public override IDisplayResult Edit(
        ContentPartFieldDefinition partFieldDefinition,
        BuildEditorContext context)
    {
        return Initialize<BerelainIconFieldSettings>(
            "BerelainIconFieldSettings_Edit",
            model =>
            {
                var settings = partFieldDefinition.GetSettings<BerelainIconFieldSettings>();
                model.AllowSizeSelection = settings.AllowSizeSelection;
                model.AllowColorSelection = settings.AllowColorSelection;
            })
            .Location("Content");
    }

    public override async Task<IDisplayResult> UpdateAsync(
        ContentPartFieldDefinition partFieldDefinition,
        UpdatePartFieldEditorContext context)
    {
        var model = new BerelainIconFieldSettings();
        await context.Updater.TryUpdateModelAsync(model, Prefix);
        context.Builder.WithSettings(model);
        return Edit(partFieldDefinition, context);
    }
}
