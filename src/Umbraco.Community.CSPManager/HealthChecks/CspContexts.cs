namespace Umbraco.Community.CSPManager.HealthChecks;

/// <summary>
/// The two CSP contexts (front-end and back-office) that CSP Manager evaluates independently, shared by
/// health checks that need to inspect both.
/// </summary>
internal static class CspContexts
{
	public static readonly (bool IsBackOffice, string Label)[] All =
	[
		(false, "front-end"),
		(true, "back-office")
	];
}
