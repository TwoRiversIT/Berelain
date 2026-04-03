using Microsoft.AspNetCore.Html;

namespace TwoRivers.Berelain.Services;

public interface IBerelainRenderService
{
    Task<IHtmlContent> RenderIconAsync(string? iconKey, string? extraCss = null);
}
