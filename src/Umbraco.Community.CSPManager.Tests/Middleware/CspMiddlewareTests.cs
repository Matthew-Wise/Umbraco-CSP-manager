namespace Umbraco.Community.CSPManager.Tests.Middleware;

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Integration.Implementations;
using Umbraco.Community.CSPManager.Middleware;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;

using IHostingEnvironment = Umbraco.Cms.Core.Hosting.IHostingEnvironment;

[TestFixture]
public class CspMiddlewareTests
{
	private IHost _host;

	private ICspService _cspService;

	private IEventAggregator _eventAggregator;
	private static Dictionary<string, string> InMemoryConfiguration => new();

	private TestHelper TestHelper { get; } = new();

	private IServiceProvider Services => _host.Services;

	[SetUp]
	public void SetUp()
	{
		InMemoryConfiguration[
			Constants.Configuration.ConfigUnattended + ":" + nameof(UnattendedSettings.InstallUnattended)] = "true";
		_cspService = Mock.Of<ICspService>();
		_eventAggregator = Mock.Of<IEventAggregator>();
		_host = new HostBuilder()
			.ConfigureWebHost(webBuilder =>
			{
				webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddSingleton(_ => _cspService);
						services.AddSingleton(_ => _eventAggregator);
						services.AddSingleton(_ => Mock.Of<IRuntimeState>(x => x.Level == RuntimeLevel.Run));
						services.AddSingleton(_ => TestHelper.GetHostingEnvironment());
#if NET6_0
						services.AddTransient(sp => new UmbracoRequestPaths(
							sp.GetRequiredService<IOptions<GlobalSettings>>(),
							sp.GetRequiredService<IHostingEnvironment>()));
#else
						services.AddTransient(sp => new UmbracoRequestPaths(
							sp.GetRequiredService<IOptions<GlobalSettings>>(),
							sp.GetRequiredService<IHostingEnvironment>(),
							sp.GetRequiredService<IOptions<UmbracoRequestPathsOptions>>()));
#endif
					})
					.Configure(app =>
					{
						app.UseMiddleware<CspMiddleware>();
					})
					.ConfigureAppConfiguration((context, configBuilder) =>
					{
						context.HostingEnvironment = TestHelper.GetWebHostEnvironment();
						configBuilder.Sources.Clear();
						configBuilder.AddInMemoryCollection(InMemoryConfiguration);
					});
			}).ConfigureUmbracoDefaults()
			.Start();
	}

	[Test]
	[TestCaseSource(typeof(MiddlewareTestCases), nameof(MiddlewareTestCases.CspMiddlewareOnlyRunsWithRuntimeRunCases))]
	public async Task CspMiddleware_OnlyRunsWithRuntimeRun(RuntimeLevel runtimeLevel, Times verifyCalls)
	{
		Mock.Get(Services.GetRequiredService<IRuntimeState>())
			.SetupGet(x => x.Level).Returns(runtimeLevel);

		await _host.GetTestClient().GetAsync("/",	HttpCompletionOption.ResponseHeadersRead);
		Mock.Get(_cspService).Verify(x => x.GetCachedCspDefinition(It.IsAny<bool>()), verifyCalls);
		Mock.Get(_eventAggregator).Verify(x => x.PublishAsync(It.IsAny<CspWritingNotification>(),
			It.IsAny<CancellationToken>()), verifyCalls);
	}

	[Test]
	[TestCaseSource(typeof(MiddlewareTestCases), nameof(MiddlewareTestCases.CspMiddlewareReturnsExpectedCspWhenEnabledCases))]
	public async Task CspMiddleware_ReturnsExpectedCspWhenEnabled(string uri, CspDefinition definition)
	{
		Mock.Get(_cspService).Setup(x => x.GetCachedCspDefinition(It.IsAny<bool>())).Returns(definition);

		var response = await _host.GetTestClient().GetAsync(uri);
		if (definition.Enabled)
		{
			await Verify(response.Headers)
				.UseDirectory(nameof(CspMiddleware_ReturnsExpectedCspWhenEnabled))
				.UseFileName(
					$"{TestContext.CurrentContext.Test.Name}");
		}
		else
		{
			response.Headers.Should().NotContainKey(CspConstants.HeaderName);
		}
	}

	[TearDown]
	public void TearDownAsync() => _host.StopAsync();
}
