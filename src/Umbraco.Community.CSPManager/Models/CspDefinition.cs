namespace Umbraco.Community.CSPManager.Models;

using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using NPoco;

[TableName((nameof(CspDefinition)))]
[PrimaryKey(nameof(Id), AutoIncrement = false)]
public class CspDefinition
{
	[PrimaryKeyColumn(AutoIncrement = false)]
	public Guid Id { get; set; }

	public bool Enabled { get; set; }

	public bool ReportOnly { get; set; }

	public bool IsBackOffice { get; set; }

	[Length(500)]
	public string? ReportingDirective { get; set; }

	[Length(500)]
	public string? ReportUri { get; set; }

	[ResultColumn]
	[Reference(ReferenceType.Many,
		ColumnName = nameof(Id),
		ReferenceMemberName = nameof(CspDefinitionSource.DefinitionId))]
	public List<CspDefinitionSource> Sources { get; set; } = new();
}


