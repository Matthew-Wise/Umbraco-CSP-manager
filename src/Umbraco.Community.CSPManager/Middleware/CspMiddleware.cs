namespace Umbraco.Community.CSPManager.Middleware;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Cms.Core;
using Cms.Core.Services;
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
		
		var definition = _cspService.GetCachedCspDefinition(context.Request.IsBackOfficeRequest());

		await _eventAggregator.PublishAsync(new CspWritingNotification(definition, context));

		if (definition is not { Enabled: true })
		{
			await _next(context);
			return;
		}

		var httpContext = new HttpContextWrapper(context);

		var cspValue = await _cspService.GenerateCspHeader(definition, httpContext);
		if (!string.IsNullOrEmpty(cspValue))
		{
			context.Response.Headers.Add(definition.ReportOnly ? CspConstants.ReportOnlyHeaderName : CspConstants.HeaderName, cspValue);
		}

		await _next(context);
	}
}
