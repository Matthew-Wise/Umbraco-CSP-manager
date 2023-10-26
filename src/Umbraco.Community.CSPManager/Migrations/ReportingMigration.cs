namespace Umbraco.Community.CSPManager.Migrations;

using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.CSPManager.Models;

public class ReportingMigration : MigrationBase
{
	public const string MigrationKey = "csp-manager-add-reporting";

	public ReportingMigration(IMigrationContext context) : base(context)
	{
	}

	protected override void Migrate()
	{
		if (!ColumnExists(nameof(CspDefinition), nameof(SchameUpdates.ReportingDirective)))
		{
			Create.Column(nameof(SchameUpdates.ReportingDirective))
			.OnTable(nameof(CspDefinition))
			.AsString(500).Nullable().Do();
		}

		if (!ColumnExists(nameof(CspDefinition), nameof(SchameUpdates.ReportUri)))
		{
			Create.Column(nameof(SchameUpdates.ReportUri))
			.OnTable(nameof(CspDefinition))
			.AsString(500).Nullable().Do();
		}
	}

	public class SchameUpdates
	{
		public string? ReportingDirective { get; set; }

		public string? ReportUri { get; set; }
	}
}
