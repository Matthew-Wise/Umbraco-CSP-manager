namespace Umbraco.Community.CSPManager.Middleware;

using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Microsoft.AspNetCore.Http;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Cms.Core.Events;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Extensions;
using Umbraco.Community.CSPManager.Models;
using CommunityToolkit.HighPerformance;

public class CspMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IRuntimeState _runtimeState;
	private readonly ICspService _cspService;
	private readonly IEventAggregator _eventAggregator;

	public CspMiddleware(
		RequestDelegate next,
		IRuntimeState runtimeState,
		ICspService cspService,
		IEventAggregator eventAggregator)
	{
		_next = next;
		_runtimeState = runtimeState;
		_cspService = cspService;
		_eventAggregator = eventAggregator;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		if (_runtimeState.Level != RuntimeLevel.Run)
		{
			await _next(context);
			return;
		}

		context.Response.OnStarting( async () =>
		{
			var definition = _cspService.GetCachedCspDefinition(context.Request.IsBackOfficeRequest());

			await _eventAggregator.PublishAsync(new CspWritingNotification(definition, context));

			if (definition is not { Enabled: true })
			{
				
				return;
			}

			var httpContext = new HttpContextWrapper(context);
			var csp = ConstructCspDictionary(definition, httpContext);
			var cspValue = string.Join(";", csp.Select(x => x.Key + " " + x.Value));

			if (!string.IsNullOrEmpty(cspValue))
			{
				context.Response.Headers.Add(definition.ReportOnly ? CspConstants.ReportOnlyHeaderName : CspConstants.HeaderName, cspValue);
			}
		});	

		await _next(context);
	}

	private IDictionary<string, string> ConstructCspDictionary(CspDefinition definition, HttpContextWrapper httpContext)
	{
		var csp = new Dictionary<string, string>();
		string? scriptNonce = null;
		if (httpContext.GetItem<string>(CspConstants.CspManagerScriptNonceSet) == "set")
		{
			scriptNonce = _cspService.GetCspScriptNonce(httpContext);
		}

		string? styleNonce = null;
		if (httpContext.GetItem<string>(CspConstants.CspManagerStyleNonceSet) == "set")
		{
			styleNonce = _cspService.GetCspStyleNonce(httpContext);
		}


		foreach (var item in CspConstants.AllDirectives.Enumerate())
		{
			var key = item.Value;
			var sources = definition.Sources
				.Where(x => x.Directives.Contains(key))
				.Select(x => x.Source).ToHashSet();



			if (!string.IsNullOrEmpty(scriptNonce) && key.Equals("script-src"))
			{
				sources.Add($"'nonce-{scriptNonce}'");
			}

			if (!string.IsNullOrEmpty(styleNonce) && key.Equals("style-src"))
			{
				sources.Add($"'nonce-{styleNonce}'");
			}

			if (sources.Count <= 0)
			{
				continue;
			}

			csp.TryAdd(item.Value, string.Join(" ", sources));
		}

		if (!string.IsNullOrWhiteSpace(definition.ReportingDirective) && !string.IsNullOrWhiteSpace(definition.ReportUri))
		{
			csp.TryAdd(definition.ReportingDirective, definition.ReportUri);
		}

		return csp;
	}
}
