using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.CSPManager.Extensions;
using Umbraco.Community.CSPManager.Logging;
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
	private readonly ILogger<CspMiddleware> _logger;
	private CspManagerOptions _cspOptions;

	/// <summary>
	/// Initializes a new instance of the <see cref="CspMiddleware"/> class.
	/// </summary>
	/// <param name="next">The next middleware in the pipeline.</param>
	/// <param name="runtimeState">The Umbraco runtime state service.</param>
	/// <param name="cspService">The CSP service for retrieving definitions.</param>
	/// <param name="eventAggregator">The event aggregator for publishing notifications.</param>
	/// <param name="cspOptions">The CSP Manager configuration options.</param>
	/// <param name="logger">The logger for diagnostic output.</param>
	public CspMiddleware(
		RequestDelegate next,
		IRuntimeState runtimeState,
		ICspService cspService,
		IEventAggregator eventAggregator,
		IOptionsMonitor<CspManagerOptions> cspOptions,
		ILogger<CspMiddleware> logger)
	{
		_next = next;
		_runtimeState = runtimeState;
		_cspService = cspService;
		_eventAggregator = eventAggregator;
		_logger = logger;

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
			try
			{
				Log.CspOnStartingFired(_logger, context.Request.Path);

				var isBackOfficeRequest = context.Request.IsBackOfficeRequest() ||
					context.Request.Path.StartsWithSegments("/umbraco");

				if (isBackOfficeRequest && _cspOptions.DisableBackOfficeHeader)
				{
					Log.CspBackOfficeDisabled(_logger);
					return;
				}

				// Deliberately not context.RequestAborted: this call populates a process-wide
				// cache that other in-flight requests await, so one client disconnecting must
				// not cancel the load and fault the shared entry for everyone else.
				var definition = await _cspService.GetCachedCspDefinitionAsync(isBackOfficeRequest, CancellationToken.None);
				await _eventAggregator.PublishAsync(new CspWritingNotification(definition, context));

				if (definition is null)
				{
					Log.CspDefinitionNotFound(_logger, isBackOfficeRequest ? "BackOffice" : "Frontend");
					return;
				}

				if (!definition.Enabled)
				{
					Log.CspDefinitionDisabled(_logger, definition.Id);
					return;
				}

				var csp = ConstructCspDictionary(definition, context);
				var cspValue = BuildCspHeader(csp);

				if (!string.IsNullOrWhiteSpace(cspValue))
				{
					var headerName = definition.ReportOnly ? Constants.ReportOnlyHeaderName : Constants.HeaderName;
					context.Response.Headers.Append(headerName, cspValue);
					Log.CspHeaderApplied(_logger, headerName, definition.Id, cspValue.Length);
				}
				else
				{
					Log.CspHeaderEmpty(_logger, definition.Id);
				}
			}
			catch (OperationCanceledException)
			{
				// The client disconnected before the response started, so there is no response
				// left to add a header to. Rethrowing from OnStarting would surface as an
				// unhandled application exception and abort the connection.
				Log.CspHeaderCancelled(_logger, context.Request.Path);
			}
			catch (Exception ex)
			{
				// CSP header injection should never break the request.
				// Log the error and continue without the CSP header.
				Log.CspHeaderConstructionFailed(_logger, context.Request.Path, ex);
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

		var scriptNonceSet = httpContext.GetItem<bool>(Constants.TagHelper.CspManagerScriptNonceSet) == true;
		var styleNonceSet = httpContext.GetItem<bool>(Constants.TagHelper.CspManagerStyleNonceSet) == true;

		if (scriptNonceSet || styleNonceSet)
		{
			var nonce = _cspService.GetOrCreateCspNonce(httpContext);

			if (scriptNonceSet)
			{
				var directive = ResolveNonceDirective(csp, Constants.Directives.ScriptSourceElement, Constants.Directives.ScriptSource);
				AddNonceToDirective(csp, directive, nonce, definition.Id);
			}

			if (styleNonceSet)
			{
				var directive = ResolveNonceDirective(csp, Constants.Directives.StyleSourceElement, Constants.Directives.StyleSource);
				AddNonceToDirective(csp, directive, nonce, definition.Id);
			}
		}

		return csp;
	}

	// script-src-elem/style-src-elem override script-src/style-src for element-level scripts and
	// styles, so when one is configured it is the only directive the browser consults for a
	// <script>/<style>/<link> - a nonce added to the broader directive would be ignored and every
	// tagged element blocked. Only the directive actually consulted gets the nonce: adding it to
	// script-src as well would also make the browser ignore 'unsafe-inline' there, silently
	// breaking the inline event handlers that script-src-attr falls back to.
	private static string ResolveNonceDirective(Dictionary<string, string> csp, string elementDirective, string fallbackDirective)
		=> csp.ContainsKey(elementDirective) ? elementDirective : fallbackDirective;

	private void AddNonceToDirective(Dictionary<string, string> csp, string directive, string nonce, Guid definitionId)
	{
		if (string.IsNullOrWhiteSpace(nonce))
		{
			return;
		}

		if (csp.TryGetValue(directive, out var existingValue))
		{
			csp[directive] = $"{existingValue} 'nonce-{nonce}'";
		}
		else
		{
			// The nonce only augments directives the user has configured; adding
			// script-src/style-src from scratch would block every other source.
			Log.CspNonceDirectiveMissing(_logger, directive, definitionId);
		}
	}
}