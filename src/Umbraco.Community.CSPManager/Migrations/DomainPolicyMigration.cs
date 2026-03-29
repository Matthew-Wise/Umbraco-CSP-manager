using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Migrations;

internal class DomainPolicyMigration : AsyncMigrationBase
{
	public const string MigrationKey = "csp-manager-add-domain-policy";

	public DomainPolicyMigration(IMigrationContext context) : base(context)
	{
	}

	protected override Task MigrateAsync()
	{
		if (!ColumnExists(nameof(CspDefinition), nameof(SchemaUpdates.DomainKey)))
		{
			Create.Column(nameof(SchemaUpdates.DomainKey))
				.OnTable(nameof(CspDefinition))
				.AsGuid().Nullable().Do();
		}

		return Task.CompletedTask;
	}

	[ExcludeFromCodeCoverage(Justification = "Migration model so not accessed directly.")]
	public sealed class SchemaUpdates
	{
		public Guid? DomainKey { get; set; }
	}
}
