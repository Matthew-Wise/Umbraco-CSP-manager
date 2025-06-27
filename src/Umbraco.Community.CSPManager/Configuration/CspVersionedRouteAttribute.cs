using Umbraco.Cms.Web.Common.Routing;

namespace Umbraco.Community.CSPManager.Configuration;

public class CspVersionedRouteAttribute : BackOfficeRouteAttribute
{
	public CspVersionedRouteAttribute(string template)
		: base($"csp/api/v{{version:apiVersion}}/{template.TrimStart('/')}")
		{}
}