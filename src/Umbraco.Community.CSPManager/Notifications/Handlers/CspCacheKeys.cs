using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Notifications.Handlers;

internal static class CspCacheKeys
{
	/// <summary>
	/// Gets the runtime cache key a definition is cached under.
	/// </summary>
	public static string For(CspDefinition definition)
		=> definition.ContentKey is { } contentKey
			? Constants.NodeCacheKey(contentKey)
			: definition.IsBackOffice ? Constants.BackOfficeCacheKey : Constants.FrontEndCacheKey;
}
