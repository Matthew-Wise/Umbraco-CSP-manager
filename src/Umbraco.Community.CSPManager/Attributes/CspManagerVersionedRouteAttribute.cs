using Umbraco.Cms.Web.Common.Routing;

namespace Umbraco.Community.CSPManager.Attributes;
public class CspManagerVersionedRouteAttribute : BackOfficeRouteAttribute
{
	public CspManagerVersionedRouteAttribute(string template)
		: base($"{Constants.ManagementApiPath}/v{{version:apiVersion}}/{template.TrimStart('/')}")
	{}
}
