using Umbraco.Cms.Core;
using Umbraco.Community.CSPManager.Models;

using CspManagerConstants = Umbraco.Community.CSPManager.Constants;

namespace Umbraco.Community.CSPManager.uSync;

/// <summary>
/// Provides alias and naming helpers for uSync CSP definitions.
/// </summary>
internal static class CspItemNames
{
	public const string BackOfficeAlias = "backoffice";

	public const string FrontEndAlias = "front-end";

	public const string NodeAliasPrefix = "node-";

	/// <summary>
	/// The entity type and XML element of node policies. The global policies keep
	/// <see cref="CspManagerConstants.EntityTypes.CspPolicy"/>, so each kind has its own uSync handler.
	/// </summary>
	public const string NodePolicyEntityType = "csp-node-policy";

	/// <summary>
	/// Gets the alias of a definition: <c>node-{content key}</c> for a node's policy for all cultures,
	/// <c>node-{content key}-{culture}</c> for its policy for one culture.
	/// </summary>
	public static string Alias(CspDefinition definition)
		=> definition.ContentKey is { } contentKey
			? string.IsNullOrWhiteSpace(definition.Culture)
				? $"{NodeAliasPrefix}{contentKey:D}"
				: $"{NodeAliasPrefix}{contentKey:D}-{definition.Culture}"
			: definition.IsBackOffice ? BackOfficeAlias : FrontEndAlias;

	/// <summary>
	/// Reads the content key and culture back from an alias made by <see cref="Alias"/>.
	/// </summary>
	public static bool TryParseNodeAlias(string alias, out Guid contentKey, out string? culture)
	{
		const int keyLength = 36; // a GUID in "D" format

		contentKey = Guid.Empty;
		culture = null;

		if (!alias.StartsWith(NodeAliasPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var rest = alias.AsSpan(NodeAliasPrefix.Length);
		if (rest.Length < keyLength || !Guid.TryParseExact(rest[..keyLength], "D", out contentKey))
		{
			return false;
		}

		if (rest.Length == keyLength)
		{
			return true;
		}

		// "-{culture}", with at least one character of culture.
		if (rest[keyLength] != '-' || rest.Length == keyLength + 1)
		{
			return false;
		}

		culture = rest[(keyLength + 1)..].ToString();
		return true;
	}

	public static string Name(CspDefinition definition)
		=> definition.ContentKey is { } contentKey
			? string.IsNullOrWhiteSpace(definition.Culture)
				? $"Node policy {contentKey:D}"
				: $"Node policy {contentKey:D} ({definition.Culture})"
			: definition.IsBackOffice ? "Backoffice" : "Frontend";

	/// <summary>
	/// Gets the uSync key of a definition: the fixed id of a global policy, or the key derived from
	/// the content key and culture of a node policy.
	/// </summary>
	public static Guid Key(CspDefinition definition)
		=> definition.ContentKey is { } contentKey ? CspItemKeys.NodePolicy(contentKey, definition.Culture) : definition.Id;

	/// <summary>
	/// Gets the UDI of a definition, with the entity type of its handler.
	/// </summary>
	public static Udi GetUdi(CspDefinition definition)
		=> Udi.Create(definition.ContentKey is null ? CspManagerConstants.EntityTypes.CspPolicy : NodePolicyEntityType, Key(definition));
}
