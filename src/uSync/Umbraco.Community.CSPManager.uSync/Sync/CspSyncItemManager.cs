using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.uSync.Logging;
using uSync.Core.Dependency;
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
		Log.GetTreeType(_logger, treeItem.Id, treeItem.SectionAlias);
		return SyncTreeType.Settings;
	}

	public override string[] EntityTypes => [CspManagerConstants.EntityTypes.CspPolicy];

	public override string[] Trees => ["Umbraco.Community.CSPManager.Tree"];

	public async Task<SyncEntity?> GetSyncEntityAsync(string key)
	{
		if (!Guid.TryParse(key, out var guidKey))
		{
			Log.InvalidCspDefinitionKey(_logger, key);
			return null;
		}

		var definition = await _cspService.GetCspDefinitionAsync(guidKey, CancellationToken.None);
		if (definition is null)
		{
			Log.CspDefinitionNotFound(_logger, key);
			return null;
		}

		return new SyncEntity
		{
			Name = CspItemNames.Name(definition),
			Udi = Udi.Create(CspManagerConstants.EntityTypes.CspPolicy, definition.Id),
		};
	}

	public override async Task<IEnumerable<SyncItem>> GetItemsAsync(SyncItem item)
	{
		var items = new List<SyncItem>();

		if (item.Udi.EntityType == CspManagerConstants.EntityTypes.CspPolicy && !item.Udi.IsRoot)
		{
			items.Add(item);
		}

		if (item.Flags.HasFlag(DependencyFlags.IncludeChildren))
		{
			items.AddRange(await GetDescendantsAsync(item, item.Flags & ~DependencyFlags.IncludeChildren));
		}

		Log.GetItemsResult(_logger, item.Udi, items.Count);
		foreach (var syncItem in items)
		{
			Log.IncludedSyncItem(_logger, syncItem.Udi, syncItem.Name, syncItem.Flags, syncItem.Change);
		}
		return items;
	}

	protected override async Task<IEnumerable<SyncItem>> GetDescendantsAsync(SyncItem item, DependencyFlags flags)
	{
		Log.GetDescendants(_logger, item.Udi, flags, item.Udi.IsRoot);
		if (!item.Udi.IsRoot)
		{
			return [];
		}

		var childFlags = flags & ~DependencyFlags.IncludeChildren;
		var items = new List<SyncItem>
		{
			new()
			{
				Name = "Backoffice",
				Udi = Udi.Create(CspManagerConstants.EntityTypes.CspPolicy, CspManagerConstants.DefaultBackofficeId),
				Flags = childFlags,
				Icon = "icon-umbraco"
			},
			new()
			{
				Name = "Frontend",
				Udi = Udi.Create(CspManagerConstants.EntityTypes.CspPolicy, CspManagerConstants.DefaultFrontEndId),
				Flags = childFlags,
				Icon = "icon-globe"
			}
		};

		foreach (var policy in await _cspService.GetAllNodePoliciesAsync(CancellationToken.None))
		{
			items.Add(new SyncItem
			{
				Name = CspItemNames.Name(policy),
				Udi = Udi.Create(CspManagerConstants.EntityTypes.CspPolicy, policy.Id),
				Flags = childFlags,
				Icon = "icon-home"
			});
		}

		return items;
	}
}
