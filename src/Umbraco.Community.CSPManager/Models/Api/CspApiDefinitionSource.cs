namespace Umbraco.Community.CSPManager.Models.Api;

/// <summary>
/// API data transfer object representing a CSP source entry.
/// </summary>
/// <remarks>
/// A source entry defines an allowed origin and the directives it applies to.
/// Examples of sources include "'self'", "'unsafe-inline'", "https://example.com", or "*.trusted.com".
/// </remarks>
public sealed class CspApiDefinitionSource
{
	/// <summary>
	/// Gets or sets the ID of the parent <see cref="CspApiDefinition"/> this source belongs to.
	/// </summary>
	public Guid DefinitionId { get; set; }

	/// <summary>
	/// Gets or sets the CSP source value (e.g., "'self'", "https://example.com", "'unsafe-inline'").
	/// </summary>
	public string Source { get; init; } = string.Empty;

	/// <summary>
	/// Gets or sets the list of CSP directives this source applies to (e.g., "script-src", "style-src").
	/// </summary>
	public List<string> Directives { get; set; } = [];

	internal static CspApiDefinitionSource FromCspDefinitionSource(CspDefinitionSource source) => new()
	{
		DefinitionId = source.DefinitionId,
		Source = source.Source,
		Directives = source.Directives,
	};

	internal static CspDefinitionSource ToCspDefinitionSource(CspApiDefinitionSource source) => new()
	{
		DefinitionId = source.DefinitionId,
		Source = source.Source,
		Directives = source.Directives,
	};
}