using Microsoft.Extensions.Configuration;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.Tests.Helpers;

namespace Umbraco.Community.CSPManager.Tests.Services;

// Runs real concurrent writes against the integration database. Without the package's write lock,
// SQLite saves that read before writing deadlock each other (each holds a read snapshot and waits
// to upgrade), so the timeout turns a regression into a failure instead of a hung test run.
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class CspServiceConcurrencyTests : UmbracoIntegrationTestWithContent
{
	private const int ConcurrentWriters = 8;
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

	private ICspService _cspService;

	protected override void CustomTestSetup(IUmbracoBuilder builder)
	{
		builder.AddComposers();
	}

	protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
	{
		base.SetUpTestConfiguration(configBuilder);
		configBuilder.AddInMemoryCollection(new Dictionary<string, string>
		{
			["Umbraco:CMS:Unattended:PackageMigrationsUnattended"] = "false"
		});
	}

	[SetUp]
	public async Task SetUpDomain()
	{
		await CspTestMigrationHelper.RunMigrationsAsync(GetRequiredService<IMigrationPlanExecutor>(), ScopeProvider, GetRequiredService<IKeyValueService>());
		_cspService = GetRequiredService<ICspService>();

		// Node policies belong to Textpage, which needs a hostname to qualify.
		await CspTestDomainHelper.AssignDomainsAsync(GetRequiredService<IDomainService>(), Textpage.Key, ["race.example.com"]);
	}

	[Test]
	public async Task ConcurrentCreatesForTheSameDomain_AllComplete_AndExactlyOnePolicyIsCreated()
	{
		var contentKey = Textpage.Key;

		// Half create through the "copy the frontend policy" path, half post a new policy directly.
		var outcomes = await RunConcurrentlyAsync(i => i % 2 == 0
			? _cspService.CreateCspDefinitionForNodeAsync(contentKey, CancellationToken.None)
			: _cspService.SaveCspDefinitionAsync(NewDomainPolicy(contentKey, $"writer{i}.example.com"), CancellationToken.None));

		Assert.Multiple(async () =>
		{
			Assert.That(outcomes.Count(o => o is null), Is.EqualTo(1), "exactly one writer creates the policy");
			Assert.That(outcomes.Where(o => o is not null), Is.All.InstanceOf<CspDefinitionValidationException>(),
				"every other writer is told the domain already has a policy");
			Assert.That(await _cspService.GetAllNodePoliciesAsync(CancellationToken.None), Has.Count.EqualTo(1));
		});
	}

	[Test]
	public async Task ConcurrentSavesOfTheGlobalPolicies_AllComplete()
	{
		var outcomes = await RunConcurrentlyAsync(i =>
		{
			var id = i % 2 == 0 ? Constants.DefaultFrontEndId : Constants.DefaultBackofficeId;
			return _cspService.SaveCspDefinitionAsync(new CspDefinition
			{
				Id = id,
				Enabled = true,
				Sources = [new CspDefinitionSource { DefinitionId = id, Source = $"writer{i}.example.com", Directives = [Constants.Directives.DefaultSource] }]
			}, CancellationToken.None);
		});

		Assert.That(outcomes, Is.All.Null, "no global save may fail");

		var frontend = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);
		var backoffice = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: true, CancellationToken.None);
		Assert.Multiple(() =>
		{
			// Each save replaces the sources, so whichever writer went last wins - but whole, not mixed.
			Assert.That(frontend.Sources, Has.Count.EqualTo(1));
			Assert.That(backoffice.Sources, Has.Count.EqualTo(1));
		});
	}

	[Test]
	public async Task ConcurrentSavesAndDeletesOfADomainPolicy_AllComplete()
	{
		var policy = await _cspService.CreateCspDefinitionForNodeAsync(Textpage.Key, CancellationToken.None);

		var outcomes = await RunConcurrentlyAsync(async i =>
		{
			if (i == ConcurrentWriters - 1)
			{
				await _cspService.DeleteCspDefinitionAsync(policy.Id, CancellationToken.None);
				return;
			}

			var update = NewDomainPolicy(Textpage.Key, $"writer{i}.example.com");
			update.Id = policy.Id;
			await _cspService.SaveCspDefinitionAsync(update, CancellationToken.None);
		});

		// A save that lands after the delete recreates the row (same id, same domain), which is the
		// documented upsert behaviour; what matters is that nothing deadlocked or failed.
		Assert.That(outcomes, Is.All.Null);
	}

	// ── Duplicate domain safety net ──────────────────────────────────────────

	// The service check and the write run under the package lock, so two of its own saves can't race.
	// A writer that bypasses the service still can: this trigger plays that writer, inserting a
	// policy for the same domain between the service's duplicate check and its own insert.
	[Test]
	public async Task SaveCspDefinitionAsync_WhenTheUniqueIndexCatchesASecondPolicy_ThrowsAValidationException()
	{
		using (var scope = ScopeProvider.CreateScope())
		{
			Assume.That(scope.Database.DatabaseType.GetProviderName(), Does.Contain("Sqlite").IgnoreCase, "the race is simulated with a SQLite trigger");
			var racer = Guid.NewGuid();
			scope.Database.Execute(
				$"""
				CREATE TRIGGER csp_test_racer BEFORE INSERT ON {nameof(CspDefinition)}
				WHEN NEW.{nameof(CspDefinition.Id)} <> '{racer}' AND NEW.{nameof(CspDefinition.ContentKey)} IS NOT NULL
				BEGIN
					INSERT INTO {nameof(CspDefinition)} ({nameof(CspDefinition.Id)}, {nameof(CspDefinition.Enabled)}, {nameof(CspDefinition.ReportOnly)}, {nameof(CspDefinition.IsBackOffice)}, {nameof(CspDefinition.UpgradeInsecureRequests)}, {nameof(CspDefinition.ContentKey)})
					VALUES ('{racer}', 0, 0, 0, 0, NEW.{nameof(CspDefinition.ContentKey)});
				END
				""");
			scope.Complete();
		}

		var ex = Assert.ThrowsAsync<CspDefinitionValidationException>(() => _cspService.SaveCspDefinitionAsync(
			new CspDefinition { ContentKey = Textpage.Key }, CancellationToken.None));

		// The failed save rolled back, trigger insert included.
		var policies = await _cspService.GetAllNodePoliciesAsync(CancellationToken.None);
		Assert.Multiple(() =>
		{
			Assert.That(ex!.MemberName, Is.EqualTo(nameof(CspDefinition.ContentKey)));
			Assert.That(ex.Message, Is.EqualTo("A policy already exists for this content node."));
			Assert.That(policies, Is.Empty);
		});
	}

	[Test]
	public void IsContentKeyUniqueViolation_RecognisesTheSqlServerErrorByIndexName()
	{
		var sqlServerLike = new FakeDbException("Cannot insert duplicate key row in object 'dbo.CspDefinition' with unique index 'IX_CspDefinition_ContentKey'. The duplicate key value is (…).");

		Assert.Multiple(() =>
		{
			Assert.That(CspService.IsContentKeyUniqueViolation(new InvalidOperationException("wrapped", sqlServerLike)), Is.True);
			Assert.That(CspService.IsContentKeyUniqueViolation(new FakeDbException("UNIQUE constraint failed: CspDefinitionSource.DefinitionId, CspDefinitionSource.Source")), Is.False);
			Assert.That(CspService.IsContentKeyUniqueViolation(new InvalidOperationException("IX_CspDefinition_ContentKey")), Is.False, "only database errors count");
		});
	}

	// Starts every writer on its own thread-pool thread (no shared ambient scope) and returns, per
	// writer, the exception it ended with or null when it succeeded.
	private static async Task<Exception[]> RunConcurrentlyAsync(Func<int, Task> write)
	{
		// Released together, so the writers really overlap instead of trickling in as they're scheduled.
		var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var tasks = Enumerable.Range(0, ConcurrentWriters)
			.Select(i => Task.Run(async () =>
			{
				await start.Task;
				try
				{
					await write(i);
					return null;
				}
				catch (Exception ex)
				{
					return ex;
				}
			}))
			.ToArray();

		start.SetResult();

		try
		{
			return await Task.WhenAll(tasks).WaitAsync(Timeout);
		}
		catch (TimeoutException)
		{
			Assert.Fail($"{tasks.Count(t => !t.IsCompleted)} of {ConcurrentWriters} concurrent writes did not complete within {Timeout.TotalSeconds}s (deadlock?)");
			throw;
		}
	}

	private static CspDefinition NewDomainPolicy(Guid contentKey, string source) => new()
	{
		Id = Guid.Empty,
		ContentKey = contentKey,
		Enabled = true,
		Sources = [new CspDefinitionSource { Source = source, Directives = [Constants.Directives.DefaultSource] }]
	};

	private sealed class FakeDbException(string message) : System.Data.Common.DbException(message);
}
