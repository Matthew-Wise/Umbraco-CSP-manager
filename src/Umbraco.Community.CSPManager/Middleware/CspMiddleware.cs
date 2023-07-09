namespace Umbraco.Community.CSPManager.Middleware;

using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Cms.Core.Events;
using Umbraco.Community.CSPManager.Controllers;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Extensions;

public class CspMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IRuntimeState _runtimeState;
	private readonly ICspService _cspService;
	private readonly LinkGenerator _linkGenerator;
	private readonly IEventAggregator _eventAggregator;

	public CspMiddleware(
		RequestDelegate next,
		IRuntimeState runtimeState,
		ICspService cspService,
		LinkGenerator linkGenerator,
		IEventAggregator eventAggregator)
	{
		_next = next;
		_runtimeState = runtimeState;
		_cspService = cspService;
		_linkGenerator = linkGenerator;
		_eventAggregator = eventAggregator;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		if (_runtimeState.Level != RuntimeLevel.Run)
		{
			await _next(context);
			return;
		}

		var definition = _cspService.GetCachedCspDefinition(context.Request.IsBackOfficeRequest());

		await _eventAggregator.PublishAsync(new CspWritingNotification(definition, context));

		if (definition is not { Enabled: true })
		{
			await _next(context);
			return;
		}

		var csp = await Task.FromResult(ConstructCspDictionary(definition));
		var cspValue = string.Join(";", csp.Select(x => x.Key + " " + x.Value));
		if (!string.IsNullOrEmpty(cspValue))
		{
			context.Response.Headers.Add(definition.ReportOnly ? CspConstants.ReportOnlyHeaderName : CspConstants.HeaderName, cspValue);
		}

		await _next(context);
	}

	private IDictionary<string, string> ConstructCspDictionary(CspDefinition definition)
	{
		var csp = definition.Sources
		.SelectMany(c => c.Directives.Select(d => new { Directive = d, c.Source }))
		.GroupBy(x => x.Directive)
		.ToDictionary(g => g.Key, g => string.Join(" ", g.Select(x => x.Source)));

		if (definition.EnableReporting)
		{
			var reportEndpoint = _linkGenerator.GetPathByAction(nameof(CspReportingController.Report), "CspReporting");
			var reportUri = string.IsNullOrWhiteSpace(definition.ReportUri) ? reportEndpoint : definition.ReportUri;
			if (!string.IsNullOrWhiteSpace(reportUri))
			{
				csp.TryAdd("report-uri", reportUri);
			}
		}

		return csp;
	}
}
