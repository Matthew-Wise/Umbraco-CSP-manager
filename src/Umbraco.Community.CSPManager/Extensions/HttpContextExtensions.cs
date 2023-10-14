namespace Umbraco.Community.CSPManager.Extensions;
using Microsoft.AspNetCore.Http;
using Models;

public static class HttpContextExtensions
{
	public static CspManagerContext? GetCspManagerContext(this HttpContext context)
	{
		if (!context.Items.ContainsKey(CspConstants.ContextKey))
		{
			context.Items[CspConstants.ContextKey] = new CspManagerContext();
		}

		return context.Items[CspConstants.ContextKey] as CspManagerContext;
	}
}
