using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Extensions;

namespace Umbraco.Community.CSPManager.Migrations;

/// <summary>
/// Adds the <see cref="Constants.Locks.Definitions"/> row to <c>umbracoLock</c>, which every write to
/// the CSP definition tables takes as a distributed write lock before it reads anything.
/// </summary>
/// <remarks>
/// Idempotent: the row is only inserted when it isn't there yet, so re-running the plan (or a site
/// whose row was added by hand) is safe. It is the last step of the plan, so both a fresh install
/// and an upgrade get it before the runtime reaches <c>Run</c> and the API can accept a save.
/// </remarks>
internal sealed class DefinitionsLockMigration : AsyncMigrationBase
{
	public const string MigrationKey = "csp-manager-add-definitions-lock";

	private const string LockTable = Cms.Core.Constants.DatabaseSchema.Tables.Lock;

	public DefinitionsLockMigration(IMigrationContext context) : base(context)
	{
	}

	protected override async Task MigrateAsync()
	{
		var exists = Sql()
			.SelectCount()
			.From(LockTable)
			.Where("id = @0", Constants.Locks.Definitions);

		if (await Database.ExecuteScalarAsync<int>(exists) > 0)
		{
			return;
		}

		Insert.IntoTable(LockTable)
			.Row(new { id = Constants.Locks.Definitions, name = Constants.Locks.DefinitionsName, value = 1 })
			.Do();
	}
}
