namespace Umbraco.Community.CSPManager.Services;

using Microsoft.AspNetCore.Http;
using Models;

public interface ICspService
{
	Task<CspDefinition?> GetCspDefinitionAsync(bool IsBackOfficeRequest);

	Task<CspDefinition?> GetCachedCspDefinitionAsync(bool IsBackOfficeRequest);

	Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition);
}
