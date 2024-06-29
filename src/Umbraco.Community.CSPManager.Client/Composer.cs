namespace Umbraco.Community.CSPManager.Client;

using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.CSPManager.Client.Configuration;

internal class Composer : IComposer
{
	public void Compose(IUmbracoBuilder builder) 
		=> builder.Services.ConfigureOptions<ConfigureSwaggerGenOptions>();
}
