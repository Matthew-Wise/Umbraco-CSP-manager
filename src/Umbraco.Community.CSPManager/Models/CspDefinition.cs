using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Umbraco.Community.CSPManager.Models;

/// <summary>
/// Represents a Content Security Policy (CSP) definition, including its configuration, reporting options, and
/// associated sources.
/// </summary>
/// <remarks>A CSP definition specifies the rules and settings for enforcing or reporting Content Security Policy
/// headers within an application. It includes options for enabling the policy, configuring reporting behavior, and
/// associating multiple sources that define allowed content origins. Instances of this class are typically used to
/// manage CSP settings for different application areas, such as front-end or back-office environments.</remarks>
[TableName((nameof(CspDefinition)))]
[PrimaryKey(nameof(Id), AutoIncrement = false)]
public class CspDefinition
{
	[PrimaryKeyColumn(AutoIncrement = false)]
	public Guid Id { get; set; }

	public bool Enabled { get; set; }

	public bool ReportOnly { get; set; }

	public bool IsBackOffice { get; set; }

	[Length(500)]
	[NullSetting(NullSetting = NullSettings.Null)]
	public string? ReportingDirective { get; set; }

	[NullSetting(NullSetting = NullSettings.Null)]
	[Length(500)]
	public string? ReportUri { get; set; }

	public bool UpgradeInsecureRequests { get; set; }

	/// <summary>
	/// Gets or sets the key of the content node this node policy belongs to: a node with at least
	/// one hostname assigned in Culture and Hostnames. <c>null</c> for the two global policies.
	/// </summary>
	/// <remarks>
	/// When set, this definition overrides the global frontend policy for requests routed through any
	/// of the node's hostnames (every culture of a multilingual site shares the one policy).
	/// </remarks>
	[NullSetting(NullSetting = NullSettings.Null)]
	public Guid? ContentKey { get; set; }

	[ResultColumn]
	[Reference(ReferenceType.Many,
		ColumnName = nameof(Id),
		ReferenceMemberName = nameof(CspDefinitionSource.DefinitionId))]
	public List<CspDefinitionSource> Sources { get; set; } = [];
}