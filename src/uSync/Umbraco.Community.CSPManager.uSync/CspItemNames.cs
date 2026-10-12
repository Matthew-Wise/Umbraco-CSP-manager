using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.uSync;

/// <summary>
/// Provides alias and naming helpers for uSync CSP definitions.
/// </summary>
internal static class CspItemNames
{
	public const string BackOfficeAlias = "backoffice";

	public const string FrontEndAlias = "front-end";

	public const string NodeAliasPrefix = "node-";

	public static string Alias(CspDefinition definition)
		=> definition.ContentKey is { } contentKey
			? $"{NodeAliasPrefix}{contentKey:D}"
			: definition.IsBackOffice ? BackOfficeAlias : FrontEndAlias;

	public static string Name(CspDefinition definition)
		=> definition.ContentKey is { } contentKey
			? $"Node policy {contentKey:D}"
			: definition.IsBackOffice ? "Backoffice" : "Frontend";
}
