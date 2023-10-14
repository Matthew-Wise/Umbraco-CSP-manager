namespace Umbraco.Community.CSPManager.TagHelpers;

using System;
using Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement(ScriptTag, Attributes = CspNonceAttributeName)]
[HtmlTargetElement(StyleTag, Attributes = CspNonceAttributeName)]
public class CspNonceTagHelper : TagHelper
{
	private const string ScriptTag = "script";
	private const string StyleTag = "style";
	private const string CspNonceAttributeName = "csp-manager-add-nonce";
	private readonly ICspNonceHelper _cspNonceHelper;

	public CspNonceTagHelper(ICspNonceHelper cspNonceHelper)
	{
		_cspNonceHelper = cspNonceHelper;
	}

	/// <summary>
	/// Specifies a whether a nonce should be added to the tag and the CSP header.
	/// </summary>
	[HtmlAttributeName(CspNonceAttributeName)]
	public bool UseCspNonce { get; set; }

	[HtmlAttributeNotBound, ViewContext]
	public ViewContext ViewContext { get; set; }

	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		if (!UseCspNonce) return;

		var httpContext = ViewContext.HttpContext;
		string nonce;
		var tag = output.TagName;

		if (tag == ScriptTag)
		{
			nonce = _cspNonceHelper.GetCspScriptNonce(httpContext);
		}
		else if (tag == StyleTag)
		{
			nonce = _cspNonceHelper.GetCspStyleNonce(httpContext);
		}
		else
		{
			throw new Exception($"Something went horribly wrong. You shouldn't be here for the tag {tag}.");
		}

		output.Attributes.Add(new TagHelperAttribute("nonce", nonce));
	}
}
