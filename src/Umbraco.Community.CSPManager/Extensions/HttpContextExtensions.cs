using Microsoft.AspNetCore.Http;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Extensions;

internal static class HttpContextExtensions
{
	public static CspManagerContext? GetCspManagerContext(this HttpContext context)
	{
		if (!context.Items.ContainsKey(Constants.ContextKey))
		{
			context.Items[Constants.ContextKey] = new CspManagerContext();
		}

		return context.Items[Constants.ContextKey] as CspManagerContext;
	}

	public static void SetItem<T>(this HttpContext context, string key, T value) where T : class 
		=> context.Items[key] = value;

	public static T? GetItem<T>(this HttpContext context, string key) where T : class 
		=> context.Items.TryGetValue(key, out var value) && value is T item ? item : null;
}
