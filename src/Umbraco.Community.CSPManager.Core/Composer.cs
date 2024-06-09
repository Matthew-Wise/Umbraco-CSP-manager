namespace Umbraco.Community.CSPManager.Core;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Community.CSPManager.Core.Middleware;
using Umbraco.Community.CSPManager.Core.Notifications;
using Umbraco.Community.CSPManager.Core.Notifications.Handlers;
using Umbraco.Community.CSPManager.Core.Services;

public sealed class Composer : IComposer
{
	public void Compose(IUmbracoBuilder builder)
	{
		builder.Services.TryAddTransient<ICspService, CspService>();
		builder.Services.Configure<UmbracoPipelineOptions>(options =>
		{
			options.AddFilter(new UmbracoPipelineFilter(
			CspConstants.PackageAlias,
				postPipeline: applicationBuilder =>
				{
					applicationBuilder.UseMiddleware<CspMiddleware>();
				}));
		});

		builder.AddNotificationHandler<CspSavedNotification, CspSavedNotificationHandler>();
	}
}
