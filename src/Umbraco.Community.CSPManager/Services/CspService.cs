﻿namespace Umbraco.Community.CSPManager.Services;

using Cms.Core.Events;
using Cms.Core.Hosting;
using Cms.Infrastructure.Scoping;
using Cms.Core.Web;
using Models;
using Umbraco.Extensions;

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

	public async Task<CspDefinition?> GetCspDefinitionAsync(bool IsBackOfficeRequest, bool? enabled = null)
	{
		CspDefinition? definition = null;
		using var scope = _scopeProvider.CreateScope();
		
		//TODO: Oembed providers - https://our.umbraco.com/documentation/extending/Embedded-Media-Provider/
		definition = await GetDefinitionAsync(scope, IsBackOfficeRequest, enabled)
			?? new CspDefinition { 
				Id = IsBackOfficeRequest ? CspConstants.DefaultBackofficeId : CspConstants.DefaultFrontEndId,
				Enabled = false,
				IsBackOffice = IsBackOfficeRequest 
			};
		
		AddWebsocketsForAspNet(definition);

		scope.Complete();
		return definition;
	}
	
	private static async Task<CspDefinition?> GetDefinitionAsync(IScope scope, bool isBackOffice, bool? enabled = null)
	{
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.LeftJoin<CspDefinitionSource>()
			.On<CspDefinition, CspDefinitionSource>((d, s) => d.Id == s.DefinitionId)
			.Where<CspDefinition>(x => x.IsBackOffice == isBackOffice);

		if(enabled != null) {
			sql.Where<CspDefinition>(x => x.Enabled == enabled);
		}

		var raw = sql.SQL;
		var data = await Task.FromResult(scope.Database.FetchOneToMany<CspDefinition>(c => c.Sources, sql));
		return data.FirstOrDefault();
	}

	public async Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition) {
		if(definition == null){
			throw new ArgumentException("Definition is null");
		}
		using var scope = _scopeProvider.CreateScope();
		
		definition = await SaveDefinitionAsync(scope, definition);

		scope.Complete();
		return definition;
	}

	private static async Task<CspDefinition> SaveDefinitionAsync(IScope scope, CspDefinition definition)
	{
		await scope.Database.SaveAsync<CspDefinition>(definition);
		foreach(var source in definition.Sources) {
			await scope.Database.SaveAsync<CspDefinitionSource>(source);
		}
		return definition;
	}

	private void AddWebsocketsForAspNet(CspDefinition? definition)
	{
		if (!_hostingEnvironment.IsDebugMode || definition == null)
		{
			return;
		}

		var source = definition.Sources.FirstOrDefault(x => x.Source.InvariantEquals("wss:"));
		if (source == null)
		{
			definition.Sources.Add(new CspDefinitionSource
			{
				DefinitionId = definition.Id,
				Source = "wss:",
				Directives = new List<string> { CspConstants.Directives.DefaultSource }
			});	
		}
	}
}