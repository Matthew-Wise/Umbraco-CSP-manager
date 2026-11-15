using Microsoft.Extensions.Configuration;
using NPoco;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.CSPManager.Migrations;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.Tests.Helpers;

namespace Umbraco.Community.CSPManager.Tests.Migrations;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class DomainPolicyMigrationTests : UmbracoIntegrationTest
{
	protected override void CustomTestSetup(IUmbracoBuilder builder)
	{
		builder.AddComposers();
	}

	protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
	{
		base.SetUpTestConfiguration(configBuilder);
		configBuilder.AddInMemoryCollection(new Dictionary<string, string>
		{
			["Umbraco:CMS:Unattended:PackageMigrationsUnattended"] = "false"
		});
	}

	// The full plan on an empty database is the fresh-install path: InitialCspManagerMigration must
	// insert its seed row through its own schema type, since CspDefinition now has a column
	// (ContentKey) that only a later migration adds.
	[Test]
	public async Task FreshInstall_RunsTheWholePlan_AndSeedsTheBackOfficePolicy()
	{
		await CspTestMigrationHelper.RunMigrationsAsync(GetRequiredService<IMigrationPlanExecutor>(), ScopeProvider, GetRequiredService<IKeyValueService>());

		var backoffice = await GetRequiredService<ICspService>().GetCspDefinitionAsync(isBackOfficeRequest: true, CancellationToken.None);
		Assert.Multiple(() =>
		{
			Assert.That(backoffice.Id, Is.EqualTo(Constants.DefaultBackofficeId));
			Assert.That(backoffice.ContentKey, Is.Null);
			Assert.That(backoffice.Sources, Is.Not.Empty);
		});
		AssertNodePolicyColumnsAndIndexesExist();
		Assert.That(CountDefinitionsLockRows(), Is.EqualTo(1));
	}

	// 18.1.0 is the last release before node policies: its plan ends at UpgradeInsecureRequestsMigration.
	[Test]
	public async Task Upgrade_FromTheVersionBeforeDomainPolicies_KeepsExistingPoliciesAsGlobal()
	{
		var executor = GetRequiredService<IMigrationPlanExecutor>();
		var keyValueService = GetRequiredService<IKeyValueService>();

		// The plan as it was in 18.1.0, under the same name so the real plan resumes from it.
		var previousPlan = new MigrationPlan(Constants.PackageAlias);
		previousPlan.From(string.Empty)
			.To<InitialCspManagerMigration>(InitialCspManagerMigration.MigrationKey)
			.To<AddCspManagerSectionToAdminUserGroupMigration>(AddCspManagerSectionToAdminUserGroupMigration.MigrationKey)
			.To<ReportingMigration>(ReportingMigration.MigrationKey)
			.To<MaxSourceLengthMigration>(MaxSourceLengthMigration.MigrationKey)
			.To<UpgradeInsecureRequestsMigration>(UpgradeInsecureRequestsMigration.MigrationKey);
		var previous = await new Upgrader(previousPlan).ExecuteAsync(executor, ScopeProvider, keyValueService);
		Assert.That(previous.Successful, Is.True, previous.Exception?.Message);

		using (var scope = ScopeProvider.CreateScope())
		{
			scope.Database.Insert(new PreviousCspDefinition { Id = Constants.DefaultFrontEndId, Enabled = true });
			scope.Complete();
		}

		await CspTestMigrationHelper.RunMigrationsAsync(executor, ScopeProvider, keyValueService);

		AssertNodePolicyColumnsAndIndexesExist();
		Assert.That(CountDefinitionsLockRows(), Is.EqualTo(1), "an upgrade gets the lock row too");
		var cspService = GetRequiredService<ICspService>();
		var frontend = await cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);
		Assert.Multiple(async () =>
		{
			Assert.That(frontend.Id, Is.EqualTo(Constants.DefaultFrontEndId));
			Assert.That(frontend.Enabled, Is.True);
			Assert.That(frontend.ContentKey, Is.Null);
			Assert.That(frontend.Culture, Is.Null);
			Assert.That(frontend.Inheritance, Is.EqualTo(CspPolicyInheritance.Replace));
			Assert.That(frontend.AppliesTo, Is.EqualTo(CspPolicyScope.Descendants));
			Assert.That(await cspService.GetAllNodePoliciesAsync(CancellationToken.None), Is.Empty);
		});

		// The upgraded row is still saved like any other.
		frontend.ReportOnly = true;
		Assert.DoesNotThrowAsync(() => cspService.SaveCspDefinitionAsync(frontend, CancellationToken.None));
	}

	// A site built from the main branch between 18.1.0 and this feature: it has node policies with
	// one policy per node. They must come through as what they were: a complete policy for all
	// cultures of the node and the pages below it.
	[Test]
	public async Task Upgrade_FromOnePolicyPerNode_KeepsExistingNodePoliciesAsCompletePoliciesForAllCultures()
	{
		var executor = GetRequiredService<IMigrationPlanExecutor>();
		var keyValueService = GetRequiredService<IKeyValueService>();

		var previousPlan = new MigrationPlan(Constants.PackageAlias);
		previousPlan.From(string.Empty)
			.To<InitialCspManagerMigration>(InitialCspManagerMigration.MigrationKey)
			.To<AddCspManagerSectionToAdminUserGroupMigration>(AddCspManagerSectionToAdminUserGroupMigration.MigrationKey)
			.To<ReportingMigration>(ReportingMigration.MigrationKey)
			.To<MaxSourceLengthMigration>(MaxSourceLengthMigration.MigrationKey)
			.To<UpgradeInsecureRequestsMigration>(UpgradeInsecureRequestsMigration.MigrationKey)
			.To<DomainPolicyMigration>(DomainPolicyMigration.MigrationKey)
			.To<DefinitionsLockMigration>(DefinitionsLockMigration.MigrationKey);
		var previous = await new Upgrader(previousPlan).ExecuteAsync(executor, ScopeProvider, keyValueService);
		Assert.That(previous.Successful, Is.True, previous.Exception?.Message);

		var policyId = Guid.NewGuid();
		var contentKey = Guid.NewGuid();
		using (var scope = ScopeProvider.CreateScope())
		{
			scope.Database.Execute(
				"INSERT INTO CspDefinition (Id, Enabled, ReportOnly, IsBackOffice, UpgradeInsecureRequests, ContentKey) VALUES (@0, @1, @2, @3, @4, @5)",
				policyId, true, true, false, false, contentKey);
			scope.Database.Execute(
				"INSERT INTO CspDefinitionSource (DefinitionId, Source, Directives) VALUES (@0, @1, @2)",
				policyId, "site.example.com", $"[\"{Constants.Directives.DefaultSource}\"]");
			scope.Complete();
		}

		await CspTestMigrationHelper.RunMigrationsAsync(executor, ScopeProvider, keyValueService);

		AssertNodePolicyColumnsAndIndexesExist();
		var policy = (await GetRequiredService<ICspService>().GetAllNodePoliciesAsync(CancellationToken.None)).Single();
		Assert.Multiple(() =>
		{
			Assert.That(policy.Id, Is.EqualTo(policyId));
			Assert.That(policy.ContentKey, Is.EqualTo(contentKey));
			Assert.That(policy.Culture, Is.Null);
			Assert.That(policy.Inheritance, Is.EqualTo(CspPolicyInheritance.Replace));
			Assert.That(policy.AppliesTo, Is.EqualTo(CspPolicyScope.Descendants));
			Assert.That(policy.Enabled, Is.True);
			Assert.That(policy.ReportOnly, Is.True);
			Assert.That(policy.Sources.Select(s => s.Source), Is.EqualTo(new[] { "site.example.com" }));
		});
	}

	// The plan can be re-run after a partial failure: every step checks what is already there.
	[Test]
	public async Task NodePolicyScopeMigration_WhenItsColumnsAndIndexesAlreadyExist_DoesNotFail()
	{
		var executor = GetRequiredService<IMigrationPlanExecutor>();
		var keyValueService = GetRequiredService<IKeyValueService>();
		await CspTestMigrationHelper.RunMigrationsAsync(executor, ScopeProvider, keyValueService);

		var again = new MigrationPlan("csp-manager-rerun");
		again.From(string.Empty).To<NodePolicyScopeMigration>("rerun");
		var result = await new Upgrader(again).ExecuteAsync(executor, ScopeProvider, keyValueService);

		Assert.That(result.Successful, Is.True, result.Exception?.Message);
		AssertNodePolicyColumnsAndIndexesExist();
	}

	// The unique indexes are the last line of defence against two policies for one node and
	// culture when a writer bypasses the service. On every database: one policy for all cultures
	// per node, one per node and culture, and any number of global-style rows without a node.
	[Test]
	public async Task UniqueIndexes_AllowOnePolicyPerNodeAndCulture()
	{
		await CspTestMigrationHelper.RunMigrationsAsync(GetRequiredService<IMigrationPlanExecutor>(), ScopeProvider, GetRequiredService<IKeyValueService>());
		var node = Guid.NewGuid();

		Assert.Multiple(() =>
		{
			Assert.That(TryInsertPolicy(node, null), Is.Null, "the node's policy for all cultures");
			Assert.That(TryInsertPolicy(node, "fr-FR"), Is.Null, "a policy for one culture next to it");
			Assert.That(TryInsertPolicy(node, "da-DK"), Is.Null, "and for another culture");
			Assert.That(TryInsertPolicy(Guid.NewGuid(), null), Is.Null, "another node's policy for all cultures");
		});

		var secondForAllCultures = TryInsertPolicy(node, null);
		var secondForCulture = TryInsertPolicy(node, "fr-FR");
		Assert.Multiple(() =>
		{
			Assert.That(secondForAllCultures, Is.Not.Null, "a second policy for all cultures must be refused");
			Assert.That(secondForCulture, Is.Not.Null, "a second policy for the same culture must be refused");
			Assert.That(CspService.IsContentKeyUniqueViolation(secondForAllCultures), Is.True, secondForAllCultures?.Message);
			Assert.That(CspService.IsContentKeyUniqueViolation(secondForCulture), Is.True, secondForCulture?.Message);
		});
	}

	// Returns the exception the insert failed with, or null when the row was stored.
	private Exception TryInsertPolicy(Guid contentKey, string culture)
	{
		try
		{
			using var scope = ScopeProvider.CreateScope();
			scope.Database.Insert(new CspDefinition { Id = Guid.NewGuid(), ContentKey = contentKey, Culture = culture, Enabled = true });
			scope.Complete();
			return null;
		}
		catch (Exception ex)
		{
			return ex;
		}
	}

	// The lock row may already exist (added by hand, or a plan re-run after a partial failure).
	[Test]
	public async Task DefinitionsLockMigration_WhenTheLockRowAlreadyExists_DoesNotFail()
	{
		using (var scope = ScopeProvider.CreateScope())
		{
			scope.Database.Execute(
				"INSERT INTO umbracoLock (id, name, value) VALUES (@0, @1, 1)",
				Constants.Locks.Definitions,
				Constants.Locks.DefinitionsName);
			scope.Complete();
		}

		await CspTestMigrationHelper.RunMigrationsAsync(GetRequiredService<IMigrationPlanExecutor>(), ScopeProvider, GetRequiredService<IKeyValueService>());

		Assert.That(CountDefinitionsLockRows(), Is.EqualTo(1));
	}

	// A save takes the lock, so it needs the row; this proves the row the migration adds is usable.
	[Test]
	public async Task FreshInstall_SavesTakeTheDefinitionsLock()
	{
		await CspTestMigrationHelper.RunMigrationsAsync(GetRequiredService<IMigrationPlanExecutor>(), ScopeProvider, GetRequiredService<IKeyValueService>());

		Assert.DoesNotThrowAsync(() => GetRequiredService<ICspService>().SaveCspDefinitionAsync(
			new CspDefinition { Id = Constants.DefaultFrontEndId, Enabled = true },
			CancellationToken.None));
	}

	private int CountDefinitionsLockRows()
	{
		using var scope = ScopeProvider.CreateScope(autoComplete: true);
		return scope.Database.ExecuteScalar<int>("SELECT COUNT(*) FROM umbracoLock WHERE id = @0", Constants.Locks.Definitions);
	}

	private void AssertNodePolicyColumnsAndIndexesExist()
	{
		using var scope = ScopeProvider.CreateScope(autoComplete: true);
		var columns = scope.SqlContext.SqlSyntax.GetColumnsInSchema(scope.Database)
			.Where(c => c.TableName.Equals(nameof(CspDefinition), StringComparison.OrdinalIgnoreCase))
			.Select(c => c.ColumnName)
			.ToList();
		var indexes = scope.SqlContext.SqlSyntax.GetDefinedIndexes(scope.Database)
			.Select(i => i.Item2)
			.ToList();

		Assert.Multiple(() =>
		{
			Assert.That(columns, Does.Contain(nameof(CspDefinition.ContentKey)));
			Assert.That(columns, Does.Contain(nameof(CspDefinition.Culture)));
			Assert.That(columns, Does.Contain(nameof(CspDefinition.Inheritance)));
			Assert.That(columns, Does.Contain(nameof(CspDefinition.AppliesTo)));
			Assert.That(indexes, Does.Contain(NodePolicyScopeMigration.ContentKeyIndexName));
			Assert.That(indexes, Does.Contain(NodePolicyScopeMigration.ContentKeyCultureIndexName));
			Assert.That(indexes, Does.Not.Contain(DomainPolicyMigration.ContentKeyIndexName),
				"the one-policy-per-node index would refuse a second culture");
		});
	}

	// The CspDefinition table as it was before the ContentKey column. NPoco maps every property.
	// ReSharper disable UnusedMember.Local, UnusedAutoPropertyAccessor.Local
	[TableName(nameof(CspDefinition))]
	[PrimaryKey(nameof(Id), AutoIncrement = false)]
	private sealed class PreviousCspDefinition
	{
		[PrimaryKeyColumn(AutoIncrement = false)]
		public Guid Id { get; set; }

		public bool Enabled { get; set; }

		public bool ReportOnly { get; set; }

		public bool IsBackOffice { get; set; }

		[NullSetting(NullSetting = NullSettings.Null)]
		public string ReportingDirective { get; set; }

		[NullSetting(NullSetting = NullSettings.Null)]
		public string ReportUri { get; set; }

		public bool UpgradeInsecureRequests { get; set; }
	}
	// ReSharper restore UnusedMember.Local, UnusedAutoPropertyAccessor.Local
}
