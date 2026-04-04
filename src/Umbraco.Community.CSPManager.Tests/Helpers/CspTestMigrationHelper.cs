using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.CSPManager.Migrations;

namespace Umbraco.Community.CSPManager.Tests.Helpers;

internal static class CspTestMigrationHelper
{
	public static async Task RunMigrationsAsync(
		IMigrationPlanExecutor executor,
		IScopeProvider scopeProvider,
		IKeyValueService keyValueService)
	{
		var upgrader = new Upgrader(new CspMigrationPlan());
		var result = await upgrader.ExecuteAsync(executor, scopeProvider, keyValueService).ConfigureAwait(false);
		if (!result.Successful)
			await TestContext.Out.WriteLineAsync(result.Exception?.Message);
		Assert.That(result.Successful, Is.True);
	}
}
