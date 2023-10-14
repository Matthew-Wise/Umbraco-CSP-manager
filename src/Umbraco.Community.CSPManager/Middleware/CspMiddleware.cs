namespace Umbraco.Community.CSPManager.Middleware;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Cms.Core;
using Cms.Core.Services;
using CommunityToolkit.HighPerformance;
using Helpers;
using Models;
using Services;
using Umbraco.Extensions;
using Cms.Core.Events;
using Notifications;

public class CspMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IRuntimeState _runtimeState;
	private readonly ICspService _cspService;
	private readonly IEventAggregator _eventAggregator;
	private readonly ICspNonceHelper _cspNonceHelper;

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
		_cspNonceHelper = (ICspNonceHelper)new CspNonceHelper();
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

		var csp = await Task.FromResult(ConstructCspDictionary(definition, context));
		var cspValue = string.Join(";", csp.Select(x => x.Key + " " + x.Value));
		if (!string.IsNullOrEmpty(cspValue))
		{
			context.Response.Headers.Add(definition.ReportOnly ? CspConstants.ReportOnlyHeaderName : CspConstants.HeaderName, cspValue);
		}
		
		await _next(context);
	}

	private IDictionary<string,string> ConstructCspDictionary(CspDefinition definition, HttpContext context)
	{
		var csp = new Dictionary<string, string>();
		foreach (var item in CspConstants.AllDirectives.Enumerate())
		{
			var key = item.Value;
			var sources = definition.Sources
				.Where(x => x.Directives.Contains(key))
				.Select(x => x.Source).ToList();
			if (sources.Any())
			{
				if (item.Value.Equals("script-src"))
				{
					sources.Add($"'nonce-{_cspNonceHelper.GetCspScriptNonce(context)}'");
				}

				//if (item.Value.Equals("style-src"))
				//{
				//	sources.Add($"'nonce-{_cspConfigOverride.GetCspStyleNonce(context)}'");
				//}
				csp.Add(item.Value, string.Join(" ", sources));
			}
		}

		return csp;
	}
}
