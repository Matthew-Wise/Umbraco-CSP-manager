using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Migrations;

public class AddCspManagerSectionToAdminUserGroupMigration : AsyncMigrationBase
{
	public const string MigrationKey = "csp-manager-add-section";


	private readonly IUserGroupService _userGroupService;

	public AddCspManagerSectionToAdminUserGroupMigration(IMigrationContext context, IUserGroupService userGroupService)
	   : base(context)
	{
		_userGroupService = userGroupService;
	}

	protected override  async Task MigrateAsync()
	{
		var result = await _userGroupService.GetAsync(UmbConstants.Security.AdminGroupAlias);
		if (result == null || result.AllowedSections.Contains<string>(Constants.SectionAlias))
		{
			return;
		}

		result.AddAllowedSection(Constants.SectionAlias);
		await _userGroupService.UpdateAsync(result, UmbConstants.Security.SuperUserKey);
	}
}
