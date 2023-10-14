namespace Umbraco.Community.CSPManager.Services;

using Cms.Core.Events;
using Cms.Core.Hosting;
using Cms.Infrastructure.Scoping;
using Models;
using NPoco.Expressions;
using Umbraco.Extensions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Community.CSPManager.Notifications;
using CommunityToolkit.HighPerformance;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

public class CspService : ICspService
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IHostingEnvironment _hostingEnvironment;
	private readonly IScopeProvider _scopeProvider;
	private readonly IAppPolicyCache _runtimeCache;
	public CspService(
		IEventAggregator eventAggregator,
		IHostingEnvironment hostingEnvironment,
		IScopeProvider scopeProvider,
		AppCaches appCaches)
	{
		_eventAggregator = eventAggregator;
		_hostingEnvironment = hostingEnvironment;
		_scopeProvider = scopeProvider;
		_runtimeCache = appCaches.RuntimeCache;
	}

	public CspDefinition GetCspDefinition(bool isBackOfficeRequest)
	{
		using var scope = _scopeProvider.CreateScope();

		//TODO: Oembed providers - https://our.umbraco.com/documentation/extending/Embedded-Media-Provider/
		CspDefinition definition = GetDefinition(scope, isBackOfficeRequest)
			?? new CspDefinition
			{
				Id = isBackOfficeRequest ? CspConstants.DefaultBackofficeId : CspConstants.DefaultFrontEndId,
				Enabled = false,
				IsBackOffice = isBackOfficeRequest
			};
		
		scope.Complete();
		return definition;
	}

	public CspDefinition? GetCachedCspDefinition(bool isBackOfficeRequest)
	{
		string cacheKey = isBackOfficeRequest ? CspConstants.BackOfficeCacheKey : CspConstants.FrontEndCacheKey;

		return _runtimeCache.GetCacheItem(cacheKey, () => GetCspDefinition(isBackOfficeRequest));
	}

	private static CspDefinition? GetDefinition(IScope scope, bool isBackOffice)
	{
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.LeftJoin<CspDefinitionSource>()
			.On<CspDefinition, CspDefinitionSource>((d, s) => d.Id == s.DefinitionId)
			.Where<CspDefinition>(x => x.IsBackOffice == isBackOffice);

		var data = scope.Database.FetchOneToMany<CspDefinition>(c => c.Sources, sql);
		return data.FirstOrDefault();
	}

	public async Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition)
	{
		using var scope = _scopeProvider.CreateScope();

		definition = await SaveDefinitionAsync(scope, definition);

		scope.Complete();

		await _eventAggregator.PublishAsync(new CspSavedNotification(definition));

		return definition;
	}

	public async Task<string> GenerateCspHeader(CspDefinition definition, HttpContextWrapper httpContext)
	{
		var csp = ConstructCspDictionary(definition, httpContext);
		var cspValue = string.Join(";", csp.Select(x => x.Key + " " + x.Value));
		return cspValue;
	}

	public string GetCspScriptNonce(HttpContextWrapper context)
	{
		var cspManagerContext = context.GetCspManagerContext();

		if (cspManagerContext == null)
		{
			return string.Empty;
		}

		if (!string.IsNullOrEmpty(cspManagerContext.ScriptNonce))
		{
			return cspManagerContext.ScriptNonce;
		}

		var nonce = GenerateCspNonceValue();

		SetCspDirectiveNonce(cspManagerContext, nonce, CspConstants.CspDirectives.ScriptSrc);

		return nonce;
	}
	public string GetCspStyleNonce(HttpContextWrapper context)
	{
		var cspManagerContext = context.GetCspManagerContext();

		if (cspManagerContext == null)
		{
			return string.Empty;
		}

		if (!string.IsNullOrEmpty(cspManagerContext.ScriptNonce))
		{
			return cspManagerContext.ScriptNonce;
		}

		var nonce = GenerateCspNonceValue();

		SetCspDirectiveNonce(cspManagerContext, nonce, CspConstants.CspDirectives.StyleSrc);

		return nonce;
	}

	public async Task SetCspHeaders(HttpContextWrapper context)
	{
		var definition = GetCachedCspDefinition(context.GetOriginalHttpContext<HttpContext>().Request.IsBackOfficeRequest());
		var cspValue = GenerateCspHeader(definition, context).Result;
		context.RemoveHttpHeader(definition.ReportOnly ? CspConstants.ReportOnlyHeaderName : CspConstants.HeaderName);
		context.SetHttpHeader(definition.ReportOnly ? CspConstants.ReportOnlyHeaderName : CspConstants.HeaderName, cspValue);
	}

	private IDictionary<string, string> ConstructCspDictionary(CspDefinition definition, HttpContextWrapper httpContext)
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
				var scriptNonce = GetCspScriptNonce(httpContext);
				if (httpContext.GetItem<string>("CspManagerScriptNonceSet") == "set" && !string.IsNullOrEmpty(scriptNonce) && key.Equals("script-src"))
				{
					sources.Add($"'nonce-{scriptNonce}'");
				}

				var styleNonce = GetCspStyleNonce(httpContext);
				if (httpContext.GetItem<string>("CspManagerStyleNonceSet") == "set" && !string.IsNullOrEmpty(styleNonce) && key.Equals("style-src"))
				{
					sources.Add($"'nonce-{styleNonce}'");
				}

				csp.Add(item.Value, string.Join(" ", sources));
			}
		}

		return csp;
	}

	private static async Task<CspDefinition> SaveDefinitionAsync(IScope scope, CspDefinition definition)
	{
		await scope.Database.SaveAsync(definition);

		var sourceValues = definition.Sources.Select(s => s.Source).ToList();
		var cmdDelete = scope.Database.DeleteManyAsync<CspDefinitionSource>()
			.Where(s => !s.Source.In(sourceValues) && s.DefinitionId == definition.Id);
		
		await 	cmdDelete.Execute();
		
		foreach (var source in definition.Sources)
		{
			await scope.Database.SaveAsync(source);
		}

		return definition;
	}

	private static void SetCspDirectiveNonce(CspManagerContext cspManagerContext, string nonce, CspConstants.CspDirectives directive)
	{
		switch (directive)
		{
			case CspConstants.CspDirectives.ScriptSrc:
				cspManagerContext.ScriptNonce = nonce;

				break;
			case CspConstants.CspDirectives.StyleSrc:
				cspManagerContext.StyleNonce = nonce;
				break;
		}
	}

	private static string GenerateCspNonceValue()
	{
		using var rng = RandomNumberGenerator.Create();
		var nonceBytes = new byte[18];
		rng.GetBytes(nonceBytes);
		return Convert.ToBase64String(nonceBytes);
	}
}
