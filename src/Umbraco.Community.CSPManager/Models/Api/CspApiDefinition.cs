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

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (!Constants.DefaultFrontEndId.Equals(Id) && !Constants.DefaultBackofficeId.Equals(Id))
		{
			yield return new ValidationResult("Invalid Id", [nameof(Id)]);
		}

		if (Sources.Count == 0) yield break;

		var sources = new HashSet<string>();
		var duplicates = new HashSet<string>();
		foreach (var source in Sources)
		{
			if (!sources.Add(source.Source))
			{
				duplicates.Add(source.Source);
			}
		}

		if (duplicates.Count > 0)
		{
			yield return new ValidationResult("Duplicate Sources found", [nameof(Sources)]);
		}
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