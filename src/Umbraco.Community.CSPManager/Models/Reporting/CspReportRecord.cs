namespace Umbraco.Community.CSPManager.Models.Reporting;

using System;
public sealed class CspReportRecord
{
	public string DocumentUri { get; init; } = string.Empty;
	public bool IsBackOffice { get; set; }
	public string Directive { get; set; } = string.Empty;
	public string BlockedUri { get; set; } = string.Empty;
	public DateTime LastRecorded { get; set; } = DateTime.Now;
	public int Instances { get; set; } = 1;
}
