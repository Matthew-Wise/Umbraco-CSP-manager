using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using NPoco;

namespace Umbraco.Community.CSPManager.Models;

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
	[NullSetting(NullSetting = NullSettings.Null)]
	public string? ReportingDirective { get; set; }

	[NullSetting(NullSetting = NullSettings.Null)]
	[Length(500)]
	public string? ReportUri { get; set; }

	public bool UpgradeInsecureRequests { get; set; }

	[ResultColumn]
	[Reference(ReferenceType.Many,
		ColumnName = nameof(Id),
		ReferenceMemberName = nameof(CspDefinitionSource.DefinitionId))]
	public List<CspDefinitionSource> Sources { get; set; } = [];
}


