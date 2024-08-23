namespace Umbraco.Community.CSPManager.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
