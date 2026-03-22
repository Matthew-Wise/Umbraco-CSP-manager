using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Strings;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Services;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;

namespace Umbraco.Community.CSPManager.uSync.Handlers;

[SyncHandler("CspDefinitionHandler", "CSP", "CspDefinitions", 3000, Icon = "icon-shield", EntityType = Constants.EntityTypes.CspPolicy)]
public class CspDefinitionHandler : SyncHandlerRoot<CspDefinition, CspDefinition>, ISyncHandler,
	INotificationAsyncHandler<CspSavedNotification>
{
	private readonly ICspService _cspService;

	public CspDefinitionHandler(ILogger<CspDefinitionHandler> logger,
		AppCaches appCaches,
		IShortStringHelper shortStringHelper,
		ISyncFileService syncFileService,
		ISyncEventService mutexService,
		ISyncConfigService uSyncConfig,
		ISyncItemFactory itemFactory,
		ICspService cspService)
		: base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
	{
		_cspService = cspService;
	}

	public override string Group => "Settings";

	public async Task HandleAsync(CspSavedNotification notification, CancellationToken cancellationToken)
	{
		try
		{
			var handlerFolders = GetDefaultHandlerFolders();
			var item = notification.CspDefinition;
			var attempts = await ExportAsync(item, handlerFolders, DefaultConfig);
			foreach (var attempt in attempts)
			{
				logger.LogWarning("Export attempt for {ItemId} changeType {ChangeType}: {Message}", item.Id, attempt.Change, attempt.Message);
				if (attempt.Success && attempt.FileName is not null)
				{
					await CleanUpAsync(item, attempt.FileName, handlerFolders[handlerFolders.Length - 1]);
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to create uSync export file");
		}
	}


	/// <summary>
	///  can be called - if uSync.Complete is attempting to delete items that might not exist on the target.
	///  we can ignore this, if the things are not directly pushed - normal syncs when things get deleted will
	///  produce the 'empty' files that delete things.
	/// </summary>
	protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(CspDefinition parent, IEnumerable<Guid> keysToKeep, bool reportOnly) => Task.FromResult(Enumerable.Empty<uSyncAction>());


	/// <summary>
	///  return child items - if there are none, return an empty list
	/// </summary>
	protected override async Task<IEnumerable<CspDefinition>> GetChildItemsAsync(CspDefinition? parent)
	{
		logger.LogWarning("GetChildItemsAsync called with parent: {ParentId}", parent?.Id.ToString() ?? "null (root)");
		if (parent != null)
		{
			logger.LogWarning("Attempting to get child items for a CSP Definition, but there are no child items: {ParentId}", parent.Id);
			return [];
		}

		var backoffice = await _cspService.GetCspDefinitionAsync(true, CancellationToken.None);
		var frontend = await _cspService.GetCspDefinitionAsync(false, CancellationToken.None);
		logger.LogWarning("GetChildItemsAsync returning 2 items: backoffice={BackofficeId} frontend={FrontendId}", backoffice.Id, frontend.Id);
		return [backoffice, frontend];
	}

	/// <summary>
	///  returns any folders - we don't have any so just return an empty list
	/// </summary>
	protected override Task<IEnumerable<CspDefinition>> GetFoldersAsync(CspDefinition? parent) => Task.FromResult(Enumerable.Empty<CspDefinition>());

	// fetches it from a service.
	protected override async Task<CspDefinition?> GetFromServiceAsync(CspDefinition? item)
	{
		logger.LogWarning("GetFromServiceAsync called for item: {ItemId} isBackOffice: {IsBackOffice}", item?.Id.ToString() ?? "null", item?.IsBackOffice);
		return item is null ? null : await _cspService.GetCspDefinitionAsync(item.Id, CancellationToken.None);
	}

	/// <summary>
	///  name doesn't really matter, its what is shown via the ui, if there isn't one, the id is fine.
	/// </summary>
	protected override string GetItemName(CspDefinition item) => item.Id.ToString();

	public new async Task<XElement?> TryFindItemNodeAsync(Guid key)
	{
		logger.LogWarning("TryFindItemNodeAsync called with key: {Key}", key);
		var result = await base.TryFindItemNodeAsync(key);
		logger.LogWarning("TryFindItemNodeAsync result for {Key}: {Found}", key, result is not null ? "FOUND" : "NULL");
		if (result is not null)
		{
			logger.LogWarning("TryFindItemNodeAsync XML: {Xml}", result.ToString());
		}
		return result;
	}

	protected new async Task<CspDefinition?> GetFromServiceAsync(Guid key)
	{
		logger.LogWarning("GetFromServiceAsync(Guid) called with key: {Key}", key);
		var result = await base.GetFromServiceAsync(key);
		logger.LogWarning("GetFromServiceAsync(Guid) result for {Key}: {Found}", key, result is not null ? "FOUND" : "NULL");
		return result;
	}
}