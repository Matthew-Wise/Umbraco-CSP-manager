using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Community.CSPManager.Services;
using uSync.Core.Dependency;
using uSync.Core.Extensions;
using uSync.Core.Sync;

using CspManagerConstants = Umbraco.Community.CSPManager.Constants;

namespace Umbraco.Community.CSPManager.uSync.Sync;

[SyncItemManager(CspManagerConstants.EntityTypes.CspPolicy)]
public class CspSyncItemManager : SyncItemManagerBase, ISyncItemManager
{
	private readonly ICspService _cspService;
	private readonly ILogger<CspSyncItemManager> _logger;

	public CspSyncItemManager(ICspService cspService, ILogger<CspSyncItemManager> logger)
	{
		_cspService = cspService;
		_logger = logger;
	}

	public override SyncTreeType GetTreeType(SyncTreeItem treeItem)
	{
		_logger.LogWarning("GetTreeType called for treeItem {TreeItemId} {SectionAlias}", treeItem.Id, treeItem.SectionAlias);

		return SyncTreeType.Settings;
	}

	public override string[] EntityTypes => [CspManagerConstants.EntityTypes.CspPolicy];

	public override string[] Trees => ["Umbraco.Community.CSPManager.Tree"];

	public async Task<SyncEntity?> GetSyncEntityAsync(string key)
	{
		if (!Guid.TryParse(key, out var guidKey))
		{
			_logger.LogWarning("Attempting to get a CSP Definition with an invalid key: {Key}", key);
			return null;
		}

		var definition = await _cspService.GetCspDefinitionAsync(guidKey, CancellationToken.None);
		if (definition is null)
		{
			_logger.LogWarning("No CSP Definition found for key: {Key}", key);
			return null;
		}

		return new SyncEntity
		{
			Name = definition.IsBackOffice ? "Backoffice" : "Frontend",
			Udi = Udi.Create(CspManagerConstants.EntityTypes.CspPolicy, definition.Id),
		};
	}

	public override Task<IEnumerable<SyncItem>> GetItemsAsync(SyncItem item)
		=> uSyncTaskHelper.FromResultOf(() => GetItems(item).AsEnumerable());

	private List<SyncItem> GetItems(SyncItem item)
	{
		var items = new List<SyncItem>();

		if (item.Udi.EntityType == CspManagerConstants.EntityTypes.CspPolicy && !item.Udi.IsRoot)
		{
			items.Add(item);
		}

		if (item.Flags.HasFlag(DependencyFlags.IncludeChildren))
		{
			items.AddRange(GetDescendants(item, item.Flags & ~DependencyFlags.IncludeChildren));
		}
		_logger.LogWarning("GetItems for item {ItemId} returned {ItemCount} items", item.Udi, items.Count);
		foreach (var syncItem in items)
		{
			_logger.LogWarning("Included item {ItemId} with name {ItemName} and flags {ItemFlags} and Change {ItemChange}", syncItem.Udi, syncItem.Name, syncItem.Flags, syncItem.Change);
		}
		return items;
	}

	protected override Task<IEnumerable<SyncItem>> GetDescendantsAsync(SyncItem item, DependencyFlags flags)
		=> Task.FromResult(GetDescendants(item, flags));

	private IEnumerable<SyncItem> GetDescendants(SyncItem item, DependencyFlags flags)
	{
		_logger.LogWarning("Getting descendants for item {ItemId} with flags {Flags} isRoot: {IsRoot}", item.Udi, flags, item.Udi.IsRoot);
		if (item.Udi.IsRoot)
		{
			return
			[
				new SyncItem
				{
					Name = "Backoffice",
					Udi = Udi.Create(CspManagerConstants.EntityTypes.CspPolicy, CspManagerConstants.DefaultBackofficeId),
					Flags = flags & ~DependencyFlags.IncludeChildren,
					Icon = "icon-umbraco"
				},
				new SyncItem
				{
					Name = "Frontend",
					Udi = Udi.Create(CspManagerConstants.EntityTypes.CspPolicy, CspManagerConstants.DefaultFrontEndId),
					Flags = flags & ~DependencyFlags.IncludeChildren,
					Icon = "icon-globe"
				}
			];
		}

		return [];
	}
}