using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Migrations;

internal class UpgradeInsecureRequestsMigration : AsyncMigrationBase
{

	public const string MigrationKey = "csp-manager-add-upgrade-insecure-requests";

	public UpgradeInsecureRequestsMigration(IMigrationContext context) : base(context)
	{
	}

	protected override Task MigrateAsync()
	{
		if (!ColumnExists(nameof(CspDefinition), nameof(SchemaUpdates.UpgradeInsecureRequests)))
		{
			Create.Column(nameof(SchemaUpdates.UpgradeInsecureRequests))
			.OnTable(nameof(CspDefinition))
			.AsBoolean().WithDefaultValue(false).Do();
		}

		return Task.CompletedTask;
	}

	[ExcludeFromCodeCoverage(Justification = "Migration model so not accessed directly.")]
	public sealed class SchemaUpdates
	{
		public bool UpgradeInsecureRequests { get; set; }
	}
}