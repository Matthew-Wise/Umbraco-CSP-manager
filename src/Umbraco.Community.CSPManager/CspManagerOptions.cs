namespace Umbraco.Community.CSPManager;

public sealed class CspManagerOptions
{
	public bool DisableBackOfficeHeader { get; set; } = false;

	/// <summary>
	/// Controls what happens when constructing the CSP header throws an unexpected exception.
	/// Defaults to <see cref="CspFailureBehavior.FailOpen"/> to preserve existing behaviour.
	/// </summary>
	public CspFailureBehavior FailureBehavior { get; set; } = CspFailureBehavior.FailOpen;
}

/// <summary>
/// The behaviour to apply when CSP header construction throws an unexpected exception.
/// </summary>
public enum CspFailureBehavior
{
	/// <summary>
	/// Let the request continue with no CSP header. Availability over security; the only
	/// signal is the <c>CspHeaderConstructionFailed</c> log entry.
	/// </summary>
	FailOpen,

	/// <summary>
	/// Apply a minimal safe fallback policy (<c>default-src 'self'</c>) instead of sending no
	/// header at all.
	/// </summary>
	FailClosed,
}