namespace Umbraco.Community.CSPManager.TagHelpers;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Services;

[HtmlTargetElement(ScriptTag, Attributes = CspNonceAttributeName)]
[HtmlTargetElement(StyleTag, Attributes = CspNonceAttributeName)]
public class CspNonceTagHelper : TagHelper
{
	private const string ScriptTag = "script";
	private const string StyleTag = "style";
	private const string CspNonceAttributeName = "csp-manager-add-nonce";
	private const string CspNonceDataAttributeName = "csp-manager-add-nonce-data-attribute";
	private readonly ICspService _cspService;
	private readonly ILogger<CspNonceTagHelper> _logger;

	public CspNonceTagHelper(ICspService cspService, ILogger<CspNonceTagHelper> logger)
	{
		_cspService = cspService;
		_logger = logger;
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
	public ViewContext ViewContext { get; set; } = null!;

	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		if (!UseCspNonce)
		{
			return;
		}

		var httpContext = new HttpContextWrapper(ViewContext.HttpContext);
		string nonce;
		string contextMarkerKey;
		var tag = output.TagName;

		switch (tag)
		{
			case ScriptTag:
				nonce = _cspService.GetCspScriptNonce(httpContext);
				contextMarkerKey = CspConstants.CspManagerScriptNonceSet;
				break;
			case StyleTag:
				nonce = _cspService.GetCspStyleNonce(httpContext);
				contextMarkerKey = CspConstants.CspManagerStyleNonceSet;
				break;
			default:
				_logger.LogWarning("CSP Nonce used on an invalid tag {Tag}", tag);
				return;
		}

		if (string.IsNullOrEmpty(httpContext.GetItem<string>(contextMarkerKey)))
		{
			httpContext.SetItem(contextMarkerKey, "set");
		}

		output.Attributes.Add(new TagHelperAttribute("nonce", nonce));

		if (IncludeDataAttribute)
		{
			output.Attributes.Add(new TagHelperAttribute("data-nonce", nonce));
		}
	}
}
