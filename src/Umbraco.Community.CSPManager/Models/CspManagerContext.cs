namespace Umbraco.Community.CSPManager.Models;

public class CspManagerContext
{
	public bool StyleNonceEnabled { get; set; }
	public string? StyleNonce { get; set; } = null;
	public bool ScriptNonceEnabled { get; set; }
	public string? ScriptNonce { get; set; } = null;
}
