using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.uSync.Logging;

namespace Umbraco.Community.CSPManager.uSync.Serializers;

/// <summary>
/// Serializes node policies. A file is matched to a policy by its content key and culture, never
/// by the definition id, which is random per environment.
/// </summary>
[SyncSerializer("5d0c1f0e-3a5e-4f6b-9a8a-2f0b8c6e7d41", "Csp Manager Node Policy Serializer", CspItemNames.NodePolicyEntityType)]
public class CspNodePolicySerializer : CspSerializerBase
{
	private readonly ILanguageService _languageService;

	public CspNodePolicySerializer(ILogger<CspNodePolicySerializer> logger, ICspService cspService, ILanguageService languageService)
		: base(logger, cspService)
	{
		_languageService = languageService;
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
		return CspItemNames.TryParseNodeAlias(alias, out var contentKey, out var culture)
			? await CspService.GetCspDefinitionForNodeAsync(contentKey, culture, CancellationToken.None)
			: null;
	}

	protected override async Task<(CspDefinition? Definition, string? Error)> ResolveDefinitionAsync(XElement node, Guid? contentKey)
	{
		if (contentKey is not { } key)
		{
			return (null, "A node policy needs the ContentKey of its content node.");
		}

		var culture = node.Element("Info")?.Element(nameof(CspDefinition.Culture)).ValueOrDefault(string.Empty);
		if (string.IsNullOrWhiteSpace(culture))
		{
			culture = null;
		}

		var existing = await CspService.GetCspDefinitionForNodeAsync(key, culture, CancellationToken.None);
		if (existing is not null)
		{
			return (existing, null);
		}

		// CspService refuses a new policy for a node or language that doesn't exist, but by throwing
		// from the save. Checked here so the item fails with a reason instead.
		if (!await CspService.ContentNodeExistsAsync(key, CancellationToken.None))
		{
			return (null, "Its content node doesn't exist on this site or is in the recycle bin. Import the content first.");
		}

		if (culture is not null && await _languageService.GetAsync(culture) is null)
		{
			return (null, $"Its culture '{culture}' isn't a language on this site. Import the languages first.");
		}

		// An empty id makes CspService assign a local one.
		return (new CspDefinition { Id = Guid.Empty, ContentKey = key, Culture = culture, IsBackOffice = false }, null);
	}
}
