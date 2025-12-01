using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Community.CSPManager.Attributes;

namespace Umbraco.Community.CSPManager.Controllers;

[Authorize(Constants.AuthorizationPolicies.SectionAccess)]
[CspManagerVersionedRouteAttribute("")]
[MapToApi(Constants.ApiName)]
public abstract class CspManagerControllerBase : ManagementApiControllerBase
{
}