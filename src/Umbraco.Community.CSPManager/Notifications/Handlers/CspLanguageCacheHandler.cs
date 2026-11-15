using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Community.CSPManager.Notifications.Handlers;

/// <summary>
/// Drops the cached language fallback chains when a language is added, changed or deleted, so a
/// node policy for a culture follows the new fallback on the next request.
/// </summary>
/// <remarks>
/// Umbraco raises <see cref="LanguageCacheRefresherNotification"/> on every server when its own
/// language cache is refreshed, so this needs no distributed message of its own.
/// </remarks>
internal sealed class CspLanguageCacheHandler : INotificationHandler<LanguageCacheRefresherNotification>
{
	private readonly IAppPolicyCache _runtimeCache;

	public CspLanguageCacheHandler(AppCaches appCaches) => _runtimeCache = appCaches.RuntimeCache;

	public void Handle(LanguageCacheRefresherNotification notification)
		=> _runtimeCache.ClearByKey(Constants.LanguageFallbackCacheKey);
}
