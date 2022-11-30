namespace Umbraco.Community.CSPManager.Controllers;

using CommunityToolkit.HighPerformance;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Community.CSPManager;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

[PluginController(CspConstants.PluginAlias)]
public class CSPManagerApiController : UmbracoAuthorizedJsonController {
    private readonly ICspService _cspService;

	public CSPManagerApiController(ICspService cspService)
	{
		_cspService = cspService;
	}

    [HttpGet]
	public async Task<CspDefinition?> GetDefinition(bool isBackOffice = false)
    {
        var definition = await _cspService.GetCspDefinitionAsync(isBackOffice);
        return definition;
    }

    [HttpGet]
	public List<string> GetCspDirectiveOptions()
    {
        var cspDirectives = new List<string>();
		foreach (var item in CspConstants.AllDirectives.Enumerate())
		{
            cspDirectives.Add(item.Value);
        }
        return cspDirectives;
    }

    [HttpPost]
    public async Task<CspDefinition> SaveDefinition(CspDefinition definition) {
        if(definition == null) {
			throw new ArgumentException("Definition is null");
		}
        if(definition.Id == Guid.Empty) {
            throw new ArgumentException("Definition Id is blank");
        }
        definition = await _cspService.SaveCspDefinitionAsync(definition);
        return definition;
    }
}