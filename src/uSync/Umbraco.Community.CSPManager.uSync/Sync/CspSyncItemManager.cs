using Umbraco.Cms.Core;
using Umbraco.Community.CSPManager.Services;
using uSync.Core.Dependency;
using uSync.Core.Extensions;
using uSync.Core.Sync;

using CspManagerConstants = Umbraco.Community.CSPManager.Constants;

namespace Umbraco.Community.CSPManager.uSync.Sync;

[SyncItemManager(Constants.CspPolicyEntityType, "")]
public class CspSyncItemManager : SyncItemManagerBase, ISyncItemManager
{
	private readonly ICspService _cspService;

	public CspSyncItemManager(ICspService cspService)
	{
		_cspService = cspService;
	}

	public override string[] EntityTypes => [Constants.CspPolicyEntityType];

	public async Task<SyncEntity?> GetSyncEntityAsync(string key)
	{
		if (!Guid.TryParse(key, out var guidKey))
			return null;

		bool? isBackOffice = guidKey == CspManagerConstants.DefaultBackofficeId ? true
			: guidKey == CspManagerConstants.DefaultFrontEndId ? false
			: null;

		if (isBackOffice is null)
			return null;

		var definition = await _cspService.GetCspDefinitionAsync(isBackOffice.Value, CancellationToken.None);
		return new SyncEntity
		{
			Icon = "icon-shield",
			Name = isBackOffice.Value ? "Backoffice" : "Frontend",
			Udi = Udi.Create(Constants.CspPolicyEntityType, definition.Id)
		};
	}

	public override Task<IEnumerable<SyncItem>> GetItemsAsync(SyncItem item)
		=> uSyncTaskHelper.FromResultOf(() => GetItems(item));

	private IEnumerable<SyncItem> GetItems(SyncItem item)
	{
		var items = new List<SyncItem>();

		if (item.Udi.EntityType == Constants.CspPolicyEntityType && !item.Udi.IsRoot)
		{
			items.Add(item);
		}

		if (item.Flags.HasFlag(DependencyFlags.IncludeChildren) || item.Udi.IsRoot)
		{
			items.AddRange(GetDescendants(item, item.Flags & ~DependencyFlags.IncludeChildren));
		}

		return items;
	}

	protected override Task<IEnumerable<SyncItem>> GetDescendantsAsync(SyncItem item, DependencyFlags flags)
		=> uSyncTaskHelper.FromResultOf(() => GetDescendants(item, flags));

	private IEnumerable<SyncItem> GetDescendants(SyncItem item, DependencyFlags flags)
	{
		if (!item.Udi.IsRoot)
			return Enumerable.Empty<SyncItem>();

		return
		[
			new SyncItem
			{
				Name = "Backoffice",
				Udi = Udi.Create(Constants.CspPolicyEntityType, CspManagerConstants.DefaultBackofficeId),
				Flags = flags & ~DependencyFlags.IncludeChildren
			},
			new SyncItem
			{
				Name = "Frontend",
				Udi = Udi.Create(Constants.CspPolicyEntityType, CspManagerConstants.DefaultFrontEndId),
				Flags = flags & ~DependencyFlags.IncludeChildren
			}
		];
	}
}
