namespace Umbraco.Community.CSPManager.Composing;

using Cms.Core.Manifest;

internal sealed class PackageManifestFilter : IManifestFilter
{
	public void Filter(List<PackageManifest> manifests)
	{
		manifests.Add(new PackageManifest
		{
			PackageName = CspConstants.PackageAlias,
			Scripts = new[] { 
				$"/App_Plugins/{CspConstants.PluginAlias}/backoffice/manage-csp/cspManagerEditController.js",
				$"/App_Plugins/{CspConstants.PluginAlias}/backoffice/manage-csp/cspManager.resource.js",
			},
			Version = typeof(PackageManifestFilter)?.Assembly?.GetName()?.Version?.ToString(3) ?? string.Empty,
			AllowPackageTelemetry = true
		});
	}
}
