using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.uSync.Logging;

namespace Umbraco.Community.CSPManager.uSync.Serializers;

/// <summary>
/// Serializes node policies. A file is matched to a policy by its content key, never by the
/// definition id, which is random per environment.
/// </summary>
[SyncSerializer("5d0c1f0e-3a5e-4f6b-9a8a-2f0b8c6e7d41", "Csp Manager Node Policy Serializer", CspItemNames.NodePolicyEntityType)]
public class CspNodePolicySerializer : CspSerializerBase
{
	public CspNodePolicySerializer(ILogger<CspNodePolicySerializer> logger, ICspService cspService)
		: base(logger, cspService)
	{
	}

	public override async Task DeleteItemAsync(CspDefinition item)
	{
		if (item.ContentKey is null) return;

		Log.DeleteNodePolicy(logger, item.Id, ItemAlias(item));
		await CspService.DeleteCspDefinitionAsync(item.Id, CancellationToken.None);
	}

	public override async Task<CspDefinition?> FindItemAsync(Guid key)
	{
		Log.FindItemByKey(logger, key);
		var policies = await CspService.GetAllNodePoliciesAsync(CancellationToken.None);
		return policies.Find(p => CspItemNames.Key(p) == key);
	}

	public override async Task<CspDefinition?> FindItemAsync(string alias)
	{
		Log.FindItemByAlias(logger, alias);
		if (alias.StartsWith(CspItemNames.NodeAliasPrefix, StringComparison.InvariantCultureIgnoreCase)
			&& Guid.TryParse(alias[CspItemNames.NodeAliasPrefix.Length..], out var contentKey))
			return await CspService.GetCspDefinitionForNodeAsync(contentKey, CancellationToken.None);
		return null;
	}

	protected override async Task<(CspDefinition? Definition, string? Error)> ResolveDefinitionAsync(XElement node, Guid? contentKey)
	{
		if (contentKey is not { } key)
		{
			return (null, "A node policy needs the ContentKey of its content node.");
		}

		var existing = await CspService.GetCspDefinitionForNodeAsync(key, CancellationToken.None);
		if (existing is not null)
		{
			return (existing, null);
		}

		// CspService refuses a new policy for a node without a hostname, but by throwing from the
		// save. Checked here so the item fails with a reason instead.
		if (!await CspService.ContentNodeHasHostnameAsync(key, CancellationToken.None))
		{
			return (null, "Its content node doesn't exist on this site, is in the recycle bin, or has no hostname in Culture and Hostnames. Import the content (with its domains) first.");
		}

		// An empty id makes CspService assign a local one.
		return (new CspDefinition { Id = Guid.Empty, ContentKey = key, IsBackOffice = false }, null);
	}
}
