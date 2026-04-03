using Microsoft.Extensions.Options;
using OrchardCore.ResourceManagement;

namespace TwoRivers.Berelain;

public sealed class ResourceManagementOptionsConfiguration : IConfigureOptions<ResourceManagementOptions>
{
    public void Configure(ResourceManagementOptions options)
    {
        var manifest = new ResourceManifest();

        manifest
            .DefineScript("BerelainIconPicker")
            .SetUrl("~/TwoRivers.Berelain/js/fa-icon-picker.js")
            .SetVersion("1.0.0");

        manifest
            .DefineStyle("BerelainIconPicker")
            .SetUrl("~/TwoRivers.Berelain/css/fa-icon-picker.css")
            .SetVersion("1.0.0");

        options.ResourceManifests.Add(manifest);
    }
}
