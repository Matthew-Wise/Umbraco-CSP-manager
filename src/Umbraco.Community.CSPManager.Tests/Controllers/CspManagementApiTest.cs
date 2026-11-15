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

	// The test server host doesn't start Umbraco's runtime. With a fresh database it boots as
	// "upgrading" (appsettings.Tests.json has PackageMigrationsUnattended on) and Umbraco finishes
	// startup in a background service: it runs the package migrations, seeds the caches and only then
	// lets the management API answer (until then every request gets a 503). A test gets its services
	// before that is done. The startup handlers write to the database (e.g. the published cache
	// rebuild), and a test that starts writing straight away deadlocks with them now and then on
	// SQLite's table locks: both sides retry until their timeouts and the run looks hung.
	//
	// Don't switch PackageMigrationsUnattended off here instead: without that background run nothing
	// marks the host ready and the API answers 503. (An InMemoryConfiguration value wouldn't apply
	// anyway: the base adds appsettings.Tests.json after it.)
	private async Task WaitForBackgroundStartupAsync()
	{
		// The class is internal to Umbraco, hence the name. Fail loudly if it is ever renamed,
		// rather than going back to a hang in a few runs in a hundred.
		var startup = Services.GetServices<IHostedService>()
			.OfType<BackgroundService>()
			.SingleOrDefault(service => service.GetType().Name == "UnattendedUpgradeBackgroundService");
		Assert.That(startup, Is.Not.Null, "Umbraco's UnattendedUpgradeBackgroundService was not found. Has it been renamed?");

		// The host has started every hosted service by the time a test gets its services, so the
		// task exists. If that ever changes, waiting for nothing would bring the hang back quietly.
		Assert.That(startup.ExecuteTask, Is.Not.Null, "Umbraco's UnattendedUpgradeBackgroundService has not been started yet.");

		await startup.ExecuteTask.WaitAsync(TimeSpan.FromMinutes(2));
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