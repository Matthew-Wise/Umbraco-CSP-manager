namespace Umbraco.Community.CSPManager.Services;

using Microsoft.AspNetCore.Http;
using Models;

public interface ICspService
{
	CspDefinition? GetCspDefinition(HttpContext httpContext);
}
