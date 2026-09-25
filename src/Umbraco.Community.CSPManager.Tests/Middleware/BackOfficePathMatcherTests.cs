using Microsoft.AspNetCore.Http;
using Umbraco.Community.CSPManager.Middleware;

namespace Umbraco.Community.CSPManager.Tests.Middleware;

[TestFixture]
public class BackOfficePathMatcherTests
{
	[Test]
	[TestCaseSource(typeof(MiddlewareTestCases), nameof(MiddlewareTestCases.BackOfficeMatchingCases))]
	public void IsBackOfficeRequest_MatchesExpectedContext(string path, bool expectedIsBackOffice)
		=> Assert.That(BackOfficePathMatcher.IsBackOfficeRequest(new PathString(path)), Is.EqualTo(expectedIsBackOffice));
}
