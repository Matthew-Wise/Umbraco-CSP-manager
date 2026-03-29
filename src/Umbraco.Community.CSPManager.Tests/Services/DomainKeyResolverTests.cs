using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Tests.Services;

[TestFixture]
public class DomainKeyResolverTests
{
	private static (IServiceScopeFactory factory, Mock<IDomainService> domainService) CreateScopeFactory(
		IEnumerable<IDomain> domains)
	{
		var domainServiceMock = new Mock<IDomainService>();
		domainServiceMock.Setup(x => x.GetAllAsync(false)).ReturnsAsync(domains);

		var serviceProvider = new Mock<IServiceProvider>();
		serviceProvider.Setup(x => x.GetService(typeof(IDomainService))).Returns(domainServiceMock.Object);

		var scope = new Mock<IServiceScope>();
		scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

		var factory = new Mock<IServiceScopeFactory>();
		factory.Setup(x => x.CreateScope()).Returns(scope.Object);

		return (factory.Object, domainServiceMock);
	}

	private static AppCaches CreateAppCaches(IAppPolicyCache? runtimeCache = null)
		=> new(
			runtimeCache ?? NoAppCache.Instance,
			Mock.Of<IRequestCache>(),
			new IsolatedCaches(_ => NoAppCache.Instance));

	private static IDomain MakeDomain(int id, Guid key, string name)
		=> Mock.Of<IDomain>(d => d.Id == id && d.Key == key && d.DomainName == name);

	[Test]
	public async Task ResolveKeyAsync_WithKnownDomainId_ReturnsGuid()
	{
		var domainGuid = Guid.NewGuid();
		var (factory, _) = CreateScopeFactory([MakeDomain(1, domainGuid, "example.com")]);
		var resolver = new DomainKeyResolver(factory, CreateAppCaches());

		var result = await resolver.ResolveKeyAsync(1);

		Assert.That(result, Is.EqualTo(domainGuid));
	}

	[Test]
	public async Task ResolveKeyAsync_WithUnknownDomainId_ReturnsNull()
	{
		var (factory, _) = CreateScopeFactory([MakeDomain(1, Guid.NewGuid(), "example.com")]);
		var resolver = new DomainKeyResolver(factory, CreateAppCaches());

		var result = await resolver.ResolveKeyAsync(999);

		Assert.That(result, Is.Null);
	}

	[Test]
	public async Task ResolveIdAsync_WithKnownDomainKey_ReturnsId()
	{
		var domainGuid = Guid.NewGuid();
		var (factory, _) = CreateScopeFactory([MakeDomain(42, domainGuid, "example.com")]);
		var resolver = new DomainKeyResolver(factory, CreateAppCaches());

		var result = await resolver.ResolveIdAsync(domainGuid);

		Assert.That(result, Is.EqualTo(42));
	}

	[Test]
	public async Task ResolveIdAsync_WithUnknownDomainKey_ReturnsNull()
	{
		var (factory, _) = CreateScopeFactory([MakeDomain(1, Guid.NewGuid(), "example.com")]);
		var resolver = new DomainKeyResolver(factory, CreateAppCaches());

		var result = await resolver.ResolveIdAsync(Guid.NewGuid());

		Assert.That(result, Is.Null);
	}

	[Test]
	public async Task GetDomainNamesAsync_ReturnsMappingOfKeyToName()
	{
		var guid1 = Guid.NewGuid();
		var guid2 = Guid.NewGuid();
		var (factory, _) = CreateScopeFactory(
		[
			MakeDomain(1, guid1, "site-a.com"),
			MakeDomain(2, guid2, "site-b.com")
		]);
		var resolver = new DomainKeyResolver(factory, CreateAppCaches());

		var names = await resolver.GetDomainNamesAsync();

		Assert.Multiple(() =>
		{
			Assert.That(names, Contains.Key(guid1));
			Assert.That(names[guid1], Is.EqualTo("site-a.com"));
			Assert.That(names, Contains.Key(guid2));
			Assert.That(names[guid2], Is.EqualTo("site-b.com"));
		});
	}

	[Test]
	public async Task GetDomainNamesAsync_ExcludesDomainsWithNullName()
	{
		var guidWithName = Guid.NewGuid();
		var guidWithoutName = Guid.NewGuid();
		var domainWithNull = Mock.Of<IDomain>(d => d.Id == 2 && d.Key == guidWithoutName);
		var (factory, _) = CreateScopeFactory(
		[
			MakeDomain(1, guidWithName, "example.com"),
			domainWithNull
		]);
		var resolver = new DomainKeyResolver(factory, CreateAppCaches());

		var names = await resolver.GetDomainNamesAsync();

		Assert.Multiple(() =>
		{
			Assert.That(names, Contains.Key(guidWithName));
			Assert.That(names, Does.Not.ContainKey(guidWithoutName));
		});
	}

	[Test]
	public async Task BuildMapping_WithNoDomains_ReturnsEmptyMapping()
	{
		var (factory, _) = CreateScopeFactory([]);
		var resolver = new DomainKeyResolver(factory, CreateAppCaches());

		var key = await resolver.ResolveKeyAsync(1);
		var id = await resolver.ResolveIdAsync(Guid.NewGuid());
		var names = await resolver.GetDomainNamesAsync();

		Assert.Multiple(() =>
		{
			Assert.That(key, Is.Null);
			Assert.That(id, Is.Null);
			Assert.That(names, Is.Empty);
		});
	}

	[Test]
	public void ClearCache_RemovesCachedMapping()
	{
		var mockCache = new Mock<IAppPolicyCache>();
		var (factory, _) = CreateScopeFactory([]);
		var resolver = new DomainKeyResolver(factory, CreateAppCaches(mockCache.Object));

		resolver.ClearCache();

		mockCache.Verify(c => c.ClearByKey(Constants.DomainIdMappingCacheKey), Times.Once);
	}

	[Test]
	public async Task Mapping_IsCachedAcrossMultipleCalls()
	{
		var domainGuid = Guid.NewGuid();
		var (factory, domainService) = CreateScopeFactory([MakeDomain(1, domainGuid, "example.com")]);

		// Spy cache that stores items in a dictionary
		var cacheStore = new Dictionary<string, object>();
		var mockCache = new Mock<IAppPolicyCache>();
		mockCache.Setup(x => x.Get(It.IsAny<string>()))
			.Returns((string key) => cacheStore.TryGetValue(key, out var v) ? v : null);
		mockCache.Setup(x => x.Insert(
				It.IsAny<string>(), It.IsAny<Func<object>>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>()))
			.Callback<string, Func<object>, TimeSpan?, bool>(
				(key, factoryFn, _, _) => cacheStore[key] = factoryFn());

		var resolver = new DomainKeyResolver(factory, CreateAppCaches(mockCache.Object));

		// Call twice — domain service should only be queried once
		await resolver.ResolveKeyAsync(1);
		await resolver.ResolveKeyAsync(1);

		domainService.Verify(s => s.GetAllAsync(false), Times.Once);
	}
}
