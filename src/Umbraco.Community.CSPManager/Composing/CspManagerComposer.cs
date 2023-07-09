namespace Umbraco.Community.CSPManager.Composing;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Community.CSPManager.Backoffice;
using Umbraco.Community.CSPManager.Middleware;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Notifications.Handlers;
using Umbraco.Community.CSPManager.Services;

public sealed class CspManagerComposer : IComposer
{
	public void Compose(IUmbracoBuilder builder)
	{
		builder.ManifestFilters().Append<PackageManifestFilter>();
		builder.AddSection<CspManagementSection>();
		builder.Services.AddTransient<ICspService, CspService>();
		builder.Services.AddTransient<IReportingService, ReportingService>();
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

		builder.Services.AddControllers(options =>
		{
			var jsonInputFormatter = options.InputFormatters
			.OfType<SystemTextJsonInputFormatter>().FirstOrDefault();
			jsonInputFormatter?.SupportedMediaTypes.Add("application/csp-report");
		});

		builder.AddNotificationHandler<ServerVariablesParsingNotification, ServerVariablesHandler>();
		builder.AddNotificationHandler<CspSavedNotification, CspSavedNotificationHandler>();
	}
}
