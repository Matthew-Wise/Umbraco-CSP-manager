using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
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

	// ICspService.GetCachedCspDefinitionAsync returns a defensive copy on every call
	// (CspService.CloneDefinition) precisely so that CspWritingNotification handlers can freely
	// mutate notification.CspDefinition - as the documented pattern in
	// docs/advanced/notification-events.md does - without corrupting what other requests get
	// served from the shared cache. This test mocks GetCachedCspDefinitionAsync to hand out a
	// fresh clone per call, matching that contract, and asserts the middleware/notification
	// pipeline keeps a handler's mutation scoped to its own request.
	[Test]
	public async Task CspMiddleware_WritingNotificationHandlerMutatesDefinition_DoesNotLeakIntoUnrelatedRequests()
	{
		Mock.Get(_cspService)
			.Setup(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new CspDefinition
			{
				Id = Constants.DefaultFrontEndId,
				Enabled = true,
				IsBackOffice = false,
				Sources = [new CspDefinitionSource { Source = "'self'", Directives = [Constants.Directives.DefaultSource] }]
			});

		Mock.Get(_eventAggregator)
			.Setup(x => x.PublishAsync(It.IsAny<CspWritingNotification>(), It.IsAny<CancellationToken>()))
			.Callback<INotification, CancellationToken>((n, _) =>
			{
				var notification = (CspWritingNotification)n;
				if (notification.HttpContext.Request.Path.StartsWithSegments("/api"))
				{
					notification.CspDefinition!.Sources.Add(new CspDefinitionSource
					{
						Source = "api.example.com",
						Directives = [Constants.Directives.ConnectSource]
					});
				}
			})
			.Returns(Task.CompletedTask);

		var apiResponse = await _host.GetTestClient().GetAsync("/api/orders");
		var apiHeader = apiResponse.Headers.GetValues(Constants.HeaderName).First();
		Assert.That(apiHeader, Does.Contain("api.example.com"));

		var unrelatedResponse = await _host.GetTestClient().GetAsync("/content/page");
		var unrelatedHeader = unrelatedResponse.Headers.GetValues(Constants.HeaderName).First();

		Assert.That(unrelatedHeader, Does.Not.Contain("api.example.com"),
			"a handler scoped to /api requests must not leak its source into unrelated requests");
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

	// script-src-elem overrides script-src for <script> elements, so a nonce added to script-src
	// would never be consulted by the browser and every tagged inline script would be blocked.
	[Test]
	public async Task CspMiddleware_WithScriptSourceElementConfigured_InjectsNonceIntoScriptSrcElemOnly()
	{
		const string testNonce = "test-nonce-abc123";
		var definition = new CspDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Enabled = true,
			IsBackOffice = false,
			Sources =
			[
				new CspDefinitionSource
				{
					Source = "'self'",
					Directives = [Constants.Directives.ScriptSource, Constants.Directives.ScriptSourceElement]
				}
			]
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

		var headerValue = response.Headers.GetValues(Constants.HeaderName).First();
		Assert.Multiple(() =>
		{
			Assert.That(headerValue, Does.Contain($"{Constants.Directives.ScriptSourceElement} 'self' 'nonce-{testNonce}'"));
			// script-src is left untouched - a second nonce there would make the browser ignore
			// 'unsafe-inline' for the inline event handlers that fall back to it.
			Assert.That(CountOccurrences(headerValue, "'nonce-"), Is.EqualTo(1));
		});
	}

	// style-src-elem overrides style-src for <style> and <link rel="stylesheet">.
	[Test]
	public async Task CspMiddleware_WithStyleSourceElementConfigured_InjectsNonceIntoStyleSrcElemOnly()
	{
		const string testNonce = "test-nonce-abc123";
		var definition = new CspDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Enabled = true,
			IsBackOffice = false,
			Sources =
			[
				new CspDefinitionSource
				{
					Source = "'self'",
					Directives = [Constants.Directives.StyleSource, Constants.Directives.StyleSourceElement]
				}
			]
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
				ctx.Items[Constants.TagHelper.CspManagerStyleNonceSet] = true;
				await next(ctx);
			});
		});

		var response = await host.GetTestClient().GetAsync("/");

		var headerValue = response.Headers.GetValues(Constants.HeaderName).First();
		Assert.Multiple(() =>
		{
			Assert.That(headerValue, Does.Contain($"{Constants.Directives.StyleSourceElement} 'self' 'nonce-{testNonce}'"));
			Assert.That(CountOccurrences(headerValue, "'nonce-"), Is.EqualTo(1));
		});
	}

	// script-src-elem present without script-src: the nonce still has to land on the directive the
	// browser consults, and the "directive missing" warning must not fire.
	[Test]
	public async Task CspMiddleware_WithOnlyScriptSourceElementConfigured_InjectsNonceWithoutWarning()
	{
		const string testNonce = "test-nonce-abc123";
		var definition = new CspDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Enabled = true,
			IsBackOffice = false,
			Sources =
			[
				new CspDefinitionSource
				{
					Source = "'self'",
					Directives = [Constants.Directives.ScriptSourceElement]
				}
			]
		};
		Mock.Get(_cspService)
			.Setup(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(definition);
		Mock.Get(_cspService)
			.Setup(x => x.GetOrCreateCspNonce(It.IsAny<HttpContext>()))
			.Returns(testNonce);

		var logger = new Mock<ILogger<CspMiddleware>>();
		logger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

		using var host = BuildTestHost(
			extraServices: s => s.AddSingleton(logger.Object),
			extraApp: app =>
			{
				app.Use(async (ctx, next) =>
				{
					ctx.Items[Constants.TagHelper.CspManagerScriptNonceSet] = true;
					await next(ctx);
				});
			});

		var response = await host.GetTestClient().GetAsync("/");

		var headerValue = response.Headers.GetValues(Constants.HeaderName).First();
		Assert.That(headerValue, Does.Contain($"{Constants.Directives.ScriptSourceElement} 'self' 'nonce-{testNonce}'"));
		logger.Verify(x => x.Log(
			LogLevel.Warning,
			It.Is<EventId>(e => e.Name == "CspNonceDirectiveMissing"),
			It.IsAny<It.IsAnyType>(),
			null,
			It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Never);
	}

	[Test]
	public async Task CspMiddleware_WithScriptNonceSetButNoScriptSrcDirective_LogsWarningAndOmitsNonce()
	{
		const string testNonce = "test-nonce-abc123";
		var definition = new CspDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Enabled = true,
			IsBackOffice = false,
			UpgradeInsecureRequests = true,
			Sources = []
		};
		Mock.Get(_cspService)
			.Setup(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(definition);
		Mock.Get(_cspService)
			.Setup(x => x.GetOrCreateCspNonce(It.IsAny<HttpContext>()))
			.Returns(testNonce);

		var logger = new Mock<ILogger<CspMiddleware>>();
		logger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);

		using var host = BuildTestHost(
			extraServices: s => s.AddSingleton(logger.Object),
			extraApp: app =>
			{
				app.Use(async (ctx, next) =>
				{
					ctx.Items[Constants.TagHelper.CspManagerScriptNonceSet] = true;
					await next(ctx);
				});
			});

		var response = await host.GetTestClient().GetAsync("/");

		var headerValue = response.Headers.GetValues(Constants.HeaderName).First();
		Assert.That(headerValue, Does.Not.Contain("nonce"));
		logger.Verify(x => x.Log(
			LogLevel.Warning,
			It.Is<EventId>(e => e.Name == "CspNonceDirectiveMissing"),
			It.IsAny<It.IsAnyType>(),
			null,
			It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
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

	[Test]
	public async Task CspMiddleware_WhenServiceIsCancelled_RequestCompletesWithoutCspHeader()
	{
		// Regression test for #130: a TaskCanceledException escaping the OnStarting callback
		// was reported by Kestrel as "The response has been aborted due to an unhandled
		// application exception" instead of being swallowed.
		Mock.Get(_cspService)
			.Setup(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new TaskCanceledException("Request aborted"));

		var logger = new Mock<ILogger<CspMiddleware>>();
		logger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);

		using var host = BuildTestHost(extraServices: s => s.AddSingleton(logger.Object));

		var response = await host.GetTestClient().GetAsync("/");

		Assert.Multiple(() =>
		{
			Assert.That(response.Headers.Contains(Constants.HeaderName), Is.False);
			Assert.That(response.Headers.Contains(Constants.ReportOnlyHeaderName), Is.False);
		});

		logger.Verify(x => x.Log(
			LogLevel.Debug,
			It.Is<EventId>(e => e.Name == "CspHeaderCancelled"),
			It.IsAny<It.IsAnyType>(),
			null,
			It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
	}

	[Test]
	public async Task CspMiddleware_DoesNotCancelDefinitionLoadWhenClientDisconnects()
	{
		// The cached definition is shared process-wide, so a per-request abort token must not
		// be threaded into the load — one client disconnecting would fault it for everyone.
		CancellationToken observedToken = new(canceled: true);
		Mock.Get(_cspService)
			.Setup(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.Callback<bool, CancellationToken>((_, token) => observedToken = token)
			.ReturnsAsync(new CspDefinition { Id = Constants.DefaultFrontEndId, Enabled = false });

		await _host.GetTestClient().GetAsync("/");

		Assert.That(observedToken.CanBeCanceled, Is.False,
			"The definition load must not be tied to the request lifetime.");
	}

	[Test]
	public async Task CspMiddleware_WhenDefinitionIsNull_LogsNotFoundRatherThanDisabled()
	{
		Mock.Get(_cspService)
			.Setup(x => x.GetCachedCspDefinitionAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((CspDefinition)null);

		var logger = new Mock<ILogger<CspMiddleware>>();
		logger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);

		using var host = BuildTestHost(extraServices: s => s.AddSingleton(logger.Object));

		var response = await host.GetTestClient().GetAsync("/");

		Assert.That(response.Headers.Contains(Constants.HeaderName), Is.False);

		logger.Verify(x => x.Log(
			LogLevel.Debug,
			It.Is<EventId>(e => e.Name == "CspDefinitionNotFound"),
			It.IsAny<It.IsAnyType>(),
			null,
			It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
		logger.Verify(x => x.Log(
			LogLevel.Debug,
			It.Is<EventId>(e => e.Name == "CspDefinitionDisabled"),
			It.IsAny<It.IsAnyType>(),
			null,
			It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Never);
	}

	private static int CountOccurrences(string value, string needle)
	{
		var count = 0;
		var index = value.IndexOf(needle, StringComparison.Ordinal);

		while (index >= 0)
		{
			count++;
			index = value.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
		}

		return count;
	}

	[TearDown]
	public async Task TearDownAsync()
	{
		await _host.StopAsync();
		_host.Dispose();
	}
}