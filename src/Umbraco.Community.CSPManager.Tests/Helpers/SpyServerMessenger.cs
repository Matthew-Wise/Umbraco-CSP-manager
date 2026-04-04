using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.Community.CSPManager.Tests.Helpers;

internal class SpyServerMessenger : IServerMessenger
{
	public int PayloadRefreshCount { get; private set; }

	public void QueueRefresh<TPayload>(ICacheRefresher refresher, TPayload[] payload)
		=> PayloadRefreshCount++;

	public void QueueRefresh<T>(ICacheRefresher refresher, Func<T, int> getNumericId, params T[] instances) { }
	public void QueueRefresh<T>(ICacheRefresher refresher, Func<T, Guid> getGuidId, params T[] instances) { }
	public void QueueRemove<T>(ICacheRefresher refresher, Func<T, int> getNumericId, params T[] instances) { }
	public void QueueRemove(ICacheRefresher refresher, params int[] numericIds) { }
	public void QueueRefresh(ICacheRefresher refresher, params int[] numericIds) { }
	public void QueueRefresh(ICacheRefresher refresher, params Guid[] guidIds) { }
	public void QueueRefreshAll(ICacheRefresher refresher) { }
	public void Sync() { }
	public void SendMessages() { }
	public void PerformRefresh(ICacheRefresher refresher, string jsonPayload) { }
	public void PerformRemove(ICacheRefresher refresher, string jsonPayload) { }
}
