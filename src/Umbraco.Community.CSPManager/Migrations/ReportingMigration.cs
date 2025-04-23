using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Migrations;

public class ReportingMigration : MigrationBase
{
	public const string MigrationKey = "csp-manager-add-reporting";

	public ReportingMigration(IMigrationContext context) : base(context)
	{
	}

	protected override void Migrate()
	{
		if (!ColumnExists(nameof(CspDefinition), nameof(SchemaUpdates.ReportingDirective)))
		{
			Create.Column(nameof(SchemaUpdates.ReportingDirective))
			.OnTable(nameof(CspDefinition))
			.AsString(500).Nullable().Do();
		}

		if (!ColumnExists(nameof(CspDefinition), nameof(SchemaUpdates.ReportUri)))
		{
			Create.Column(nameof(SchemaUpdates.ReportUri))
			.OnTable(nameof(CspDefinition))
			.AsString(500).Nullable().Do();
		}
	}

	public class SchemaUpdates
	{
		public string? ReportingDirective { get; set; }

		public string? ReportUri { get; set; }
	}
}
