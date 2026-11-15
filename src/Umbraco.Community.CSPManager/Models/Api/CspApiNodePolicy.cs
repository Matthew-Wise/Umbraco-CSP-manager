namespace Umbraco.Community.CSPManager.Models.Api;

/// <summary>
/// Summary of a node policy, as listed under the Frontend policy in the backoffice tree.
/// </summary>
public sealed class CspApiNodePolicy
{
	/// <summary>
	/// Gets or sets the id of the node policy.
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// Gets or sets the key of the content node the policy belongs to.
	/// </summary>
	public Guid ContentKey { get; set; }

	/// <summary>
	/// Gets or sets the name of the content node, or <c>null</c> when the node no longer exists.
	/// </summary>
	public string? ContentName { get; set; }

	/// <summary>
	/// Gets or sets the culture (ISO code) the policy is for, or <c>null</c> for the node's policy for
	/// all cultures.
	/// </summary>
	public string? Culture { get; set; }

	/// <summary>
	/// Gets or sets whether the policy is complete or adds its sources to the policy it inherits.
	/// </summary>
	public CspPolicyInheritance Inheritance { get; set; }

	/// <summary>
	/// Gets or sets whether the policy also applies to the pages below its node.
	/// </summary>
	public CspPolicyScope AppliesTo { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the policy is enabled.
	/// </summary>
	public bool Enabled { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the policy is orphaned: its node was deleted or is in
	/// the recycle bin, or its culture is no longer one of the site's languages. An orphaned policy
	/// has no effect on any request; it is kept so it comes back into use when the node is restored
	/// or the language is added again, and can be deleted.
	/// </summary>
	public bool IsOrphaned { get; set; }
}
