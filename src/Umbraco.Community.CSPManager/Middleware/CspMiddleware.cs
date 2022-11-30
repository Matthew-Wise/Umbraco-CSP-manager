namespace Umbraco.Community.CSPManager.Middleware;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Cms.Core;
using Cms.Core.Services;
using CommunityToolkit.HighPerformance;
using Models;
using Services;
using Umbraco.Extensions;
using Umbraco.Cms.Core.Events;
using Umbraco.Community.CSPManager.Notifications;

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
		if (_runtimeState.Level == RuntimeLevel.Install)
		{
			await _next(context);
			return;
		}
		
		//TODO: Caching
		
		var definition = await _cspService.GetCspDefinitionAsync(context.Request.IsBackOfficeRequest());

		_eventAggregator.Publish(new CspWritingNotification(definition, context));

		if (definition is not { Enabled: true })
		{
			await _next(context);
			return;
		}

		var csp = await Task.FromResult(ConstructCspDictionary(definition));
		var cspValue = string.Join(";", csp.Select(x => x.Key + " " + x.Value));
		if (!string.IsNullOrEmpty(cspValue))
		{
			context.Response.Headers.Add("Content-Security-Policy", cspValue);
		}
		
		await _next(context);
	}

	public IDictionary<string,string> ConstructCspDictionary(CspDefinition definition)
	{
		var csp = new Dictionary<string, string>();
		foreach (var item in CspConstants.AllDirectives.Enumerate())
		{
			var key = item.Value;
			var sources = definition.Sources
				.Where(x => x.Directives.Contains(key))
				.Select(x => x.Source).ToArray();
			if (sources.Any())
			{
				csp.Add(item.Value, string.Join(" ", sources));
			}
		}

		return csp;
	}
}
