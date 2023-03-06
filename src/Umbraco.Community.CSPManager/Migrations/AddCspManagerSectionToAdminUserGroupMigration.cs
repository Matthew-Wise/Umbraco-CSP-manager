namespace Umbraco.Community.CSPManager.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using UmbConstants = Umbraco.Cms.Core.Constants;

public class AddCspManagerSectionToAdminUserGroupMigration : MigrationBase
{
	public const string MigrationKey = "csp-manager-add-section";


	private readonly IUserService _userService;

	public AddCspManagerSectionToAdminUserGroupMigration(IMigrationContext context, IUserService userService)
	   : base(context)
	{
		_userService = userService;
	}

	protected override void Migrate()
	{
		var userGroup = _userService.GetUserGroupByAlias(UmbConstants.Security.AdminGroupAlias);

		if (userGroup != null && !userGroup.AllowedSections.Contains(CspConstants.PluginAlias))
		{
			userGroup.AddAllowedSection(CspConstants.PluginAlias);

			_userService.Save(userGroup);
		}
	}
}
