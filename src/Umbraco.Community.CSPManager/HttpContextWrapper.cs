namespace Umbraco.Community.CSPManager;

using Microsoft.AspNetCore.Http;
using Models;

public class HttpContextWrapper
{
	public HttpContext Context { get; private set; }

	public HttpContextWrapper(HttpContext context)
	{
		Context = context;
	}

	public CspManagerContext? GetCspManagerContext() => GetCspManagerContext(CspConstants.ContextKey);

	public void SetItem<T>(string key, T value) where T : class
	{ 
		Context.Items[key] = value;
	}

	public T? GetItem<T>(string key) where T : class => Context.Items.TryGetValue(key, out var value) && value is T item ? item : null;

	public void SetHttpHeader(string name, string value)
	{
		Context.Response.Headers[name] = value;
	}

	public void RemoveHttpHeader(string name)
	{
		Context.Response.Headers.Remove(name);
	}

	private CspManagerContext? GetCspManagerContext(string contextKey)
	{
		if (!Context.Items.ContainsKey(contextKey))
		{
			Context.Items[contextKey] = new CspManagerContext();
		}

		return Context.Items[contextKey] as CspManagerContext;
	}
}
