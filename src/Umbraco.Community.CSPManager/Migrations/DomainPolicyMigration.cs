using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Migrations;

/// <summary>
/// Adds the nullable <c>ContentKey</c> column used by node policies, plus a unique index so a
/// content node can never end up with two policies even when two saves race.
/// </summary>
internal sealed class DomainPolicyMigration : AsyncMigrationBase
{
	public const string MigrationKey = "csp-manager-add-domain-policy";

	public const string ContentKeyIndexName = "IX_CspDefinition_ContentKey";

	public DomainPolicyMigration(IMigrationContext context) : base(context)
	{
	}

	protected override Task MigrateAsync()
	{
		if (!ColumnExists(nameof(CspDefinition), nameof(SchemaUpdates.ContentKey)))
		{
			Create.Column(nameof(SchemaUpdates.ContentKey))
				.OnTable(nameof(CspDefinition))
				.AsGuid().Nullable().Do();
		}

		if (!IndexExists(ContentKeyIndexName))
		{
			// Filtered so the two global rows (both NULL) don't collide: SQL Server treats NULLs as
			// equal in a plain unique index. The partial-index syntax is the same on SQLite.
			Execute.Sql(
				$"CREATE UNIQUE INDEX {SqlSyntax.GetQuotedName(ContentKeyIndexName)} " +
				$"ON {SqlSyntax.GetQuotedTableName(nameof(CspDefinition))} ({SqlSyntax.GetQuotedColumnName(nameof(SchemaUpdates.ContentKey))}) " +
				$"WHERE {SqlSyntax.GetQuotedColumnName(nameof(SchemaUpdates.ContentKey))} IS NOT NULL")
				.Do();
		}

		return Task.CompletedTask;
	}

	[ExcludeFromCodeCoverage(Justification = "Migration model so not accessed directly.")]
	public sealed class SchemaUpdates
	{
		public Guid? ContentKey { get; set; }
	}
}
