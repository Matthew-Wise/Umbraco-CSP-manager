using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Community.CSPManager.Models;
using uSync.Core.Dependency;

using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.uSync.Sync;

public class CspDependencyChecker : ISyncDependencyChecker<CspDefinition>
{
	// After content (1000) and domains (1100), so a push creates the node first.
	private static readonly int NodePolicyOrder = DependencyOrders.Domain + 50;

	public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

	public Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(CspDefinition item, DependencyFlags flags)
	{
		if (item == null) return Task.FromResult<IEnumerable<uSyncDependency>>([]);

		List<uSyncDependency> dependencies =
		[
			new uSyncDependency
			{
				Name = CspItemNames.Name(item),
				Udi = CspItemNames.GetUdi(item),
				Order = item.ContentKey is null ? 11 : NodePolicyOrder,
				Flags = DependencyFlags.None,
				Level = 0
			}
		];

		if (item.ContentKey is { } contentKey)
		{
			// A node policy can only be created for a node that exists on the target. The node takes
			// the push's own flags (e.g. ancestors, dependencies) but never its children.
			// Its hostnames are not declared: Umbraco doesn't persist a key for domains (IDomain.Key
			// is new on every load), so a domain dependency could never be found. The target must
			// already have the node's hostnames; the import fails with a clear reason otherwise.
			dependencies.Add(new uSyncDependency
			{
				Name = $"Content node {contentKey:D}",
				Udi = Udi.Create(UmbracoConstants.UdiEntityType.Document, contentKey),
				Order = DependencyOrders.Content,
				Flags = flags & ~DependencyFlags.IncludeChildren,
				Level = 0
			});
		}

		return Task.FromResult<IEnumerable<uSyncDependency>>(dependencies);
	}
}
