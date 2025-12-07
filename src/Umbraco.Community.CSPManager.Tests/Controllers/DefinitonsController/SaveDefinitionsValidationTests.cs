using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.JsonDiffPatch.Nunit;
using Umbraco.Community.CSPManager.Controllers;
using Umbraco.Community.CSPManager.Models.Api;

namespace Umbraco.Community.CSPManager.Tests.Controllers.DefinitonsController;

internal class SaveDefinitionsValidationTests : CspManagementApiTest<DefinitionsController>
{
	protected override Expression<Func<DefinitionsController, object>> MethodSelector => x => x.SaveDefinition(default!, default);

	private static int _testCounter = 0;

	[Test]
	[TestCaseSource(nameof(ValidationErrorCases))]
	public async Task SaveDefinition_ReturnsBadRequest_WhenValidationFails(CspApiDefinition definition, string expectedResponse)
	{
		var testNumber = Interlocked.Increment(ref _testCounter);
		await AuthenticateClientAsync(Client, $"validation-test{testNumber}@example.com", "1234567890", true);

		var content = JsonContent.Create(definition);
		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.PostAsync(url, content);

		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		var actual = await result.Content.ReadAsStringAsync();

		JsonAssert.AreEqual(expectedResponse, actual, true);
	}

	private static IEnumerable<TestCaseData> ValidationErrorCases
	{
		get
		{
			yield return new TestCaseData(
				new CspApiDefinition { Id = Guid.Empty },
				"""
				{
					"type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					"title": "One or more validation errors occurred.",
					"status": 400,
					"errors": [
						{
							"$.id": ["Invalid Id"]
						}
					]
				}
				""")
			{ TestName = "Empty Id returns validation error" };

			yield return new TestCaseData(
				new CspApiDefinition
				{
					Id = Constants.DefaultBackofficeId,
					Sources = [new() { Source = "test" }, new() { Source = "test" }]
				},
				"""
				{
					"type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					"title": "One or more validation errors occurred.",
					"status": 400,
					"errors": [
						{
							"$.sources": ["Duplicate sources found: 'test'"]
						}
					]
				}
				""")
			{ TestName = "Duplicate sources returns validation error" };

			yield return new TestCaseData(
				new CspApiDefinition
				{
					Id = Constants.DefaultBackofficeId,
					ReportingDirective = "invalid-directive"
				},
				"""
				{
					"type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					"title": "One or more validation errors occurred.",
					"status": 400,
					"errors": [
						{
							"$.reportingDirective": ["ReportingDirective must be 'report-uri' or 'report-to'"]
						}
					]
				}
				""")
			{ TestName = "Invalid ReportingDirective returns validation error" };

			yield return new TestCaseData(
				new CspApiDefinition
				{
					Id = Constants.DefaultBackofficeId,
					ReportingDirective = "report-uri"
				},
				"""
				{
					"type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					"title": "One or more validation errors occurred.",
					"status": 400,
					"errors": [
						{
							"$.reportUri": ["ReportUri is required when ReportingDirective is set"]
						}
					]
				}
				""")
			{ TestName = "Missing ReportUri when ReportingDirective is set returns validation error" };

			yield return new TestCaseData(
				new CspApiDefinition
				{
					Id = Constants.DefaultBackofficeId,
					ReportingDirective = "report-uri",
					ReportUri = "ftp://example.com/report"
				},
				"""
				{
					"type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					"title": "One or more validation errors occurred.",
					"status": 400,
					"errors": [
						{
							"$.reportUri": ["ReportUri must use HTTP or HTTPS scheme when using an absolute URI"]
						}
					]
				}
				""")
			{ TestName = "Non-HTTP scheme for report-uri returns validation error" };

			yield return new TestCaseData(
				new CspApiDefinition
				{
					Id = Constants.DefaultBackofficeId,
					Sources = [new() { Source = "'self'", Directives = ["invalid-directive"] }]
				},
				"""
				{
					"type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
					"title": "One or more validation errors occurred.",
					"status": 400,
					"errors": [
						{
							"$.sources": ["Unknown directive 'invalid-directive' in source ''self''"]
						}
					]
				}
				""")
			{ TestName = "Unknown directive returns validation error" };
		}
	}
}