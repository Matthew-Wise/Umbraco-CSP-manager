namespace Umbraco.Community.CSPManager.Services;

using Cms.Core.Events;
using Cms.Core.Hosting;
using Cms.Infrastructure.Scoping;
using Models;
using NPoco.Expressions;
using Umbraco.Extensions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Community.CSPManager.Notifications;

public class CspService : ICspService
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IHostingEnvironment _hostingEnvironment;
	private readonly IScopeProvider _scopeProvider;
	private readonly IAppPolicyCache _runtimeCache;

	public CspService(
		IEventAggregator eventAggregator,
		IHostingEnvironment hostingEnvironment,
		IScopeProvider scopeProvider,
		AppCaches appCaches)
	{
		_eventAggregator = eventAggregator;
		_hostingEnvironment = hostingEnvironment;
		_scopeProvider = scopeProvider;
		_runtimeCache = appCaches.RuntimeCache;
	}

	public CspDefinition GetCspDefinition(bool isBackOfficeRequest)
	{
		using var scope = _scopeProvider.CreateScope();

		//TODO: Oembed providers - https://our.umbraco.com/documentation/extending/Embedded-Media-Provider/
		CspDefinition definition = GetDefinition(scope, isBackOfficeRequest)
			?? new CspDefinition
			{
				Id = isBackOfficeRequest ? CspConstants.DefaultBackofficeId : CspConstants.DefaultFrontEndId,
				Enabled = false,
				IsBackOffice = isBackOfficeRequest
			};
		
		scope.Complete();
		return definition;
	}

	public CspDefinition? GetCachedCspDefinition(bool isBackOfficeRequest)
	{
		string cacheKey = isBackOfficeRequest ? CspConstants.BackOfficeCacheKey : CspConstants.FrontEndCacheKey;

		return _runtimeCache.GetCacheItem(cacheKey, () => GetCspDefinition(isBackOfficeRequest));
	}

	private static CspDefinition? GetDefinition(IScope scope, bool isBackOffice)
	{
		var sql = scope.SqlContext.Sql()
			.SelectAll()
			.From<CspDefinition>()
			.LeftJoin<CspDefinitionSource>()
			.On<CspDefinition, CspDefinitionSource>((d, s) => d.Id == s.DefinitionId)
			.Where<CspDefinition>(x => x.IsBackOffice == isBackOffice);

		var data = scope.Database.FetchOneToMany<CspDefinition>(c => c.Sources, sql);
		return data.FirstOrDefault();
	}

	public async Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition)
	{
		using var scope = _scopeProvider.CreateScope();

		definition = await SaveDefinitionAsync(scope, definition);

		scope.Complete();

		await _eventAggregator.PublishAsync(new CspSavedNotification(definition));

		return definition;
	}

	private static async Task<CspDefinition> SaveDefinitionAsync(IScope scope, CspDefinition definition)
	{
		await scope.Database.SaveAsync(definition);

		var sourceValues = definition.Sources.Select(s => s.Source).ToList();
		var cmdDelete = scope.Database.DeleteManyAsync<CspDefinitionSource>()
			.Where(s => !s.Source.In(sourceValues) && s.DefinitionId == definition.Id);
		
		await 	cmdDelete.Execute();
		
		foreach (var source in definition.Sources)
		{
			await scope.Database.SaveAsync(source);
		}

		return definition;
	}
}
