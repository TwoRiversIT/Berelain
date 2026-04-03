using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;
using TwoRivers.Berelain.Models;

namespace TwoRivers.Berelain.Services;

public sealed class BerelainRenderService : IBerelainRenderService
{
    private readonly IBerelainMetadataService _metadataService;

    public BerelainRenderService(IBerelainMetadataService metadataService)
    {
        _metadataService = metadataService;
    }

    public async Task<IHtmlContent> RenderIconAsync(string? iconKey, string? extraCss = null)
    {
        if (string.IsNullOrWhiteSpace(iconKey))
        {
            return HtmlString.Empty;
        }

        if (!BerelainIconKey.TryParse(iconKey, out var parsed) || parsed is null)
        {
            return HtmlString.Empty;
        }

        var allowlist = await _metadataService.GetRuntimeAllowlistAsync();
        if (!allowlist.Contains(parsed.ToCanonicalKey()))
        {
            return HtmlString.Empty;
        }

        var cssClasses = parsed.ToCssClasses();
        if (!string.IsNullOrWhiteSpace(extraCss))
        {
            cssClasses = $"{cssClasses} {extraCss.Trim()}";
        }

        var encodedClasses = HtmlEncoder.Default.Encode(cssClasses);
        return new HtmlString($"<i class=\"{encodedClasses}\" aria-hidden=\"true\"></i>");
    }
}
