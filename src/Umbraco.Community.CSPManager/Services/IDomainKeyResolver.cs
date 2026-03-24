namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Resolves Umbraco domain identifiers without per-request database hits.
/// Maintains a cached mapping between domain integer IDs (available from routing) and Guid Keys (stored in CSP policies).
/// </summary>
public interface IDomainKeyResolver
{
	/// <summary>
	/// Resolves the Guid Key for a domain given its integer ID from the routing layer.
	/// </summary>
	Task<Guid?> ResolveKeyAsync(int domainId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Resolves the integer ID for a domain given its Guid Key.
	/// </summary>
	Task<int?> ResolveIdAsync(Guid domainKey, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns a dictionary mapping Guid Key to domain name, for display purposes.
	/// </summary>
	Task<IReadOnlyDictionary<Guid, string>> GetDomainNamesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Clears the cached domain mapping, forcing a reload on next access.
	/// </summary>
	void ClearCache();
}
