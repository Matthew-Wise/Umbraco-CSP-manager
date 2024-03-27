namespace Umbraco.Community.CSPManager.Services;

using Microsoft.AspNetCore.Http;
using Umbraco.Community.CSPManager.Models;

public interface ICspService
{
	public CspDefinition GetCspDefinition(bool isBackOfficeRequest);

	public CspDefinition? GetCachedCspDefinition(bool isBackOfficeRequest);

	Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition);

	string GetCspScriptNonce(HttpContext context);

	string GetCspStyleNonce(HttpContext context);
}
