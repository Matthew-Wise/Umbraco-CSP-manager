namespace Umbraco.Community.CSPManager.Core.Notifications;

using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.CSPManager.Core.Models;

/// <summary>
/// Notification published on <see cref="Models.CspDefinition"/> save
/// </summary>
public class CspSavedNotification : INotification
{
	public CspSavedNotification(CspDefinition cspDefinition)
	{
		CspDefinition = cspDefinition;
	}

	public CspDefinition CspDefinition { get; set; }
}
