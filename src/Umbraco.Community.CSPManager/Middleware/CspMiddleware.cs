using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
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
/// <para>
/// A frontend request that Umbraco routed through a hostname (Culture &amp; Hostnames) gets the domain
/// policy of the content node that hostname is assigned to, if it has one, instead of the global
/// frontend policy. Every hostname of a node (e.g. one per culture) shares that policy. A disabled node policy follows
/// <see cref="CspManagerOptions.DisabledNodePolicyBehavior"/>. Backoffice requests never use
/// node policies.
/// </para>
/// </remarks>
public class CspMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IRuntimeState _runtimeState;
	private readonly ICspService _cspService;
	private readonly IEventAggregator _eventAggregator;
	private readonly ILogger<CspMiddleware> _logger;
	private readonly IUmbracoContextAccessor _umbracoContextAccessor;
	private readonly IIdKeyMap _idKeyMap;
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
	/// <param name="umbracoContextAccessor">Gives access to the routed request's domain.</param>
	/// <param name="idKeyMap">Maps the routed domain's content node id to the key its policy is stored against.</param>
	public CspMiddleware(
		RequestDelegate next,
		IRuntimeState runtimeState,
		ICspService cspService,
		IEventAggregator eventAggregator,
		IOptionsMonitor<CspManagerOptions> cspOptions,
		ILogger<CspMiddleware> logger,
		IUmbracoContextAccessor umbracoContextAccessor,
		IIdKeyMap idKeyMap)
	{
		_next = next;
		_runtimeState = runtimeState;
		_cspService = cspService;
		_eventAggregator = eventAggregator;
		_logger = logger;
		_umbracoContextAccessor = umbracoContextAccessor;
		_idKeyMap = idKeyMap;

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

				// Routing has finished by the time the response starts, so the matched domain is known.
				var definition = isBackOfficeRequest ? null : await GetDomainDefinitionAsync(context);

				// Deliberately not context.RequestAborted: this call populates a process-wide
				// cache that other in-flight requests await, so one client disconnecting must
				// not cancel the load and fault the shared entry for everyone else.
				definition ??= await _cspService.GetCachedCspDefinitionAsync(isBackOfficeRequest, CancellationToken.None);
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

	// Returns the node policy to apply, or null to use the global frontend policy. A disabled
	// node policy is returned (so no header is sent) only when configured with NoHeader.
	private async Task<CspDefinition?> GetDomainDefinitionAsync(HttpContext context)
	{
		try
		{
			// Wildcard (culture-only) domains only set the culture; a request is never routed by one.
			if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)
				|| umbracoContext.PublishedRequest?.Domain is not { IsWildcard: false } domain
				|| string.IsNullOrWhiteSpace(domain.Name))
			{
				return null;
			}

			// IIdKeyMap caches the mapping, so after the first request this costs no database call.
			var nodeKey = _idKeyMap.GetKeyForId(domain.ContentId, UmbracoObjectTypes.Document);
			if (!nodeKey.Success)
			{
				return null;
			}

			var contentKey = nodeKey.Result;

			// Shared cache again, so not context.RequestAborted (see the global lookup).
			var definition = await _cspService.GetCachedCspDefinitionForNodeAsync(contentKey, CancellationToken.None);
			if (definition is null)
			{
				return null;
			}

			if (definition.Enabled || _cspOptions.DisabledNodePolicyBehavior == DisabledNodePolicyBehavior.NoHeader)
			{
				Log.CspDomainPolicyApplied(_logger, definition.Id, contentKey, context.Request.Path);
				return definition;
			}

			Log.CspDomainPolicyDisabledFallback(_logger, definition.Id, context.Request.Path);
			return null;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			// Better the global frontend policy than no policy at all.
			Log.CspDomainPolicyLookupFailed(_logger, context.Request.Path, ex);
			return null;
		}
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

		// Dedupe on whole tokens per directive. A substring check against the joined value
		// would drop "example.com" once "cdn.example.com" is present for the same directive.
		var emitted = new Dictionary<string, HashSet<string>>();

		foreach (var source in definition.Sources)
		{
			// Saves are validated, but rows stored before that validation existed, or sources a
			// CspWritingNotification handler added, are not. A control character (CR/LF) makes the
			// server reject the whole header, so the response would go out with no CSP at all.
			if (!IsSafeHeaderValue(source.Source))
			{
				Log.CspUnsafeHeaderValueSkipped(_logger, definition.Id, "source");
				continue;
			}

			foreach (var directive in source.Directives)
			{
				if (!IsSafeHeaderValue(directive))
				{
					Log.CspUnsafeHeaderValueSkipped(_logger, definition.Id, "directive");
					continue;
				}

				if (!emitted.TryGetValue(directive, out var tokens))
				{
					tokens = new HashSet<string>(StringComparer.Ordinal);
					emitted[directive] = tokens;
				}

				if (!tokens.Add(source.Source))
				{
					continue;
				}

				csp[directive] = csp.TryGetValue(directive, out var existingValue)
					? $"{existingValue} {source.Source}"
					: source.Source;
			}
		}

		if (!string.IsNullOrWhiteSpace(definition.ReportingDirective) && !string.IsNullOrWhiteSpace(definition.ReportUri))
		{
			if (IsSafeHeaderValue(definition.ReportingDirective) && IsSafeHeaderValue(definition.ReportUri))
			{
				csp.TryAdd(definition.ReportingDirective, definition.ReportUri);
			}
			else
			{
				Log.CspUnsafeHeaderValueSkipped(_logger, definition.Id, "reporting directive");
			}
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
				AddNonceToDirectives(csp, nonce, definition.Id,
					Constants.Directives.ScriptSource, Constants.Directives.ScriptSourceElement);
			}

			if (styleNonceSet)
			{
				AddNonceToDirectives(csp, nonce, definition.Id,
					Constants.Directives.StyleSource, Constants.Directives.StyleSourceElement);
			}
		}

		return csp;
	}

	private static bool IsSafeHeaderValue(string? value)
		=> value is not null && !value.Any(char.IsControl);

	// A browser that understands script-src-elem/style-src-elem consults it for <script>/<style>/<link>
	// and ignores script-src/style-src for those elements; a browser that predates it only knows the
	// broader directive. Putting the nonce on every configured directive in the pair keeps nonced
	// elements working in both. (A nonce makes 'unsafe-inline' in that directive ignored; sites that
	// need inline event handlers or style attributes alongside nonces should grant 'unsafe-inline'
	// via script-src-attr/style-src-attr, which the nonce never touches.)
	private void AddNonceToDirectives(Dictionary<string, string> csp, string nonce, Guid definitionId, params string[] directives)
	{
		if (string.IsNullOrWhiteSpace(nonce))
		{
			return;
		}

		var added = false;

		foreach (var directive in directives)
		{
			if (csp.TryGetValue(directive, out var existingValue))
			{
				csp[directive] = $"{existingValue} 'nonce-{nonce}'";
				added = true;
			}
		}

		if (!added)
		{
			// The nonce only augments directives the user has configured; adding
			// script-src/style-src from scratch would block every other source.
			Log.CspNonceDirectiveMissing(_logger, string.Join(", ", directives), definitionId);
		}
	}
}