namespace Umbraco.Community.CSPManager.Notifications;

using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.CSPManager.Models;

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
