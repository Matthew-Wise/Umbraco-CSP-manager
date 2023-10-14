namespace Umbraco.Community.CSPManager.Helpers;

using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Models;
using Extensions;

public class CspNonceHelper : ICspNonceHelper
{
	public string GetCspScriptNonce(HttpContext context)
	{
		var cspManagerContext = context.GetCspManagerContext();

		if (cspManagerContext == null)
		{
			return string.Empty;
		}

		if (!string.IsNullOrEmpty(cspManagerContext.ScriptNonce))
		{
			return cspManagerContext.ScriptNonce;
		}

		var nonce = GenerateCspNonceValue();

		SetCspDirectiveNonce(cspManagerContext, nonce, CspConstants.CspDirectives.ScriptSrc);

		return nonce;
	}
	public string GetCspStyleNonce(HttpContext context)
	{
		var cspManagerContext = context.GetCspManagerContext();

		if (cspManagerContext == null)
		{
			return string.Empty;
		}

		if (!string.IsNullOrEmpty(cspManagerContext.ScriptNonce))
		{
			return cspManagerContext.ScriptNonce;
		}

		var nonce = GenerateCspNonceValue();

		SetCspDirectiveNonce(cspManagerContext, nonce, CspConstants.CspDirectives.StyleSrc);

		return nonce;
	}

	private void SetCspDirectiveNonce(CspManagerContext cspManagerContext, string nonce, CspConstants.CspDirectives directive)
	{
		switch (directive)
		{
			case CspConstants.CspDirectives.ScriptSrc:
				cspManagerContext.ScriptNonce = nonce;
				break;
			case CspConstants.CspDirectives.StyleSrc:
				cspManagerContext.StyleNonce = nonce;
				break;
		}
	}

	private string GenerateCspNonceValue()
	{
		using var rng = RandomNumberGenerator.Create();
		var nonceBytes = new byte[18];
		rng.GetBytes(nonceBytes);
		return Convert.ToBase64String(nonceBytes);
	}
}
