namespace Umbraco.Community.CSPManager.Models.Api;

/// <summary>
/// A content node a node policy can belong to, with its hostnames in Culture &amp; Hostnames (none
/// for a page inside a site) and whether it has a node policy for all cultures. A node policy
/// belongs to the node, so it covers every one of these hostnames unless the node also has a policy
/// for the hostname's culture.
/// </summary>
public sealed class CspApiNodeInfo
{
	/// <summary>
	/// Gets or sets the key of the content node.
	/// </summary>
	public Guid ContentKey { get; set; }

	/// <summary>
	/// Gets or sets the name of the content node.
	/// </summary>
	public string ContentName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the hostnames assigned to the node (wildcard, culture-only domains excluded), in
	/// Umbraco's sort order.
	/// </summary>
	public List<CspApiDomainName> Domains { get; set; } = [];

	/// <summary>
	/// Gets or sets a value indicating whether the node already has a node policy for all cultures.
	/// </summary>
	public bool HasCspPolicy { get; set; }

	/// <summary>
	/// Gets or sets the id of the node's policy for all cultures, if it has one.
	/// </summary>
	public Guid? CspDefinitionId { get; set; }
}
