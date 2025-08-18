using Microsoft.AspNetCore.Authorization;

namespace Umbraco.Community.CSPManager.Authorization;

public sealed class CspManagerApplicationRequirement : IAuthorizationRequirement
{
	/// <summary>
	///  list of applications that this requirement will check against. 
	/// </summary>
	public string[] Applications { get; }

	/// <summary>
	///  create a new requirement for the given applications
	/// </summary>
	/// <param name="applications"></param>
	public CspManagerApplicationRequirement(params string[] applications)
	{
		Applications = applications;
	}
}
