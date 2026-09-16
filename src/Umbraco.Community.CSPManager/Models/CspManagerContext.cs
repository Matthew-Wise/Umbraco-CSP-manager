namespace Umbraco.Community.CSPManager.Models;

public class CspManagerContext
{
	public string? Nonce { get; set; } = null;

	public HashSet<string>? ScriptHashes { get; set; }

	public HashSet<string>? StyleHashes { get; set; }
}