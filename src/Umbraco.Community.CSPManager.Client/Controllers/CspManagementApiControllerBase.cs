namespace Umbraco.Community.CSPManager.Client.Controllers;

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Community.CSPManager.Core.Models;
using Umbraco.Community.CSPManager.Core.Services;

[VersionedApiBackOfficeRoute("csp/manage")]
[ApiExplorerSettings(GroupName = "Content Security Policy")]
[MapToApi("csp")]
public class CspManagementApiControllerBase : ManagementApiControllerBase
{
}

[SuppressMessage("Sonar", "S6934", Justification = "Base class uses VersionedApiBackOfficeRoute")]
public class CspManagementApiController : CspManagementApiControllerBase
{
	private readonly ICspService _cspService;

	public CspManagementApiController(ICspService cspService)
	{
		_cspService = cspService;
	}

	[HttpGet("get/{isBackOffice:bool}")]
	[ProducesResponseType(typeof(CspDefinition), 200)]
	public CspDefinition Get(bool isBackOffice) => _cspService.GetCspDefinition(isBackOffice);

	[HttpGet("directives")]

	[ProducesResponseType(typeof(string[]), 200)]
	public string[] GetCspDirectiveOptions() => CspConstants.AllDirectives.ToArray();

	[HttpPost("save")]
	[ProducesResponseType(typeof(CspDefinition), 200)]
	public async Task<CspDefinition> Save(CspDefinition definition)
	{
		if (definition.Id == Guid.Empty)
		{
			throw new ArgumentOutOfRangeException(nameof(definition), "Definition Id is blank");
		}

		return await _cspService.SaveCspDefinitionAsync(definition);
	}
}