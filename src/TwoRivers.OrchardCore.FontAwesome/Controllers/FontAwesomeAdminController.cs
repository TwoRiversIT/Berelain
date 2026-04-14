using TwoRivers.Berelain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TwoRivers.Berelain.Controllers;

[Authorize]
[Route("berelain/admin")]
public sealed class BerelainAdminController : Controller
{
    private readonly IBerelainMetadataService _metadataService;
    private readonly IBerelainDiagnosticsService _diagnosticsService;
    private readonly IEnumerable<IBerelainSvgService> _svgServices;

    public BerelainAdminController(
        IBerelainMetadataService metadataService,
        IBerelainDiagnosticsService diagnosticsService,
        IEnumerable<IBerelainSvgService> svgServices)
    {
        _metadataService = metadataService;
        _diagnosticsService = diagnosticsService;
        _svgServices = svgServices;
    }

    [HttpGet("curated-icons")]
    public async Task<IActionResult> CuratedIcons(CancellationToken cancellationToken)
    {
        var manifest = await _metadataService.BuildCuratedManifestAsync(cancellationToken);

        return Json(new
        {
            generatedUtc = manifest.GeneratedUtc,
            count = manifest.IconCount,
            icons = manifest.Icons.Select(icon => new
            {
                key = icon.Key,
                family = icon.Family,
                style = icon.Style,
                name = icon.Name,
                label = icon.Label,
                searchTerms = icon.SearchTerms,
                isFree = icon.IsFree
            })
        });
    }

    [HttpGet("diagnostics")]
    public async Task<IActionResult> Diagnostics(CancellationToken cancellationToken)
    {
        var snapshot = await _diagnosticsService.GetSnapshotAsync(cancellationToken);

        return Json(new
        {
            generatedUtc = snapshot.GeneratedUtc,
            selectedIconCount = snapshot.SelectedIconCount,
            runtimeAllowlistCount = snapshot.RuntimeAllowlistCount,
            usedAssignmentsTotal = snapshot.UsedAssignmentsTotal,
            usedCanonicalAssignments = snapshot.UsedCanonicalAssignments,
            usedLegacyAssignments = snapshot.UsedLegacyAssignments,
            distinctUsedCanonicalCount = snapshot.DistinctUsedCanonicalCount,
            usedCanonicalInAllowlistCount = snapshot.UsedCanonicalInAllowlistCount,
            usedCanonicalOutsideAllowlistCount = snapshot.UsedCanonicalOutsideAllowlistCount,
            unusedSelectedIconCount = snapshot.UnusedSelectedIconCount,
            usedCanonicalOutsideAllowlistKeys = snapshot.UsedCanonicalOutsideAllowlistKeys,
            topUsedCanonicalKeys = snapshot.TopUsedCanonicalKeys
        });
    }

    /// <summary>
    /// Serves the curated SVG sprite file for inline use on the front end.
    /// No authentication required — this is a public static-like resource.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/berelain/sprite.svg")]
    public async Task<IActionResult> Sprite(CancellationToken cancellationToken)
    {
        var svgService = _svgServices.FirstOrDefault();
        if (svgService is null)
        {
            return NotFound();
        }

        var spriteManifest = await svgService.GetSpriteManifestAsync(cancellationToken);
        if (spriteManifest is null || !System.IO.File.Exists(spriteManifest.SpriteFilePath))
        {
            return NotFound();
        }

        var etag = $"\"{spriteManifest.GeneratedUtc.Ticks}\"";
        Response.Headers["ETag"] = etag;
        Response.Headers["Cache-Control"] = "public, max-age=86400";

        var stream = System.IO.File.OpenRead(spriteManifest.SpriteFilePath);
        return File(stream, "image/svg+xml");
    }
}
