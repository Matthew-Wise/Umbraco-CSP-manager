using System.Text.Json.Serialization;

namespace Umbraco.Community.CSPManager.Models;

/// <summary>
/// Which pages a node policy applies to.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CspPolicyScope>))]
public enum CspPolicyScope
{
	/// <summary>
	/// The content node and every page below it (unless a page below has a policy of its own).
	/// The default, and what every policy created before scopes existed is.
	/// </summary>
	Descendants = 0,

	/// <summary>
	/// Only the content node itself.
	/// </summary>
	NodeOnly = 1,
}
