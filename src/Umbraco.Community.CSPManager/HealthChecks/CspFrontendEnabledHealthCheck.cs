using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.HealthChecks;

/// <summary>
/// Health check that reports whether a Content Security Policy is enabled for front-end requests.
/// </summary>
[HealthCheck(
	"3F8A6C1E-9B4D-4A7F-8E2C-6D1F9A3B5C8E",
	"CSP Enabled for Front-End",
	Description = "Checks whether a Content Security Policy is enabled for front-end requests.",
	Group = "Security")]
public sealed class CspFrontendEnabledHealthCheck : HealthCheck
{
	private const string EnableActionAlias = "enableFrontendCsp";
	private static readonly Guid CheckId = new("3F8A6C1E-9B4D-4A7F-8E2C-6D1F9A3B5C8E");

	private readonly ICspService _cspService;

	public CspFrontendEnabledHealthCheck(ICspService cspService)
	{
		_cspService = cspService;
	}

	public override async Task<IEnumerable<HealthCheckStatus>> GetStatus()
	{
		var definition = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);

		if (definition.Enabled)
		{
			return
			[
				new HealthCheckStatus("A Content Security Policy is enabled for front-end requests.")
				{
					ResultType = StatusResultType.Success
				}
			];
		}

		return
		[
			new HealthCheckStatus(
				"No Content Security Policy is enforced for front-end requests. Visitors are not protected " +
				"against XSS and data-injection attacks until a policy is enabled.")
			{
				ResultType = StatusResultType.Warning,
				Actions =
				[
					new HealthCheckAction(EnableActionAlias, CheckId)
					{
						Name = "Enable",
						Description = "Enable the existing front-end Content Security Policy."
					}
				]
			}
		];
	}

	public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
	{
		if (action.Alias != EnableActionAlias)
		{
			throw new InvalidOperationException($"Action '{action.Alias}' is not supported by this health check.");
		}

		var definition = _cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None)
			.GetAwaiter().GetResult();
		definition.Enabled = true;
		_cspService.SaveCspDefinitionAsync(definition, CancellationToken.None).GetAwaiter().GetResult();

		return new HealthCheckStatus("The front-end Content Security Policy has been enabled.")
		{
			ResultType = StatusResultType.Success
		};
	}
}
