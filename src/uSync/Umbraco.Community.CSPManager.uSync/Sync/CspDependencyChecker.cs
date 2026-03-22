using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Community.CSPManager.Models;
using uSync.Core.Dependency;
namespace Umbraco.Community.CSPManager.uSync.Sync;

public class CspDependencyChecker : ISyncDependencyChecker<CspDefinition>
{
	public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

	public Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(CspDefinition item, DependencyFlags flags)
	{
		if (item == null) return Task.FromResult<IEnumerable<uSyncDependency>>([]);

		return Task.FromResult<IEnumerable<uSyncDependency>>([
			new uSyncDependency
			{
				Name = item.IsBackOffice ? "Backoffice" : "Frontend",
				Udi = Udi.Create(Constants.EntityTypes.CspPolicy, item.Id),
				Order = DependencyOrders.Languages,
				Flags = DependencyFlags.None,
				Level = 0
			}
		]);
	}
}