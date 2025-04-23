using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Notifications;

/// <summary>
/// Notification published, when building a <see cref="Models.CspDefinition"/> for the provided <see cref="Microsoft.AspNetCore.Http.HttpContext"/>.
/// </summary>
public class CspWritingNotification : INotification
{
	public CspWritingNotification(CspDefinition? cspDefinition, HttpContext httpContext)
	{
		HttpContext = httpContext;
		CspDefinition = cspDefinition;
	}

	public CspDefinition? CspDefinition { get; set; }

	public HttpContext HttpContext { get; set; }
}
