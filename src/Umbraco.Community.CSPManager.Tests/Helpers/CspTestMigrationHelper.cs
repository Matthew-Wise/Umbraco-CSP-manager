using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.CSPManager.Migrations;

namespace Umbraco.Community.CSPManager.Tests.Helpers;

internal static class CspTestMigrationHelper
{
	/// <summary>
	/// Runs a migration plan and asserts it succeeded: the package's own plan, unless another
	/// (e.g. the plan of an earlier release) is given.
	/// </summary>
	public static async Task RunMigrationsAsync(
		IMigrationPlanExecutor executor,
		IScopeProvider scopeProvider,
		IKeyValueService keyValueService,
		MigrationPlan plan = null)
	{
		var upgrader = new Upgrader(plan ?? new CspMigrationPlan());
		var result = await upgrader.ExecuteAsync(executor, scopeProvider, keyValueService).ConfigureAwait(false);
		if (!result.Successful)
			await TestContext.Out.WriteLineAsync(result.Exception?.Message);
		Assert.That(result.Successful, Is.True);
	}
}