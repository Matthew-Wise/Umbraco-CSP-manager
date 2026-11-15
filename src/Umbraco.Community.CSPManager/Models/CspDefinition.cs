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
	/// Gets or sets the key of the content node this node policy belongs to. <c>null</c> for the
	/// two global policies.
	/// </summary>
	/// <remarks>
	/// When set, this definition applies to requests for that content node and, depending on
	/// <see cref="AppliesTo"/>, the pages below it. How it combines with the policy that would
	/// apply otherwise is decided by <see cref="Inheritance"/>.
	/// </remarks>
	[NullSetting(NullSetting = NullSettings.Null)]
	public Guid? ContentKey { get; set; }

	/// <summary>
	/// Gets or sets the culture (ISO code, e.g. <c>fr-FR</c>) this node policy is for.
	/// <c>null</c> means every culture without a policy of its own, and is always <c>null</c> for
	/// the two global policies.
	/// </summary>
	/// <remarks>
	/// A request in a culture without a policy on the node follows Umbraco's language fallback,
	/// then the node's policy for all cultures.
	/// </remarks>
	[Length(35)]
	[NullSetting(NullSetting = NullSettings.Null)]
	public string? Culture { get; set; }

	/// <summary>
	/// Gets or sets whether this node policy is complete (<see cref="CspPolicyInheritance.Replace"/>)
	/// or adds its sources to the policy that applies without it
	/// (<see cref="CspPolicyInheritance.Extend"/>). Always <see cref="CspPolicyInheritance.Replace"/>
	/// for the two global policies.
	/// </summary>
	public CspPolicyInheritance Inheritance { get; set; }

	/// <summary>
	/// Gets or sets whether this node policy applies to the pages below its content node too.
	/// Has no meaning for the two global policies.
	/// </summary>
	public CspPolicyScope AppliesTo { get; set; }

	[ResultColumn]
	[Reference(ReferenceType.Many,
		ColumnName = nameof(Id),
		ReferenceMemberName = nameof(CspDefinitionSource.DefinitionId))]
	public List<CspDefinitionSource> Sources { get; set; } = [];
}