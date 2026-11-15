using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Community.CSPManager.Notifications;

/// <summary>
/// Notification published when constructing the CSP header for the provided <see cref="Microsoft.AspNetCore.Http.HttpContext"/>
/// throws, before any fallback header is written to the response.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FallbackPolicy"/> is pre-populated from <see cref="CspManagerOptions.FailureBehavior"/>:
/// <c>null</c> for <see cref="CspFailureBehavior.FailOpen"/> and <see cref="Constants.FailClosedFallbackPolicy"/>
/// for <see cref="CspFailureBehavior.FailClosed"/>. Backoffice requests always start from <c>null</c>, because a
/// same-origin-only policy would stop the backoffice rendering. Handlers can replace it with a policy of their own,
/// or set it to <c>null</c> to send no header at all, whichever failure behavior is configured.
/// </para>
/// <para>
/// <see cref="ReportOnly"/> is pre-populated from the CSP definition when it loaded before the failure, and is
/// <c>false</c> when it did not.
/// </para>
/// </remarks>
public class CspHeaderConstructionFailedNotification : INotification
{
	public CspHeaderConstructionFailedNotification(
		Exception exception,
		HttpContext httpContext,
		bool isBackOfficeRequest,
		string? fallbackPolicy,
		bool reportOnly)
	{
		Exception = exception;
		HttpContext = httpContext;
		IsBackOfficeRequest = isBackOfficeRequest;
		FallbackPolicy = fallbackPolicy;
		ReportOnly = reportOnly;
	}

	/// <summary>
	/// The exception thrown while constructing the CSP header.
	/// </summary>
	public Exception Exception { get; }

	public HttpContext HttpContext { get; }

	/// <summary>
	/// Whether the failed header was for a backoffice request rather than a frontend one.
	/// </summary>
	public bool IsBackOfficeRequest { get; }

	/// <summary>
	/// The policy sent in place of the failed header. <c>null</c> or whitespace sends no header,
	/// which is the fail-open default.
	/// </summary>
	public string? FallbackPolicy { get; set; }

	/// <summary>
	/// Whether <see cref="FallbackPolicy"/> is sent as a report-only header rather than an enforced one.
	/// Pre-populated from the CSP definition when it loaded before the failure; otherwise <c>false</c>.
	/// </summary>
	public bool ReportOnly { get; set; }
}
