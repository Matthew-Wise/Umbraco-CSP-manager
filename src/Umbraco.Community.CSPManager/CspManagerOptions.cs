using System.Text.Json.Serialization;

namespace Umbraco.Community.CSPManager;

/// <summary>
/// Options for CSP Manager, bound from the <c>CspManager</c> configuration section.
/// </summary>
public sealed class CspManagerOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether the CSP header is left off backoffice responses.
	/// </summary>
	public bool DisableBackOfficeHeader { get; set; } = false;

	/// <summary>
	/// Controls what happens when CSP header construction throws. Defaults to
	/// <see cref="CspFailureBehavior.FailOpen"/>.
	/// </summary>
	public CspFailureBehavior FailureBehavior { get; set; } = CspFailureBehavior.FailOpen;

	/// <summary>
	/// Gets or sets what happens when the node policy a request would get is disabled. Defaults to
	/// <see cref="DisabledNodePolicyBehavior.FallbackToGlobal"/>.
	/// </summary>
	public DisabledNodePolicyBehavior DisabledNodePolicyBehavior { get; set; } = DisabledNodePolicyBehavior.FallbackToGlobal;
}

/// <summary>
/// Specifies the policy fallback behavior when a node policy is disabled.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DisabledNodePolicyBehavior>))]
public enum DisabledNodePolicyBehavior
{
	/// <summary>
	/// Skips the disabled policy, as if it didn't exist: the request gets the next policy that
	/// applies to it (another culture's or an ancestor's policy), and otherwise the global frontend
	/// policy.
	/// </summary>
	FallbackToGlobal = 0,

	/// <summary>
	/// Sends no CSP header for the requests a disabled complete policy would apply to. A disabled
	/// policy that only adds sources (<see cref="Models.CspPolicyInheritance.Extend"/>) is still
	/// skipped: it has no header of its own to switch off.
	/// </summary>
	NoHeader = 1,
}
