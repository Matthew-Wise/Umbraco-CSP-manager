namespace Umbraco.Community.CSPManager.Notifications;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.CSPManager.Models.Reporting;

public sealed class CspReportSavingNotification : CancelableObjectNotification<ReportModel>
{
	public CspReportSavingNotification(ReportModel target, EventMessages messages) : base(target, messages)
	{
		Report = target;
	}

	public ReportModel Report { get; }
}
