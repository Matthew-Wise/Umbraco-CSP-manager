namespace Umbraco.Community.CSPManager;

using Microsoft.AspNetCore.Http;
using Models;

public class HttpContextWrapper : IHttpContextWrapper
{
	private readonly HttpContext _context;

	public HttpContextWrapper(HttpContext context)
	{
		_context = context;
	}

	public T GetOriginalHttpContext<T>() where T : class
	{
		return _context as T;
	}

	public CspManagerContext GetCspManagerContext()
	{
		return GetCspManagerContext(CspConstants.ContextKey);
	}

	public void SetItem<T>(string key, T value) where T : class
	{
		_context.Items[key] = value;
	}

	public T GetItem<T>(string key) where T : class
	{
		return _context.Items.ContainsKey(key) ? (T)_context.Items[key] : null;
	}

	public void SetHttpHeader(string name, string value)
	{
		_context.Response.Headers[name] = value;
	}

	public void RemoveHttpHeader(string name)
	{
		_context.Response.Headers.Remove(name);
	}

	private CspManagerContext GetCspManagerContext(string contextKey)
	{
		if (!_context.Items.ContainsKey(contextKey))
		{
			_context.Items[contextKey] = new CspManagerContext();
		}

		return (CspManagerContext)_context.Items[contextKey];
	}
}
