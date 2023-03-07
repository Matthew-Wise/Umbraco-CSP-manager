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
				$"/App_Plugins/{CspConstants.PluginAlias}/backoffice/manage-csp/manage/cspManagerManageController.js",
				$"/App_Plugins/{CspConstants.PluginAlias}/backoffice/manage-csp/evaluate/cspManagerEvaluateController.js",
				$"/App_Plugins/{CspConstants.PluginAlias}/backoffice/manage-csp/evaluate/csp-evaluator.js"
			},
			Version = typeof(PackageManifestFilter)?.Assembly?.GetName()?.Version?.ToString(3) ?? string.Empty,
			AllowPackageTelemetry = true
		});
	}
}
