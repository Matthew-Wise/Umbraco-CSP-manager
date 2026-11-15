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

namespace Umbraco.Community.CSPManager.uSync.Handlers;

/// <summary>
/// Shared behaviour of the CSP handlers. Each handler exports one kind of definition, with its own
/// serializer, so each kind has its own XML element, entity type and folder.
/// </summary>
/// <typeparam name="TSerializer">The serializer of the definitions this handler exports.</typeparam>
public abstract class CspHandlerBase<TSerializer> : SyncHandlerRoot<CspDefinition, CspDefinition>, ISyncHandler,
	INotificationAsyncHandler<CspSavedNotification>, INotificationAsyncHandler<CspDeletedNotification>
	where TSerializer : ISyncSerializer<CspDefinition>
{
	protected CspHandlerBase(ILogger<SyncHandlerRoot<CspDefinition, CspDefinition>> logger,
		AppCaches appCaches,
		IShortStringHelper shortStringHelper,
		ISyncFileService syncFileService,
		ISyncEventService mutexService,
		ISyncConfigService uSyncConfig,
		ISyncItemFactory itemFactory,
		ICspService cspService)
		: base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory)
	{
		CspService = cspService;

		// The base class takes the first serializer for CspDefinition; both handlers share that
		// type, so pick this handler's own. TypeName routes files to a handler by XML element.
		serializer = itemFactory.GetSerializers<CspDefinition>().OfType<TSerializer>().First();
		TypeName = serializer.ItemType;
	}

	protected ICspService CspService { get; }

	/// <summary>
	/// Gets whether a definition is exported by this handler.
	/// </summary>
	protected abstract bool Handles(CspDefinition definition);

	public async Task HandleAsync(CspSavedNotification notification, CancellationToken cancellationToken)
	{
		if (!ShouldProcessEvent() || !Handles(notification.CspDefinition)) return;

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
	/// Exports a delete marker when a node policy is deleted to propagate removal on import.
	/// </summary>
	public async Task HandleAsync(CspDeletedNotification notification, CancellationToken cancellationToken)
	{
		if (!ShouldProcessEvent() || !Handles(notification.CspDefinition)) return;

		try
		{
			await ExportDeletedItemAsync(notification.CspDefinition, GetDefaultHandlerFolders(), DefaultConfig);
			Log.CspDeleteExported(logger, notification.CspDefinition.Id);
		}
		catch (Exception ex)
		{
			Log.CspExportFailed(logger, ex);
		}
	}

	/// <summary>
	///  Definitions are a flat list without children, so a clean (uSync.Complete deleting the
	///  children a source didn't send) has nothing to remove under any of them.
	/// </summary>
	protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(CspDefinition parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
		=> Task.FromResult(Enumerable.Empty<uSyncAction>());

	/// <summary>
	///  returns any folders - we don't have any so just return an empty list
	/// </summary>
	protected override Task<IEnumerable<CspDefinition>> GetFoldersAsync(CspDefinition? parent)
		=> Task.FromResult(Enumerable.Empty<CspDefinition>());

	// fetches it from a service.
	protected override async Task<CspDefinition?> GetFromServiceAsync(CspDefinition? item)
		=> item is null ? null : await CspService.GetCspDefinitionAsync(item.Id, CancellationToken.None);

	protected override string GetItemName(CspDefinition item) => CspItemNames.Name(item);
}
