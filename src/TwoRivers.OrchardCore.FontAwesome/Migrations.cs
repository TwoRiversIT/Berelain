using global::OrchardCore.ContentManagement.Metadata;
using global::OrchardCore.ContentManagement.Metadata.Settings;
using global::OrchardCore.Data.Migration;

namespace TwoRivers.Berelain;

public sealed class Migrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionManager;

    public Migrations(IContentDefinitionManager contentDefinitionManager)
    {
        _contentDefinitionManager = contentDefinitionManager;
    }

    public async Task<int> CreateAsync()
    {
        await _contentDefinitionManager.AlterTypeDefinitionAsync("BerelainSettings", type => type
            .DisplayedAs("Font Awesome Settings")
            .WithSettings(new ContentTypeSettings
            {
                Creatable = false,
                Listable = false,
                Draftable = false,
                Versionable = false,
                Securable = false,
                Stereotype = "CustomSettings"
            })
            .WithPart("BerelainSettings", part => part.WithPosition("0")));

        await _contentDefinitionManager.AlterPartDefinitionAsync("BerelainSettings", part => part
            .WithField("AllowClassic", field => field
                .OfType("BooleanField")
                .WithDisplayName("Allow Classic Family")
                .WithPosition("0"))
            .WithField("AllowDuotone", field => field
                .OfType("BooleanField")
                .WithDisplayName("Allow Duotone Family")
                .WithPosition("1"))
            .WithField("AllowSharp", field => field
                .OfType("BooleanField")
                .WithDisplayName("Allow Sharp Family")
                .WithPosition("2"))
            .WithField("AllowSharpDuotone", field => field
                .OfType("BooleanField")
                .WithDisplayName("Allow Sharp Duotone Family")
                .WithPosition("3"))
            .WithField("AllowBrands", field => field
                .OfType("BooleanField")
                .WithDisplayName("Allow Brands Family")
                .WithPosition("4"))
            .WithField("AllowSolid", field => field
                .OfType("BooleanField")
                .WithDisplayName("Allow Solid Style")
                .WithPosition("5"))
            .WithField("AllowRegular", field => field
                .OfType("BooleanField")
                .WithDisplayName("Allow Regular Style")
                .WithPosition("6"))
            .WithField("AllowLight", field => field
                .OfType("BooleanField")
                .WithDisplayName("Allow Light Style")
                .WithPosition("7"))
            .WithField("AllowThin", field => field
                .OfType("BooleanField")
                .WithDisplayName("Allow Thin Style")
                .WithPosition("8")));

        return 1;
    }

    public async Task<int> UpdateFrom1Async()
    {
        await _contentDefinitionManager.AlterTypeDefinitionAsync("BerelainSettings", type => type
            .WithSettings(new ContentTypeSettings
            {
                Creatable = false,
                Listable = false,
                Draftable = false,
                Versionable = false,
                Securable = false,
                Stereotype = "CustomSettings"
            }));

        return 2;
    }
}
