﻿namespace Umbraco.Community.CSPManager.Extensions;

using Microsoft.AspNetCore.Mvc.Rendering;

public static class CspHtmlHelpers
{
	/// <summary>
	/// Generates a CSP nonce HTML attribute. The 120-bit random nonce will be included in the CSP script-src directive.
	/// </summary>
	/// <param name="helper"></param>
	public static string? CspScriptNonceValue(this IHtmlHelper helper)
	{
		var context = helper.ViewContext.HttpContext;
		var nonce = context.GetCspManagerContext()?.ScriptNonce;

		return nonce;
	}

	/// <summary>
	/// Generates a CSP nonce HTML attribute. The 120-bit random nonce will be included in the CSP style-src directive.
	/// </summary>
	/// <param name="helper"></param>
	public static string? CspStyleNonceValue(this IHtmlHelper helper)
	{
		var context = helper.ViewContext.HttpContext;
		var nonce = context.GetCspManagerContext()?.StyleNonce;

		return nonce;
	}
}