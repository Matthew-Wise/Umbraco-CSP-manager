using Umbraco.Cms.Core;
using Umbraco.Community.CSPManager.Models;
using CspConstants = Umbraco.Community.CSPManager.Constants;

namespace Umbraco.Community.CSPManager.Tests.Middleware;

internal static class MiddlewareTestCases
{
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
			yield return new TestCaseData(RuntimeLevel.Run, Times.Once());
			yield return new TestCaseData(RuntimeLevel.Install, Times.Never());
			yield return new TestCaseData(RuntimeLevel.Upgrade, Times.Never());
			yield return new TestCaseData(RuntimeLevel.Boot, Times.Never());
			yield return new TestCaseData(RuntimeLevel.BootFailed, Times.Never());
			yield return new TestCaseData(RuntimeLevel.Unknown, Times.Never());
		}
	}
}
