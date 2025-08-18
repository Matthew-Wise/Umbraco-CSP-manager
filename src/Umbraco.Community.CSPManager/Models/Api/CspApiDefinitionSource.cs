namespace Umbraco.Community.CSPManager.Models.Api;
public sealed class CspApiDefinitionSource
{
	public Guid DefinitionId { get; set; }

	public string Source { get; init; } = string.Empty;

	public List<string> Directives { get; set; } = [];

	internal static CspApiDefinitionSource FromCspDefinitonSource(CspDefinitionSource source) => new()
	{
		DefinitionId = source.DefinitionId,
		Source = source.Source,
		Directives = source.Directives,
	};

	internal static CspDefinitionSource ToCspDefinitonSource(CspApiDefinitionSource source) => new()
	{
		DefinitionId = source.DefinitionId,
		Source = source.Source,
		Directives = source.Directives,
	};
}
