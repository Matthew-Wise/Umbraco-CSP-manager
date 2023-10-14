namespace Umbraco.Community.CSPManager.Extensions;

using Microsoft.AspNetCore.Mvc.Rendering;
using Services;

public static class CspHtmlHelpers
{
	/// <summary>
	/// Generates a CSP nonce HTML attribute. The 120-bit random nonce will be included in the CSP script-src directive.
	/// </summary>
	/// <param name="helper"></param>
	public static string? CspScriptNonceValue(this IHtmlHelper helper)
	{
		var httpContext = new HttpContextWrapper(helper.ViewContext.HttpContext);
		var cspService = helper.ViewContext.HttpContext.RequestServices.GetService(typeof(ICspService)) as ICspService;
		var cspManagerContext = httpContext.GetCspManagerContext();
		var nonce = cspManagerContext?.ScriptNonce;

		// First reference to a nonce, set header and mark that header has been set. We only need to set it once.
		if (string.IsNullOrEmpty(httpContext.GetItem<string>("CspManagerScriptNonceSet")))
		{
			httpContext.SetItem("CspManagerScriptNonceSet", "set");
			cspService.SetCspHeaders(httpContext);
		}

		return nonce;
	}

	/// <summary>
	/// Generates a CSP nonce HTML attribute. The 120-bit random nonce will be included in the CSP style-src directive.
	/// </summary>
	/// <param name="helper"></param>
	public static string? CspStyleNonceValue(this IHtmlHelper helper)
	{
		var httpContext = new HttpContextWrapper(helper.ViewContext.HttpContext);
		var cspService = helper.ViewContext.HttpContext.RequestServices.GetService(typeof(ICspService)) as ICspService;
		var cspManagerContext = httpContext.GetCspManagerContext();
		var nonce = cspManagerContext?.StyleNonce;

		// First reference to a nonce, set header and mark that header has been set. We only need to set it once.
		if (string.IsNullOrEmpty(httpContext.GetItem<string>("CspManagerScriptNonceSet")))
		{
			httpContext.SetItem("CspManagerScriptNonceSet", "set");
			cspService.SetCspHeaders(httpContext);
		}

		return nonce;
	}
}
