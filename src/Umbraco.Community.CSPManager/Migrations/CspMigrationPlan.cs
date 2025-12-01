using Umbraco.Cms.Core.Packaging;

namespace Umbraco.Community.CSPManager.Migrations;

public sealed class CspMigrationPlan : PackageMigrationPlan
{
	public CspMigrationPlan() : base(Constants.PackageAlias)
	{
	}

	protected override void DefinePlan()
	{
		To<InitialCspManagerMigration>(InitialCspManagerMigration.MigrationKey);
		To<AddCspManagerSectionToAdminUserGroupMigration>(AddCspManagerSectionToAdminUserGroupMigration.MigrationKey);
		To<ReportingMigration>(ReportingMigration.MigrationKey);
		To<MaxSourceLengthMigration>(MaxSourceLengthMigration.MigrationKey);
		To<UpgradeInsecureRequestsMigration>(UpgradeInsecureRequestsMigration.MigrationKey);
	}
}