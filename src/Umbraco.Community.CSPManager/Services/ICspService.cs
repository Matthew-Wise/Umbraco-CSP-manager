using Microsoft.AspNetCore.Http;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Services;
public interface ICspService
{
	public CspDefinition GetCspDefinition(bool isBackOfficeRequest);

	public CspDefinition? GetCachedCspDefinition(bool isBackOfficeRequest);

	Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition);

	string GetOrCreateCspScriptNonce(HttpContext context);

	string GetOrCreateCspStyleNonce(HttpContext context);
}