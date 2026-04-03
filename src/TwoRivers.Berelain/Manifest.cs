using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Font Awesome",
    Author = "Two Rivers",
    Website = "",
    Version = "1.0.0",
    Description = "Font Awesome icon management, curation, and rendering for Orchard Core.",
    Category = "Content")]

[assembly: Feature(
    Id = "TwoRivers.Berelain",
    Name = "Font Awesome",
    Description = "Core icon management: metadata service, curation settings, icon field, diagnostics, and CSS rendering.",
    Category = "Content")]

[assembly: Feature(
    Id = "TwoRivers.Berelain.SvgSprites",
    Name = "Font Awesome SVG Sprites",
    Description = "Generates curated SVG sprite sheets and renders icons as inline <svg><use> elements.",
    Dependencies = ["TwoRivers.Berelain"],
    Category = "Content")]
