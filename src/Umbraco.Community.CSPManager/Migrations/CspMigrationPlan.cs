﻿namespace Umbraco.Community.CSPManager.Migrations;

using Umbraco.Cms.Core.Packaging;

public sealed class CspMigrationPlan : PackageMigrationPlan
{
	public CspMigrationPlan() : base(CspConstants.PackageAlias)
	{
	}

	protected override void DefinePlan()
	{
		To<InitialCspManagerMigration>(InitialCspManagerMigration.MigrationKey);
		To<AddCspManagerSectionToAdminUserGroupMigration>(AddCspManagerSectionToAdminUserGroupMigration.MigrationKey);
		To<ReportingMigration>(ReportingMigration.MigrationKey);
	}
}
