namespace Umbraco.Community.CSPManager.Notifications.Handlers;

using Microsoft.AspNetCore.Routing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.CSPManager.Controllers;
using Umbraco.Extensions;

internal sealed class ServerVariablesHandler : INotificationHandler<ServerVariablesParsingNotification>
{
	private readonly LinkGenerator _linkGenerator;

	public ServerVariablesHandler(LinkGenerator linkGenerator)
	{
		_linkGenerator = linkGenerator;
	}

	public void Handle(ServerVariablesParsingNotification notification)
	{
		var serverVariables = notification.ServerVariables;
		var umbracoUrlsObject = serverVariables["umbracoUrls"] ?? throw new ArgumentException("umbracoUrls is Null");

		if (umbracoUrlsObject is not Dictionary<string, object> umbracoUrls)
		{
			throw new ArgumentException("umbracoUrls is not a dictionary");
		}

		var packageControllerUrl = _linkGenerator.GetUmbracoApiServiceBaseUrl<CSPManagerApiController>(controller => controller.GetDefinition(false));

		umbracoUrls[CspConstants.ServerVariables.BaseUrl] = packageControllerUrl ?? throw new InvalidOperationException("Unable to get base url for Csp Manager API controller");
	}
}
