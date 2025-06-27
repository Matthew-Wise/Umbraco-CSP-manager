using Umbraco.Cms.Api.Management.OpenApi;

namespace Umbraco.Community.CSPManager.Configuration;

public class CspApiOperationSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase
{
	protected override string ApiName => Constants.ApiName;
}