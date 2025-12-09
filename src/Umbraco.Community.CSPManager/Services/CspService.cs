using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPoco.Expressions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.CSPManager.Extensions;
using Umbraco.Community.CSPManager.Logging;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Extensions;

namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Implementation of <see cref="ICspService"/> that manages CSP definitions using
/// Umbraco's scoping and caching infrastructure.
/// </summary>
/// <remarks>
/// This service uses NPoco ORM for database operations and Umbraco's runtime cache
/// for performance. It also integrates with the event aggregator to publish notifications
/// when CSP definitions are saved.
/// </remarks>
internal sealed class CspService : ICspService
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IScopeProvider _scopeProvider;
	private readonly IAppPolicyCache _runtimeCache;
	private readonly ILogger<CspService> _logger;

	public CspService(
		IEventAggregator eventAggregator,
		IScopeProvider scopeProvider,
		AppCaches caches,
		ILogger<CspService> logger)
	{
		_eventAggregator = eventAggregator;
		_scopeProvider = scopeProvider;
		_runtimeCache = caches.RuntimeCache;
		_logger = logger;
	}

	public async Task<CspDefinition?> GetCachedCspDefinitionAsync(bool isBackOfficeRequest, CancellationToken cancellationToken)
	{
		var cacheKey = isBackOfficeRequest ? Constants.BackOfficeCacheKey : Constants.FrontEndCacheKey;
		var context = isBackOfficeRequest ? "BackOffice" : "Frontend";
		var factoryCalled = false;

		var result = await _runtimeCache.GetCacheItemAsync(cacheKey, async () =>
		{
			factoryCalled = true;
			return await GetCspDefinitionAsync(isBackOfficeRequest, cancellationToken);
		}, timeout: null);

		if (!factoryCalled && result is not null)
		{
			Log.CspDefinitionRetrievedFromCache(_logger, result.Id, context);
		}

		return result;
	}

	public async Task<CspDefinition> GetCspDefinitionAsync(bool isBackOfficeRequest, CancellationToken cancellationToken)
	{
		var context = isBackOfficeRequest ? "BackOffice" : "Frontend";
		Log.LoadingCspDefinitionFromDatabase(_logger, context);

		using var scope = _scopeProvider.CreateScope();

		CspDefinition definition = await GetDefinitionAsync(scope, isBackOfficeRequest, cancellationToken)
			?? new CspDefinition
			{
				Id = isBackOfficeRequest ? Constants.DefaultBackofficeId : Constants.DefaultFrontEndId,
				Enabled = false,
				IsBackOffice = isBackOfficeRequest
			};

		scope.Complete();
		return definition;
	}

	public string GetOrCreateCspScriptNonce(HttpContext context)
	{
		var cspManagerContext = context.GetOrCreateCspManagerContext();

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

	public string GetOrCreateCspStyleNonce(HttpContext context)
	{
		var cspManagerContext = context.GetOrCreateCspManagerContext();

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

	public async Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition, CancellationToken cancellationToken)
	{
		var context = definition.IsBackOffice ? "BackOffice" : "Frontend";
		Log.SavingCspDefinition(_logger, definition.Id, context);

		try
		{
			using var scope = _scopeProvider.CreateScope();

			definition = await SaveDefinitionAsync(scope, definition, cancellationToken);

			scope.Complete();

			await _eventAggregator.PublishAsync(new CspSavedNotification(definition), cancellationToken);

			Log.CspDefinitionSaved(_logger, definition.Id, definition.Sources.Count);

			return definition;
		}
		catch (Exception ex)
		{
			Log.CspDefinitionSaveFailed(_logger, definition.Id, ex);
			throw;
		}
	}

	private static async Task<CspDefinition> SaveDefinitionAsync(IScope scope, CspDefinition definition, CancellationToken cancellationToken)
	{
		await scope.Database.SaveAsync(definition, cancellationToken);

		//Empty sources have no value and clog up the header so remove them
		definition.Sources = [.. definition.Sources.Where(s => !string.IsNullOrWhiteSpace(s.Source))];

		var sourceValues = definition.Sources.Select(s => s.Source).ToList();
		var cmdDelete = scope.Database.DeleteManyAsync<CspDefinitionSource>()
			.Where(s => !s.Source.In(sourceValues) && s.DefinitionId == definition.Id);

		await cmdDelete.Execute(cancellationToken);

		foreach (var source in definition.Sources)
		{
			await scope.Database.SaveAsync(source, cancellationToken);
		}

		return definition;
	}

	// Two queries instead of a single join because FetchOneToMany has no async variant.
	// The extra round-trip is acceptable here since results are cached and cache misses are rare.
	private static async Task<CspDefinition?> GetDefinitionAsync(IScope scope, bool isBackOffice, CancellationToken cancellationToken)
	{
		var definitionSql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.Where<CspDefinition>(x => x.IsBackOffice == isBackOffice);

		var definition = await scope.Database.FirstOrDefaultAsync<CspDefinition>(definitionSql, cancellationToken);

		if (definition is null)
		{
			return null;
		}

		var sourcesSql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinitionSource>()
			.Where<CspDefinitionSource>(x => x.DefinitionId == definition.Id);

		definition.Sources = await scope.Database.FetchAsync<CspDefinitionSource>(sourcesSql, cancellationToken);

		return definition;
	}

	private static string GenerateCspNonceValue()
	{
		Span<byte> nonceBytes = stackalloc byte[16]; // 16 bytes = 128 bits
		RandomNumberGenerator.Fill(nonceBytes);
		return Convert.ToBase64String(nonceBytes);
	}
}