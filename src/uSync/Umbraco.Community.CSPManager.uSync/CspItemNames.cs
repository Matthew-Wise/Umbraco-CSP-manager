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

	public static string Alias(CspDefinition definition)
		=> definition.ContentKey is { } contentKey
			? $"{NodeAliasPrefix}{contentKey:D}"
			: definition.IsBackOffice ? BackOfficeAlias : FrontEndAlias;

	public static string Name(CspDefinition definition)
		=> definition.ContentKey is { } contentKey
			? $"Node policy {contentKey:D}"
			: definition.IsBackOffice ? "Backoffice" : "Frontend";

	/// <summary>
	/// Gets the uSync key of a definition: the fixed id of a global policy, or the key derived from
	/// the content key of a node policy.
	/// </summary>
	public static Guid Key(CspDefinition definition)
		=> definition.ContentKey is { } contentKey ? CspItemKeys.NodePolicy(contentKey) : definition.Id;

	/// <summary>
	/// Gets the UDI of a definition, with the entity type of its handler.
	/// </summary>
	public static Udi GetUdi(CspDefinition definition)
		=> Udi.Create(definition.ContentKey is null ? CspManagerConstants.EntityTypes.CspPolicy : NodePolicyEntityType, Key(definition));
}
