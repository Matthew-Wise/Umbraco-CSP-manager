using Microsoft.AspNetCore.Http;

namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Tracks CSP header construction failures observed by <see cref="Middleware.CspMiddleware"/> so that
/// health checks can surface them without depending on log scraping.
/// </summary>
/// <remarks>
/// The middleware deliberately fails open: a construction failure never breaks the request, it just
/// means the response ships without a CSP header. That makes the failure invisible outside the logs
/// unless something else is watching for it - this is that something else.
/// </remarks>
public interface ICspHealthMonitor
{
	/// <summary>
	/// Gets the most recent recorded header construction failure, or <c>null</c> if none has occurred
	/// since the application started or since the last <see cref="Reset"/>.
	/// </summary>
	CspHeaderFailure? LastFailure { get; }

	/// <summary>
	/// Records a CSP header construction failure.
	/// </summary>
	void RecordFailure(PathString path, Exception exception);

	/// <summary>
	/// Clears the tracked failure, e.g. after an administrator has investigated it via the health check dashboard.
	/// </summary>
	void Reset();
}

/// <summary>
/// Details of a CSP header construction failure recorded by <see cref="ICspHealthMonitor"/>.
/// </summary>
/// <param name="OccurredUtc">When the failure was recorded.</param>
/// <param name="Path">The request path that was being processed when construction failed.</param>
/// <param name="Message">The exception message from the failure.</param>
public sealed record CspHeaderFailure(DateTimeOffset OccurredUtc, string Path, string Message);
