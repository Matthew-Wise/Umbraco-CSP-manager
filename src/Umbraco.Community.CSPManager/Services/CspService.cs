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

		// IAppPolicyCache.Get holds a lock while it claims the cache slot, so the Task we return
		// here is inserted synchronously - before the DB call it wraps has even started - rather
		// than after it completes like GetCacheItemAsync would. That closes a race where a save's
		// ClearByKey fires while a load is in flight: the entry it clears actually exists, so the
		// load can't resurrect stale data by inserting its result afterwards. It also means
		// concurrent callers for the same key share this one Task instead of each hitting the DB.
		var load = (Task<CspDefinition>)_runtimeCache.Get(cacheKey, () =>
		{
			factoryCalled = true;

			// Not the caller's token: this load is shared by every request waiting on the same key.
			return GetCspDefinitionAsync(isBackOfficeRequest, CancellationToken.None);
		}, timeout: null)!;

		CspDefinition definition;

		try
		{
			definition = await load.WaitAsync(cancellationToken);
		}
		catch (Exception) when (load.IsFaulted)
		{
			// Don't leave a failed load cached, or every later request replays the same failure.
			_runtimeCache.Clear(cacheKey);
			throw;
		}

		if (!factoryCalled)
		{
			Log.CspDefinitionRetrievedFromCache(_logger, definition.Id, context);
		}

		return definition;
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
		var context = definition.IsBackOffice ? "BackOffice" : "Frontend";
		Log.SavingCspDefinition(_logger, definition.Id, context);

		try
		{
			using (var scope = _scopeProvider.CreateScope())
			{
				definition = await SaveDefinitionAsync(scope, definition, cancellationToken);

				scope.Complete();
			}

			// Publish after the scope disposes, i.e. after commit - otherwise a request in that
			// window could reload the pre-save rows and cache them.
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