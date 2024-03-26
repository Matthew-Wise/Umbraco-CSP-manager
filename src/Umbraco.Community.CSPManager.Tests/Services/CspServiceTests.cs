namespace Umbraco.Community.CSPManager.Tests.Services;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Community.CSPManager.Migrations;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Cms.Core.Cache;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Community.CSPManager.Notifications;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class CspServiceTests : UmbracoIntegrationTest
{
	private CspService _sud;

	private IEventAggregator EventAggregator { get; set; }

	/// <summary>
	/// Taken from <see cref="Umbraco.Cms.Tests.Integration.Umbraco.Infrastructure.Scoping.ScopedRepositoryTests"/>
	/// </summary>
	/// <param name="services"></param>
	protected override void ConfigureTestServices(IServiceCollection services)
	{
		// this is what's created core web runtime
		var appCaches = new AppCaches(
			new DeepCloneAppCache(new ObjectCacheAppCache()),
			NoAppCache.Instance,
			new IsolatedCaches(_ => new DeepCloneAppCache(new ObjectCacheAppCache())));

		services.AddUnique(appCaches);
	}

	protected override T GetRequiredService<T>()
	{
		if (typeof(T) == typeof(IEventAggregator))
		{
			EventAggregator = Mock.Of<IEventAggregator>();
			return (T)EventAggregator;
		}
		
		return base.GetRequiredService<T>();
	}

	[SetUp]
	public void SetUp()
	{
		using (ScopeProvider.CreateScope(autoComplete: true))
		{
			var upgrader = new Upgrader(new CspMigrationPlan());
			upgrader.Execute(GetRequiredService<IMigrationPlanExecutor>(), ScopeProvider, Mock.Of<IKeyValueService>());
		}

		_sud = new CspService(GetRequiredService<IEventAggregator>(),
			ScopeProvider,
			AppCaches);
	}

	[Test]
	[TestCase(false)]
	[TestCase(true)]
	public void GetCspDefinition_ReturnsDefaultDefinitionIfNoneIsStored(bool isBackOffice)
	{
		var definition = _sud.GetCspDefinition(isBackOffice);
		definition.Should().NotBeNull();
		definition.IsBackOffice.Should().Be(isBackOffice);
		definition.Enabled.Should().BeFalse();
		definition.Id.Should().Be(isBackOffice ? CspConstants.DefaultBackofficeId : CspConstants.DefaultFrontEndId);
	}

	[Test]
	public void GetCspDefinition_ReturnsStoredDefinition()
	{
		CspDefinition storedDefinition = new()
		{
			Id = CspConstants.DefaultBackofficeId,
			Enabled = true,
			IsBackOffice = true,
			Sources = CspConstants.DefaultBackOfficeCsp
		};

		using (var scope = ScopeProvider.CreateScope(autoComplete: true))
		{
			scope.Database.Save(storedDefinition);
		}

		var definition = _sud.GetCspDefinition(true);
		definition.Should().BeEquivalentTo(storedDefinition);
	}

	[Test]
	[TestCase(false, true)]
	[TestCase(false, false)]
	[TestCase(true, true)]
	[TestCase(true, false)]
	public void GetCachedCspDefinition(bool isBackOffice, bool cached)
	{
		CspDefinition cachedDefinition = new() { Enabled = true, IsBackOffice = isBackOffice, Id = Guid.NewGuid() };

		if (cached)
		{
			AppCaches.RuntimeCache.Insert(
				isBackOffice ? CspConstants.BackOfficeCacheKey : CspConstants.FrontEndCacheKey,
				() => cachedDefinition);
		}

		var definition = _sud.GetCachedCspDefinition(isBackOffice);

		definition.Should().NotBeNull();
		definition.IsBackOffice.Should().Be(isBackOffice);

		if (cached)
		{
			definition.Should().BeEquivalentTo(cachedDefinition);
			return;
		}

		definition.Should().NotBeEquivalentTo(cachedDefinition);
	}

	[Test]
	[TestCaseSource(nameof(SaveCspDefinitionSource))]
	public async Task SaveCspDefinitionAsync_HandlesSourceChanges(CspDefinition definition)
	{
		var result = await _sud.SaveCspDefinitionAsync(definition);
		result.Should().BeEquivalentTo(definition);

		using var scope = ScopeProvider.CreateScope(autoComplete: true);

		var data = scope.Database.FetchOneToMany<CspDefinition>(
			c => c.Sources,
			scope.SqlContext.Sql()
				.Select("*")
				.From<CspDefinition>()
				.LeftJoin<CspDefinitionSource>()
				.On<CspDefinition, CspDefinitionSource>((d, s) => d.Id == s.DefinitionId)
				.Where<CspDefinition>(x => x.IsBackOffice == definition.IsBackOffice)
		).FirstOrDefault();

		data.Should().BeEquivalentTo(definition);
	}
	
	[Test]
	public async Task SaveCspDefinitionAsync_PublishesCspSavedNotification()
	{
		var definition = new CspDefinition
		{
			Id = CspConstants.DefaultBackofficeId,
			Enabled = true,
			IsBackOffice = true,
			Sources = CspConstants.DefaultBackOfficeCsp.GetRange(0, CspConstants.DefaultBackOfficeCsp.Count - 1)
		};
		
		await _sud.SaveCspDefinitionAsync(definition);
		Mock.Get(EventAggregator).Verify( x => x.PublishAsync(
			It.Is<CspSavedNotification>(n => n.CspDefinition == definition),
			It.IsAny<CancellationToken>()), Times.Once);
	}
	
	public static IEnumerable<TestCaseData> SaveCspDefinitionSource
	{
		get
		{
			var oneLessSource = new CspDefinition
			{
				Id = CspConstants.DefaultBackofficeId,
				Enabled = true,
				IsBackOffice = true,
				Sources = CspConstants.DefaultBackOfficeCsp.GetRange(0, CspConstants.DefaultBackOfficeCsp.Count - 1)
			};

			yield return new TestCaseData(oneLessSource) { TestName = "Remove a CSP Source from a Definition" };

			var additionalSource = CspConstants.DefaultBackOfficeCsp.ToList();
			additionalSource.Add(new CspDefinitionSource
			{
				DefinitionId = CspConstants.DefaultBackofficeId,
				Directives = new() { CspConstants.Directives.BaseUri },
				Source = "test"
			});

			yield return new TestCaseData(new CspDefinition
			{
				Id = CspConstants.DefaultBackofficeId,
				Enabled = true,
				IsBackOffice = true,
				Sources = additionalSource
			}) { TestName = "Add a CSP Source to a Definition" };
		}
	}
}
