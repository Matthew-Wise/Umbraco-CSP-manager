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
	/// Gets or sets the policy behavior when a request matches a domain whose policy is disabled.
	/// Defaults to <see cref="DisabledNodePolicyBehavior.FallbackToGlobal"/>.
	/// </summary>
	public DisabledNodePolicyBehavior DisabledNodePolicyBehavior { get; set; } = DisabledNodePolicyBehavior.FallbackToGlobal;
}

/// <summary>
/// Specifies the policy fallback behavior when a node policy is disabled.
/// </summary>
public enum DisabledNodePolicyBehavior
{
	/// <summary>
	/// Applies the default global frontend policy.
	/// </summary>
	FallbackToGlobal = 0,

	/// <summary>
	/// Sends no CSP header for the domain.
	/// </summary>
	NoHeader = 1,
}
