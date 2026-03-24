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