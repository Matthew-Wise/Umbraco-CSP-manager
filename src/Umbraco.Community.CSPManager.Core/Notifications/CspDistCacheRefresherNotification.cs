namespace Umbraco.Community.CSPManager.Core.Notifications;

using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

public class CspDistCacheRefresherNotification
	: CacheRefresherNotification
{
	public CspDistCacheRefresherNotification(
		object messageObject, 
		MessageType messageType
	) : base(messageObject, messageType) { }
}
