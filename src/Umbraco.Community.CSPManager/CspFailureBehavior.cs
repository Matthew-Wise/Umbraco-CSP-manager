namespace Umbraco.Community.CSPManager;

/// <summary>
/// Controls what the <see cref="Middleware.CspMiddleware"/> does when constructing the CSP
/// header throws (for example, a source value Kestrel rejects as an invalid header value).
/// </summary>
public enum CspFailureBehavior
{
	/// <summary>
	/// The request continues with no CSP header at all. Availability is prioritized over
	/// security; the failure is only visible in the logs. This is the default.
	/// </summary>
	FailOpen = 0,

	/// <summary>
	/// The request continues with a minimal fallback policy (<c>default-src 'self'</c>) instead
	/// of no header at all. Security is prioritized over availability - a misconfigured policy
	/// that throws will still leave the site under a restrictive, same-origin-only policy.
	/// </summary>
	FailClosed = 1,
}
