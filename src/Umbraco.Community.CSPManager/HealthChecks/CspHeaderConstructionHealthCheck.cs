using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.HealthChecks;

/// <summary>
/// Health check that surfaces recent CSP header construction failures recorded by
/// <see cref="ICspHealthMonitor"/>.
/// </summary>
/// <remarks>
/// <see cref="Middleware.CspMiddleware"/> fails open on a construction error: the request still
/// completes, just without a CSP header. That is the correct behaviour for the request, but it means a
/// failure is otherwise only visible in the logs - this check gives it a place in the health check
/// dashboard too.
/// </remarks>
[HealthCheck(
	"9C2E7A4F-3B8D-4F1E-A6C9-1E8F4A2D7B3C",
	"CSP Header Construction",
	Description = "Checks whether CSP Manager has recently failed to construct a CSP header at runtime.",
	Group = "Security")]
public sealed class CspHeaderConstructionHealthCheck : HealthCheck
{
	private const string DismissActionAlias = "dismissCspHeaderFailure";
	private static readonly Guid CheckId = new("9C2E7A4F-3B8D-4F1E-A6C9-1E8F4A2D7B3C");

	private readonly ICspHealthMonitor _healthMonitor;

	public CspHeaderConstructionHealthCheck(ICspHealthMonitor healthMonitor)
	{
		_healthMonitor = healthMonitor;
	}

	public override Task<IEnumerable<HealthCheckStatus>> GetStatus()
	{
		var failure = _healthMonitor.LastFailure;

		if (failure is null)
		{
			return Task.FromResult<IEnumerable<HealthCheckStatus>>(
			[
				new HealthCheckStatus(
					"No CSP header construction failures have been recorded since the application started.")
				{
					ResultType = StatusResultType.Success
				}
			]);
		}

		return Task.FromResult<IEnumerable<HealthCheckStatus>>(
		[
			new HealthCheckStatus(
				$"CSP header construction failed for '{failure.Path}' at {failure.OccurredUtc:u}: " +
				$"{failure.Message}. The request completed without a CSP header instead of failing.")
			{
				ResultType = StatusResultType.Error,
				Actions =
				[
					new HealthCheckAction(DismissActionAlias, CheckId)
					{
						Name = "Dismiss",
						Description = "Clear the recorded failure after investigating it."
					}
				]
			}
		]);
	}

	public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
	{
		if (action.Alias != DismissActionAlias)
		{
			throw new InvalidOperationException($"Action '{action.Alias}' is not supported by this health check.");
		}

		_healthMonitor.Reset();

		return new HealthCheckStatus("The recorded CSP header construction failure has been cleared.")
		{
			ResultType = StatusResultType.Success
		};
	}
}
