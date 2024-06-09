namespace Umbraco.Community.CSPManager.Core.Migrations;

using System;
using System.Linq;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using UmbConstants = Umbraco.Cms.Core.Constants;

public class AddCspManagerSectionToAdminUserGroupMigration : MigrationBase
{
	public const string MigrationKey = "csp-manager-add-section";


	private readonly IUserGroupService _userGroupService;

	public AddCspManagerSectionToAdminUserGroupMigration(IMigrationContext context, IUserGroupService userGroupService)
	   : base(context)
	{
		_userGroupService = userGroupService;
	}

	protected override void Migrate()
	{
		var result = _userGroupService.GetAsync(UmbConstants.Security.AdminGroupAlias).Result;
		if (result == null || result.AllowedSections.Contains<string>(CspConstants.PluginAlias))
		{
			return;
		}

		result.AddAllowedSection("workflow");
		_userGroupService.UpdateAsync(result, UmbConstants.Security.SuperUserKey);
	}
}
