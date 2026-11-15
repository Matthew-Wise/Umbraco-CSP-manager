using System.Linq.Expressions;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Tests.Integration.ManagementApi;
using Umbraco.Community.CSPManager.Extensions;
using Umbraco.Community.CSPManager.Tests.Helpers;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Controllers;

[TestFixture]
public abstract class CspManagementApiTest<T> : ManagementApiTest<T>
	where T : ManagementApiControllerBase
{
	protected CspManagementApiTest()
	{
		// Umbraco 17.3 runs package migrations via a background service when PackageMigrationsUnattended=true,
		// which conflicts with the manual migration execution in SetUp. Disable it so tests manage their own migrations.
		InMemoryConfiguration["Umbraco:CMS:Unattended:PackageMigrationsUnattended"] = "false";
	}

	protected override Expression<Func<T, object>> MethodSelector { get; set; }

	protected IShortStringHelper ShortStringHelper;

	private IUserGroup CspUserGroup;

	protected override void CustomTestSetup(IUmbracoBuilder builder)
	{
		builder.Services.AddControllers()
		.AddApplicationPart(typeof(Umbraco.Community.CSPManager.Controllers.DefinitionsController).Assembly);

		builder.AddCspManager();
		builder.SetMediaFileSystem(_ => Mock.Of<IFileSystem>());
		builder.Services.Configure<ImagingSettings>(options =>
		{
			if (options.HMACSecretKey.Length == 0)
			{
				byte[] secret = new byte[64];
				RandomNumberGenerator.Fill(secret);
				options.HMACSecretKey = secret;
			}
		});
	}

	[SetUp]
	public async Task SetUp()
	{
		await WaitForBackgroundStartupAsync();
		await CspTestMigrationHelper.RunMigrationsAsync(GetRequiredService<IMigrationPlanExecutor>(), GetRequiredService<IScopeProvider>(), GetRequiredService<IKeyValueService>());
		ShortStringHelper = GetRequiredService<IShortStringHelper>();
	}

	// Now and then (a few tests in a hundred) Umbraco sees the fresh test database as upgrading and
	// finishes booting in a background service (UnattendedUpgradeBackgroundService), so the test gets
	// its services before startup is done. The startup handlers write to the database (e.g. the
	// published cache rebuild), and a test that starts writing straight away can deadlock with them on
	// SQLite's table locks: both sides then retry until their timeouts and the run looks hung.
	private async Task WaitForBackgroundStartupAsync()
	{
		foreach (var service in Services.GetServices<IHostedService>().OfType<BackgroundService>())
		{
			if (service.GetType().Name == "UnattendedUpgradeBackgroundService" && service.ExecuteTask is { } startup)
			{
				await startup.WaitAsync(TimeSpan.FromMinutes(2));
			}
		}
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