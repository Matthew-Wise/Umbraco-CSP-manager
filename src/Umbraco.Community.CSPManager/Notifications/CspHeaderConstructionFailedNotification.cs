using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Community.CSPManager.Notifications;

/// <summary>
/// Notification published when constructing the CSP header for the provided <see cref="Microsoft.AspNetCore.Http.HttpContext"/>
/// throws, before any fallback header is written to the response.
/// </summary>
/// <remarks>
/// <see cref="FallbackPolicy"/> is pre-populated from <see cref="CspManagerOptions.FailureBehavior"/>:
/// <c>null</c> for <see cref="CspFailureBehavior.FailOpen"/> and <see cref="Constants.FailClosedFallbackPolicy"/>
/// for <see cref="CspFailureBehavior.FailClosed"/>. Handlers can replace it with a policy of their own,
/// or set it to <c>null</c> to send no header at all, whichever failure behavior is configured.
/// </remarks>
public class CspHeaderConstructionFailedNotification : INotification
{
	public CspHeaderConstructionFailedNotification(Exception exception, HttpContext httpContext, string? fallbackPolicy)
	{
		Exception = exception;
		HttpContext = httpContext;
		FallbackPolicy = fallbackPolicy;
	}

	/// <summary>
	/// The exception thrown while constructing the CSP header.
	/// </summary>
	public Exception Exception { get; }

	public HttpContext HttpContext { get; set; }

	/// <summary>
	/// The policy sent in place of the failed header. <c>null</c> or whitespace sends no header,
	/// which is the fail-open default.
	/// </summary>
	public string? FallbackPolicy { get; set; }

	/// <summary>
	/// Whether <see cref="FallbackPolicy"/> is sent as a report-only header rather than an enforced one.
	/// Defaults to <c>false</c>.
	/// </summary>
	public bool ReportOnly { get; set; }
}
