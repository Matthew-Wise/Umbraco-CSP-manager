namespace Umbraco.Community.CSPManager;

public sealed class CspManagerOptions
{
	public bool DisableBackOfficeHeader { get; set; } = false;

	/// <summary>
	/// Controls what happens when CSP header construction throws. Defaults to
	/// <see cref="CspFailureBehavior.FailOpen"/>, matching the package's original behavior.
	/// </summary>
	public CspFailureBehavior FailureBehavior { get; set; } = CspFailureBehavior.FailOpen;
}