using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.uSync.Handlers;

namespace Umbraco.Community.CSPManager.uSync;

internal class Composer : IComposer
{
	public void Compose(IUmbracoBuilder builder)
	{
		builder.AddNotificationAsyncHandler<CspSavedNotification, CspDefinitionHandler>();

		UdiParser.RegisterUdiType(Constants.CspPolicyEntityType, UdiType.GuidUdi);
	}
}
