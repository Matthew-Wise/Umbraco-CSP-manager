namespace Umbraco.Community.CSPManager.Controllers;

using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Community.CSPManager;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

[PluginController(CspConstants.PluginAlias)]
public sealed class CSPManagerApiController : UmbracoAuthorizedJsonController
{
	private readonly ICspService _cspService;

	public CSPManagerApiController(ICspService cspService)
	{
		_cspService = cspService;
	}

	[HttpGet]
	public CspDefinition GetDefinition(bool isBackOffice = false) => _cspService.GetCspDefinition(isBackOffice);

	[HttpGet]
#pragma warning disable CA1822 // Mark members as static
	public ICollection<string> GetCspDirectiveOptions() => CspConstants.AllDirectives.ToArray();
#pragma warning restore CA1822 // Mark members as static

	[HttpPost]
	public async Task<CspDefinition> SaveDefinition(CspDefinition definition)
	{
		if (definition.Id == Guid.Empty)
		{
			throw new ArgumentOutOfRangeException(nameof(definition), "Definition Id is blank");
		}

		return await _cspService.SaveCspDefinitionAsync(definition);
	}
}
