using Umbraco.Cms.Core.Cache;
using Umbraco.Community.CSPManager.Notifications.Handlers;

namespace Umbraco.Community.CSPManager.Tests.Helpers;

internal class StubCacheRefresher : ICacheRefresher
{
	public Guid RefresherUniqueId => CspDistributedCacheRefresher.UniqueId;
	public string Name => "Stub";
	public void RefreshAll() { }
	public void Refresh(int id) { }
	public void Remove(int id) { }
	public void Refresh(Guid id) { }
}
