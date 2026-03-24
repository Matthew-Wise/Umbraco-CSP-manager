namespace Umbraco.Community.CSPManager.Models.Api;

/// <summary>
/// Represents an Umbraco domain with CSP policy status information.
/// </summary>
public sealed class CspDomainInfo
{
	/// <summary>
	/// Gets or sets the stable Guid Key of the Umbraco domain.
	/// </summary>
	public Guid Key { get; set; }

	/// <summary>
	/// Gets or sets the domain name (e.g. "example.com").
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value indicating whether this domain has a CSP policy configured.
	/// </summary>
	public bool HasCspPolicy { get; set; }

	/// <summary>
	/// Gets or sets the CSP definition ID for this domain, if a policy exists.
	/// </summary>
	public Guid? CspDefinitionId { get; set; }
}
