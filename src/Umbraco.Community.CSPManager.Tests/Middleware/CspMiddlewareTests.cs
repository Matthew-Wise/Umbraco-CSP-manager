using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
		_host = BuildTestHost();
	}

	private IHost BuildTestHost(
		Action<IServiceCollection> extraServices = null,
		Action<IApplicationBuilder> extraApp = null)
	{
		var runtimeState = Mock.Of<IRuntimeState>(x => x.Level == RuntimeLevel.Run);
		var runtime = Mock.Of<IRuntime>(x => x.State == runtimeState);

		return new HostBuilder()
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
						extraServices?.Invoke(services);
						services.Configure<ImagingSettings>(options =>
						{
							if (options.HMACSecretKey.Length == 0)
							{
								byte[] secret = new byte[64];
								RandomNumberGenerator.Fill(secret);
								options.HMACSecretKey = secret;
							}
						});
					})
					.Configure(app =>
					{
						app.UseMiddleware<CspMiddleware>();
						extraApp?.Invoke(app);
					})
					.ConfigureAppConfiguration((context, configBuilder) =>
					{
						context.HostingEnvironment = TestHelper.GetWebHostEnvironment();
						configBuilder.Sources.Clear();
						configBuilder.AddInMemoryCollection(InMemoryConfiguration);
					});
			})
			.ConfigureUmbracoDefaults()
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

	[Test]
	[TestCaseSource(typeof(MiddlewareTestCases), nameof(MiddlewareTestCases.CspMiddlewareHeaderContentCases))]
	public async Task CspMiddleware_ReturnsExpectedCspHeaderContent(string uri, CspDefinition definition, string expectedHeaderName, string expectedHeaderValue)
	{
		Mock.Get(_cspService)
			.Setup(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(definition);

		var response = await _host.GetTestClient().GetAsync(uri);

		Assert.That(response.Headers.Contains(expectedHeaderName), Is.True);
		var headerValue = response.Headers.GetValues(expectedHeaderName).FirstOrDefault();
		Assert.That(headerValue, Is.EqualTo(expectedHeaderValue));
	}

	[Test]
	public async Task CspMiddleware_WithDisableBackOfficeHeader_DoesNotSetHeaderForBackofficeRequest()
	{
		var definition = new CspDefinition { Enabled = true, IsBackOffice = true, Sources = Constants.DefaultBackOfficeCsp };
		Mock.Get(_cspService)
			.Setup(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(definition);

		using var host = BuildTestHost(
			extraServices: s => s.Configure<CspManagerOptions>(o => o.DisableBackOfficeHeader = true));

		var response = await host.GetTestClient().GetAsync("/umbraco");

		Assert.Multiple(() =>
		{
			Assert.That(response.Headers.Contains(Constants.HeaderName), Is.False);
			Assert.That(response.Headers.Contains(Constants.ReportOnlyHeaderName), Is.False);
		});
	}

	[Test]
	public async Task CspMiddleware_WithScriptNonceSetInItems_InjectsNonceIntoScriptSrc()
	{
		const string testNonce = "test-nonce-abc123";
		var definition = new CspDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Enabled = true,
			IsBackOffice = false,
			Sources = [new CspDefinitionSource { Source = "'self'", Directives = [Constants.Directives.ScriptSource] }]
		};
		Mock.Get(_cspService)
			.Setup(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(definition);
		Mock.Get(_cspService)
			.Setup(x => x.GetOrCreateCspNonce(It.IsAny<HttpContext>()))
			.Returns(testNonce);

		using var host = BuildTestHost(extraApp: app =>
		{
			app.Use(async (ctx, next) =>
			{
				ctx.Items[Constants.TagHelper.CspManagerScriptNonceSet] = true;
				await next(ctx);
			});
		});

		var response = await host.GetTestClient().GetAsync("/");

		Assert.That(response.Headers.Contains(Constants.HeaderName), Is.True);
		var headerValue = response.Headers.GetValues(Constants.HeaderName).First();
		Assert.That(headerValue, Does.Contain($"'nonce-{testNonce}'"));
	}

	[Test]
	public async Task CspMiddleware_WhenServiceThrows_RequestCompletesWithoutCspHeader()
	{
		Mock.Get(_cspService)
			.Setup(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Test exception"));

		// Exception is swallowed in OnStarting — request completes without throwing
		var response = await _host.GetTestClient().GetAsync("/");

		Assert.Multiple(() =>
		{
			Assert.That(response.Headers.Contains(Constants.HeaderName), Is.False);
			Assert.That(response.Headers.Contains(Constants.ReportOnlyHeaderName), Is.False);
		});
	}

	[TearDown]
	public async Task TearDownAsync()
	{
		await _host.StopAsync();
		_host.Dispose();
	}
}