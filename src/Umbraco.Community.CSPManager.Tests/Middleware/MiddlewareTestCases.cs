using Umbraco.Cms.Core;
using Umbraco.Community.CSPManager.Models;
using CspConstants = Umbraco.Community.CSPManager.Constants;

namespace Umbraco.Community.CSPManager.Tests.Middleware;

internal static class MiddlewareTestCases
{
	public static IEnumerable<TestCaseData> CspMiddlewareHeaderContentCases
	{
		get
		{
			yield return new TestCaseData(
				"/",
				new CspDefinition
				{
					Id = CspConstants.DefaultFrontEndId,
					Enabled = true,
					IsBackOffice = false,
					Sources = [new CspDefinitionSource { Source = "'self'", Directives = [CspConstants.Directives.DefaultSource] }]
				},
				CspConstants.HeaderName,
				"default-src 'self'")
			{ TestName = "Frontend enabled - CSP header set with correct value" };

			yield return new TestCaseData(
				"/umbraco",
				new CspDefinition
				{
					Id = CspConstants.DefaultBackofficeId,
					Enabled = true,
					IsBackOffice = true,
					UpgradeInsecureRequests = true,
					Sources = [new CspDefinitionSource { Source = "'self'", Directives = [CspConstants.Directives.DefaultSource] }]
				},
				CspConstants.HeaderName,
				"default-src 'self';upgrade-insecure-requests")
			{ TestName = "UpgradeInsecureRequests - directive included in header" };

			yield return new TestCaseData(
				"/umbraco",
				new CspDefinition
				{
					Id = CspConstants.DefaultBackofficeId,
					Enabled = true,
					IsBackOffice = true,
					ReportingDirective = CspConstants.ReportingDirectives.ReportTo,
					ReportUri = "https://report.example.com",
					Sources = [new CspDefinitionSource { Source = "'self'", Directives = [CspConstants.Directives.DefaultSource] }]
				},
				CspConstants.HeaderName,
				"default-src 'self';report-to https://report.example.com")
			{ TestName = "ReportingDirective and ReportUri - included in header" };

			yield return new TestCaseData(
				"/umbraco",
				new CspDefinition
				{
					Id = CspConstants.DefaultBackofficeId,
					Enabled = true,
					IsBackOffice = true,
					Sources =
					[
						new CspDefinitionSource { Source = "'self'", Directives = [CspConstants.Directives.DefaultSource] },
						new CspDefinitionSource { Source = "'self'", Directives = [CspConstants.Directives.DefaultSource] }
					]
				},
				CspConstants.HeaderName,
				"default-src 'self'")
			{ TestName = "Duplicate source - deduplicated in header" };

			// Dedupe must compare whole tokens: a substring check silently dropped "example.com"
			// because "cdn.example.com" already contained it.
			yield return new TestCaseData(
				"/",
				new CspDefinition
				{
					Id = CspConstants.DefaultFrontEndId,
					Enabled = true,
					IsBackOffice = false,
					Sources =
					[
						new CspDefinitionSource { Source = "https://cdn.example.com", Directives = [CspConstants.Directives.ScriptSource] },
						new CspDefinitionSource { Source = "example.com", Directives = [CspConstants.Directives.ScriptSource] },
						new CspDefinitionSource { Source = "'self'", Directives = [CspConstants.Directives.ScriptSource] }
					]
				},
				CspConstants.HeaderName,
				"script-src https://cdn.example.com example.com 'self'")
			{ TestName = "Source that is a substring of an earlier source - both emitted" };
		}
	}

	public static IEnumerable<TestCaseData> CspMiddlewareReturnsExpectedCspWhenEnabledCases
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

	// Which CSP definition a path is matched to. Shared by the middleware tests and the
	// BackOfficePathMatcher unit tests so the two stay in step.
	public static IEnumerable<TestCaseData> BackOfficeMatchingCases
	{
		get
		{
			// Backoffice: the shell, its deep links, and the backoffice APIs.
			yield return Case("/umbraco", true);
			yield return Case("/umbraco/", true);
			yield return Case("/umbraco/login", true);
			yield return Case("/umbraco/oauth_complete", true);
			yield return Case("/umbraco/section/content/workspace/document/edit/1", true);
			yield return Case("/UMBRACO/Section/content", true);
			yield return Case("/umbraco/preview", true);
			yield return Case("/umbraco/backoffice/x", true);
			yield return Case("/umbraco/management/api/v1/document", true);
			yield return Case("/umbraco/openapi/index.html", true);

			// PluginController routes sit under /umbraco too, so they keep the backoffice policy.
			yield return Case("/umbraco/MyPlugin/Ctrl/Action", true);

			// Front end: Umbraco's public areas under /umbraco, and content URLs that merely start with "umbraco".
			yield return Case("/umbraco/surface/Contact/Submit", false);
			yield return Case("/umbraco/api/MyApi/Get", false);
			yield return Case("/umbraco/delivery/api/v2/content", false);
			yield return Case("/umbraco-partners", false);
			yield return Case("/umbraco-partners/sub-page", false);
			yield return Case("/umbracofoo", false);
			yield return Case("/", false);
			yield return Case("/about-us", false);

			static TestCaseData Case(string path, bool isBackOffice) =>
				new TestCaseData(path, isBackOffice).SetArgDisplayNames(path, isBackOffice ? "backoffice" : "frontend");
		}
	}

	public static IEnumerable<TestCaseData> CspMiddlewareOnlyRunsWithRuntimeRunCases
	{
		get
		{
			yield return new TestCaseData(RuntimeLevel.Run, Times.Once()).SetName("RunTimeLevel Run Middleware runs Once");
			yield return new TestCaseData(RuntimeLevel.Install, Times.Never()).SetName("RunTimeLevel Install Middleware runs Never");
			yield return new TestCaseData(RuntimeLevel.Upgrade, Times.Never()).SetName("RunTimeLevel Upgrade Middleware runs Never");
			yield return new TestCaseData(RuntimeLevel.Boot, Times.Never()).SetName("RunTimeLevel Boot Middleware runs Never");
			yield return new TestCaseData(RuntimeLevel.BootFailed, Times.Never()).SetName("RunTimeLevel BootFailed Middleware runs Never");
			yield return new TestCaseData(RuntimeLevel.Unknown, Times.Never()).SetName("RunTimeLevel Unknown Middleware runs Never");
		}
	}
}