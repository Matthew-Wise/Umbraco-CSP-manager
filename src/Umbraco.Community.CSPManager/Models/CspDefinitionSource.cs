
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using NPoco;

namespace Umbraco.Community.CSPManager.Models;

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
