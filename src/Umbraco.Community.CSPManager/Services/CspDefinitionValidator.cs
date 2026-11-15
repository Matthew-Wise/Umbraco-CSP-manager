using System.ComponentModel.DataAnnotations;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Validates CSP definition header tokens, directives, and reporting configuration.
/// </summary>
/// <remarks>
/// Ensures values contain no characters (whitespace, semicolons, commas, or control characters)
/// that would invalidate or corrupt the outgoing HTTP header.
/// </remarks>
public static class CspDefinitionValidator
{
	/// <summary>
	/// Maximum length for a source value (database column limit).
	/// </summary>
	internal const int MaxSourceLength = 4000;

	/// <summary>
	/// Validates the header content of a definition.
	/// </summary>
	/// <param name="definition">The definition to validate.</param>
	/// <returns>The validation errors; empty when the definition is valid.</returns>
	public static IReadOnlyList<ValidationResult> Validate(CspDefinition definition)
	{
		ArgumentNullException.ThrowIfNull(definition);

		return
		[
			.. Validate(
				definition.ReportingDirective,
				definition.ReportUri,
				[.. definition.Sources.Select(s => (s.Source, (IReadOnlyCollection<string>)s.Directives))])
		];
	}

	internal static IEnumerable<ValidationResult> Validate(
		string? reportingDirective,
		string? reportUri,
		IReadOnlyList<(string Source, IReadOnlyCollection<string> Directives)> sources)
		=> ValidateReporting(reportingDirective, reportUri).Concat(ValidateSources(sources));

	private static IEnumerable<ValidationResult> ValidateReporting(string? reportingDirective, string? reportUri)
	{
		if (string.IsNullOrWhiteSpace(reportingDirective))
		{
			yield break;
		}

		// ReportingDirective must be one of the valid values
		if (reportingDirective != Constants.ReportingDirectives.ReportUri &&
			reportingDirective != Constants.ReportingDirectives.ReportTo)
		{
			yield return new ValidationResult(
				$"ReportingDirective must be '{Constants.ReportingDirectives.ReportUri}' or '{Constants.ReportingDirectives.ReportTo}'",
				[nameof(CspDefinition.ReportingDirective)]);
			yield break;
		}

		// If a reporting directive is set, ReportUri is required
		if (string.IsNullOrWhiteSpace(reportUri))
		{
			yield return new ValidationResult(
				"ReportUri is required when ReportingDirective is set",
				[nameof(CspDefinition.ReportUri)]);
			yield break;
		}

		// The value is written straight after the directive, so the same token rules as sources apply.
		if (reportUri.Any(IsInvalidTokenCharacter))
		{
			yield return new ValidationResult(
				"ReportUri must be a single value: it cannot contain whitespace, ';', ',' or control characters",
				[nameof(CspDefinition.ReportUri)]);
			yield break;
		}

		// Validate ReportUri format based on directive type
		if (reportingDirective == Constants.ReportingDirectives.ReportUri)
		{
			// report-uri accepts absolute or relative URIs
			if (!Uri.TryCreate(reportUri, UriKind.RelativeOrAbsolute, out var uri))
			{
				yield return new ValidationResult(
					"ReportUri must be a valid URI when using report-uri directive",
					[nameof(CspDefinition.ReportUri)]);
			}
			else if (uri.IsAbsoluteUri && uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
			{
				yield return new ValidationResult(
					"ReportUri must use HTTP or HTTPS scheme when using an absolute URI",
					[nameof(CspDefinition.ReportUri)]);
			}
		}
		// report-to uses an endpoint name, not a URI - no URL validation needed
	}

	private static IEnumerable<ValidationResult> ValidateSources(IReadOnlyList<(string Source, IReadOnlyCollection<string> Directives)> sources)
	{
		if (sources.Count == 0)
		{
			yield break;
		}

		// CSP host and keyword matching is case-insensitive, and SQL Server's default collation
		// treats the (DefinitionId, Source) key the same way, so two sources differing only by
		// case would collide on save. Treat them as duplicates up front.
		var sourceSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach ((string source, IReadOnlyCollection<string> directives) in sources)
		{
			var value = source ?? string.Empty;

			// Check for duplicate sources
			if (!sourceSet.Add(value))
			{
				duplicates.Add(value);
			}

			// Check source length
			if (value.Length > MaxSourceLength)
			{
				yield return new ValidationResult(
					$"Source '{TruncateForDisplay(value)}' exceeds maximum length of {MaxSourceLength} characters",
					[nameof(CspDefinition.Sources)]);
			}

			// A source is a single header token. Whitespace or ';' would splice extra tokens or
			// directives into the header, ',' would split it into two header values, and a control
			// character makes Kestrel reject the header so the response ships with no CSP at all.
			// (Whitespace-only sources are dropped by the service on save, so skip those here.)
			if (!string.IsNullOrWhiteSpace(value) && value.Any(IsInvalidTokenCharacter))
			{
				yield return new ValidationResult(
					$"Source '{TruncateForDisplay(value)}' must be a single token: it cannot contain whitespace, ';', ',' or control characters",
					[nameof(CspDefinition.Sources)]);
			}

			// Validate directives are known CSP directives
			foreach (var directive in directives ?? [])
			{
				if (!Constants.AllDirectives.Contains(directive))
				{
					yield return new ValidationResult(
						$"Unknown directive '{TruncateForDisplay(directive)}' in source '{TruncateForDisplay(value)}'",
						[nameof(CspDefinition.Sources)]);
				}
			}
		}

		if (duplicates.Count > 0)
		{
			var duplicateList = string.Join(", ", duplicates.Select(d => $"'{TruncateForDisplay(d)}'"));
			yield return new ValidationResult(
				$"Duplicate sources found: {duplicateList}",
				[nameof(CspDefinition.Sources)]);
		}
	}

	private static bool IsInvalidTokenCharacter(char c)
		=> char.IsWhiteSpace(c) || char.IsControl(c) || c == ';' || c == ',';

	// Messages end up in logs and API responses, so control characters (CR/LF) are masked.
	private static string TruncateForDisplay(string? value, int maxLength = 50)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}

		var display = value.Any(char.IsControl)
			? string.Concat(value.Select(c => char.IsControl(c) ? '?' : c))
			: value;

		return display.Length <= maxLength
			? display
			: string.Concat(display.AsSpan(0, maxLength - 3), "...");
	}
}
