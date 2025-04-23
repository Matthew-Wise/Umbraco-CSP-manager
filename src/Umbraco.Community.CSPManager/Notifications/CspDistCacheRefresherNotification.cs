using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.Community.CSPManager.Notifications;
public class CspDistCacheRefresherNotification
	: CacheRefresherNotification
{
	public CspDistCacheRefresherNotification(
		object messageObject, 
		MessageType messageType
	) : base(messageObject, messageType) { }
}
