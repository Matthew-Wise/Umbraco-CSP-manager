namespace Umbraco.Community.CSPManager.Tests.Middleware;

using System.Collections.Generic;
using System.Threading.Tasks;
using Cms.Core;
using Cms.Core.Configuration.Models;
using Cms.Core.Events;
using Cms.Core.Routing;
using Cms.Core.Services;
using Cms.Tests.Integration.Implementations;
using CSPManager.Middleware;
using CSPManager.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Models;
using Notifications;
using IHostingEnvironment = Cms.Core.Hosting.IHostingEnvironment;

[TestFixture]
public class CspMiddlewareTests
{
	private IHost _host;

	private ICspService _cspService;

	private IEventAggregator _eventAggregator;

	private IRuntimeState _runtimeState;
	private IRuntime _runtime;
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
		_runtimeState = Mock.Of<IRuntimeState>(x => x.Level == RuntimeLevel.Run);
		_runtime = Mock.Of<IRuntime>(x => x.State == _runtimeState);

		_host = new HostBuilder()
			.ConfigureWebHost(webBuilder =>
			{
				webBuilder
					.UseTestServer()
					.ConfigureServices(services =>
					{
						services.AddSingleton(_ => _cspService);
						services.AddSingleton(_ => _eventAggregator);
						services.AddSingleton(_ => _runtimeState);

						services.AddSingleton(_ => TestHelper.GetHostingEnvironment());
#if NET6_0
						services.AddTransient(sp => new UmbracoRequestPaths(
							sp.GetRequiredService<IOptions<GlobalSettings>>(),
							sp.GetRequiredService<IHostingEnvironment>()));
#elif NET7_0
						services.AddTransient(sp => new UmbracoRequestPaths(
							sp.GetRequiredService<IOptions<GlobalSettings>>(),
							sp.GetRequiredService<IHostingEnvironment>(),
							sp.GetRequiredService<IOptions<UmbracoRequestPathsOptions>>()));
#else
						services.AddSingleton(_ => _runtime);

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
	[TestCaseSource(nameof(CspMiddlewareOnlyRunsWithRuntimeRunCases))]
	public async Task CspMiddleware_OnlyRunsWithRuntimeRun(RuntimeLevel runtimeLevel, Times verifyCalls)
	{
		Mock.Get(Services.GetRequiredService<IRuntimeState>())
			.SetupGet(x => x.Level).Returns(runtimeLevel);

		await _host.GetTestClient().GetAsync("/");
		Mock.Get(_cspService).Verify(x => x.GetCachedCspDefinition(It.IsAny<bool>()), verifyCalls);
		Mock.Get(_eventAggregator).Verify(x => x.PublishAsync(It.IsAny<CspWritingNotification>(),
			It.IsAny<CancellationToken>()), verifyCalls);
	}

	[Test]
	[TestCaseSource(nameof(CspMiddlewareReturnsExpectedCspWhenEnabledCases))]
	public async Task CspMiddleware_ReturnsExpectedCspWhenEnabled(string uri, CspDefinition definition)
	{
		Mock.Get(_cspService).Setup(x => x.GetCachedCspDefinition(It.IsAny<bool>())).Returns(definition);

		var response = await _host.GetTestClient().GetAsync(uri);
		if (definition.Enabled)
		{
#if NET6_0 || NET7_0
			await Verify(response.Headers)
				.UseFileName(
					$"{nameof(CspMiddleware_ReturnsExpectedCspWhenEnabled)}_{TestContext.CurrentContext.Test.Name}.DotNet7_0");
#else
			await Verify(response.Headers)
				.UseFileName(
					$"{nameof(CspMiddleware_ReturnsExpectedCspWhenEnabled)}_{TestContext.CurrentContext.Test.Name}");
#endif
		}
		else
		{
			response.Headers.Should().NotContainKey(CspConstants.HeaderName);
		}
	}

	private static IEnumerable<TestCaseData> CspMiddlewareReturnsExpectedCspWhenEnabledCases
	{
		get
		{
			yield return new TestCaseData("/umbraco",
				new CspDefinition
				{
					Id = CspConstants.DefaultBackofficeId,
					Enabled = true,
					IsBackOffice = true,
					Sources = CspConstants.DefaultBackOfficeCsp
				})
			{ TestName = "Backoffice enabled" };

			yield return new TestCaseData("/umbraco",
				new CspDefinition
				{
					Id = CspConstants.DefaultBackofficeId,
					Enabled = true,
					IsBackOffice = true,
					ReportOnly = true,
					Sources = CspConstants.DefaultBackOfficeCsp
				})
			{ TestName = "Backoffice Report Only" };

			yield return new TestCaseData("/umbraco",
				new CspDefinition
				{
					Id = CspConstants.DefaultBackofficeId,
					Enabled = false,
					IsBackOffice = true,
					Sources = CspConstants.DefaultBackOfficeCsp
				})
			{ TestName = "Backoffice disabled" };
		}
	}

	public static IEnumerable<TestCaseData> CspMiddlewareOnlyRunsWithRuntimeRunCases
	{
		get
		{
			yield return new TestCaseData(RuntimeLevel.Run, Times.Once());
			yield return new TestCaseData(RuntimeLevel.Install, Times.Never());
			yield return new TestCaseData(RuntimeLevel.Upgrade, Times.Never());
			yield return new TestCaseData(RuntimeLevel.Boot, Times.Never());
			yield return new TestCaseData(RuntimeLevel.BootFailed, Times.Never());
			yield return new TestCaseData(RuntimeLevel.Unknown, Times.Never());
		}
	}

	[TearDown]
	public void TearDownAsync() => _host.StopAsync();
}
