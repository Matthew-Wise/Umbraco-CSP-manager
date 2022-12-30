namespace Umbraco.Community.CSPManager.Composing;

using Backoffice;
using Cms.Core.Composing;
using Cms.Core.DependencyInjection;
using Cms.Web.Common.ApplicationBuilder;
using Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Services;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Notifications.Handlers;

public sealed class CspManagerComposer : IComposer
{
	public void Compose(IUmbracoBuilder builder)
	{
		builder.ManifestFilters().Append<PackageManifestFilter>();
		builder.AddSection<CspManagementSection>();
		builder.Services.AddTransient<ICspService, CspService>();
		builder.Services.Configure<UmbracoPipelineOptions>(options =>
		{
			options.AddFilter(new UmbracoPipelineFilter(
				CspConstants.PackageAlias,
				_ => { },
				applicationBuilder =>
				{
					applicationBuilder.UseMiddleware<CspMiddleware>();
				},
				_ => { }));
		});

		builder.AddNotificationHandler<ServerVariablesParsingNotification, ServerVariablesHandler>();
		builder.AddNotificationHandler<CspSavingNotification, CspSavingNotificationHandler>();
	}
}
