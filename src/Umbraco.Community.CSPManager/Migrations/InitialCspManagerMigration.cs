using NPoco;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Migrations;

public sealed class InitialCspManagerMigration : AsyncMigrationBase
{
	public const string MigrationKey = "csp-manager-init";

	public InitialCspManagerMigration(IMigrationContext context) : base(context)
	{
	}

	protected override async Task MigrateAsync()
	{
		if (!TableExists(nameof(CspDefinition)))
		{
			Create.Table<CspDefinitionSchema>().Do();
			await Context.Database.InsertAsync<CspDefinition>(new()
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

			await Context.Database.InsertBulkAsync<CspDefinitionSource>(
				Constants.DefaultBackOfficeCsp.Select(source =>
				{
					source.DefinitionId = Constants.DefaultBackofficeId;
					return source;
				}));
		}
	}

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
	[PrimaryKey([nameof(DefinitionId), nameof(Source)])]
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
}
