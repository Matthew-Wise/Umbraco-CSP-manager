namespace Umbraco.Community.CSPManager.Models.Reporting;

using System.Text.Json.Serialization;

public sealed record ReportModel([property: JsonPropertyName("csp-report")] BrowserCspReport? CspReport);

public sealed record BrowserCspReport(
	[property: JsonPropertyName("document-uri")] string? DocumentUri,
	[property: JsonPropertyName("referrer")] string? Referrer,
	[property: JsonPropertyName("violated-directive")] string? ViolatedDirective,
	[property: JsonPropertyName("effective-directive")] string? EffectiveDirective,
	[property: JsonPropertyName("original-policy")] string? OriginalPolicy,
	[property: JsonPropertyName("blocked-uri")] string? BlockedUri,
	[property: JsonPropertyName("status-code")] int? StatusCode);
