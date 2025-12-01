using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.CSPManager.Extensions;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Extensions;

namespace Umbraco.Community.CSPManager.Middleware;

public class CspMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IRuntimeState _runtimeState;
	private readonly ICspService _cspService;
	private readonly IEventAggregator _eventAggregator;
	private CspManagerOptions _cspOptions;

	public CspMiddleware(
		RequestDelegate next,
		IRuntimeState runtimeState,
		ICspService cspService,
		IEventAggregator eventAggregator,
		IOptionsMonitor<CspManagerOptions> cspOptions)
	{
		_next = next;
		_runtimeState = runtimeState;
		_cspService = cspService;
		_eventAggregator = eventAggregator;

		cspOptions.OnChange(config =>
		{
			_cspOptions = config;
		});
		_cspOptions = cspOptions.CurrentValue;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		if (_runtimeState.Level != RuntimeLevel.Run)
		{
			await _next(context);
			return;
		}

		context.Response.OnStarting(async () =>
		{
			var isBackOfficeRequest = context.Request.IsBackOfficeRequest() ||
			context.Request.Path.StartsWithSegments("/umbraco");

			if (isBackOfficeRequest && _cspOptions.DisableBackOfficeHeader)
			{
				return;
			}

			var definition = _cspService.GetCachedCspDefinition(isBackOfficeRequest);
			await _eventAggregator.PublishAsync(new CspWritingNotification(definition, context));

			if (definition is not { Enabled: true })
			{

				return;
			}

			var csp = ConstructCspDictionary(definition, context);
			var cspValue = string.Join(";", csp.Select(kvp => $"{kvp.Key} {kvp.Value}"));

			if (!string.IsNullOrWhiteSpace(cspValue))
			{
				context.Response.Headers.Append(definition.ReportOnly ? Constants.ReportOnlyHeaderName : Constants.HeaderName, cspValue);
			}
		});

		await _next(context);
	}

	private Dictionary<string, string> ConstructCspDictionary(CspDefinition definition, HttpContext httpContext)
	{
		var csp = definition.Sources
		.SelectMany(c => c.Directives.Select(d => new { Directive = d, c.Source }))
		.GroupBy(x => x.Directive)
		.ToDictionary(g => g.Key, g => string.Join(" ", new HashSet<string>(g.Select(x => x.Source))));

		if (!string.IsNullOrWhiteSpace(definition.ReportingDirective) && !string.IsNullOrWhiteSpace(definition.ReportUri))
		{
			csp.TryAdd(definition.ReportingDirective, definition.ReportUri);
		}

		if (definition.UpgradeInsecureRequests)
		{
			csp.TryAdd(Constants.Directives.UpgradeInsecureRequests, "");
		}

		string? scriptNonce = null;
		if (httpContext.GetItem<bool>(Constants.TagHelper.CspManagerScriptNonceSet) == true)
		{
			scriptNonce = _cspService.GetOrCreateCspScriptNonce(httpContext);
			AddNonceToDirective(csp, Constants.Directives.ScriptSource, scriptNonce);
		}

		string? styleNonce = null;
		if (httpContext.GetItem<bool>(Constants.TagHelper.CspManagerStyleNonceSet) == true)
		{
			styleNonce = _cspService.GetOrCreateCspStyleNonce(httpContext);
			AddNonceToDirective(csp, Constants.Directives.StyleSource, styleNonce);
		}

		return csp;
	}

	private static void AddNonceToDirective(Dictionary<string, string> csp, string directive, string nonce)
	{
		if (!string.IsNullOrWhiteSpace(nonce) && csp.TryGetValue(directive, out var existingValue))
		{
			csp[directive] = $"{existingValue} 'nonce-{nonce}'";
		}
	}
}