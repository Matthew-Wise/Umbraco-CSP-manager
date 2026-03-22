using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Strings;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.uSync.Logging;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;

using CspManagerConstants = Umbraco.Community.CSPManager.Constants;

namespace Umbraco.Community.CSPManager.uSync.Handlers;

[SyncHandler("CspDefinitionHandler", "CSP", "CspDefinitions", 3000, Icon = "icon-shield", EntityType = CspManagerConstants.EntityTypes.CspPolicy)]
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
				Log.CspExportAttempt(logger, item.Id, attempt.Change, attempt.Message);
				if (attempt.Success && attempt.FileName is not null)
				{
					await CleanUpAsync(item, attempt.FileName, handlerFolders[handlerFolders.Length - 1]);
				}
			}
		}
		catch (Exception ex)
		{
			Log.CspExportFailed(logger, ex);
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
		if (parent != null)
		{
			Log.NoChildItemsForDefinition(logger, parent.Id);
			return [];
		}

		var backoffice = await _cspService.GetCspDefinitionAsync(true, CancellationToken.None);
		var frontend = await _cspService.GetCspDefinitionAsync(false, CancellationToken.None);
		Log.GetChildItemsResult(logger, backoffice.Id, frontend.Id);
		return [backoffice, frontend];
	}

	/// <summary>
	///  returns any folders - we don't have any so just return an empty list
	/// </summary>
	protected override Task<IEnumerable<CspDefinition>> GetFoldersAsync(CspDefinition? parent)
		=> Task.FromResult(Enumerable.Empty<CspDefinition>());

	// fetches it from a service.
	protected override async Task<CspDefinition?> GetFromServiceAsync(CspDefinition? item)
		=> item is null ? null : await _cspService.GetCspDefinitionAsync(item.Id, CancellationToken.None);

	/// <summary>
	///  name doesn't really matter, its what is shown via the ui, if there isn't one, the id is fine.
	/// </summary>
	protected override string GetItemName(CspDefinition item) => item.Id.ToString();
}