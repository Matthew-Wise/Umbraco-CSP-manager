using Umbraco.Community.CSPManager.Models;
using uSync.Core.Tracking;

namespace Umbraco.Community.CSPManager.uSync.Trackers;

internal class CspDefinitionTracker : SyncXmlTrackAndMerger<CspDefinition>, ISyncTracker<CspDefinition>
{
	public CspDefinitionTracker(SyncSerializerCollection serializers) : base(serializers)
	{
	}

	public override List<TrackingItem> TrackingItems =>
	[
		TrackingItem.Single(nameof(CspDefinition.Enabled), $"Info/{nameof(CspDefinition.Enabled)}"),
		TrackingItem.Single(nameof(CspDefinition.ReportOnly),  $"Info/{nameof(CspDefinition.ReportOnly)}"),
		TrackingItem.Single(nameof(CspDefinition.ReportUri),  $"Info/{nameof(CspDefinition.ReportUri)}"),
		TrackingItem.Single(nameof(CspDefinition.ReportingDirective),  $"Info/{nameof(CspDefinition.ReportingDirective)}"),
		TrackingItem.Single(nameof(CspDefinition.UpgradeInsecureRequests),  $"Info/{nameof(CspDefinition.UpgradeInsecureRequests)}"),
		TrackingItem.Many("Source", "Sources/Source", "@value"),
	];
}