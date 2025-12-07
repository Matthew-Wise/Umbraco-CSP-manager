using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Api.Management.Security.Authorization;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Extensions;

namespace Umbraco.Community.CSPManager.Authorization;

internal sealed class CspManagerAllowedApplicationHandler : MustSatisfyRequirementAuthorizationHandler<CspManagerApplicationRequirement>
{
	private readonly IAuthorizationHelper _authorizationHelper;

	/// <summary>
	///  new handler for the given authorization helper
	/// </summary>
	public CspManagerAllowedApplicationHandler(IAuthorizationHelper authorizationHelper)
		=> _authorizationHelper = authorizationHelper;

	/// <summary>
	///   check to see if this is authorized
	/// </summary>
	protected override Task<bool> IsAuthorized(AuthorizationHandlerContext context, CspManagerApplicationRequirement requirement)
	{
		var allowed = _authorizationHelper.TryGetUmbracoUser(context.User, out IUser? user)
					  && user.AllowedSections.ContainsAny(requirement.Applications);
		return Task.FromResult(allowed);
	}
}