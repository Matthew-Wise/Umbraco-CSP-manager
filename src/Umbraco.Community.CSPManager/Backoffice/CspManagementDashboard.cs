namespace Umbraco.Community.CSPManager.Backoffice;

using Umbraco.Cms.Core.Dashboards;

public sealed class CspManagementDashboard : IDashboard
{
	public string Alias => CspConstants.PackageAlias;
	public string? View => $"/App_Plugins/{CspConstants.PluginAlias}/backoffice/dashboard.html";
	public string[] Sections => new[] { CspConstants.PluginAlias };
	public IAccessRule[] AccessRules => Array.Empty<IAccessRule>();
}
