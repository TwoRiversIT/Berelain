using TwoRivers.Berelain.Services;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace TwoRivers.Berelain;

[Feature("TwoRivers.Berelain.SvgSprites")]
public sealed class StartupSvgSprites : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IBerelainSvgService, BerelainSvgService>();
    }
}
