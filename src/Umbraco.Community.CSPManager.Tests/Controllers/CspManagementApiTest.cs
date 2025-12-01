using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Tests.Integration.ManagementApi;
using Umbraco.Community.CSPManager.Controllers;
using Umbraco.Community.CSPManager.Extensions;
using Umbraco.Community.CSPManager.Migrations;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Controllers;

[TestFixture]
public abstract class CspManagementApiTest<T> : ManagementApiTest<T>
	where T : ManagementApiControllerBase
{
	protected override Expression<Func<T, object>> MethodSelector { get; set; }

	protected IShortStringHelper ShortStringHelper;

	private IUserGroup CspUserGroup;

	protected override void CustomTestSetup(IUmbracoBuilder builder)
	{
		builder.Services.AddControllers()
		.AddApplicationPart(typeof(DefinitionsController).Assembly);

		builder.AddCspManager();
	}

	[SetUp]
	public async Task SetUp()
	{
		var upgrader = new Upgrader(new CspMigrationPlan());
		var result = await upgrader.ExecuteAsync(GetRequiredService<IMigrationPlanExecutor>(), GetRequiredService<IScopeProvider>(), GetRequiredService<IKeyValueService>()).ConfigureAwait(false);
		Assert.That(result.Successful, Is.True);

		ShortStringHelper = GetRequiredService<IShortStringHelper>();
	}

	protected async Task<IUserGroup> CreateCspUserGroupAsync()
	{
		if (CspUserGroup is not null) return CspUserGroup;

		var userGroup = new UserGroup(ShortStringHelper)
		{
			Name = "Test",
			Alias = "test",
			Permissions = new HashSet<string> { ActionBrowse.ActionLetter },
			HasAccessToAllLanguages = true,
			StartContentId = -1,
			StartMediaId = -1
		};
		userGroup.AddAllowedSection(Constants.SectionAlias);

		var groupCreationResult = await GetRequiredService<IUserGroupService>().CreateAsync(userGroup, UmbConstants.Security.SuperUserKey);
		Assert.That(groupCreationResult.Success, Is.True);
		CspUserGroup = groupCreationResult.Result;
		return CspUserGroup;
	}
}