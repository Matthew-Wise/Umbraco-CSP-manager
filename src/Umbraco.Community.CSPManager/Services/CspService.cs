using System.Data.Common;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPoco.Expressions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
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
	private readonly ILanguageService _languageService;
	private readonly IDocumentNavigationQueryService _navigation;
	private readonly IOptionsMonitor<CspManagerOptions> _options;

	private const string NodeAlreadyHasPolicyMessage = "A policy already exists for this content node and culture.";

	public CspService(
		IEventAggregator eventAggregator,
		IScopeProvider scopeProvider,
		AppCaches caches,
		ILogger<CspService> logger,
		IDomainService domainService,
		IEntityService entityService,
		ILanguageService languageService,
		IDocumentNavigationQueryService navigation,
		IOptionsMonitor<CspManagerOptions> options)
	{
		_eventAggregator = eventAggregator;
		_scopeProvider = scopeProvider;
		_runtimeCache = caches.RuntimeCache;
		_logger = logger;
		_domainNodes = new CspDomainNodeLookup(domainService, entityService);
		_languageService = languageService;
		_navigation = navigation;
		_options = options;
	}

	public async Task<CspDefinition?> GetCachedCspDefinitionAsync(bool isBackOfficeRequest, CancellationToken cancellationToken)
	{
		// The cached instance is shared by every caller until the next save invalidates it -
		// CspWritingNotification hands it to consumer code, and the documented pattern for that
		// notification mutates CspDefinition.Sources directly. Handing out the cached reference
		// itself would let one handler's mutation leak into every other request sharing the
		// cache. Returning a defensive copy keeps that mutation scoped to its own request.
		return await GetSharedCspDefinitionAsync(isBackOfficeRequest, cancellationToken) is { } shared
			? CloneDefinition(shared)
			: null;
	}

	// The cached instance itself: only for callers that read it and never hand it out.
	private async Task<CspDefinition?> GetSharedCspDefinitionAsync(bool isBackOfficeRequest, CancellationToken cancellationToken)
	{
		var cacheKey = isBackOfficeRequest ? Constants.BackOfficeCacheKey : Constants.FrontEndCacheKey;
		var context = isBackOfficeRequest ? "BackOffice" : "Frontend";

		// Not the caller's token: this load is shared by every request waiting on the same key.
		var (definition, fromCache) = await GetCachedAsync<CspDefinition?>(
			cacheKey,
			async () => await GetCspDefinitionAsync(isBackOfficeRequest, CancellationToken.None),
			cancellationToken);

		if (definition is not null && fromCache)
		{
			Log.CspDefinitionRetrievedFromCache(_logger, definition.Id, context);
		}

		return definition;
	}

	public async Task<CspDefinition?> GetCachedCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken)
	{
		var snapshot = await GetCachedNodePoliciesAsync(cancellationToken);

		foreach (var policy in snapshot.ForNode(contentKey))
		{
			if (policy.Culture is null)
			{
				return CloneDefinition(policy);
			}
		}

		return null;
	}

	public async Task<CspDefinition?> ResolveCspDefinitionAsync(Guid? contentKey, Guid? domainContentKey, string? culture, CancellationToken cancellationToken)
	{
		if (contentKey is null && domainContentKey is null)
		{
			return null;
		}

		// The common case: a site without node policies pays one cache lookup per request.
		var snapshot = await GetCachedNodePoliciesAsync(cancellationToken);
		if (snapshot.IsEmpty)
		{
			return null;
		}

		var resolution = CspNodePolicyResolver.Resolve(
			snapshot,
			GetPath(contentKey, domainContentKey),
			contentKey,
			snapshot.HasCulturePolicies ? await GetCultureChainAsync(culture, cancellationToken) : [],
			_options.CurrentValue.DisabledNodePolicyBehavior);

		if (resolution.IsEmpty)
		{
			return null;
		}

		if (resolution.Additions.Count == 0)
		{
			// Copies, for the same reason as the global lookup: the snapshot is shared.
			return CloneDefinition(resolution.Base!);
		}

		// Extending policies with no complete policy above them extend the global frontend policy.
		// Merge only reads its inputs and builds a new definition, so the shared instance will do.
		var baseDefinition = resolution.Base ?? await GetSharedCspDefinitionAsync(isBackOfficeRequest: false, cancellationToken);

		return baseDefinition is null ? null : CspNodePolicyResolver.Merge(baseDefinition, resolution.Additions);
	}

	// The page and its ancestors, nearest first, from Umbraco's in-memory navigation structure (no
	// database call). The node of the matched hostname goes last when it isn't among them, e.g. for
	// a "not found" page stored outside the site, so the site's policy still applies to it.
	private List<Guid> GetPath(Guid? contentKey, Guid? domainContentKey)
	{
		var path = new List<Guid>();

		if (contentKey is { } pageKey)
		{
			// The page's key followed by its ancestors' keys. A page the navigation structure
			// doesn't know (yet) is still its own path.
			if (_navigation.TryGetAncestorsOrSelfKeys(pageKey, out var keys))
			{
				path.AddRange(keys);
			}
			else
			{
				path.Add(pageKey);
			}
		}

		if (domainContentKey is { } domainKey && !path.Contains(domainKey))
		{
			path.Add(domainKey);
		}

		return path;
	}

	private Task<CspNodePolicySnapshot> GetCachedNodePoliciesAsync(CancellationToken cancellationToken)
		=> GetCachedValueAsync(
			Constants.NodePoliciesCacheKey,
			async () =>
			{
				Log.LoadingCspDefinitionFromDatabase(_logger, "Node policies");
				return new CspNodePolicySnapshot(await GetAllNodePoliciesAsync(CancellationToken.None));
			},
			cancellationToken);

	// The request's culture followed by its fallback languages, e.g. fr-CA, fr-FR. Umbraco stores
	// one fallback per language, so each language has one chain; all of them are built once.
	private async Task<IReadOnlyList<string>> GetCultureChainAsync(string? culture, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(culture))
		{
			return [];
		}

		var chains = await GetCachedValueAsync(Constants.LanguageFallbackCacheKey, LoadCultureChainsAsync, cancellationToken);

		// A culture that isn't a language (any more) has no fallback, but can still have a policy.
		return chains.TryGetValue(culture, out var chain) ? chain : [culture];
	}

	private async Task<Dictionary<string, string[]>> LoadCultureChainsAsync()
	{
		var fallbacks = (await _languageService.GetAllAsync())
			.ToDictionary(l => l.IsoCode, l => l.FallbackIsoCode, StringComparer.OrdinalIgnoreCase);

		var chains = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
		foreach (var isoCode in fallbacks.Keys)
		{
			var chain = new List<string> { isoCode };
			var current = isoCode;

			// Umbraco refuses a circular fallback, but a loop here would hang every request.
			while (fallbacks.TryGetValue(current, out var next)
				&& !string.IsNullOrWhiteSpace(next)
				&& !chain.Contains(next, StringComparer.OrdinalIgnoreCase))
			{
				chain.Add(next);
				current = next;
			}

			chains[isoCode] = [.. chain];
		}

		return chains;
	}

	private async Task<T> GetCachedValueAsync<T>(string cacheKey, Func<Task<T>> loader, CancellationToken cancellationToken)
		=> (await GetCachedAsync(cacheKey, loader, cancellationToken)).Value;

	private async Task<(T Value, bool FromCache)> GetCachedAsync<T>(
		string cacheKey,
		Func<Task<T>> loader,
		CancellationToken cancellationToken)
	{
		// The steady state: a finished load is cached. A plain read takes the cache's shared lock;
		// the factory overload below takes its upgradeable lock, which one request holds at a time.
		if (_runtimeCache.Get(cacheKey) is Task<T> { IsCompletedSuccessfully: true } cached)
		{
			return (cached.Result, true);
		}

		var factoryCalled = false;

		// IAppPolicyCache.Get holds a lock while it claims the cache slot, so the Task we return
		// here is inserted synchronously - before the DB call it wraps has even started - rather
		// than after it completes like GetCacheItemAsync would. That closes a race where a save's
		// ClearByKey fires while a load is in flight: the entry it clears actually exists, so the
		// load can't resurrect stale data by inserting its result afterwards. It also means
		// concurrent callers for the same key share this one Task instead of each hitting the DB.
		var load = (Task<T>)_runtimeCache.Get(cacheKey, () =>
		{
			factoryCalled = true;
			return loader();
		}, timeout: null)!;

		try
		{
			return (await load.WaitAsync(cancellationToken), !factoryCalled);
		}
		catch (Exception) when (load.IsFaulted)
		{
			// Don't leave a failed load cached, or every later request replays the same failure.
			_runtimeCache.Clear(cacheKey);
			throw;
		}
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
		Culture = definition.Culture,
		Inheritance = definition.Inheritance,
		AppliesTo = definition.AppliesTo,
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

	public Task<CspDefinition?> GetCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken)
		=> GetCspDefinitionForNodeAsync(contentKey, culture: null, cancellationToken);

	public async Task<CspDefinition?> GetCspDefinitionForNodeAsync(Guid contentKey, string? culture, CancellationToken cancellationToken)
	{
		culture = NormalizeCulture(culture);
		Log.LoadingCspDefinitionFromDatabase(_logger, NodeContext(contentKey, culture));

		using var scope = _scopeProvider.CreateScope();
		var definition = await FindNodePolicyAsync(scope, contentKey, culture, cancellationToken);
		if (definition is not null)
		{
			definition.Sources = await LoadSourcesAsync(scope, definition.Id, cancellationToken);
		}

		scope.Complete();
		return definition;
	}

	// The policy of a node for one culture, or (culture null) its policy for all cultures, without
	// its sources. A node has a handful of policies at most, so they are all read and the culture
	// is compared here: the database would compare it case-insensitively on SQL Server but exactly
	// on SQLite.
	private static async Task<CspDefinition?> FindNodePolicyAsync(IScope scope, Guid contentKey, string? culture, CancellationToken cancellationToken)
	{
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.Where<CspDefinition>(x => x.ContentKey == contentKey);

		return (await scope.Database.FetchAsync<CspDefinition>(sql, cancellationToken))
			.Find(p => CspNodePolicyResolver.CultureEquals(p.Culture, culture));
	}

	private static string? NormalizeCulture(string? culture)
		=> string.IsNullOrWhiteSpace(culture) ? null : culture.Trim();

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

	public Task<CspDefinition> CreateCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken)
		=> CreateCspDefinitionForNodeAsync(contentKey, culture: null, CspPolicyInheritance.Replace, CspPolicyScope.Descendants, cancellationToken);

	public async Task<CspDefinition> CreateCspDefinitionForNodeAsync(
		Guid contentKey,
		string? culture,
		CspPolicyInheritance inheritance,
		CspPolicyScope appliesTo,
		CancellationToken cancellationToken)
	{
		var id = Guid.NewGuid();

		var definition = new CspDefinition
		{
			Id = id,
			ContentKey = contentKey,
			Culture = NormalizeCulture(culture),
			Inheritance = inheritance,
			AppliesTo = appliesTo,
			IsBackOffice = false,
			// Always on: creating a node policy means it should apply. Copying the switch of the
			// policy it starts from would silently create a policy that does nothing whenever that
			// policy is off.
			Enabled = true,
		};

		// An extending policy starts empty: it inherits everything. A complete policy starts as a
		// copy of what the node gets today (a policy further up the tree, or the Frontend policy),
		// so creating it changes no header until it is edited.
		if (inheritance == CspPolicyInheritance.Replace)
		{
			var current = await ResolveCspDefinitionAsync(contentKey, domainContentKey: null, definition.Culture, cancellationToken)
				?? await GetCspDefinitionAsync(isBackOfficeRequest: false, cancellationToken);

			definition.ReportOnly = current.ReportOnly;
			definition.ReportingDirective = current.ReportingDirective;
			definition.ReportUri = current.ReportUri;
			definition.UpgradeInsecureRequests = current.UpgradeInsecureRequests;
			definition.Sources =
			[
				.. current.Sources.Select(s => new CspDefinitionSource
				{
					DefinitionId = id,
					Source = s.Source,
					Directives = [.. s.Directives]
				})
			];
		}

		// The node-exists, culture-exists and one-policy-per-node-and-culture checks live in the save path.
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

			// Looked up before the write lock: these services open their own scopes (and take
			// Umbraco's read locks), and nothing should be waited on while holding our lock.
			var nodeExists = isDomainPolicy && _domainNodes.Exists(definition.ContentKey!.Value);
			var language = isDomainPolicy && definition.Culture is not null
				? await FindLanguageIsoCodeAsync(definition.Culture)
				: null;

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
					await EnsureValidNodePolicyAsync(scope, definition, nodeExists, language, cancellationToken);
				}

				definition = await SaveDefinitionAsync(scope, definition, cancellationToken);

				scope.Complete();
			}
			catch (Exception ex) when (isDomainPolicy && IsContentKeyUniqueViolation(ex))
			{
				// The filtered unique indexes are the last line of defence against a second policy for a
				// node and culture (e.g. a writer that bypasses this service). Report it like the service check.
				throw new CspDefinitionValidationException(nameof(CspDefinition.ContentKey), NodeAlreadyHasPolicyMessage);
			}

			// Publish after the scope disposes, i.e. after commit - otherwise a request in that
			// window could reload the pre-save rows and cache them.
			await _eventAggregator.PublishAsync(new CspSavedNotification(definition), cancellationToken);

			Log.CspDefinitionSaved(_logger, definition.Id, definition.Sources.Count);

			return definition;
		}
		catch (CspDefinitionValidationException ex)
		{
			// The message can quote posted values. The validator masks control characters, but
			// strip line breaks here too so the log can never be forged from a request body.
			Log.CspDefinitionSaveRejected(_logger, definition.Id, ex.Message.Replace("\r", string.Empty).Replace("\n", string.Empty));
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

			if (NormalizeCulture(definition.Culture) is not null)
			{
				throw new CspDefinitionValidationException(nameof(CspDefinition.Culture), "The global frontend and backoffice policies cannot be assigned to a culture.");
			}

			if (definition.Inheritance != CspPolicyInheritance.Replace)
			{
				throw new CspDefinitionValidationException(nameof(CspDefinition.Inheritance), "The global frontend and backoffice policies have nothing to inherit from.");
			}

			// The Id decides which global policy this is, not the posted flag - otherwise the
			// frontend row could be flagged as a second backoffice policy.
			definition.IsBackOffice = definition.Id == Constants.DefaultBackofficeId;
			definition.Culture = null;
			definition.AppliesTo = CspPolicyScope.Descendants;
			return false;
		}

		if (definition.ContentKey is not { } contentKey || contentKey == Guid.Empty)
		{
			throw new CspDefinitionValidationException(nameof(CspDefinition.Id), "Only the global frontend and backoffice policies can be saved without a content node.");
		}

		if (!Enum.IsDefined(definition.Inheritance))
		{
			throw new CspDefinitionValidationException(nameof(CspDefinition.Inheritance), "Unknown inheritance option.");
		}

		if (!Enum.IsDefined(definition.AppliesTo))
		{
			throw new CspDefinitionValidationException(nameof(CspDefinition.AppliesTo), "Unknown applies-to option.");
		}

		// Node policies replace or extend the frontend policy; the backoffice never uses them.
		definition.IsBackOffice = false;
		definition.Culture = NormalizeCulture(definition.Culture);

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

	public Task<bool> ContentNodeExistsAsync(Guid contentKey, CancellationToken cancellationToken)
		=> Task.FromResult(_domainNodes.Exists(contentKey));

	// The language's own spelling of an ISO code, or null when there is no such language.
	private async Task<string?> FindLanguageIsoCodeAsync(string culture)
		=> (await _languageService.GetAsync(culture))?.IsoCode;

	// Inside the locked save scope, so the checks and the write see the same data.
	private static async Task EnsureValidNodePolicyAsync(IScope scope, CspDefinition definition, bool nodeExists, string? language, CancellationToken cancellationToken)
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

			if (!CspNodePolicyResolver.CultureEquals(existing.Culture, definition.Culture))
			{
				throw new CspDefinitionValidationException(nameof(CspDefinition.Culture), "The culture of an existing node policy cannot be changed.");
			}

			// Keep the stored spelling. The language may have been deleted since, which leaves the
			// policy unused but still editable and deletable.
			definition.Culture = existing.Culture;
			return;
		}

		// Creating: for a language that exists, stored in the language's own spelling so the
		// unique index sees one culture however the caller wrote it.
		if (definition.Culture is not null)
		{
			definition.Culture = language
				?? throw new CspDefinitionValidationException(nameof(CspDefinition.Culture), "The culture is not one of the site's languages.");
		}

		// One policy per node and culture (also backed by unique indexes), for a node that exists.
		if (await FindNodePolicyAsync(scope, contentKey, definition.Culture, cancellationToken) is not null)
		{
			throw new CspDefinitionValidationException(nameof(CspDefinition.ContentKey), NodeAlreadyHasPolicyMessage);
		}

		if (!nodeExists)
		{
			throw new CspDefinitionValidationException(nameof(CspDefinition.ContentKey), "The content node does not exist or is in the recycle bin.");
		}
	}

	// A violation of one of NodePolicyScopeMigration's filtered unique indexes. Matched on the message
	// because the provider exception types (SqliteException, SqlException) aren't referenced here:
	// SQL Server names the index, SQLite names the columns (ContentKey first in both indexes).
	internal static bool IsContentKeyUniqueViolation(Exception exception)
	{
		for (var current = exception; current is not null; current = current.InnerException)
		{
			if (current is DbException
				&& (current.Message.Contains(NodePolicyScopeMigration.ContentKeyIndexName, StringComparison.OrdinalIgnoreCase)
					|| current.Message.Contains(NodePolicyScopeMigration.ContentKeyCultureIndexName, StringComparison.OrdinalIgnoreCase)
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

		definition.Sources = await LoadSourcesAsync(scope, definition.Id, cancellationToken);

		return definition;
	}

	private static Task<List<CspDefinitionSource>> LoadSourcesAsync(IScope scope, Guid definitionId, CancellationToken cancellationToken)
	{
		var sourcesSql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinitionSource>()
			.Where<CspDefinitionSource>(x => x.DefinitionId == definitionId);

		return scope.Database.FetchAsync<CspDefinitionSource>(sourcesSql, cancellationToken);
	}

	private static bool IsGlobalId(Guid id) => id == Constants.DefaultFrontEndId || id == Constants.DefaultBackofficeId;

	private static string NodeContext(Guid contentKey, string? culture)
		=> culture is null ? $"Node policy of node {contentKey:D}" : $"Node policy of node {contentKey:D} for {culture}";

	private static string GetContextName(CspDefinition definition)
		=> definition.ContentKey is { } contentKey
			? NodeContext(contentKey, definition.Culture)
			: definition.IsBackOffice ? "BackOffice" : "Frontend";

	private static string GenerateCspNonceValue()
	{
		Span<byte> nonceBytes = stackalloc byte[16]; // 16 bytes = 128 bits
		RandomNumberGenerator.Fill(nonceBytes);
		return Convert.ToBase64String(nonceBytes);
	}
}