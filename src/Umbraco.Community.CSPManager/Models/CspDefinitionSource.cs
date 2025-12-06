using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Umbraco.Community.CSPManager.Models;

/// <summary>
/// Represents a source entry in a Content Security Policy definition.
/// </summary>
/// <remarks>
/// A source defines an allowed origin (e.g., "'self'", "https://example.com", "'unsafe-inline'")
/// and the CSP directives it applies to. Multiple sources combine to form the complete CSP policy.
/// The source and definition ID together form a composite primary key.
/// </remarks>
[TableName((nameof(CspDefinitionSource)))]
[PrimaryKey([nameof(DefinitionId), nameof(Source)])]
public class CspDefinitionSource
{
	[PrimaryKeyColumn(
		AutoIncrement = false,
		OnColumns = $"{nameof(DefinitionId)}, {nameof(Source)}")]
	[ForeignKey(typeof(CspDefinition))]
	public Guid DefinitionId { get; set; }

	[PrimaryKeyColumn(
		AutoIncrement = false,
		OnColumns = $"{nameof(DefinitionId)}, {nameof(Source)}")]
	[Length(4000)]
	public string Source { get; init; } = string.Empty;

	[SerializedColumn(Name = nameof(Directives))]
	[SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
	public List<string> Directives { get; set; } = [];
}