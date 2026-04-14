using TwoRivers.Berelain.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace TwoRivers.Berelain.Services;

public sealed class BerelainSvgService : IBerelainSvgService
{
    // Web-root-relative prefix candidates for the SVG source files.
    private static readonly string[] SvgRootUriCandidates =
    [
        "lib/fontawesome/svgs",
        "lib/fontawesome-pro/svgs",
        "lib/fontawesome-pro-plus-7.2.0-web/svgs",
    ];

    // Source-tree-relative candidates for development (ContentRoot-relative).
    private static readonly string[] SourceTreeSvgRootCandidates =
    [
        "wwwroot\\lib\\fontawesome\\svgs",
        "wwwroot\\lib\\fontawesome-pro\\svgs",
        "wwwroot\\lib\\fontawesome-pro-plus-7.2.0-web\\svgs",
    ];

    private static readonly JsonSerializerOptions ManifestJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IWebHostEnvironment _webHostEnvironment;

    // Tracks the cached manifest so callers avoid re-reading the file on every request.
    private BerelainSpriteManifest? _cachedManifest;

    public BerelainSvgService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public bool IsSvgModeAvailable() => ResolveSvgsRoot() is not null;

    public async Task BuildSpriteAsync(
        IReadOnlyList<BerelainCatalogItem> icons,
        CancellationToken cancellationToken = default)
    {
        var svgsRoot = ResolveSvgsRoot();
        if (svgsRoot is null)
        {
            return;
        }

        var symbols = new List<string>(icons.Count);
        var spriteIds = new List<string>(icons.Count);

        foreach (var icon in icons)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var svgDirectory = GetSvgDirectory(icon.Family, icon.Style);
            var svgFilePath = Path.Combine(svgsRoot, svgDirectory, icon.Name + ".svg");

            if (!File.Exists(svgFilePath))
            {
                continue;
            }

            var svgContent = await File.ReadAllTextAsync(svgFilePath, cancellationToken);
            var (viewBox, innerXml) = ExtractSvgContent(svgContent);

            if (innerXml is null)
            {
                continue;
            }

            var symbolId = $"fa-{icon.Family}-{icon.Style}-{icon.Name}";
            symbols.Add($"<symbol id=\"{symbolId}\" viewBox=\"{viewBox}\">{innerXml}</symbol>");
            spriteIds.Add(symbolId);
        }

        var outputDirectory = Path.Combine(_webHostEnvironment.ContentRootPath, "App_Data", "FontAwesome");
        Directory.CreateDirectory(outputDirectory);

        var spritePath = Path.Combine(outputDirectory, "curated-sprite.svg");
        var spriteBuilder = new StringBuilder();
        spriteBuilder.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" style=\"display:none\">");
        foreach (var symbol in symbols)
        {
            spriteBuilder.AppendLine(symbol);
        }
        spriteBuilder.Append("</svg>");

        await File.WriteAllTextAsync(spritePath, spriteBuilder.ToString(), cancellationToken);

        var manifest = new BerelainSpriteManifest(
            GeneratedUtc: DateTimeOffset.UtcNow,
            SettingsSignature: string.Empty,
            SpriteFilePath: spritePath,
            IconCount: spriteIds.Count,
            SpriteIds: spriteIds);

        var manifestPath = Path.Combine(outputDirectory, "curated-sprite-manifest.json");
        await File.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(manifest, ManifestJsonOptions),
            cancellationToken);

        _cachedManifest = manifest;
    }

    public async Task<BerelainSpriteManifest?> GetSpriteManifestAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedManifest is not null)
        {
            return _cachedManifest;
        }

        var manifestPath = Path.Combine(
            _webHostEnvironment.ContentRootPath,
            "App_Data",
            "FontAwesome",
            "curated-sprite-manifest.json");

        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        _cachedManifest = JsonSerializer.Deserialize<BerelainSpriteManifest>(json, ManifestJsonOptions);
        return _cachedManifest;
    }

    /// <summary>
    /// Resolves the physical path to the FA SVG source directory, or null if not found.
    /// Uses the web root file provider first (production), then source-tree paths (development).
    /// </summary>
    private string? ResolveSvgsRoot()
    {
        var provider = _webHostEnvironment.WebRootFileProvider;

        foreach (var candidate in SvgRootUriCandidates)
        {
            // Probe a well-known subdirectory; if it exposes a physical path, we can derive the root.
            var solidDir = provider.GetDirectoryContents($"{candidate}/solid");
            if (!solidDir.Exists)
            {
                continue;
            }

            var firstSvg = solidDir.FirstOrDefault(f =>
                f.Name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) && f.PhysicalPath is not null);

            if (firstSvg?.PhysicalPath is not null)
            {
                // PhysicalPath: .../svgs/solid/name.svg — go up two levels to reach .../svgs
                var svgsRoot = Path.GetDirectoryName(Path.GetDirectoryName(firstSvg.PhysicalPath));
                if (svgsRoot is not null && Directory.Exists(svgsRoot))
                {
                    return svgsRoot;
                }
            }
        }

        foreach (var relativePath in SourceTreeSvgRootCandidates)
        {
            var physicalPath = Path.GetFullPath(
                Path.Combine(_webHostEnvironment.ContentRootPath, relativePath));

            if (Directory.Exists(physicalPath))
            {
                return physicalPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Maps a canonical FA key (family/style) to the on-disk SVG subdirectory name.
    /// FA Pro stores svgs as: classic family → style name; sharp family → sharp-{style}; duotone/solid → duotone.
    /// </summary>
    private static string GetSvgDirectory(string family, string style) =>
        (family.ToLowerInvariant(), style.ToLowerInvariant()) switch
        {
            ("classic", _) => style,
            ("brands", _) => "brands",
            ("sharp", _) => $"sharp-{style}",
            ("duotone", "solid") => "duotone",
            ("duotone", _) => $"duotone-{style}",
            _ => $"{family}-{style}"
        };

    /// <summary>
    /// Extracts the viewBox attribute and inner XML from an SVG file.
    /// Returns (viewBox, innerXml) where innerXml is null if parsing fails.
    /// </summary>
    private static (string viewBox, string? innerXml) ExtractSvgContent(string svgContent)
    {
        try
        {
            var doc = XDocument.Parse(svgContent);
            var viewBox = doc.Root?.Attribute("viewBox")?.Value ?? "0 0 512 512";
            var innerXml = string.Concat(
                doc.Root?.Elements()
                    .Select(element => element.ToString(SaveOptions.DisableFormatting))
                ?? []);

            return (viewBox, innerXml.Length > 0 ? innerXml : null);
        }
        catch
        {
            return ("0 0 512 512", null);
        }
    }
}
