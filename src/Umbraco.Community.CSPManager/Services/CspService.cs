using System.Data.Common;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPoco.Expressions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.CSPManager.Extensions;
using Umbraco.Community.CSPManager.Logging;
using Umbraco.Community.CSPManager.Migrations;
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
	private readonly CspDomainNodeLookup _domainNodes;

	private const string DomainAlreadyHasPolicyMessage = "A policy already exists for this content node.";

	public CspService(
		IEventAggregator eventAggregator,
		IScopeProvider scopeProvider,
		AppCaches caches,
		ILogger<CspService> logger,
		IDomainService domainService,
		IEntityService entityService)
	{
		_eventAggregator = eventAggregator;
		_scopeProvider = scopeProvider;
		_runtimeCache = caches.RuntimeCache;
		_logger = logger;
		_domainNodes = new CspDomainNodeLookup(domainService, entityService);
	}

	public Task<CspDefinition?> GetCachedCspDefinitionAsync(bool isBackOfficeRequest, CancellationToken cancellationToken)
	{
		var cacheKey = isBackOfficeRequest ? Constants.BackOfficeCacheKey : Constants.FrontEndCacheKey;
		var context = isBackOfficeRequest ? "BackOffice" : "Frontend";

		// Not the caller's token: this load is shared by every request waiting on the same key.
		return GetCachedAsync(
			cacheKey,
			context,
			async () => await GetCspDefinitionAsync(isBackOfficeRequest, CancellationToken.None),
			cancellationToken);
	}

	public Task<CspDefinition?> GetCachedCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken)
		// A domain without a policy caches a completed Task whose result is null. That Task is
		// the negative-cache entry: frontend requests on domains without an override don't query
		// the database every time. Creating a policy for the domain clears it like any save.
		=> GetCachedAsync(
			Constants.NodeCacheKey(contentKey),
			DomainContext(contentKey),
			() => GetCspDefinitionForNodeAsync(contentKey, CancellationToken.None),
			cancellationToken);

	private async Task<CspDefinition?> GetCachedAsync(
		string cacheKey,
		string context,
		Func<Task<CspDefinition?>> loader,
		CancellationToken cancellationToken)
	{
		var factoryCalled = false;

		// IAppPolicyCache.Get holds a lock while it claims the cache slot, so the Task we return
		// here is inserted synchronously - before the DB call it wraps has even started - rather
		// than after it completes like GetCacheItemAsync would. That closes a race where a save's
		// ClearByKey fires while a load is in flight: the entry it clears actually exists, so the
		// load can't resurrect stale data by inserting its result afterwards. It also means
		// concurrent callers for the same key share this one Task instead of each hitting the DB.
		var load = (Task<CspDefinition?>)_runtimeCache.Get(cacheKey, () =>
		{
			factoryCalled = true;
			return loader();
		}, timeout: null)!;

		CspDefinition? definition;

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

		if (definition is null)
		{
			return null;
		}

		if (!factoryCalled)
		{
			Log.CspDefinitionRetrievedFromCache(_logger, definition.Id, context);
		}

		// The cached instance is shared by every caller until the next save invalidates it -
		// CspWritingNotification hands it to consumer code, and the documented pattern for that
		// notification mutates CspDefinition.Sources directly. Handing out the cached reference
		// itself would let one handler's mutation leak into every other request sharing the
		// cache. Returning a defensive copy keeps that mutation scoped to its own request.
		return CloneDefinition(definition);
	}

	private static CspDefinition CloneDefinition(CspDefinition definition) => new()
	{
		Id = definition.Id,
		Enabled = definition.Enabled,
		ReportOnly = definition.ReportOnly,
		IsBackOffice = definition.IsBackOffice,
		ReportingDirective = definition.ReportingDirective,
		ReportUri = definition.ReportUri,
		UpgradeInsecureRequests = definition.UpgradeInsecureRequests,
		ContentKey = definition.ContentKey,
		Sources =
		[
			.. definition.Sources.Select(s => new CspDefinitionSource
			{
				DefinitionId = s.DefinitionId,
				Source = s.Source,
				Directives = [.. s.Directives]
			})
		]
	};

	public async Task<CspDefinition?> GetCspDefinitionAsync(Guid key, CancellationToken cancellationToken)
	{
		using var scope = _scopeProvider.CreateScope();
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.Where<CspDefinition>(x => x.Id == key);
		var definition = await LoadWithSourcesAsync(scope, sql, cancellationToken);

		scope.Complete();
		return definition;
	}

	public async Task<CspDefinition?> GetCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken)
	{
		Log.LoadingCspDefinitionFromDatabase(_logger, DomainContext(contentKey));

		using var scope = _scopeProvider.CreateScope();
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.Where<CspDefinition>(x => x.ContentKey == contentKey);
		var definition = await LoadWithSourcesAsync(scope, sql, cancellationToken);

		scope.Complete();
		return definition;
	}

	public async Task<List<CspDefinition>> GetAllNodePoliciesAsync(CancellationToken cancellationToken)
	{
		using var scope = _scopeProvider.CreateScope();

		var definitionSql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.WhereNotNull<CspDefinition>(x => x.ContentKey);
		var definitions = await scope.Database.FetchAsync<CspDefinition>(definitionSql, cancellationToken);

		if (definitions.Count > 0)
		{
			var ids = definitions.Select(d => d.Id).ToArray();
			var sourcesSql = scope.SqlContext.Sql()
				.SelectAll()
				.From<CspDefinitionSource>()
				.WhereIn<CspDefinitionSource>(x => x.DefinitionId, ids);
			var sources = (await scope.Database.FetchAsync<CspDefinitionSource>(sourcesSql, cancellationToken))
				.ToLookup(x => x.DefinitionId);

			foreach (var definition in definitions)
			{
				definition.Sources = [.. sources[definition.Id]];
			}
		}

		scope.Complete();
		return definitions;
	}

	public async Task<CspDefinition> CreateCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken)
	{
		var frontend = await GetCspDefinitionAsync(isBackOfficeRequest: false, cancellationToken);
		var id = Guid.NewGuid();

		var definition = new CspDefinition
		{
			Id = id,
			ContentKey = contentKey,
			IsBackOffice = false,
			// Always on: creating a node policy means it should apply. Copying the Frontend policy's
			// switch would silently create a policy that does nothing whenever Frontend is off.
			Enabled = true,
			ReportOnly = frontend.ReportOnly,
			ReportingDirective = frontend.ReportingDirective,
			ReportUri = frontend.ReportUri,
			UpgradeInsecureRequests = frontend.UpgradeInsecureRequests,
			Sources =
			[
				.. frontend.Sources.Select(s => new CspDefinitionSource
				{
					DefinitionId = id,
					Source = s.Source,
					Directives = [.. s.Directives]
				})
			]
		};

		// The node-has-a-hostname and one-policy-per-node checks live in the save path.
		return await SaveCspDefinitionAsync(definition, cancellationToken);
	}

	public async Task DeleteCspDefinitionAsync(Guid id, CancellationToken cancellationToken)
	{
		if (IsGlobalId(id))
		{
			throw new CspDefinitionValidationException(nameof(CspDefinition.Id), "The global frontend and backoffice policies cannot be deleted.");
		}

		CspDefinition? definition;

		using (var scope = _scopeProvider.CreateScope())
		{
			// Before any read, like every write: see SaveCspDefinitionAsync.
			scope.EagerWriteLock(Constants.Locks.Definitions);

			var sql = scope.SqlContext.Sql()
				.SelectAll()
				.From<CspDefinition>()
				.Where<CspDefinition>(x => x.Id == id);
			definition = await LoadWithSourcesAsync(scope, sql, cancellationToken);

			if (definition is not null)
			{
				await scope.Database.DeleteManyAsync<CspDefinitionSource>()
					.Where(s => s.DefinitionId == id)
					.Execute(cancellationToken);
				await scope.Database.DeleteManyAsync<CspDefinition>()
					.Where(d => d.Id == id)
					.Execute(cancellationToken);
			}

			scope.Complete();
		}

		if (definition is null)
		{
			return;
		}

		// After the scope disposes (post-commit), for the same reason as saves: invalidating
		// before commit would let a request in that window re-cache the row being deleted.
		await _eventAggregator.PublishAsync(new CspDeletedNotification(definition), cancellationToken);

		Log.CspDefinitionDeleted(_logger, definition.Id, GetContextName(definition));
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
		Log.SavingCspDefinition(_logger, definition.Id, GetContextName(definition));

		try
		{
			// Empty sources have no value and clog up the header, so they are dropped rather than
			// validated or stored.
			definition.Sources = [.. definition.Sources.Where(s => !string.IsNullOrWhiteSpace(s.Source))];

			EnsureValidContent(definition);
			var isDomainPolicy = EnsureValidIdentityShape(definition);

			// Looked up before the write lock: IDomainService opens its own scope (and takes Umbraco's
			// Domains read lock), and nothing should be waited on while holding our lock.
			var nodeHasHostname = isDomainPolicy && await _domainNodes.HasHostnameAsync(definition.ContentKey!.Value);

			try
			{
				using var scope = _scopeProvider.CreateScope();

				// Taken before the first read. Without it, two saves each read (the existing row, the
				// duplicate count) and then both try to write: on SQLite neither can upgrade its read
				// snapshot to a write, so both retry until the request threads are exhausted. The
				// lock makes the saves queue up instead, and makes the identity checks below and the
				// write one atomic step on every database.
				scope.EagerWriteLock(Constants.Locks.Definitions);

				if (isDomainPolicy)
				{
					await EnsureValidDomainPolicyAsync(scope, definition, nodeHasHostname, cancellationToken);
				}

				definition = await SaveDefinitionAsync(scope, definition, cancellationToken);

				scope.Complete();
			}
			catch (Exception ex) when (isDomainPolicy && IsContentKeyUniqueViolation(ex))
			{
				// The filtered unique index is the last line of defence against a second policy for a
				// domain (e.g. a writer that bypasses this service). Report it like the service check.
				throw new CspDefinitionValidationException(nameof(CspDefinition.ContentKey), DomainAlreadyHasPolicyMessage);
			}

			// Publish after the scope disposes, i.e. after commit - otherwise a request in that
			// window could reload the pre-save rows and cache them.
			await _eventAggregator.PublishAsync(new CspSavedNotification(definition), cancellationToken);

			Log.CspDefinitionSaved(_logger, definition.Id, definition.Sources.Count);

			return definition;
		}
		catch (CspDefinitionValidationException ex)
		{
			Log.CspDefinitionSaveRejected(_logger, definition.Id, ex.Message);
			throw;
		}
		catch (Exception ex)
		{
			Log.CspDefinitionSaveFailed(_logger, definition.Id, ex);
			throw;
		}
	}

	// The same header rules as the management API, for every caller: a value that splices in a
	// directive or carries CR/LF would otherwise reach the header (or make Kestrel drop it).
	private static void EnsureValidContent(CspDefinition definition)
	{
		var errors = CspDefinitionValidator.Validate(definition);
		if (errors.Count == 0)
		{
			return;
		}

		throw new CspDefinitionValidationException(
			errors[0].MemberNames.FirstOrDefault() ?? nameof(CspDefinition.Sources),
			string.Join(" ", errors.Select(e => e.ErrorMessage)));
	}

	// The service, not the caller, decides what a definition is: anything that can reach the save
	// (the management API, uSync, custom code) must not be able to turn a global policy into a
	// node policy, change the content node of a node policy, or create a second global policy.
	// These rules need no database; returns whether the definition is a node policy.
	private static bool EnsureValidIdentityShape(CspDefinition definition)
	{
		if (IsGlobalId(definition.Id))
		{
			if (definition.ContentKey is not null)
			{
				throw new CspDefinitionValidationException(nameof(CspDefinition.ContentKey), "The global frontend and backoffice policies cannot be assigned to a content node.");
			}

			// The Id decides which global policy this is, not the posted flag - otherwise the
			// frontend row could be flagged as a second backoffice policy.
			definition.IsBackOffice = definition.Id == Constants.DefaultBackofficeId;
			return false;
		}

		if (definition.ContentKey is not { } contentKey || contentKey == Guid.Empty)
		{
			throw new CspDefinitionValidationException(nameof(CspDefinition.Id), "Only the global frontend and backoffice policies can be saved without a content node.");
		}

		// Node policies replace the frontend policy; the backoffice never uses them.
		definition.IsBackOffice = false;

		// A new node policy is posted without an id: the server assigns it, so a caller can't
		// pick an id that collides with (and so overwrites) another definition.
		if (definition.Id == Guid.Empty)
		{
			definition.Id = Guid.NewGuid();
		}

		return true;
	}

	public Task<bool> ContentNodeHasHostnameAsync(Guid contentKey, CancellationToken cancellationToken)
		=> _domainNodes.HasHostnameAsync(contentKey);

	// Inside the locked save scope, so the checks and the write see the same data.
	private static async Task EnsureValidDomainPolicyAsync(IScope scope, CspDefinition definition, bool nodeHasHostname, CancellationToken cancellationToken)
	{
		var contentKey = definition.ContentKey!.Value;

		var existingSql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.Where<CspDefinition>(x => x.Id == definition.Id);
		var existing = await scope.Database.FirstOrDefaultAsync<CspDefinition>(existingSql, cancellationToken);

		if (existing is not null)
		{
			if (existing.ContentKey != contentKey)
			{
				throw new CspDefinitionValidationException(nameof(CspDefinition.ContentKey), "The content node of an existing node policy cannot be changed.");
			}

			return;
		}

		// Creating: one policy per node (also backed by a unique index), for a node with a hostname.
		var duplicateSql = scope.SqlContext.Sql()
			.SelectCount()
			.From<CspDefinition>()
			.Where<CspDefinition>(x => x.ContentKey == contentKey);
		if (await scope.Database.ExecuteScalarAsync<int>(duplicateSql, cancellationToken) > 0)
		{
			throw new CspDefinitionValidationException(nameof(CspDefinition.ContentKey), DomainAlreadyHasPolicyMessage);
		}

		if (!nodeHasHostname)
		{
			throw new CspDefinitionValidationException(nameof(CspDefinition.ContentKey), "The content node does not exist, is in the recycle bin, or has no hostname assigned in Culture and Hostnames (culture-only domains don't count).");
		}
	}

	// A violation of DomainPolicyMigration's filtered unique index. Matched on the message because
	// the provider exception types (SqliteException, SqlException) aren't referenced here: SQL Server
	// names the index, SQLite names the column.
	internal static bool IsContentKeyUniqueViolation(Exception exception)
	{
		for (var current = exception; current is not null; current = current.InnerException)
		{
			if (current is DbException
				&& (current.Message.Contains(DomainPolicyMigration.ContentKeyIndexName, StringComparison.OrdinalIgnoreCase)
					|| current.Message.Contains($"UNIQUE constraint failed: {nameof(CspDefinition)}.{nameof(CspDefinition.ContentKey)}", StringComparison.OrdinalIgnoreCase)))
			{
				return true;
			}
		}

		return false;
	}

	private static async Task<CspDefinition> SaveDefinitionAsync(IScope scope, CspDefinition definition, CancellationToken cancellationToken)
	{
		await scope.Database.SaveAsync(definition, cancellationToken);

		// Sources always belong to the definition being saved. Trusting the posted DefinitionId
		// would let a save of one policy write sources into another.
		foreach (var source in definition.Sources)
		{
			source.DefinitionId = definition.Id;
		}

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
	// ContentKey IS NULL keeps node policies (IsBackOffice = false too) out of the global lookup.
	private static Task<CspDefinition?> GetDefinitionAsync(IScope scope, bool isBackOffice, CancellationToken cancellationToken)
	{
		var definitionSql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.Where<CspDefinition>(x => x.IsBackOffice == isBackOffice)
			.WhereNull<CspDefinition>(x => x.ContentKey);

		return LoadWithSourcesAsync(scope, definitionSql, cancellationToken);
	}

	private static async Task<CspDefinition?> LoadWithSourcesAsync(IScope scope, NPoco.Sql<Umbraco.Cms.Infrastructure.Persistence.ISqlContext> definitionSql, CancellationToken cancellationToken)
	{
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

	private static bool IsGlobalId(Guid id) => id == Constants.DefaultFrontEndId || id == Constants.DefaultBackofficeId;

	private static string DomainContext(Guid contentKey) => $"Node policy of node {contentKey:D}";

	private static string GetContextName(CspDefinition definition)
		=> definition.ContentKey is { } contentKey
			? DomainContext(contentKey)
			: definition.IsBackOffice ? "BackOffice" : "Frontend";

	private static string GenerateCspNonceValue()
	{
		Span<byte> nonceBytes = stackalloc byte[16]; // 16 bytes = 128 bits
		RandomNumberGenerator.Fill(nonceBytes);
		return Convert.ToBase64String(nonceBytes);
	}
}