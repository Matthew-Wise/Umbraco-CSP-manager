using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Umbraco.Community.CSPManager.Models.Api;
public sealed class CspApiDefinition : IValidatableObject
{
	public Guid Id { get; set; }

	public bool Enabled { get; set; }

	public bool ReportOnly { get; set; }

	public bool IsBackOffice { get; set; }

	public string? ReportingDirective { get; set; }

	public string? ReportUri { get; set; }

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

		if(duplicates.Count > 0)
		{
			yield return new ValidationResult("Duplicate Sources found", [nameof(Sources)]);
		}
	}

	internal static CspApiDefinition FromCspDefiniton(CspDefinition definition)
		=> new()
		{
			Id = definition.Id,
			Enabled = definition.Enabled,
			ReportingDirective = definition.ReportingDirective,
			IsBackOffice = definition.IsBackOffice,
			ReportOnly = definition.ReportOnly,
			ReportUri = definition.ReportUri,
			Sources = definition.Sources.ConvertAll(CspApiDefinitionSource.FromCspDefinitonSource),
		};

	

	internal CspDefinition ToCspDefiniton()
		=> new()
		{
			Id = Id,
			Enabled = Enabled,
			ReportOnly = ReportOnly,
			IsBackOffice = IsBackOffice,
			ReportingDirective = ReportingDirective,
			ReportUri = ReportUri,
			Sources = Sources.ConvertAll(CspApiDefinitionSource.ToCspDefinitonSource)
		};
}
