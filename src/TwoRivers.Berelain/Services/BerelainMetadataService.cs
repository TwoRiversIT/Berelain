using TwoRivers.Berelain.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using OrchardCore.ContentManagement;
using OrchardCore.Settings;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace TwoRivers.Berelain.Services;

public sealed class BerelainMetadataService : IBerelainMetadataService
{
    // Web-root-relative asset candidates under the host application's wwwroot.
    // The module does not assume a specific theme or host project name.
    private static readonly string[] MetadataPathCandidates =
    [
        "lib/fontawesome/metadata/icon-families.yml",
        "lib/fontawesome-pro/metadata/icon-families.yml",
        "lib/fontawesome-pro-plus-7.2.0-web/metadata/icon-families.yml"
    ];

    private static readonly string[] SourceTreeMetadataPathCandidates =
    [
        "wwwroot\\lib\\fontawesome\\metadata\\icon-families.yml",
        "wwwroot\\lib\\fontawesome-pro\\metadata\\icon-families.yml",
        "wwwroot\\lib\\fontawesome-pro-plus-7.2.0-web\\metadata\\icon-families.yml"
    ];

    private static readonly string[] CssPathCandidates =
    [
        "lib/fontawesome/css/all.min.css",
        "lib/fontawesome-pro/css/all.min.css",
        "lib/fontawesome-pro-plus-7.2.0-web/css/all.min.css"
    ];

    private static readonly string[] SourceTreeCssPathCandidates =
    [
        "wwwroot\\lib\\fontawesome\\css\\all.min.css",
        "wwwroot\\lib\\fontawesome-pro\\css\\all.min.css",
        "wwwroot\\lib\\fontawesome-pro-plus-7.2.0-web\\css\\all.min.css"
    ];

    private static readonly Regex LabelWordRegex = new("[a-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex FontFaceBlockRegex = new(@"@font-face\{[^}]+\}", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex FontFamilyRegex = new(@"font-family:\s*['""]?([^'""};]+)['""]?;", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex IconSelectorRegex = new(@"\.fa-(?:[a-z0-9-]+[,\s{])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex IconNameRegex = new(@"\.fa-([a-z0-9-]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly JsonSerializerOptions _manifestJsonOptions = new(JsonSerializerDefaults.Web);

    private IReadOnlyList<BerelainCatalogItem> _cachedMetadataCatalog = Array.Empty<BerelainCatalogItem>();
    private bool _hasCachedMetadataCatalog;
    private DateTimeOffset _cachedMetadataLastModifiedUtc = DateTimeOffset.MinValue;
    private string _cachedMetadataSource = string.Empty;

    private IReadOnlyList<BerelainCatalogItem> _cachedAllowedIcons = Array.Empty<BerelainCatalogItem>();
    private bool _hasCachedAllowedIcons;
    private DateTimeOffset _cachedAllowedIconsLastModifiedUtc = DateTimeOffset.MinValue;
    private string _cachedAllowedIconsSettingsSignature = string.Empty;
    private IReadOnlySet<string> _cachedRuntimeAllowlist = new HashSet<string>(StringComparer.Ordinal);
    private bool _hasCachedRuntimeAllowlist;
    private string _cachedRuntimeAllowlistSignature = string.Empty;
    private BerelainCuratedManifest? _cachedManifest;

    public BerelainMetadataService(IWebHostEnvironment webHostEnvironment, IServiceScopeFactory serviceScopeFactory)
    {
        _webHostEnvironment = webHostEnvironment;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<IReadOnlyList<BerelainCatalogItem>> GetNormalizedCatalogAsync(CancellationToken cancellationToken = default)
    {
        var metadataFile = ResolveMetadataFile();
        if (metadataFile is null || !metadataFile.Exists)
        {
            return Array.Empty<BerelainCatalogItem>();
        }

        if (IsMetadataCacheValid(metadataFile))
        {
            return _cachedMetadataCatalog;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (IsMetadataCacheValid(metadataFile))
            {
                return _cachedMetadataCatalog;
            }

            var loadedCatalog = await LoadAndNormalizeCatalogAsync(metadataFile, cancellationToken);
            _cachedMetadataCatalog = new ReadOnlyCollection<BerelainCatalogItem>(loadedCatalog);
            _cachedMetadataLastModifiedUtc = metadataFile.LastModified;
            _cachedMetadataSource = metadataFile.PhysicalPath ?? metadataFile.Name;
            _hasCachedMetadataCatalog = true;

            await PersistNormalizedCatalogAsync(
                _cachedMetadataCatalog,
                _cachedMetadataSource,
                _cachedMetadataLastModifiedUtc,
                cancellationToken);

            return _cachedMetadataCatalog;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<IReadOnlyList<BerelainCatalogItem>> GetAllowedIconsAsync(CancellationToken cancellationToken = default)
    {
        var metadataFile = ResolveMetadataFile();
        if (metadataFile is null || !metadataFile.Exists)
        {
            return Array.Empty<BerelainCatalogItem>();
        }

        ContentItem? settingsItem;
        await using (var scope = _serviceScopeFactory.CreateAsyncScope())
        {
            var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
            settingsItem = await siteService.GetCustomSettingsAsync("BerelainSettings");
        }
        var allowlist = BerelainAllowlist.FromContentItem(settingsItem);
        var settingsSignature = allowlist.ToSignature();
        var normalizedCatalog = await GetNormalizedCatalogAsync(cancellationToken);

        if (IsAllowedIconsCacheValid(metadataFile, settingsSignature))
        {
            return _cachedAllowedIcons;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (IsAllowedIconsCacheValid(metadataFile, settingsSignature))
            {
                return _cachedAllowedIcons;
            }

            var filtered = FilterAllowedIcons(normalizedCatalog, allowlist);
            _cachedAllowedIcons = new ReadOnlyCollection<BerelainCatalogItem>(filtered);
            _cachedAllowedIconsLastModifiedUtc = metadataFile.LastModified;
            _cachedAllowedIconsSettingsSignature = settingsSignature;
            _hasCachedAllowedIcons = true;

            return _cachedAllowedIcons;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<BerelainCuratedManifest> BuildCuratedManifestAsync(CancellationToken cancellationToken = default)
    {
        var metadataFile = ResolveMetadataFile();
        if (metadataFile is null || !metadataFile.Exists)
        {
            return new BerelainCuratedManifest(
                DateTimeOffset.UtcNow,
                string.Empty,
                string.Empty,
                0,
                Array.Empty<BerelainCatalogItem>());
        }

        ContentItem? settingsItem;
        await using (var scope = _serviceScopeFactory.CreateAsyncScope())
        {
            var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
            settingsItem = await siteService.GetCustomSettingsAsync("BerelainSettings");
        }
        var allowlist = BerelainAllowlist.FromContentItem(settingsItem);
        var settingsSignature = allowlist.ToSignature();

        var allowedIcons = await GetAllowedIconsAsync(cancellationToken);

        if (_cachedManifest is not null
            && _cachedAllowedIconsLastModifiedUtc == metadataFile.LastModified
            && string.Equals(_cachedManifest.SettingsSignature, settingsSignature, StringComparison.Ordinal)
            && _cachedManifest.IconCount == allowedIcons.Count)
        {
            return _cachedManifest;
        }

        var manifest = new BerelainCuratedManifest(
            DateTimeOffset.UtcNow,
            metadataFile.PhysicalPath ?? metadataFile.Name,
            settingsSignature,
            allowedIcons.Count,
            allowedIcons);

        await PersistManifestAsync(manifest, cancellationToken);
        _cachedManifest = manifest;

        return manifest;
    }

    public async Task<IReadOnlySet<string>> GetRuntimeAllowlistAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await BuildCuratedManifestAsync(cancellationToken);
        var signature = BuildRuntimeAllowlistSignature(manifest);

        if (IsRuntimeAllowlistCacheValid(signature))
        {
            return _cachedRuntimeAllowlist;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (IsRuntimeAllowlistCacheValid(signature))
            {
                return _cachedRuntimeAllowlist;
            }

            var allowlist = manifest.Icons
                .Select(static x => x.Key)
                .ToHashSet(StringComparer.Ordinal);

            _cachedRuntimeAllowlist = allowlist;
            _cachedRuntimeAllowlistSignature = signature;
            _hasCachedRuntimeAllowlist = true;

            await PersistRuntimeAllowlistAsync(manifest, allowlist, cancellationToken);

            return _cachedRuntimeAllowlist;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task GenerateSubsetCssAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await BuildCuratedManifestAsync(cancellationToken);

        if (manifest.IconCount == 0)
        {
            await PersistSubsetCssAsync(string.Empty, cancellationToken);
            return;
        }

        var cssPath = ResolveCssFile();
        if (string.IsNullOrEmpty(cssPath) || !File.Exists(cssPath))
        {
            await PersistSubsetCssAsync(string.Empty, cancellationToken);
            return;
        }

        var fullCss = await File.ReadAllTextAsync(cssPath, cancellationToken);

        var curatedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var icon in manifest.Icons)
        {
            curatedNames.Add(icon.Name);
        }

        var neededFontFamilies = new HashSet<string>(StringComparer.Ordinal);
        foreach (var icon in manifest.Icons)
        {
            var fontFamily = MapToFontFamily(icon.Family, icon.Style);
            if (!string.IsNullOrEmpty(fontFamily))
            {
                neededFontFamilies.Add(fontFamily);
            }
        }

        var fontFaces = ExtractFontFaces(fullCss, neededFontFamilies);
        var iconRules = ExtractIconRules(fullCss, curatedNames);

        await PersistSubsetCssAsync(string.Concat(fontFaces, iconRules), cancellationToken);
    }

    private bool IsMetadataCacheValid(IFileInfo metadataFile)
    {
        return _hasCachedMetadataCatalog
            && _cachedMetadataLastModifiedUtc == metadataFile.LastModified;
    }

    private bool IsAllowedIconsCacheValid(IFileInfo metadataFile, string settingsSignature)
    {
        return _hasCachedAllowedIcons
            && _cachedAllowedIconsLastModifiedUtc == metadataFile.LastModified
            && string.Equals(_cachedAllowedIconsSettingsSignature, settingsSignature, StringComparison.Ordinal);
    }

    private bool IsRuntimeAllowlistCacheValid(string signature)
    {
        return _hasCachedRuntimeAllowlist
            && string.Equals(_cachedRuntimeAllowlistSignature, signature, StringComparison.Ordinal);
    }

    private static string BuildRuntimeAllowlistSignature(BerelainCuratedManifest manifest)
    {
        return string.Concat(
            manifest.SettingsSignature,
            "|",
            manifest.IconCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private IFileInfo? ResolveMetadataFile()
    {
        var provider = _webHostEnvironment.WebRootFileProvider;
        foreach (var candidate in MetadataPathCandidates)
        {
            var fileInfo = provider.GetFileInfo(candidate);
            if (fileInfo.Exists)
            {
                return fileInfo;
            }
        }

        foreach (var relativePath in SourceTreeMetadataPathCandidates)
        {
            var physicalPath = Path.GetFullPath(Path.Combine(_webHostEnvironment.ContentRootPath, relativePath));
            if (!File.Exists(physicalPath))
            {
                continue;
            }

            var directoryPath = Path.GetDirectoryName(physicalPath);
            var fileName = Path.GetFileName(physicalPath);
            if (string.IsNullOrWhiteSpace(directoryPath) || string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            var physicalProvider = new PhysicalFileProvider(directoryPath);
            var fileInfo = physicalProvider.GetFileInfo(fileName);
            if (fileInfo.Exists)
            {
                return fileInfo;
            }
        }

        return null;
    }

    private string ResolveCssFile()
    {
        var provider = _webHostEnvironment.WebRootFileProvider;
        foreach (var candidate in CssPathCandidates)
        {
            var fileInfo = provider.GetFileInfo(candidate);
            if (fileInfo.Exists && fileInfo.PhysicalPath is not null)
            {
                return fileInfo.PhysicalPath;
            }
        }

        foreach (var relativePath in SourceTreeCssPathCandidates)
        {
            var physicalPath = Path.GetFullPath(Path.Combine(_webHostEnvironment.ContentRootPath, relativePath));
            if (File.Exists(physicalPath))
            {
                return physicalPath;
            }
        }

        return string.Empty;
    }

    private async Task PersistManifestAsync(BerelainCuratedManifest manifest, CancellationToken cancellationToken)
    {
        var outputDirectory = Path.Combine(_webHostEnvironment.ContentRootPath, "App_Data", "FontAwesome");
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(outputDirectory, "curated-icons.json");
        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(stream, manifest, _manifestJsonOptions, cancellationToken);
    }

    private async Task PersistNormalizedCatalogAsync(
        IReadOnlyList<BerelainCatalogItem> catalog,
        string source,
        DateTimeOffset lastModifiedUtc,
        CancellationToken cancellationToken)
    {
        var outputDirectory = Path.Combine(_webHostEnvironment.ContentRootPath, "App_Data", "FontAwesome");
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(outputDirectory, "normalized-icons.json");
        await using var stream = File.Create(outputPath);

        var payload = new
        {
            generatedUtc = DateTimeOffset.UtcNow,
            metadataSource = source,
            metadataLastModifiedUtc = lastModifiedUtc,
            iconCount = catalog.Count,
            icons = catalog
        };

        await JsonSerializer.SerializeAsync(stream, payload, _manifestJsonOptions, cancellationToken);
    }

    private async Task PersistRuntimeAllowlistAsync(
        BerelainCuratedManifest manifest,
        IReadOnlySet<string> allowlist,
        CancellationToken cancellationToken)
    {
        var outputDirectory = Path.Combine(_webHostEnvironment.ContentRootPath, "App_Data", "FontAwesome");
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(outputDirectory, "runtime-allowlist.json");
        await using var stream = File.Create(outputPath);

        var items = manifest.Icons
            .Select(static icon =>
            {
                var cssClasses = string.Empty;
                if (BerelainIconKey.TryParse(icon.Key, out var parsed) && parsed is not null)
                {
                    cssClasses = parsed.ToCssClasses();
                }

                return new
                {
                    key = icon.Key,
                    cssClasses,
                    family = icon.Family,
                    style = icon.Style,
                    name = icon.Name
                };
            })
            .OrderBy(static x => x.key, StringComparer.Ordinal)
            .ToList();

        var payload = new
        {
            generatedUtc = DateTimeOffset.UtcNow,
            settingsSignature = manifest.SettingsSignature,
            iconCount = allowlist.Count,
            keys = allowlist.OrderBy(static x => x, StringComparer.Ordinal).ToArray(),
            icons = items
        };

        await JsonSerializer.SerializeAsync(stream, payload, _manifestJsonOptions, cancellationToken);
    }

    private async Task PersistSubsetCssAsync(string css, CancellationToken cancellationToken)
    {
        var outputDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "lib", "fontawesome");
        Directory.CreateDirectory(outputDirectory);

        var outputPath = Path.Combine(outputDirectory, "curated-icons.min.css");
        await File.WriteAllTextAsync(outputPath, css, Encoding.UTF8, cancellationToken);
    }

    private static string ExtractFontFaces(string css, ISet<string> neededFontFamilies)
    {
        var fontFaces = new StringBuilder();
        foreach (Match match in FontFaceBlockRegex.Matches(css))
        {
            var block = match.Value;
            var familyMatch = FontFamilyRegex.Match(block);
            if (!familyMatch.Success)
            {
                continue;
            }

            var fontFamily = familyMatch.Groups[1].Value.Trim().Replace(" ", "", StringComparison.Ordinal);
            if (neededFontFamilies.Contains(fontFamily))
            {
                fontFaces.Append(block);
            }
        }

        return fontFaces.ToString();
    }

    private static string ExtractIconRules(string css, ISet<string> curatedNames)
    {
        var rules = new StringBuilder();

        foreach (var line in css.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!IconSelectorRegex.IsMatch(line))
            {
                continue;
            }

            var hasMatchingIcon = false;
            foreach (Match m in IconNameRegex.Matches(line))
            {
                if (curatedNames.Contains(m.Groups[1].Value))
                {
                    hasMatchingIcon = true;
                    break;
                }
            }

            if (hasMatchingIcon)
            {
                rules.Append(line);
                rules.Append('\n');
            }
        }

        return rules.ToString();
    }

    private static string MapToFontFamily(string family, string style)
    {
        return (family, style) switch
        {
            ("classic", "solid") => "FontAwesome6Pro",
            ("classic", "regular") => "FontAwesome6Pro",
            ("classic", "light") => "FontAwesome6Pro",
            ("classic", "thin") => "FontAwesome6Pro",
            ("brands", _) => "FontAwesome6Brands",
            ("duotone", _) => "FontAwesome6Duotone",
            ("sharp", _) => "FontAwesome6Sharp",
            ("sharp-duotone", _) => "FontAwesome6SharpDuotone",
            _ => string.Empty
        };
    }

    private static Task<List<BerelainCatalogItem>> LoadAndNormalizeCatalogAsync(
        IFileInfo metadataFile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = metadataFile.CreateReadStream();
        using var streamReader = new StreamReader(stream);

        var yaml = new YamlStream();
        yaml.Load(streamReader);

        var root = yaml.Documents.Count > 0 ? yaml.Documents[0].RootNode as YamlMappingNode : null;
        if (root is null)
        {
            return Task.FromResult(new List<BerelainCatalogItem>());
        }

        var normalized = new List<BerelainCatalogItem>(capacity: root.Children.Count * 2);

        foreach (var iconNode in root.Children)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (iconNode.Key is not YamlScalarNode iconNameNode || iconNode.Value is not YamlMappingNode iconMap)
            {
                continue;
            }

            var iconName = NormalizeKeyToken(iconNameNode.Value);
            if (string.IsNullOrWhiteSpace(iconName))
            {
                continue;
            }

            var label = GetScalar(iconMap, "label") ?? iconName;
            var unicode = GetScalar(iconMap, "unicode") ?? string.Empty;
            var searchTerms = BuildSearchTerms(iconMap, iconName, label);

            var freePairs = GetFamilyStylePairs(iconMap, "free");
            var allPairs = new HashSet<(string Family, string Style)>(freePairs);
            allPairs.UnionWith(GetFamilyStylePairs(iconMap, "pro"));

            foreach (var pair in allPairs)
            {
                var key = $"{pair.Family}/{pair.Style}/{iconName}";
                normalized.Add(new BerelainCatalogItem(
                    key,
                    pair.Family,
                    pair.Style,
                    iconName,
                    label,
                    unicode,
                    searchTerms,
                    freePairs.Contains(pair)));
            }
        }

        normalized = DeduplicateByKey(normalized);
        normalized.Sort(static (a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));
        return Task.FromResult(normalized);
    }

    private static List<BerelainCatalogItem> FilterAllowedIcons(
        IReadOnlyList<BerelainCatalogItem> catalog,
        BerelainAllowlist allowlist)
    {
        var filtered = new List<BerelainCatalogItem>(catalog.Count);
        foreach (var icon in catalog)
        {
            if (allowlist.IsAllowed(icon.Family, icon.Style))
            {
                filtered.Add(icon);
            }
        }

        return filtered;
    }

    private static List<BerelainCatalogItem> DeduplicateByKey(List<BerelainCatalogItem> catalog)
    {
        var byKey = new Dictionary<string, BerelainCatalogItem>(StringComparer.Ordinal);
        foreach (var item in catalog)
        {
            if (byKey.TryGetValue(item.Key, out var existing))
            {
                if (!existing.IsFree && item.IsFree)
                {
                    byKey[item.Key] = item;
                }

                continue;
            }

            byKey[item.Key] = item;
        }

        return byKey.Values.ToList();
    }

    private static HashSet<(string Family, string Style)> GetFamilyStylePairs(YamlMappingNode iconMap, string licenseKey)
    {
        var pairs = new HashSet<(string Family, string Style)>();

        var familyStylesByLicense = GetMapping(iconMap, "familyStylesByLicense");
        var byLicense = familyStylesByLicense is null ? null : GetSequence(familyStylesByLicense, licenseKey);
        if (byLicense is null)
        {
            return pairs;
        }

        foreach (var node in byLicense.Children)
        {
            if (node is not YamlMappingNode pairMap)
            {
                continue;
            }

            var family = NormalizeKeyToken(GetScalar(pairMap, "family"));
            var style = NormalizeKeyToken(GetScalar(pairMap, "style"));
            if (string.IsNullOrWhiteSpace(family) || string.IsNullOrWhiteSpace(style))
            {
                continue;
            }

            pairs.Add((family, style));
        }

        return pairs;
    }

    private static IReadOnlyList<string> BuildSearchTerms(YamlMappingNode iconMap, string iconName, string label)
    {
        var terms = new HashSet<string>(StringComparer.Ordinal);

        AddTerm(terms, iconName);
        AddTerm(terms, label);

        foreach (var token in SplitWords(label))
        {
            AddTerm(terms, token);
        }

        var searchMap = GetMapping(iconMap, "search");
        var termsNode = searchMap is null ? null : GetSequence(searchMap, "terms");
        if (termsNode is null)
        {
            return terms.Count == 0
                ? Array.Empty<string>()
                : new ReadOnlyCollection<string>(terms.OrderBy(static x => x, StringComparer.Ordinal).ToList());
        }

        foreach (var child in termsNode.Children)
        {
            if (child is not YamlScalarNode termNode)
            {
                continue;
            }

            var value = termNode.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                AddTerm(terms, value);
            }
        }

        return terms.Count == 0
            ? Array.Empty<string>()
            : new ReadOnlyCollection<string>(terms.OrderBy(static x => x, StringComparer.Ordinal).ToList());
    }

    private static IEnumerable<string> SplitWords(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (Match match in LabelWordRegex.Matches(value))
        {
            if (match.Success)
            {
                yield return match.Value;
            }
        }
    }

    private static void AddTerm(HashSet<string> terms, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = NormalizeSearchTerm(value);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            terms.Add(normalized);
        }
    }

    private static string NormalizeSearchTerm(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == ' ')
            {
                builder.Append(ch);
            }
        }

        return builder
            .ToString()
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static YamlMappingNode? GetMapping(YamlMappingNode source, string key)
    {
        var node = GetNode(source, key);
        return node as YamlMappingNode;
    }

    private static YamlSequenceNode? GetSequence(YamlMappingNode source, string key)
    {
        var node = GetNode(source, key);
        return node as YamlSequenceNode;
    }

    private static YamlNode? GetNode(YamlMappingNode source, string key)
    {
        foreach (var entry in source.Children)
        {
            if (entry.Key is YamlScalarNode scalarKey && string.Equals(scalarKey.Value, key, StringComparison.Ordinal))
            {
                return entry.Value;
            }
        }

        return null;
    }

    private static string? GetScalar(YamlMappingNode source, string key)
    {
        return GetNode(source, key) is YamlScalarNode scalar ? scalar.Value : null;
    }

    private static string NormalizeKeyToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized.StartsWith("fa-", StringComparison.Ordinal) ? normalized[3..] : normalized;
    }
}
