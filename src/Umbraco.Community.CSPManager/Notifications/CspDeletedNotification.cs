using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Notifications;

/// <summary>
/// Notification published after a domain CSP policy has been deleted.
/// </summary>
public class CspDeletedNotification : INotification
{
	public CspDeletedNotification(CspDefinition cspDefinition)
	{
		CspDefinition = cspDefinition;
	}

	/// <summary>
	/// Gets or sets the deleted CSP definition.
	/// </summary>
	public CspDefinition CspDefinition { get; set; }
}
