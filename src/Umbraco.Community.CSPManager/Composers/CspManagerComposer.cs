
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Community.CSPManager.Configuration;
using Umbraco.Community.CSPManager.Middleware;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Notifications.Handlers;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Composers;

public sealed class Composer : IComposer
{
	public void Compose(IUmbracoBuilder builder)
	{
		builder.Services.ConfigureOptions<ConfigCspApiSwaggerGenOptions>();

		builder.Services.AddTransient<ICspService, CspService>();

		builder.Services.Configure<UmbracoPipelineOptions>(options =>
		{
			options.AddFilter(new UmbracoPipelineFilter(
				Constants.PackageAlias,
				postPipeline: applicationBuilder =>
				{
					applicationBuilder.UseMiddleware<CspMiddleware>();
				}));
		});

		builder.AddNotificationHandler<CspSavedNotification, CspSavedNotificationHandler>();
    }
}
