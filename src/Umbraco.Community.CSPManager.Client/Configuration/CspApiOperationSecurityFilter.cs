namespace Umbraco.Community.CSPManager.Client.Configuration;

using Umbraco.Cms.Api.Management.OpenApi;

internal class CspApiOperationSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase
{
	protected override string ApiName => "csp";
}