using Microsoft.AspNetCore.Http;

namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Default in-memory implementation of <see cref="ICspHealthMonitor"/>.
/// </summary>
/// <remarks>
/// Registered as a singleton so the single most recent failure is visible to every request and to the
/// health check dashboard, regardless of which request recorded it.
/// </remarks>
internal sealed class CspHealthMonitor : ICspHealthMonitor
{
	private CspHeaderFailure? _lastFailure;

	public CspHeaderFailure? LastFailure => Volatile.Read(ref _lastFailure);

	public void RecordFailure(PathString path, Exception exception)
		=> Volatile.Write(ref _lastFailure, new CspHeaderFailure(DateTimeOffset.UtcNow, path.Value ?? string.Empty, exception.Message));

	public void Reset() => Volatile.Write(ref _lastFailure, null);
}
