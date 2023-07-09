namespace Umbraco.Community.CSPManager.Controllers;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Models.Reporting;
using Umbraco.Community.CSPManager.Services;

[Route("api/csp-manager/")]
public sealed class CspReportingController : ControllerBase
{
	private readonly ILogger<CspReportingController> _logger;
	private readonly IReportingService _reportingService;

	public CspReportingController(ILogger<CspReportingController> logger, IReportingService reportingService)
	{
		_logger = logger;
		_reportingService = reportingService;
	}

	[HttpPost]
	[AllowAnonymous]
	public async Task<IActionResult> Report([FromBody] ReportModel report)
	{
		try
		{
			await _reportingService.SaveAsync(report);
			return Ok();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to save CSP Report.");
			throw;
		}
	}
}
