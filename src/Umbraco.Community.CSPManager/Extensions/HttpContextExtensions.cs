using Microsoft.AspNetCore.Http;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Extensions;

internal static class HttpContextExtensions
{
	public static CspManagerContext? GetOrCreateCspManagerContext(this HttpContext context)
	{
		if (!context.Items.TryGetValue(Constants.TagHelper.ContextKey, out var cspContext))
		{
			cspContext = new CspManagerContext();
			context.Items[Constants.TagHelper.ContextKey] = cspContext;
		}

		return cspContext as CspManagerContext;
	}

	public static T? GetItem<T>(this HttpContext context, string key) where T : struct
		=> context.Items.TryGetValue(key, out var value) && value is T item ? item : null;
}