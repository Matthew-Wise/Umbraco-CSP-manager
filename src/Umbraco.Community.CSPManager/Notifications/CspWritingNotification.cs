namespace Umbraco.Community.CSPManager.Notifications;

using Microsoft.AspNetCore.Http;
using Models;
using Umbraco.Cms.Core.Notifications;

/// <summary>
/// Notification published, when building a <see cref="Models.CspDefinition"/> for the provided <see cref="HttpContext"/>.
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
