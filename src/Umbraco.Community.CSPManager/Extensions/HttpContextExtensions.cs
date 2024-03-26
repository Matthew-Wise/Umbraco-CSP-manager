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

	public static void SetItem<T>(this HttpContext context, string key, T value) where T : class
	{
		context.Items[key] = value;
	}

	public static T? GetItem<T>(this HttpContext context, string key) where T : class => context.Items.TryGetValue(key, out var value) && value is T item ? item : null;
}
