namespace Umbraco.Community.CSPManager.Services;

using Microsoft.AspNetCore.Http;
using Models;

public interface ICspService
{
	Task<CspDefinition?> GetCspDefinitionAsync(bool IsBackOfficeRequest, bool? enabled);

	Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition);
}
