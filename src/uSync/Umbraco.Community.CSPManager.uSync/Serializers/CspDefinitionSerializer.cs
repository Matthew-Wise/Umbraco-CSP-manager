using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.uSync.Logging;

using CspManagerConstants = Umbraco.Community.CSPManager.Constants;

namespace Umbraco.Community.CSPManager.uSync.Serializers;

/// <summary>
/// Serializes the two global policies (Frontend and Backoffice). Node policies have their own
/// <see cref="CspNodePolicySerializer"/>.
/// </summary>
[SyncSerializer("f8c88eea-50c1-4146-95e9-a3c148339aea", "Csp Manager Serializer", CspManagerConstants.EntityTypes.CspPolicy)]
public class CspDefinitionSerializer : CspSerializerBase
{
	public CspDefinitionSerializer(ILogger<CspDefinitionSerializer> logger, ICspService cspService)
		: base(logger, cspService)
	{
	}

	/// <summary>
	/// Global definitions cannot be deleted.
	/// </summary>
	public override Task DeleteItemAsync(CspDefinition item) => Task.CompletedTask;

	public override async Task<CspDefinition?> FindItemAsync(Guid key)
	{
		Log.FindItemByKey(logger, key);
		return IsGlobalId(key) ? await CspService.GetCspDefinitionAsync(key, CancellationToken.None) : null;
	}

	public override async Task<CspDefinition?> FindItemAsync(string alias)
	{
		Log.FindItemByAlias(logger, alias);
		if (alias.Equals(CspItemNames.BackOfficeAlias, StringComparison.InvariantCultureIgnoreCase))
			return await CspService.GetCspDefinitionAsync(true, CancellationToken.None);
		if (alias.Equals(CspItemNames.FrontEndAlias, StringComparison.InvariantCultureIgnoreCase))
			return await CspService.GetCspDefinitionAsync(false, CancellationToken.None);
		return null;
	}

	protected override async Task<(CspDefinition? Definition, string? Error)> ResolveDefinitionAsync(XElement node, Guid? contentKey)
	{
		if (contentKey is not null)
		{
			return (null, $"Node policies are <{CspItemNames.NodePolicyEntityType}> items. Run a uSync export to regenerate this file.");
		}

		var key = node.GetKey();
		if (!IsGlobalId(key))
		{
			return (null, "Only the Frontend and Backoffice policies are global CSP definitions.");
		}

		var definition = await FindItemAsync(key)
			?? new CspDefinition { Id = key, IsBackOffice = key == CspManagerConstants.DefaultBackofficeId };
		return (definition, null);
	}

	private static bool IsGlobalId(Guid key)
		=> key == CspManagerConstants.DefaultBackofficeId || key == CspManagerConstants.DefaultFrontEndId;
}
