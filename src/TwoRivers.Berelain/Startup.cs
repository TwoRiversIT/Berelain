using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using OrchardCore.ResourceManagement;
using TwoRivers.Berelain.Drivers;
using TwoRivers.Berelain.Fields;
using TwoRivers.Berelain.Handlers;
using TwoRivers.Berelain.Services;

namespace TwoRivers.Berelain;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IBerelainMetadataService, BerelainMetadataService>();
        services.AddScoped<IBerelainDiagnosticsService, BerelainDiagnosticsService>();
        services.AddScoped<IBerelainRenderService, BerelainRenderService>();
        services.AddScoped<IContentHandler, BerelainSettingsContentHandler>();

        services.AddContentField<BerelainIconField>()
            .UseDisplayDriver<BerelainIconFieldDisplayDriver>();
        services.AddScoped<IContentPartFieldDefinitionDisplayDriver, BerelainIconFieldSettingsDisplayDriver>();
        services.AddTransient<IConfigureOptions<ResourceManagementOptions>, ResourceManagementOptionsConfiguration>();
        services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

        services.AddScoped<IDataMigration, Migrations>();
    }
}
