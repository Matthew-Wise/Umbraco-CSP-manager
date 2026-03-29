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

	public async Task<CspDefinition?> GetCspDefinitionAsync(Guid key, CancellationToken cancellationToken)
	{
		using var scope = _scopeProvider.CreateScope();
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.Where<CspDefinition>(x => x.Id == key);
		var definition = await scope.Database.FirstOrDefaultAsync<CspDefinition>(sql, cancellationToken);

		if (definition is not null)
		{
			var sourcesSql = scope.SqlContext.Sql()
				.SelectAll()
				.From<CspDefinitionSource>()
				.Where<CspDefinitionSource>(x => x.DefinitionId == definition.Id);
			definition.Sources = await scope.Database.FetchAsync<CspDefinitionSource>(sourcesSql, cancellationToken);
		}

		scope.Complete();
		return definition;
	}

	public async Task<CspDefinition> GetCspDefinitionAsync(bool isBackOfficeRequest, CancellationToken cancellationToken)
	{
		var context = isBackOfficeRequest ? "BackOffice" : "Frontend";
		Log.LoadingCspDefinitionFromDatabase(_logger, context);

		using var scope = _scopeProvider.CreateScope();

		CspDefinition definition = await GetGlobalDefinitionAsync(scope, isBackOfficeRequest, cancellationToken)
			?? new CspDefinition
			{
				Id = isBackOfficeRequest ? Constants.DefaultBackofficeId : Constants.DefaultFrontEndId,
				Enabled = false,
				IsBackOffice = isBackOfficeRequest
			};

		scope.Complete();
		return definition;
	}

	public async Task<CspDefinition?> GetCspDefinitionForDomainAsync(Guid domainKey, CancellationToken cancellationToken)
	{
		using var scope = _scopeProvider.CreateScope();

		var definitionSql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.Where<CspDefinition>(x => x.DomainKey == domainKey && x.IsBackOffice == false);

		var definition = await scope.Database.FirstOrDefaultAsync<CspDefinition>(definitionSql, cancellationToken);

		if (definition is not null)
		{
			var sourcesSql = scope.SqlContext.Sql()
				.SelectAll()
				.From<CspDefinitionSource>()
				.Where<CspDefinitionSource>(x => x.DefinitionId == definition.Id);
			definition.Sources = await scope.Database.FetchAsync<CspDefinitionSource>(sourcesSql, cancellationToken);
		}

		scope.Complete();
		return definition;
	}

	public async Task<CspDefinition?> GetCachedCspDefinitionForDomainAsync(Guid domainKey, CancellationToken cancellationToken)
	{
		var cacheKey = Constants.DomainCacheKey(domainKey);

		return await _runtimeCache.GetCacheItemAsync(cacheKey, async () =>
			await GetCspDefinitionForDomainAsync(domainKey, cancellationToken),
			timeout: null);
	}

	public async Task<List<CspDefinition>> GetAllDomainPoliciesAsync(CancellationToken cancellationToken)
	{
		using var scope = _scopeProvider.CreateScope();

		var definitionSql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.WhereNotNull<CspDefinition>(x => x.DomainKey);

		var definitions = await scope.Database.FetchAsync<CspDefinition>(definitionSql, cancellationToken);

		foreach (var definition in definitions)
		{
			var sourcesSql = scope.SqlContext.Sql()
				.SelectAll()
				.From<CspDefinitionSource>()
				.Where<CspDefinitionSource>(x => x.DefinitionId == definition.Id);
			definition.Sources = await scope.Database.FetchAsync<CspDefinitionSource>(sourcesSql, cancellationToken);
		}

		scope.Complete();
		return definitions;
	}

	public async Task DeleteCspDefinitionAsync(Guid id, CancellationToken cancellationToken)
	{
		if (id == Constants.DefaultBackofficeId || id == Constants.DefaultFrontEndId)
		{
			throw new InvalidOperationException("Global CSP policies cannot be deleted.");
		}

		using var scope = _scopeProvider.CreateScope();

		var definition = await scope.Database.FirstOrDefaultAsync<CspDefinition>(
			scope.SqlContext.Sql().SelectAll().From<CspDefinition>().Where<CspDefinition>(x => x.Id == id),
			cancellationToken);

		if (definition is null)
		{
			scope.Complete();
			return;
		}

		await scope.Database.DeleteManyAsync<CspDefinitionSource>()
			.Where(s => s.DefinitionId == id)
			.Execute(cancellationToken);

		await scope.Database.DeleteAsync(definition, cancellationToken);

		scope.Complete();

		await _eventAggregator.PublishAsync(new CspSavedNotification(definition), cancellationToken);
	}

	public string GetOrCreateCspNonce(HttpContext context)
	{
		var cspManagerContext = context.GetOrCreateCspManagerContext();

		if (cspManagerContext == null)
		{
			return string.Empty;
		}

		if (!string.IsNullOrEmpty(cspManagerContext.Nonce))
		{
			return cspManagerContext.Nonce;
		}

		var nonce = GenerateCspNonceValue();

		cspManagerContext.Nonce = nonce;

		return nonce;
	}

	public async Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition, CancellationToken cancellationToken)
	{
		var context = definition.IsBackOffice ? "BackOffice" : definition.DomainKey.HasValue ? $"Domain:{definition.DomainKey}" : "Frontend";
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
	// The AND DomainKey IS NULL clause ensures global policies are not confused with domain-specific ones.
	private static async Task<CspDefinition?> GetGlobalDefinitionAsync(IScope scope, bool isBackOffice, CancellationToken cancellationToken)
	{
		var definitionSql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.Where<CspDefinition>(x => x.IsBackOffice == isBackOffice)
			.WhereNull<CspDefinition>(x => x.DomainKey);

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
