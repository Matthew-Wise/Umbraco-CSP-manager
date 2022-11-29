namespace Umbraco.Community.CSPManager.Middleware;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Cms.Core;
using Cms.Core.Services;
using CommunityToolkit.HighPerformance;
using Models;
using Services;

public class CspMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IRuntimeState _runtimeState;
	private readonly ICspService _cspService;

	public CspMiddleware(
		RequestDelegate next,
		IRuntimeState runtimeState,
		ICspService cspService)
	{
		_next = next;
		_runtimeState = runtimeState;
		_cspService = cspService;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		if (_runtimeState.Level == RuntimeLevel.Install)
		{
			await _next(context);
			return;
		}
		
		//TODO: Caching
		
		var definition = _cspService.GetCspDefinition(context);

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

	private static IDictionary<string,string> ConstructCspDictionary(CspDefinition definition)
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
