namespace Umbraco.Community.CSPManager;

using Models;

public interface IHttpContextWrapper
{
	T GetOriginalHttpContext<T>() where T : class;

	CspManagerContext GetCspManagerContext();

	void RemoveHttpHeader(string name);

	void SetHttpHeader(string name, string value);

	T GetItem<T>(string key) where T : class;

	void SetItem<T>(string key, T value) where T : class;
}
