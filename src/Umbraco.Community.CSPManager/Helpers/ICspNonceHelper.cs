namespace Umbraco.Community.CSPManager.Helpers;

using Microsoft.AspNetCore.Http;

public interface ICspNonceHelper
{
	string GetCspStyleNonce(HttpContext context);
	string GetCspScriptNonce(HttpContext context);
}
