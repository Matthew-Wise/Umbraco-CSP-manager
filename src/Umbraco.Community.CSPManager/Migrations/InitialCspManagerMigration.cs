namespace Umbraco.Community.CSPManager.Migrations;

using NPoco;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using Umbraco.Community.CSPManager.Models;

public sealed class InitialCspManagerMigration : MigrationBase
{
	public const string MigrationKey = "csp-manager-init";
	private readonly IUmbracoVersion UmbracoVersion;

	public InitialCspManagerMigration(IMigrationContext context, IUmbracoVersion umbracoVersion) : base(context)
	{
		UmbracoVersion = umbracoVersion;
	}

	protected override void Migrate()
	{
		if (!TableExists(nameof(CspDefinition)))
		{
			Create.Table<CspDefinitionSchema>().Do();
			Context.Database.Insert(new CspDefinition
			{
				Id = CspConstants.DefaultBackofficeId,
				IsBackOffice = true,
				Enabled = false,
				ReportOnly = false
			});
		}

		if (!TableExists(nameof(CspDefinitionSource)))
		{
			Create.Table<CspDefinitionSourceSchema>().Do();
			foreach (var source in CspConstants.DefaultBackOfficeCsp)
			{
				if (UmbracoVersion.Version.Major >= 13 && source.Source == "www.gravatar.com")
				{
					continue;
				}

				Context.Database.Insert(source);
			}
		}
	}

	[TableName((nameof(CspDefinition)))]
	[PrimaryKey(nameof(Id), AutoIncrement = false)]
	private class CspDefinitionSchema
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
		public List<CspDefinitionSource> Sources { get; set; } = new();
	}

	[TableName((nameof(CspDefinitionSource)))]
	[PrimaryKey(new[] { nameof(DefinitionId), nameof(Source) })]
	private class CspDefinitionSourceSchema
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
		public List<string> Directives { get; set; } = new();
	}
}
