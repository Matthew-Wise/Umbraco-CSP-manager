using System.ComponentModel.DataAnnotations;
using Umbraco.Community.CSPManager.Models.Api;

namespace Umbraco.Community.CSPManager.Tests.Models;

[TestFixture]
public class CspApiDefinitionValidationTests
{
	[Test]
	public void Validate_WithValidFrontEndId_ReturnsNoErrors()
	{
		var definition = new CspApiDefinition { Id = Constants.DefaultFrontEndId };

		var results = ValidateModel(definition);

		Assert.That(results, Is.Empty);
	}

	[Test]
	public void Validate_WithValidBackOfficeId_ReturnsNoErrors()
	{
		var definition = new CspApiDefinition { Id = Constants.DefaultBackofficeId };

		var results = ValidateModel(definition);

		Assert.That(results, Is.Empty);
	}

	[Test]
	public void Validate_WithInvalidId_ReturnsError()
	{
		var definition = new CspApiDefinition { Id = Guid.NewGuid() };

		var results = ValidateModel(definition);

		Assert.That(results, Has.Count.EqualTo(1));
		Assert.That(results.FirstOrDefault()?.ErrorMessage, Is.EqualTo("Invalid Id"));
	}

	[Test]
	public void Validate_ReportUriDirective_WithAbsoluteHttpsUrl_ReturnsNoErrors()
	{
		var definition = new CspApiDefinition
		{
			Id = Constants.DefaultFrontEndId,
			ReportingDirective = Constants.ReportingDirectives.ReportUri,
			ReportUri = "https://example.com/csp-report"
		};

		var results = ValidateModel(definition);

		Assert.That(results, Is.Empty);
	}

	[Test]
	public void Validate_ReportUriDirective_WithRelativeUrl_ReturnsNoErrors()
	{
		var definition = new CspApiDefinition
		{
			Id = Constants.DefaultFrontEndId,
			ReportingDirective = Constants.ReportingDirectives.ReportUri,
			ReportUri = "/api/csp-report"
		};

		var results = ValidateModel(definition);

		Assert.That(results, Is.Empty);
	}

	[Test]
	public void Validate_ReportToDirective_WithEndpointName_ReturnsNoErrors()
	{
		var definition = new CspApiDefinition
		{
			Id = Constants.DefaultFrontEndId,
			ReportingDirective = Constants.ReportingDirectives.ReportTo,
			ReportUri = "csp-endpoint"
		};

		var results = ValidateModel(definition);

		Assert.That(results, Is.Empty);
	}

	[Test]
	public void Validate_ReportUriDirective_WithFtpScheme_ReturnsError()
	{
		var definition = new CspApiDefinition
		{
			Id = Constants.DefaultFrontEndId,
			ReportingDirective = Constants.ReportingDirectives.ReportUri,
			ReportUri = "ftp://example.com/report"
		};

		var results = ValidateModel(definition);

		Assert.That(results, Has.Count.EqualTo(1));
		Assert.That(results.FirstOrDefault()?.MemberNames, Contains.Item("ReportUri"));
	}

	[Test]
	public void Validate_ReportingDirective_WithMissingReportUri_ReturnsError()
	{
		var definition = new CspApiDefinition
		{
			Id = Constants.DefaultFrontEndId,
			ReportingDirective = Constants.ReportingDirectives.ReportUri,
			ReportUri = null
		};

		var results = ValidateModel(definition);

		Assert.That(results, Has.Count.EqualTo(1));
		Assert.That(results.FirstOrDefault()?.ErrorMessage, Is.EqualTo("ReportUri is required when ReportingDirective is set"));
	}

	[Test]
	public void Validate_InvalidReportingDirective_ReturnsError()
	{
		var definition = new CspApiDefinition
		{
			Id = Constants.DefaultFrontEndId,
			ReportingDirective = "invalid-directive",
			ReportUri = "https://example.com"
		};

		var results = ValidateModel(definition);

		Assert.That(results, Has.Count.EqualTo(1));
		Assert.That(results.FirstOrDefault()?.MemberNames, Contains.Item("ReportingDirective"));
	}

	[Test]
	public void Validate_DuplicateSources_ReturnsErrorWithSourceName()
	{
		var definition = new CspApiDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Sources =
			[
				new() { Source = "'self'", Directives = [Constants.Directives.DefaultSource] },
				new() { Source = "'self'", Directives = [Constants.Directives.ScriptSource] }
			]
		};

		var results = ValidateModel(definition);

		Assert.That(results.Any(r => r.ErrorMessage != null && r.ErrorMessage.Contains("Duplicate sources found: ''self''")), Is.True);
	}

	[Test]
	public void Validate_MultipleDuplicateSources_ListsAllDuplicates()
	{
		var definition = new CspApiDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Sources =
			[
				new() { Source = "'self'", Directives = [Constants.Directives.DefaultSource] },
				new() { Source = "https://example.com", Directives = [Constants.Directives.ScriptSource] },
				new() { Source = "'self'", Directives = [Constants.Directives.StyleSource] },
				new() { Source = "https://example.com", Directives = [Constants.Directives.ImageSource] }
			]
		};

		var results = ValidateModel(definition);
		var errorMessage = results.FirstOrDefault(r => r.MemberNames.Contains("Sources"))?.ErrorMessage;

		Assert.That(errorMessage, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(errorMessage, Does.Contain("'self'"));
			Assert.That(errorMessage, Does.Contain("https://example.com"));
		});
	}

	[Test]
	public void Validate_UnknownDirective_ReturnsError()
	{
		var definition = new CspApiDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Sources =
			[
				new() { Source = "'self'", Directives = ["not-a-real-directive"] }
			]
		};

		var results = ValidateModel(definition);

		Assert.That(results, Has.Count.EqualTo(1));
		Assert.That(results.FirstOrDefault()?.ErrorMessage, Does.Contain("Unknown directive"));
	}

	[Test]
	public void Validate_ValidDirectives_ReturnsNoErrors()
	{
		var definition = new CspApiDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Sources =
			[
				new()
				{
					Source = "'self'",
					Directives =
					[
						Constants.Directives.DefaultSource,
						Constants.Directives.ScriptSource,
						Constants.Directives.StyleSource
					]
				}
			]
		};

		var results = ValidateModel(definition);

		Assert.That(results, Is.Empty);
	}

	[Test]
	public void Validate_SourceExceedsMaxLength_ReturnsError()
	{
		var longSource = new string('a', 4001);
		var definition = new CspApiDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Sources =
			[
				new() { Source = longSource, Directives = [Constants.Directives.DefaultSource] }
			]
		};

		var results = ValidateModel(definition);

		Assert.That(results, Has.Count.EqualTo(1));
		Assert.That(results.FirstOrDefault()?.ErrorMessage, Does.Contain("exceeds maximum length"));
	}

	private static List<ValidationResult> ValidateModel(CspApiDefinition definition)
	{
		var validationContext = new ValidationContext(definition);
		var results = new List<ValidationResult>();
		Validator.TryValidateObject(definition, validationContext, results, validateAllProperties: true);
		return results;
	}
}
