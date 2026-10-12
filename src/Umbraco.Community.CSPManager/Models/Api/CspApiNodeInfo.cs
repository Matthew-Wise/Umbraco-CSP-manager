namespace Umbraco.Community.CSPManager.Models.Api;

/// <summary>
/// A content node with at least one hostname in Culture &amp; Hostnames, and whether it has a domain
/// policy. A node policy belongs to the node, so it covers every one of these hostnames.
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
	/// Gets or sets a value indicating whether the node already has a node policy.
	/// </summary>
	public bool HasCspPolicy { get; set; }

	/// <summary>
	/// Gets or sets the id of the node's policy, if it has one.
	/// </summary>
	public Guid? CspDefinitionId { get; set; }
}
