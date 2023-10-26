namespace Umbraco.Community.CSPManager.Services;

using Umbraco.Community.CSPManager.Models;

public interface ICspService
{
	public CspDefinition GetCspDefinition(bool isBackOfficeRequest);

	public CspDefinition? GetCachedCspDefinition(bool isBackOfficeRequest);

	Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition);

	string GetCspScriptNonce(HttpContextWrapper context);

	string GetCspStyleNonce(HttpContextWrapper context);

}
