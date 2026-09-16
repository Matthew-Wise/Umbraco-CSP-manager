using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.TagHelpers;

/// <summary>
/// Computes a CSP <c>'sha256-...'</c> source for a static inline <c>&lt;script&gt;</c> or
/// <c>&lt;style&gt;</c> block and adds it to the matching CSP directive.
/// </summary>
/// <remarks>
/// Unlike <see cref="CspNonceTagHelper"/>, this does not modify the tag's own request-independent
/// content, so it works with output caching and CDNs - the same hash is valid on every request as
/// long as the content itself never changes. It must not be used on content that varies per
/// request (user data, timestamps, generated values); the hash would not match and the block
/// would be blocked.
/// </remarks>
[HtmlTargetElement(Constants.TagHelper.ScriptTag, Attributes = CspHashAttributeName)]
[HtmlTargetElement(Constants.TagHelper.StyleTag, Attributes = CspHashAttributeName)]
public class CspHashTagHelper : TagHelper
{
	private const string CspHashAttributeName = "csp-manager-add-hash";
	private const string CspHashDataAttributeName = "csp-manager-add-hash-data-attribute";
	private const string SrcAttributeName = "src";

	private readonly ICspService _cspService;
	private readonly ILogger<CspHashTagHelper> _logger;

	public CspHashTagHelper(ICspService cspService, ILogger<CspHashTagHelper> logger)
	{
		_cspService = cspService;
		_logger = logger;
	}

	/// <summary>
	/// Specifies whether a CSP hash source should be computed for this tag's content and added to the CSP header.
	/// </summary>
	[HtmlAttributeName(CspHashAttributeName)]
	public bool UseCspHash { get; set; }

	/// <summary>
	/// Specifies whether the computed hash should also be output as a data attribute.
	/// </summary>
	[HtmlAttributeName(CspHashDataAttributeName)]
	public bool IncludeDataAttribute { get; set; }

	[HtmlAttributeNotBound, ViewContext]
	public ViewContext ViewContext { get; set; } = null!;

	public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
	{
		if (!UseCspHash)
		{
			return;
		}

		var tag = output.TagName;

		CspHashTarget? target = tag switch
		{
			Constants.TagHelper.ScriptTag => CspHashTarget.Script,
			Constants.TagHelper.StyleTag => CspHashTarget.Style,
			_ => null
		};

		if (target is null)
		{
			_logger.LogWarning("CSP Hash used on an invalid tag {Tag}", tag);
			return;
		}

		if (output.Attributes.ContainsName(SrcAttributeName))
		{
			// A src-loaded script has no inline content for us to hash - the browser would need a
			// hash of the fetched resource, which this tag helper does not compute.
			_logger.LogWarning("CSP Hash used on a {Tag} tag with a '{SrcAttribute}' attribute; hashes only apply to inline content and were not added", tag, SrcAttributeName);
			return;
		}

		var httpContext = ViewContext.HttpContext;
		var childContent = await output.GetChildContentAsync();
		var content = childContent.GetContent();

		var hash = _cspService.AddCspHash(httpContext, target.Value, content);

		if (IncludeDataAttribute && !string.IsNullOrEmpty(hash))
		{
			output.Attributes.Add(new TagHelperAttribute("data-csp-hash", hash.Trim('\'')));
		}
	}
}
