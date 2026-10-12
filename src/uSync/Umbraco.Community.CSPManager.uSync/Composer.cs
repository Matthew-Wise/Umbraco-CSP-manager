using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.uSync.Handlers;

using CspManagerConstants = Umbraco.Community.CSPManager.Constants;

namespace Umbraco.Community.CSPManager.uSync;

internal class Composer : IComposer
{
	public void Compose(IUmbracoBuilder builder)
	{
		builder.AdduSync();

		builder.AddNotificationAsyncHandler<CspSavedNotification, CspDefinitionHandler>();
		builder.AddNotificationAsyncHandler<CspDeletedNotification, CspDefinitionHandler>();
		builder.AddNotificationAsyncHandler<CspSavedNotification, CspNodePolicyHandler>();
		builder.AddNotificationAsyncHandler<CspDeletedNotification, CspNodePolicyHandler>();
		UdiParser.RegisterUdiType(CspManagerConstants.EntityTypes.CspPolicy, UdiType.GuidUdi);
		UdiParser.RegisterUdiType(CspItemNames.NodePolicyEntityType, UdiType.GuidUdi);
	}
}