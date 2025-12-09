using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Integration.Implementations;
using Umbraco.Community.CSPManager.Middleware;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Services;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Middleware;

[TestFixture]
public class CspMiddlewareTests
{
	private IHost _host;

	private ICspService _cspService;

	private IEventAggregator _eventAggregator;
	private static Dictionary<string, string> InMemoryConfiguration => [];

	private TestHelper TestHelper { get; } = new();

	private IServiceProvider Services => _host.Services;

	[SetUp]
	public void SetUp()
	{
		InMemoryConfiguration[
			UmbConstants.Configuration.ConfigUnattended + ":" + nameof(UnattendedSettings.InstallUnattended)] = "true";
		_cspService = Mock.Of<ICspService>();
		_eventAggregator = Mock.Of<IEventAggregator>();

		var runtimeState = Mock.Of<IRuntimeState>(x => x.Level == RuntimeLevel.Run);
		var runtime = Mock.Of<IRuntime>(x => x.State == runtimeState);

		_host = new HostBuilder()
			.ConfigureWebHost(webBuilder =>
			{
				webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddSingleton(_ => _cspService);
						services.AddSingleton(_ => _eventAggregator);
						services.AddSingleton(_ => runtimeState);
						services.AddSingleton(_ => runtime);
						services.AddSingleton(_ => TestHelper.GetHostingEnvironment());
						services.AddSingleton<IUmbracoVersion, UmbracoVersion>();
						services.AddTransient(sp => new UmbracoRequestPaths(
							TestHelper.GetHostingEnvironment(),
							sp.GetRequiredService<IOptions<UmbracoRequestPathsOptions>>()));
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

		await _host.GetTestClient().GetAsync("/", HttpCompletionOption.ResponseHeadersRead);
		Mock.Get(_cspService).Verify(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), verifyCalls);
		Mock.Get(_eventAggregator).Verify(x => x.PublishAsync(It.IsAny<CspWritingNotification>(),
			It.IsAny<CancellationToken>()), verifyCalls);
	}

	[Test]
	[TestCaseSource(typeof(MiddlewareTestCases), nameof(MiddlewareTestCases.CspMiddlewareReturnsExpectedCspWhenEnabledCases))]
	public async Task CspMiddleware_ReturnsExpectedCspWhenEnabled(string uri, CspDefinition definition)
	{
		Mock.Get(_cspService).Setup(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(definition);

		var response = await _host.GetTestClient().GetAsync(uri);
		if (definition.Enabled)
		{
			string expectedHeaderName = definition.ReportOnly
				? Constants.ReportOnlyHeaderName
				: Constants.HeaderName;

			Assert.That(response.Headers.Contains(expectedHeaderName), Is.True);
			var headerValues = response.Headers.GetValues(expectedHeaderName).FirstOrDefault();
			Assert.That(headerValues, Is.Not.Null);

			var expectedCsp = "default-src 'self' marketplace.umbraco.com our.umbraco.com;script-src 'self' 'unsafe-inline' 'unsafe-eval';style-src 'self' 'unsafe-inline';img-src 'self' our.umbraco.com data: dashboard.umbraco.com;font-src 'self'";
			Assert.That(headerValues, Is.EqualTo(expectedCsp));
		}
		else
		{
			Assert.That(response.Headers.Contains(Constants.HeaderName), Is.False);
		}
	}

	[TearDown]
	public async Task TearDownAsync()
	{
		await _host.StopAsync();
		_host.Dispose();
	}
}