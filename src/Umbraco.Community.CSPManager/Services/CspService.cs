namespace Umbraco.Community.CSPManager.Services;

using Cms.Core.Events;
using Cms.Core.Hosting;
using Cms.Infrastructure.Scoping;
using Cms.Core.Web;
using Extensions;
using Microsoft.AspNetCore.Http;
using Models;
using Notifications;
using NPoco;

public class CspService : ICspService
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IHostingEnvironment _hostingEnvironment;
	private readonly IUmbracoContextAccessor _umbracoContextAccessor;
	private readonly IScopeProvider _scopeProvider;

	public CspService(IEventAggregator eventAggregator, IHostingEnvironment hostingEnvironment,
		IUmbracoContextAccessor umbracoContextAccessor, IScopeProvider scopeProvider)
	{
		_eventAggregator = eventAggregator;
		_hostingEnvironment = hostingEnvironment;
		_umbracoContextAccessor = umbracoContextAccessor;
		_scopeProvider = scopeProvider;
	}

	public CspDefinition? GetCspDefinition(HttpContext httpContext)
	{
		CspDefinition? definition = null;
		using var scope = _scopeProvider.CreateScope();
		if (_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
		{
			if (umbracoContext.IsFrontEndUmbracoRequest())
			{
				definition = GetDefinition(scope, false) ??
				             new CspDefinition { Enabled = false, IsBackOffice = false };
			}
		}

		if (httpContext.Request.IsBackOfficeRequest())
		{
			//TODO: Oembed providers - https://our.umbraco.com/documentation/extending/Embedded-Media-Provider/
			definition = GetDefinition(scope, true) ?? new CspDefinition { Enabled = false, IsBackOffice = true };
		}

		AddWebsocketsForAspNet(definition);

		_eventAggregator.Publish(new CspWritingNotification(definition, httpContext));

		scope.Complete();
		return definition;
	}
	
	private static CspDefinition? GetDefinition(IScope scope, bool isBackOffice)
	{
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.LeftJoin<CspDefinitionSource>()
			.On<CspDefinition, CspDefinitionSource>((d, s) => d.Id == s.DefinitionId)
			.Where<CspDefinition>(x => x.IsBackOffice == isBackOffice && x.Enabled == true);
		var raw = sql.SQL;
		var data = scope.Database.FetchOneToMany<CspDefinition>(c => c.Sources, sql);
		return data.FirstOrDefault();
	}

	private void AddWebsocketsForAspNet(CspDefinition? definition)
	{
		if (!_hostingEnvironment.IsDebugMode || definition == null)
		{
			return;
		}

		var source = definition.Sources.FirstOrDefault(x => x.Source.InvariantEquals("wws:"));
		if (source == null)
		{
			definition.Sources.Add(new CspDefinitionSource
			{
				Source = "wss:",
				Directives = new List<string> { CspConstants.Directives.DefaultSource }
			});	
		}
	}
}
