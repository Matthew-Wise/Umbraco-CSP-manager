using NPoco;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Migrations;

public sealed class InitialCspManagerMigration : MigrationBase
{
	public const string MigrationKey = "csp-manager-init";

	public InitialCspManagerMigration(IMigrationContext context) : base(context)
	{
	}

	protected override void Migrate()
	{
		if (!TableExists(nameof(CspDefinition)))
		{
			Create.Table<CspDefinitionSchema>().Do();
			Context.Database.Insert(nameof(CspDefinition), nameof(CspDefinition.Id), false, new
			{
				Id = Constants.DefaultBackofficeId,
				IsBackOffice = true,
				Enabled = false,
				ReportOnly = false
			});
		}

		if (!TableExists(nameof(CspDefinitionSource)))
		{
			Create.Table<CspDefinitionSourceSchema>().Do();
			foreach (var source in Constants.DefaultBackOfficeCsp)
			{
				Context.Database.Insert(nameof(CspDefinitionSource), $"{nameof(CspDefinitionSource.DefinitionId)},{nameof(CspDefinitionSource.Source)}", false, source);
			}
		}
	}

#pragma warning disable S1144 // Unused private types or members should be removed
	[TableName((nameof(CspDefinition)))]
	[PrimaryKey(nameof(Id), AutoIncrement = false)]
	private sealed class CspDefinitionSchema
	{
		[PrimaryKeyColumn(AutoIncrement = false)]
		public Guid Id { get; set; }

		public bool Enabled { get; set; }

		public bool ReportOnly { get; set; }

		public bool IsBackOffice { get; set; }

		[ResultColumn]
		[Reference(ReferenceType.Many,
			ColumnName = nameof(Id),
			ReferenceMemberName = nameof(CspDefinitionSource.DefinitionId))]
		public List<CspDefinitionSource> Sources { get; set; } = [];
	}

	[TableName((nameof(CspDefinitionSource)))]
	[PrimaryKey(new[] { nameof(DefinitionId), nameof(Source) })]
	private sealed class CspDefinitionSourceSchema
	{
		[PrimaryKeyColumn(
			AutoIncrement = false,
			OnColumns = $"{nameof(DefinitionId)}, {nameof(Source)}")]
		[ForeignKey(typeof(CspDefinition))]
		public Guid DefinitionId { get; set; }

		[PrimaryKeyColumn(
			AutoIncrement = false,
			OnColumns = $"{nameof(DefinitionId)}, {nameof(Source)}")]
		public string Source { get; init; } = string.Empty;

		[SerializedColumn(Name = nameof(Directives))]
		[SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
		public List<string> Directives { get; set; } = [];
	}

#pragma warning restore S1144 // Unused private types or members should be removed
}
