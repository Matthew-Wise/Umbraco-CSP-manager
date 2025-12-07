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

	public async Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition, CancellationToken cancellationToken = default)
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
		Span<byte> nonceBytes = stackalloc byte[16]; // 16 bytes = 128 bits
		RandomNumberGenerator.Fill(nonceBytes);
		return Convert.ToBase64String(nonceBytes);
	}
}