namespace Umbraco.Community.CSPManager.Notifications;

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Models;
using Umbraco.Cms.Core.Notifications;

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
