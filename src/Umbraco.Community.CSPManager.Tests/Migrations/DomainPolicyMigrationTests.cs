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
		AssertContentKeyColumnAndIndexExist();
		Assert.That(CountDefinitionsLockRows(), Is.EqualTo(1));
	}

	[Test]
	public async Task Upgrade_FromTheVersionBeforeDomainPolicies_KeepsExistingPoliciesAsGlobal()
	{
		var executor = GetRequiredService<IMigrationPlanExecutor>();
		var keyValueService = GetRequiredService<IKeyValueService>();

		// The plan as it was before this feature, under the same name so the real plan resumes from it.
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

		AssertContentKeyColumnAndIndexExist();
		Assert.That(CountDefinitionsLockRows(), Is.EqualTo(1), "an upgrade gets the lock row too");
		var cspService = GetRequiredService<ICspService>();
		var frontend = await cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);
		Assert.Multiple(async () =>
		{
			Assert.That(frontend.Id, Is.EqualTo(Constants.DefaultFrontEndId));
			Assert.That(frontend.Enabled, Is.True);
			Assert.That(frontend.ContentKey, Is.Null);
			Assert.That(await cspService.GetAllNodePoliciesAsync(CancellationToken.None), Is.Empty);
		});
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

	private void AssertContentKeyColumnAndIndexExist()
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
			Assert.That(indexes, Does.Contain(DomainPolicyMigration.ContentKeyIndexName));
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
