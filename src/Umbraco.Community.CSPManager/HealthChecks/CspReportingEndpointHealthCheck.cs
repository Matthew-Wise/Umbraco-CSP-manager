using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.HealthChecks;

/// <summary>
/// Health check that flags policies using the <c>report-to</c> reporting directive, which requires a
/// matching <c>Reporting-Endpoints</c> response header that CSP Manager does not send on its own.
/// </summary>
/// <remarks>
/// This is an informational reminder rather than a definitive pass/fail: whether the header is actually
/// present depends on the rest of the application (a custom middleware, reverse proxy, or a
/// <see cref="Notifications.CspWritingNotification"/> handler could all add it), which this check has no
/// reliable way to observe from configuration alone.
/// </remarks>
[HealthCheck(
	"1A6F9C3E-8B2D-4E7A-9F1C-5A8E3D6B2F4C",
	"CSP Reporting Endpoint Configuration",
	Description = "Checks for policies using 'report-to', which requires a matching Reporting-Endpoints header.",
	Group = "Security")]
public sealed class CspReportingEndpointHealthCheck : HealthCheck
{
	private readonly ICspService _cspService;

	public CspReportingEndpointHealthCheck(ICspService cspService)
	{
		_cspService = cspService;
	}

	public override async Task<IEnumerable<HealthCheckStatus>> GetStatus()
	{
		var statuses = new List<HealthCheckStatus>();

		foreach (var (isBackOffice, label) in CspContexts.All)
		{
			var definition = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: isBackOffice, CancellationToken.None);

			if (!definition.Enabled || definition.ReportingDirective != Constants.ReportingDirectives.ReportTo)
			{
				continue;
			}

			statuses.Add(new HealthCheckStatus(
				$"The {label} policy reports violations to the '{definition.ReportUri}' endpoint via " +
				"'report-to', but CSP Manager does not send a matching Reporting-Endpoints header. Unless " +
				"your site sets that header by another means, browsers will silently discard these reports.")
			{
				ResultType = StatusResultType.Info
			});
		}

		if (statuses.Count == 0)
		{
			statuses.Add(new HealthCheckStatus("No policy uses the 'report-to' reporting directive.")
			{
				ResultType = StatusResultType.Success
			});
		}

		return statuses;
	}

	public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
		=> throw new InvalidOperationException($"Action '{action.Alias}' is not supported by this health check.");
}
