using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Controllers;

[ApiVersion("1.0")]
[MapToApi(Constants.ApiName)]
[ApiExplorerSettings(GroupName = "Definitions")]
public class DefinitionsController(ICspService cspService) : ManagementApiControllerBase
{
	[HttpGet("Definitions")]
	public CspDefinition GetDefinition(bool isBackOffice = false) => cspService.GetCspDefinition(isBackOffice);

	[HttpPost("Definitions/save")]
	public async Task<CspDefinition> SaveDefinition(CspDefinition definition)
	{
		if (definition.Id == Guid.Empty)
		{
			throw new ArgumentOutOfRangeException(nameof(definition), "Definition Id is blank");
		}

		return await cspService.SaveCspDefinitionAsync(definition);
	}
}