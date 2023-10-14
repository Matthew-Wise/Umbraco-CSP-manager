namespace Umbraco.Community.CSPManager.Services;

using Models;

public interface ICspService
{
	public CspDefinition GetCspDefinition(bool isBackOfficeRequest);

	public CspDefinition? GetCachedCspDefinition(bool isBackOfficeRequest);

	Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition);

	Task<string> GenerateCspHeader(CspDefinition definition, HttpContextWrapper httpContext);

	string GetCspScriptNonce(HttpContextWrapper context);

	string GetCspStyleNonce(HttpContextWrapper context);

	Task SetCspHeaders(HttpContextWrapper context);
}
