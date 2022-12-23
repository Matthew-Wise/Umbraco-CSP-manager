namespace Umbraco.Community.CSPManager.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.CSPManager.Models;

internal class CspSavingNotification : INotification
{
	public CspSavingNotification(CspDefinition? cspDefinition)
	{
		CspDefinition = cspDefinition;
	}

	public CspDefinition? CspDefinition { get; set; }
}
