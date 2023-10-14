namespace Umbraco.Community.CSPManager.TagHelpers;

using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Services;

[HtmlTargetElement(ScriptTag, Attributes = CspNonceAttributeName)]
[HtmlTargetElement(StyleTag, Attributes = CspNonceAttributeName)]
public class CspNonceTagHelper : TagHelper
{
	private const string ScriptTag = "script";
	private const string StyleTag = "style";
	private const string CspNonceAttributeName = "csp-manager-add-nonce";
	private const string CspNonceDataAttributeName = "csp-manager-add-nonce-data-attribute";
	private ICspService _cspService;

	public CspNonceTagHelper(ICspService cspService)
	{
		_cspService = cspService;
	}

	/// <summary>
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
	public ViewContext ViewContext { get; set; }

	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		if (!UseCspNonce) return;

		var httpContext = new HttpContextWrapper(ViewContext.HttpContext);
		string nonce;
		string contextMarkerKey;
		var tag = output.TagName;

		if (tag == ScriptTag)
		{
			nonce = _cspService.GetCspScriptNonce(httpContext);
			contextMarkerKey = "CspManagerScriptNonceSet";
		}
		else if (tag == StyleTag)
		{
			nonce = _cspService.GetCspStyleNonce(httpContext);
			contextMarkerKey = "CspManagerStyleNonceSet";
		}
		else
		{
			throw new Exception($"Something went horribly wrong. You shouldn't be here for the tag {tag}.");
		}

		// First reference to a nonce, set header and mark that header has been set. We only need to set it once.
		if (string.IsNullOrEmpty(httpContext.GetItem<string>(contextMarkerKey)))
		{
			httpContext.SetItem(contextMarkerKey, "set");
			_cspService.SetCspHeaders(httpContext);
		}

		output.Attributes.Add(new TagHelperAttribute("nonce", nonce));

		if (IncludeDataAttribute)
		{
			output.Attributes.Add(new TagHelperAttribute("data-nonce", nonce));
		}
	}
}
