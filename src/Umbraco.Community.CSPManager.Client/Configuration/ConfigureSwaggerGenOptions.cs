namespace Umbraco.Community.CSPManager.Client.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

internal class ConfigureSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
	public void Configure(SwaggerGenOptions options)
	{
		options.SwaggerDoc(
			"csp",
			new OpenApiInfo
			{
				Title = "Contet Security Policy Management API",
				Version = "1.0",
				Description = "CSP Manager API"
			});

		options.OperationFilter<CspApiOperationSecurityFilter>();

	}
}