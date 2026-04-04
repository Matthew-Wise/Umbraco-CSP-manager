using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.TagHelpers;


[HtmlTargetElement(Constants.TagHelper.ScriptTag, Attributes = CspNonceAttributeName)]
[HtmlTargetElement(Constants.TagHelper.StyleTag, Attributes = CspNonceAttributeName)]
[HtmlTargetElement(Constants.TagHelper.LinkTag, Attributes = CspNonceAttributeName)]
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
		var tag = output.TagName;

		string contextMarkerKey = tag switch
		{
			Constants.TagHelper.ScriptTag => Constants.TagHelper.CspManagerScriptNonceSet,
			Constants.TagHelper.StyleTag or Constants.TagHelper.LinkTag => Constants.TagHelper.CspManagerStyleNonceSet,
			_ => string.Empty
		};

		if (string.IsNullOrEmpty(contextMarkerKey))
		{
			_logger.LogWarning("CSP Nonce used on an invalid tag {Tag}", tag);
			return;
		}

		var nonce = _cspService.GetOrCreateCspNonce(httpContext);


		httpContext.Items[contextMarkerKey] = true;

		output.Attributes.Add(new TagHelperAttribute("nonce", nonce));

		if (IncludeDataAttribute)
		{
			output.Attributes.Add(new TagHelperAttribute("data-nonce", nonce));
		}
	}
}