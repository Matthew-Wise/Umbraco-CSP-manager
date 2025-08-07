using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using NPoco.Expressions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.CSPManager.Extensions;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Extensions;

namespace Umbraco.Community.CSPManager.Services;

internal sealed class CspService : ICspService
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IScopeProvider _scopeProvider;
	private readonly IAppPolicyCache _runtimeCache;
	public CspService(
		IEventAggregator eventAggregator,
		IScopeProvider scopeProvider,
		AppCaches appCaches)
	{
		_eventAggregator = eventAggregator;
		_scopeProvider = scopeProvider;
		_runtimeCache = appCaches.RuntimeCache;
	}
	public CspDefinition? GetCachedCspDefinition(bool isBackOfficeRequest)
	{
		var cacheKey = isBackOfficeRequest ? Constants.BackOfficeCacheKey : Constants.FrontEndCacheKey;
		return _runtimeCache.GetCacheItem(cacheKey, () => GetCspDefinition(isBackOfficeRequest));
	}

	public CspDefinition GetCspDefinition(bool isBackOfficeRequest)
	{
		using var scope = _scopeProvider.CreateScope();

		CspDefinition definition = GetDefinition(scope, isBackOfficeRequest)
			?? new CspDefinition
			{
				Id = isBackOfficeRequest ? Constants.DefaultBackofficeId : Constants.DefaultFrontEndId,
				Enabled = false,
				IsBackOffice = isBackOfficeRequest
			};

		scope.Complete();
		return definition;
	}

	public string GetCspScriptNonce(HttpContext context)
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

		cspManagerContext.ScriptNonce = nonce;

		return nonce;
	}

	public string GetCspStyleNonce(HttpContext context)
	{
		var cspManagerContext = context.GetCspManagerContext();

		if (cspManagerContext == null)
		{
			return string.Empty;
		}

		if (!string.IsNullOrEmpty(cspManagerContext.StyleNonce))
		{
			return cspManagerContext.StyleNonce;
		}

		var nonce = GenerateCspNonceValue();

		cspManagerContext.StyleNonce = nonce;

		return nonce;
	}

	public async Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition)
	{
		using var scope = _scopeProvider.CreateScope();

		definition = await SaveDefinitionAsync(scope, definition);

		scope.Complete();

		await _eventAggregator.PublishAsync(new CspSavedNotification(definition));

		return definition;
	}

	private static async Task<CspDefinition> SaveDefinitionAsync(IScope scope, CspDefinition definition)
	{
		await scope.Database.SaveAsync(definition);

		definition.Sources = definition.Sources.Where(s => !string.IsNullOrWhiteSpace(s.Source)).ToList();

		var sourceValues = definition.Sources.Select(s => s.Source).ToList();
		var cmdDelete = scope.Database.DeleteManyAsync<CspDefinitionSource>()
			.Where(s => !s.Source.In(sourceValues) && s.DefinitionId == definition.Id);

		await cmdDelete.Execute();

		foreach (var source in definition.Sources)
		{
			await scope.Database.SaveAsync(source);
		}

		return definition;
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

	private static string GenerateCspNonceValue()
	{
		using var rng = RandomNumberGenerator.Create();
		var nonceBytes = new byte[18];
		rng.GetBytes(nonceBytes);
		return Convert.ToBase64String(nonceBytes);
	}
}
