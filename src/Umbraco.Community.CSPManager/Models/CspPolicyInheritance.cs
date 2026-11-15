using System.Text.Json.Serialization;

namespace Umbraco.Community.CSPManager.Models;

/// <summary>
/// How a node policy relates to the policy that would apply without it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CspPolicyInheritance>))]
public enum CspPolicyInheritance
{
	/// <summary>
	/// A complete policy of its own: nothing is inherited. The default, and what every policy
	/// created before inheritance existed is.
	/// </summary>
	Replace = 0,

	/// <summary>
	/// The policy only holds extra sources. They are added to the policy that applies without it:
	/// a less specific policy on the same node, the nearest policy up the content tree, or the
	/// global frontend policy. Status, report-only mode and reporting come from that base policy.
	/// </summary>
	Extend = 1,
}
