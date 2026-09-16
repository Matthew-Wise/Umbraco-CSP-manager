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

	/// <summary>
	/// Reads the per-request <see cref="CspManagerContext"/> without creating one, so callers
	/// that only want to read accumulated state (e.g. the middleware) don't allocate a context
	/// for requests no tag helper ever touched.
	/// </summary>
	public static CspManagerContext? GetCspManagerContext(this HttpContext context)
		=> context.Items.TryGetValue(Constants.TagHelper.ContextKey, out var cspContext) ? cspContext as CspManagerContext : null;

	public static T? GetItem<T>(this HttpContext context, string key) where T : struct
		=> context.Items.TryGetValue(key, out var value) && value is T item ? item : null;
}