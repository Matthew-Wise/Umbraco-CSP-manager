using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.HealthChecks;

/// <summary>
/// Health check that reports enforced policies allowing <c>'unsafe-inline'</c> or <c>'unsafe-eval'</c> in
/// <c>script-src</c>, which largely defeats the XSS protection a Content Security Policy provides.
/// </summary>
/// <remarks>
/// Only <c>script-src</c> and <c>script-src-elem</c> are checked, and only for enforced (non report-only)
/// policies. <c>script-src-attr</c>/<c>style-src-attr</c> are deliberately excluded: the nonce tag helper
/// documents granting <c>'unsafe-inline'</c> there as the supported way to allow inline event handlers
/// alongside a nonce, since a nonce never touches the <c>-attr</c> directives.
/// </remarks>
[HealthCheck(
	"B7E2F4A8-1C9D-4F6E-8A3B-5D7C9E1F2A6B",
	"CSP Unsafe Script Sources",
	Description = "Checks enforced policies for 'unsafe-inline' or 'unsafe-eval' in script-src.",
	Group = "Security")]
public sealed class CspUnsafeScriptSourceHealthCheck : HealthCheck
{
	private static readonly string[] UnsafeTokens = ["'unsafe-inline'", "'unsafe-eval'"];
	private static readonly string[] CheckedDirectives =
		[Constants.Directives.ScriptSource, Constants.Directives.ScriptSourceElement];

	private readonly ICspService _cspService;

	public CspUnsafeScriptSourceHealthCheck(ICspService cspService)
	{
		_cspService = cspService;
	}

	public override async Task<IEnumerable<HealthCheckStatus>> GetStatus()
	{
		var statuses = new List<HealthCheckStatus>();

		foreach (var (isBackOffice, label) in CspContexts.All)
		{
			var definition = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: isBackOffice, CancellationToken.None);

			if (!definition.Enabled || definition.ReportOnly)
			{
				// Report-only policies don't block content, so unsafe keywords there aren't an enforcement gap.
				continue;
			}

			var unsafeTokensFound = definition.Sources
				.Where(s => UnsafeTokens.Contains(s.Source) && s.Directives.Any(CheckedDirectives.Contains))
				.Select(s => s.Source)
				.Distinct()
				.ToList();

			if (unsafeTokensFound.Count > 0)
			{
				statuses.Add(new HealthCheckStatus(
					$"The enforced {label} policy allows {string.Join(" and ", unsafeTokensFound)} in script-src, " +
					"which lets inline or eval'd script run and largely defeats CSP's XSS protection. Prefer " +
					"nonces (see the CSP nonce tag helper) instead.")
				{
					ResultType = StatusResultType.Warning
				});
			}
		}

		if (statuses.Count == 0)
		{
			statuses.Add(new HealthCheckStatus(
				"No enforced policy allows 'unsafe-inline' or 'unsafe-eval' in script-src.")
			{
				ResultType = StatusResultType.Success
			});
		}

		return statuses;
	}

	public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
		=> throw new InvalidOperationException($"Action '{action.Alias}' is not supported by this health check.");
}
