using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.CSPManager.Models.Api;

/// <summary>
/// API data transfer object representing a Content Security Policy definition.
/// </summary>
/// <remarks>
/// This class is used for API requests and responses. It includes validation logic
/// to ensure the definition has a valid ID and no duplicate sources.
/// </remarks>
public sealed class CspApiDefinition : IValidatableObject
{
	/// <summary>
	/// Gets or sets the unique identifier for this CSP definition.
	/// Must be either <see cref="Constants.DefaultFrontEndId"/> or <see cref="Constants.DefaultBackofficeId"/>.
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this CSP policy is enabled.
	/// When <c>false</c>, no CSP header will be added to responses.
	/// </summary>
	public bool Enabled { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether to use report-only mode.
	/// When <c>true</c>, uses the Content-Security-Policy-Report-Only header instead of Content-Security-Policy.
	/// </summary>
	public bool ReportOnly { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this definition is for the Umbraco backoffice.
	/// </summary>
	public bool IsBackOffice { get; set; }

	/// <summary>
	/// Gets or sets the reporting directive to use (e.g., "report-uri" or "report-to").
	/// </summary>
	public string? ReportingDirective { get; set; }

	/// <summary>
	/// Gets or sets the URI where CSP violation reports should be sent.
	/// </summary>
	public string? ReportUri { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether to include the upgrade-insecure-requests directive.
	/// When <c>true</c>, browsers will upgrade HTTP requests to HTTPS.
	/// </summary>
	public bool UpgradeInsecureRequests { get; set; }

	/// <summary>
	/// Gets or sets the list of CSP sources and their associated directives.
	/// </summary>
	public List<CspApiDefinitionSource> Sources { get; set; } = [];

	/// <summary>
	/// Maximum length for a source value (database column limit).
	/// </summary>
	private const int MaxSourceLength = 4000;

	/// <summary>
	/// Validates the CSP definition according to CSP specification rules.
	/// </summary>
	/// <param name="validationContext">The validation context.</param>
	/// <returns>A collection of validation results.</returns>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		// Validate Id is one of the known definition IDs
		if (!Constants.DefaultFrontEndId.Equals(Id) && !Constants.DefaultBackofficeId.Equals(Id))
		{
			yield return new ValidationResult("Invalid Id", [nameof(Id)]);
		}

		// Validate ReportingDirective if provided
		foreach (var result in ValidateReporting())
		{
			yield return result;
		}

		// Validate Sources
		foreach (var result in ValidateSources())
		{
			yield return result;
		}
	}

	private IEnumerable<ValidationResult> ValidateReporting()
	{
		if (string.IsNullOrWhiteSpace(ReportingDirective))
		{
			yield break;
		}

		// ReportingDirective must be one of the valid values
		if (ReportingDirective != Constants.ReportingDirectives.ReportUri &&
			ReportingDirective != Constants.ReportingDirectives.ReportTo)
		{
			yield return new ValidationResult(
				$"ReportingDirective must be '{Constants.ReportingDirectives.ReportUri}' or '{Constants.ReportingDirectives.ReportTo}'",
				[nameof(ReportingDirective)]);
			yield break;
		}

		// If a reporting directive is set, ReportUri is required
		if (string.IsNullOrWhiteSpace(ReportUri))
		{
			yield return new ValidationResult(
				"ReportUri is required when ReportingDirective is set",
				[nameof(ReportUri)]);
			yield break;
		}

		// Validate ReportUri format based on directive type
		if (ReportingDirective == Constants.ReportingDirectives.ReportUri)
		{
			// report-uri accepts absolute or relative URIs
			if (!Uri.TryCreate(ReportUri, UriKind.RelativeOrAbsolute, out var uri))
			{
				yield return new ValidationResult(
					"ReportUri must be a valid URI when using report-uri directive",
					[nameof(ReportUri)]);
			}
			else if (uri.IsAbsoluteUri && uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
			{
				yield return new ValidationResult(
					"ReportUri must use HTTP or HTTPS scheme when using an absolute URI",
					[nameof(ReportUri)]);
			}
		}
		// report-to uses an endpoint name, not a URI - no URL validation needed
	}

	private IEnumerable<ValidationResult> ValidateSources()
	{
		if (Sources.Count == 0)
		{
			yield break;
		}

		var validDirectives = Constants.AllDirectives.ToArray();
		var sourceSet = new HashSet<string>();
		var duplicates = new HashSet<string>();

		for (var i = 0; i < Sources.Count; i++)
		{
			var source = Sources[i];

			// Check for duplicate sources
			if (!sourceSet.Add(source.Source))
			{
				duplicates.Add(source.Source);
			}

			// Check source length
			if (source.Source.Length > MaxSourceLength)
			{
				yield return new ValidationResult(
					$"Source '{TruncateForDisplay(source.Source)}' exceeds maximum length of {MaxSourceLength} characters",
					[nameof(Sources)]);
			}

			// Validate directives are known CSP directives
			foreach (var directive in source.Directives)
			{
				if (!validDirectives.Contains(directive))
				{
					yield return new ValidationResult(
						$"Unknown directive '{directive}' in source '{TruncateForDisplay(source.Source)}'",
						[nameof(Sources)]);
				}
			}
		}

		if (duplicates.Count > 0)
		{
			var duplicateList = string.Join(", ", duplicates.Select(d => $"'{TruncateForDisplay(d)}'"));
			yield return new ValidationResult(
				$"Duplicate sources found: {duplicateList}",
				[nameof(Sources)]);
		}
	}

	private static string TruncateForDisplay(string value, int maxLength = 50)
	{
		if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
		{
			return value;
		}

		return string.Concat(value.AsSpan(0, maxLength - 3), "...");
	}

	internal static CspApiDefinition FromCspDefinition(CspDefinition definition)
		=> new()
		{
			Id = definition.Id,
			Enabled = definition.Enabled,
			UpgradeInsecureRequests = definition.UpgradeInsecureRequests,
			ReportingDirective = definition.ReportingDirective,
			IsBackOffice = definition.IsBackOffice,
			ReportOnly = definition.ReportOnly,
			ReportUri = definition.ReportUri,
			Sources = definition.Sources.ConvertAll(CspApiDefinitionSource.FromCspDefinitionSource),
		};



	internal CspDefinition ToCspDefinition()
		=> new()
		{
			Id = Id,
			Enabled = Enabled,
			UpgradeInsecureRequests = UpgradeInsecureRequests,
			ReportOnly = ReportOnly,
			IsBackOffice = IsBackOffice,
			ReportingDirective = ReportingDirective,
			ReportUri = ReportUri,
			Sources = Sources.ConvertAll(CspApiDefinitionSource.ToCspDefinitionSource)
		};
}