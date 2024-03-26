namespace Umbraco.Community.CSPManager.Migrations;

using System.Data.SqlTypes;
using Newtonsoft.Json;
using NPoco;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Extensions;

public class MaxSourceLengthMigration : MigrationBase
{
	public const string MigrationKey = "csp-manager-max-source-length";

	public MaxSourceLengthMigration(IMigrationContext context) : base(context)
	{
	}

	protected override void Migrate()
	{

		if (TableExists(nameof(CspDefinitionSource)))
		{
			var tempName = $"{nameof(CspDefinitionSource)}Old";
			Rename.Table(nameof(CspDefinitionSource)).To(tempName).Do();
			Delete.KeysAndIndexes(tempName).Do();


			Create.Table<CspDefinitionSourceSchema>().Do();

			var dataCopy = Insert.IntoTable(nameof(CspDefinitionSource));

			var getAllSources = Sql().SelectAll().From(tempName);

			foreach (var row in  Database.Fetch<CspDefinitionSourceSchema>(getAllSources))
			{
				dataCopy.Row(new
				{
					row.DefinitionId,
					Source = SqlSyntax.EscapeString(row.Source),
					Directives = JsonConvert.SerializeObject(row.Directives)
				});
			}
			
			dataCopy.Do();

			Delete.Table(tempName).Do();
		}

	}

	[TableName((nameof(CspDefinitionSource)))]
	[PrimaryKey(new[] { nameof(DefinitionId), nameof(Source) })]
	public sealed class CspDefinitionSourceSchema
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
		public List<string> Directives { get; set; } = new();
	}
}