using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Migrations;

/// <summary>
/// Adds the columns that let any content node have a policy per culture, with inheritance:
/// <c>Culture</c>, <c>Inheritance</c> and <c>AppliesTo</c>. Replaces the one-policy-per-node unique
/// index with one-policy-per-node-and-culture.
/// </summary>
/// <remarks>
/// Schema only. A node policy that exists before this step keeps a <c>null</c> culture and the
/// default <see cref="CspPolicyInheritance.Replace"/> and <see cref="CspPolicyScope.Descendants"/>,
/// which is exactly how it behaved before: one complete policy for the node and everything below it.
/// </remarks>
internal sealed class NodePolicyScopeMigration : AsyncMigrationBase
{
	public const string MigrationKey = "csp-manager-add-node-policy-scope";

	/// <summary>One policy for all cultures per node.</summary>
	public const string ContentKeyIndexName = "IX_CspDefinition_ContentKey_AllCultures";

	/// <summary>One policy per node and culture.</summary>
	public const string ContentKeyCultureIndexName = "IX_CspDefinition_ContentKey_Culture";

	public NodePolicyScopeMigration(IMigrationContext context) : base(context)
	{
	}

	protected override Task MigrateAsync()
	{
		if (!ColumnExists(nameof(CspDefinition), nameof(SchemaUpdates.Culture)))
		{
			Create.Column(nameof(SchemaUpdates.Culture))
				.OnTable(nameof(CspDefinition))
				.AsString(35).Nullable().Do();
		}

		if (!ColumnExists(nameof(CspDefinition), nameof(SchemaUpdates.Inheritance)))
		{
			Create.Column(nameof(SchemaUpdates.Inheritance))
				.OnTable(nameof(CspDefinition))
				.AsInt32().NotNullable().WithDefaultValue((int)CspPolicyInheritance.Replace).Do();
		}

		if (!ColumnExists(nameof(CspDefinition), nameof(SchemaUpdates.AppliesTo)))
		{
			Create.Column(nameof(SchemaUpdates.AppliesTo))
				.OnTable(nameof(CspDefinition))
				.AsInt32().NotNullable().WithDefaultValue((int)CspPolicyScope.Descendants).Do();
		}

		// The index from DomainPolicyMigration allowed a single policy per node, whatever its culture.
		if (IndexExists(DomainPolicyMigration.ContentKeyIndexName))
		{
			Delete.Index(DomainPolicyMigration.ContentKeyIndexName).OnTable(nameof(CspDefinition)).Do();
		}

		// Two filtered indexes rather than one on (ContentKey, Culture): SQLite treats NULLs as
		// distinct in a unique index and SQL Server treats them as equal, so a single index would
		// either allow two all-culture policies for a node (SQLite) or behave differently per
		// database. Both skip the two global rows (ContentKey IS NULL).
		if (!IndexExists(ContentKeyIndexName))
		{
			Execute.Sql(
				$"CREATE UNIQUE INDEX {SqlSyntax.GetQuotedName(ContentKeyIndexName)} " +
				$"ON {Table} ({ContentKey}) " +
				$"WHERE {ContentKey} IS NOT NULL AND {Culture} IS NULL")
				.Do();
		}

		if (!IndexExists(ContentKeyCultureIndexName))
		{
			Execute.Sql(
				$"CREATE UNIQUE INDEX {SqlSyntax.GetQuotedName(ContentKeyCultureIndexName)} " +
				$"ON {Table} ({ContentKey}, {Culture}) " +
				$"WHERE {ContentKey} IS NOT NULL AND {Culture} IS NOT NULL")
				.Do();
		}

		return Task.CompletedTask;
	}

	private string Table => SqlSyntax.GetQuotedTableName(nameof(CspDefinition));

	private string ContentKey => SqlSyntax.GetQuotedColumnName(nameof(CspDefinition.ContentKey));

	private string Culture => SqlSyntax.GetQuotedColumnName(nameof(SchemaUpdates.Culture));

	[ExcludeFromCodeCoverage(Justification = "Migration model so not accessed directly.")]
	public sealed class SchemaUpdates
	{
		public string? Culture { get; set; }

		public int Inheritance { get; set; }

		public int AppliesTo { get; set; }
	}
}
