using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Umbraco.Community.CSPManager.Configuration;

public class ConfigCspApiSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
	public void Configure(SwaggerGenOptions options)
	{
		options.SwaggerDoc(
		  Constants.ApiName,
		  new OpenApiInfo
		  {
			  Title = "CSP Management Api",
			  Version = "Latest",
			  Description = "Api access CSP Management operations"
		  });

		options.OperationFilter<CspApiOperationSecurityFilter>();
	}
}