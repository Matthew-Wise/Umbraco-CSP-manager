using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.TagHelpers;


[HtmlTargetElement(Constants.TagHelper.ScriptTag, Attributes = CspNonceAttributeName)]
[HtmlTargetElement(Constants.TagHelper.StyleTag, Attributes = CspNonceAttributeName)]
public class CspNonceTagHelper : TagHelper
{
	private const string CspNonceAttributeName = "csp-manager-add-nonce";
	private const string CspNonceDataAttributeName = "csp-manager-add-nonce-data-attribute";

	private readonly ICspService _cspService;
	private readonly ILogger<CspNonceTagHelper> _logger;

	public CspNonceTagHelper(ICspService cspService, ILogger<CspNonceTagHelper> logger)
	{
		_cspService = cspService;
		_logger = logger;
	}

	// <summary>
	/// Specifies a whether a nonce should be added to the tag and the CSP header.
	/// </summary>
	[HtmlAttributeName(CspNonceAttributeName)]
	public bool UseCspNonce { get; set; }

	/// <summary>
	/// Specifies a whether a nonce should also be output as a data attribute
	/// </summary>
	[HtmlAttributeName(CspNonceDataAttributeName)]
	public bool IncludeDataAttribute { get; set; }

	[HtmlAttributeNotBound, ViewContext]
	public ViewContext ViewContext { get; set; } = null!;

	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		if (!UseCspNonce)
		{
			return;
		}

		var httpContext = ViewContext.HttpContext;
		string nonce;
		string contextMarkerKey;
		var tag = output.TagName;

		switch (tag)
		{
			case Constants.TagHelper.ScriptTag:
				nonce = _cspService.GetOrCreateCspScriptNonce(httpContext);
				contextMarkerKey = Constants.TagHelper.CspManagerScriptNonceSet;
				break;
			case Constants.TagHelper.StyleTag:
				nonce = _cspService.GetOrCreateCspStyleNonce(httpContext);
				contextMarkerKey = Constants.TagHelper.CspManagerStyleNonceSet;
				break;
			default:
				_logger.LogWarning("CSP Nonce used on an invalid tag {Tag}", tag);
				return;
		}


		httpContext.Items[contextMarkerKey] = true;

		output.Attributes.Add(new TagHelperAttribute("nonce", nonce));

		if (IncludeDataAttribute)
		{
			output.Attributes.Add(new TagHelperAttribute("data-nonce", nonce));
		}
	}
}