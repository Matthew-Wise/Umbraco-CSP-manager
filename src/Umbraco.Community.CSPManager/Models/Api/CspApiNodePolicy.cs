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
	/// Gets or sets a value indicating whether the policy is enabled.
	/// </summary>
	public bool Enabled { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the policy is orphaned: its node was deleted, is in the
	/// recycle bin or no longer has a hostname. An orphaned policy has no effect on any request; it is
	/// kept so it comes back into use when a hostname is (re-)assigned to the node, and can be deleted.
	/// </summary>
	public bool IsOrphaned { get; set; }
}
