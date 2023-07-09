namespace Umbraco.Community.CSPManager.Services;

using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.CSPManager.Models.Reporting;
using Umbraco.Community.CSPManager.Notifications;

public sealed class ReportingService : IReportingService
{
	private readonly IEventAggregator _eventAggregator;
	
	private readonly IScopeProvider _scopeProvider;

	private readonly IEventMessagesFactory _eventMessagesFactory;

	private readonly UmbracoRequestPaths _umbracoRequestPaths;

	public ReportingService(IScopeProvider scopeProvider, IEventAggregator eventAggregator, IEventMessagesFactory eventMessagesFactory, UmbracoRequestPaths umbracoRequestPaths)
	{
		_scopeProvider = scopeProvider;
		_eventAggregator = eventAggregator;
		_eventMessagesFactory = eventMessagesFactory;
		_umbracoRequestPaths = umbracoRequestPaths;
	}

	public async Task SaveAsync(ReportModel report)
	{
		if(report.CspReport == null)
		{
			return;
		}

		var eventMessages = _eventMessagesFactory.Get();
		if(await _eventAggregator.PublishCancelableAsync(new CspReportSavingNotification(report, eventMessages)))
		{
			return; //Action was cancelled.
		}

		using var scope = _scopeProvider.CreateScope();
				
		if(Uri.TryCreate(report.CspReport.DocumentUri, UriKind.Absolute, out var documentUri) == false)
		{
			scope.Complete();
			return;
		}

		var isBackOffice = _umbracoRequestPaths.IsBackOfficeRequest(documentUri.AbsolutePath);
		var directive = report.CspReport.EffectiveDirective;
		var blockedUri = report.CspReport.BlockedUri;

		/*TODO: How do I want to store this information, 
		 * blockedUri + directive as a key
		 */

		CspReportRecord cspRecord = new()
		{
			DocumentUri = documentUri.OriginalString, //TODO: Need to store these linked to ReportRecord..
			IsBackOffice = isBackOffice,//TODO: Need to store these linked to ReportRecord..
			Directive = directive ?? string.Empty,
			BlockedUri = blockedUri ?? string.Empty,
			Instances = 1,//TODO: Need to store these linked to ReportRecord..
			LastRecorded = DateTime.Now //Is this tied to each documentUri or this pairing?
		};

		//await scope.Database.SaveAsync(cspRecord);

		scope.Complete();
	}
}
