namespace Umbraco.Community.CSPManager.Models.Api;

/// <summary>
/// A hostname assigned to a content node in Culture &amp; Hostnames.
/// </summary>
public sealed class CspApiDomainName
{
	/// <summary>
	/// Gets or sets the domain name, e.g. <c>example.com</c> or <c>example.com/en</c>.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the culture (ISO code) assigned to the domain, if any.
	/// </summary>
	public string? Culture { get; set; }
}
