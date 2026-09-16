using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.HealthChecks;

/// <summary>
/// Health check that flags use of the deprecated <c>report-uri</c> reporting directive.
/// </summary>
[HealthCheck(
	"D4A1C9E2-8F3B-4E7A-9C1D-2B6F5A8E7D31",
	"CSP Deprecated Directives",
	Description = "Checks for use of the deprecated 'report-uri' reporting directive.",
	Group = "Security")]
public sealed class CspDeprecatedDirectivesHealthCheck : HealthCheck
{
	private readonly ICspService _cspService;

	public CspDeprecatedDirectivesHealthCheck(ICspService cspService)
	{
		_cspService = cspService;
	}

	public override async Task<IEnumerable<HealthCheckStatus>> GetStatus()
	{
		var statuses = new List<HealthCheckStatus>();

		foreach (var (isBackOffice, label) in CspContexts.All)
		{
			var definition = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: isBackOffice, CancellationToken.None);

			if (!definition.Enabled || definition.ReportingDirective != Constants.ReportingDirectives.ReportUri)
			{
				continue;
			}

			statuses.Add(new HealthCheckStatus(
				$"The {label} policy uses the deprecated '{Constants.ReportingDirectives.ReportUri}' " +
				$"reporting directive. Browsers that support the Reporting API ignore it in favour of " +
				$"'{Constants.ReportingDirectives.ReportTo}'; switch to it in the policy settings once you " +
				"have a Reporting-Endpoints header configured.")
			{
				ResultType = StatusResultType.Warning
			});
		}

		if (statuses.Count == 0)
		{
			statuses.Add(new HealthCheckStatus("No policy uses a deprecated CSP reporting directive.")
			{
				ResultType = StatusResultType.Success
			});
		}

		return statuses;
	}

	public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
		=> throw new InvalidOperationException($"Action '{action.Alias}' is not supported by this health check.");
}
