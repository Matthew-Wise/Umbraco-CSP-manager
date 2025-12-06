using System.Text;
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

/// <summary>
/// ASP.NET Core middleware that injects Content Security Policy headers into HTTP responses.
/// </summary>
/// <remarks>
/// <para>
/// This middleware intercepts all requests and adds the appropriate CSP header based on the
/// configured policy for either the frontend or backoffice context. It supports both
/// enforcing (Content-Security-Policy) and report-only (Content-Security-Policy-Report-Only) modes.
/// </para>
/// <para>
/// The middleware only runs when Umbraco is in the <see cref="Umbraco.Cms.Core.RuntimeLevel.Run"/> state.
/// It also respects the <see cref="CspManagerOptions.DisableBackOfficeHeader"/> configuration option.
/// </para>
/// </remarks>
public class CspMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IRuntimeState _runtimeState;
	private readonly ICspService _cspService;
	private readonly IEventAggregator _eventAggregator;
	private CspManagerOptions _cspOptions;

	/// <summary>
	/// Initializes a new instance of the <see cref="CspMiddleware"/> class.
	/// </summary>
	/// <param name="next">The next middleware in the pipeline.</param>
	/// <param name="runtimeState">The Umbraco runtime state service.</param>
	/// <param name="cspService">The CSP service for retrieving definitions.</param>
	/// <param name="eventAggregator">The event aggregator for publishing notifications.</param>
	/// <param name="cspOptions">The CSP Manager configuration options.</param>
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

	/// <summary>
	/// Processes the HTTP request and adds CSP headers to the response.
	/// </summary>
	/// <param name="context">The HTTP context for the current request.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// The CSP header is added using <see cref="HttpResponse.OnStarting"/> to ensure it is
	/// set before any response body is written. A <see cref="Notifications.CspWritingNotification"/>
	/// is published before the header is constructed, allowing other components to modify
	/// or react to the CSP being applied.
	/// </remarks>
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
			var cspValue = BuildCspHeader(csp);

			if (!string.IsNullOrWhiteSpace(cspValue))
			{
				context.Response.Headers.Append(definition.ReportOnly ? Constants.ReportOnlyHeaderName : Constants.HeaderName, cspValue);
			}
		});

		await _next(context);
	}

	private static string BuildCspHeader(Dictionary<string, string> csp)
	{
		if (csp.Count == 0) return string.Empty;

		var builder = new StringBuilder(256); // Pre-allocate reasonable size
		foreach (var kvp in csp)
		{
			if (builder.Length > 0) builder.Append(';');
			builder.Append(kvp.Key);
			if (!string.IsNullOrEmpty(kvp.Value))
			{
				builder.Append(' ').Append(kvp.Value);
			}
		}
		return builder.ToString();
	}

	private Dictionary<string, string> ConstructCspDictionary(CspDefinition definition, HttpContext httpContext)
	{
		var csp = new Dictionary<string, string>(definition.Sources.Count);

		foreach (var source in definition.Sources)
		{
			foreach (var directive in source.Directives)
			{
				if (!csp.TryGetValue(directive, out var existingValue))
				{
					csp[directive] = source.Source;
				}
				else if (!existingValue.Contains(source.Source))
				{
					csp[directive] = $"{existingValue} {source.Source}";
				}
			}
		}

		if (!string.IsNullOrWhiteSpace(definition.ReportingDirective) && !string.IsNullOrWhiteSpace(definition.ReportUri))
		{
			csp.TryAdd(definition.ReportingDirective, definition.ReportUri);
		}

		if (definition.UpgradeInsecureRequests)
		{
			csp.TryAdd(Constants.Directives.UpgradeInsecureRequests, "");
		}

		if (httpContext.GetItem<bool>(Constants.TagHelper.CspManagerScriptNonceSet) == true)
		{
			string? scriptNonce = _cspService.GetOrCreateCspScriptNonce(httpContext);
			AddNonceToDirective(csp, Constants.Directives.ScriptSource, scriptNonce);
		}

		if (httpContext.GetItem<bool>(Constants.TagHelper.CspManagerStyleNonceSet) == true)
		{
			string? styleNonce = _cspService.GetOrCreateCspStyleNonce(httpContext);
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