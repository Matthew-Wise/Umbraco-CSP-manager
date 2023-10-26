namespace Umbraco.Community.CSPManager.Backoffice;

using Umbraco.Cms.Core.Sections;

public sealed class CspManagementSection : ISection
{
	public string Alias => CspConstants.PluginAlias;
	public string Name => "Csp Manager";
}
