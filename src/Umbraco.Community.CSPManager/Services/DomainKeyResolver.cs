using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Resolves Umbraco domain identifiers using a cached mapping built from <see cref="IDomainService"/>.
/// </summary>
/// <remarks>
/// The domain mapping (int ID → Guid Key) is built lazily on first access and cached with a 30-minute
/// TTL. This avoids per-request database calls: the routing layer only exposes a domain's integer ID,
/// while CSP policies store the stable Guid Key. The cache is also cleared explicitly on demand.
/// </remarks>
internal sealed class DomainKeyResolver : IDomainKeyResolver
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IAppPolicyCache _runtimeCache;

	public DomainKeyResolver(IServiceScopeFactory scopeFactory, AppCaches appCaches)
	{
		_scopeFactory = scopeFactory;
		_runtimeCache = appCaches.RuntimeCache;
	}

	public async Task<Guid?> ResolveKeyAsync(int domainId, CancellationToken cancellationToken = default)
	{
		var mapping = await GetMappingAsync(cancellationToken);
		return mapping.IdToKey.TryGetValue(domainId, out var key) ? key : null;
	}

	public async Task<int?> ResolveIdAsync(Guid domainKey, CancellationToken cancellationToken = default)
	{
		var mapping = await GetMappingAsync(cancellationToken);
		return mapping.KeyToId.TryGetValue(domainKey, out var id) ? id : null;
	}

	public async Task<IReadOnlyDictionary<Guid, string>> GetDomainNamesAsync(CancellationToken cancellationToken = default)
	{
		var mapping = await GetMappingAsync(cancellationToken);
		return mapping.KeyToName;
	}

	public void ClearCache() => _runtimeCache.ClearByKey(Constants.DomainIdMappingCacheKey);

	private async Task<DomainMapping> GetMappingAsync(CancellationToken cancellationToken)
	{
		var result = await _runtimeCache.GetCacheItemAsync(
			Constants.DomainIdMappingCacheKey,
			async () => await BuildMappingAsync(cancellationToken),
			timeout: TimeSpan.FromMinutes(30));

		return result ?? new DomainMapping();
	}

	private async Task<DomainMapping> BuildMappingAsync(CancellationToken cancellationToken)
	{
		using var scope = _scopeFactory.CreateScope();
		var domainService = scope.ServiceProvider.GetRequiredService<IDomainService>();
		var domains = await domainService.GetAllAsync(includeWildcards: false);

		var idToKey = new Dictionary<int, Guid>();
		var keyToId = new Dictionary<Guid, int>();
		var keyToName = new Dictionary<Guid, string>();

		foreach (var domain in domains)
		{
			idToKey[domain.Id] = domain.Key;
			keyToId[domain.Key] = domain.Id;
			if (domain.DomainName is not null)
			{
				keyToName[domain.Key] = domain.DomainName;
			}
		}

		return new DomainMapping(idToKey, keyToId, keyToName);
	}

	private sealed record DomainMapping(
		IReadOnlyDictionary<int, Guid> IdToKey,
		IReadOnlyDictionary<Guid, int> KeyToId,
		IReadOnlyDictionary<Guid, string> KeyToName)
	{
		public DomainMapping() : this(
			new Dictionary<int, Guid>(),
			new Dictionary<Guid, int>(),
			new Dictionary<Guid, string>())
		{
		}
	}
}
