using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.uSync.Logging;

using CspManagerConstants = Umbraco.Community.CSPManager.Constants;
using uSyncConstants = uSync.Core.uSyncConstants;

namespace Umbraco.Community.CSPManager.uSync.Serializers;

[SyncSerializer("f8c88eea-50c1-4146-95e9-a3c148339aea", "Csp Manager Serializer", CspManagerConstants.EntityTypes.CspPolicy)]
public class CspDefinitionSerializer : SyncSerializerRoot<CspDefinition>, ISyncSerializer<CspDefinition>
{
	private readonly ICspService _cspService;

	public CspDefinitionSerializer(
		ILogger<CspDefinitionSerializer> logger,
		ICspService cspService) : base(logger)
	{
		_cspService = cspService;
	}

	/// <summary>
	/// Deletes a node policy. Global definitions cannot be deleted.
	/// </summary>
	public override async Task DeleteItemAsync(CspDefinition item)
	{
		if (item.ContentKey is null) return;

		Log.DeleteNodePolicy(logger, item.Id, ItemAlias(item));
		await _cspService.DeleteCspDefinitionAsync(item.Id, CancellationToken.None);
	}

	public override async Task<CspDefinition?> FindItemAsync(Guid key)
	{
		Log.FindItemByKey(logger, key);
		return await _cspService.GetCspDefinitionAsync(key, CancellationToken.None);
	}

	public override async Task<CspDefinition?> FindItemAsync(string alias)
	{
		Log.FindItemByAlias(logger, alias);
		if (alias.Equals(CspItemNames.BackOfficeAlias, StringComparison.InvariantCultureIgnoreCase))
			return await _cspService.GetCspDefinitionAsync(true, CancellationToken.None);
		if (alias.Equals(CspItemNames.FrontEndAlias, StringComparison.InvariantCultureIgnoreCase))
			return await _cspService.GetCspDefinitionAsync(false, CancellationToken.None);
		if (alias.StartsWith(CspItemNames.NodeAliasPrefix, StringComparison.InvariantCultureIgnoreCase)
			&& Guid.TryParse(alias[CspItemNames.NodeAliasPrefix.Length..], out var contentKey))
			return await _cspService.GetCspDefinitionForNodeAsync(contentKey, CancellationToken.None);
		return null;
	}

	public override string ItemAlias(CspDefinition item) => CspItemNames.Alias(item);

	public override Guid ItemKey(CspDefinition item) => item.Id;

	public override async Task SaveItemAsync(CspDefinition item) => await _cspService.SaveCspDefinitionAsync(item, CancellationToken.None);

	protected override async Task<SyncAttempt<CspDefinition>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
	{

		var nodeKey = node.GetKey();
		var alias = node.GetAlias();
		Log.DeserializeStart(logger, alias, nodeKey);

		var infoNode = node.Element("Info");
		if (infoNode is null)
		{
			return SyncAttempt<CspDefinition>.Fail(alias, ChangeType.Fail, "No Info node");
		}

		var contentKey = infoNode.Element(nameof(CspDefinition.ContentKey)).ValueOrDefault(Guid.Empty);
		var definition = await FindItemAsync(nodeKey);

		if (definition is null && contentKey != Guid.Empty)
		{
			// The node may already have a policy created on this environment under another id.
			// One policy per node, so update that one rather than fail on a second.
			definition = await _cspService.GetCspDefinitionForNodeAsync(contentKey, CancellationToken.None);
		}

		if (definition is null)
		{
			if (nodeKey == CspManagerConstants.DefaultBackofficeId || nodeKey == CspManagerConstants.DefaultFrontEndId)
			{
				definition = new CspDefinition
				{
					Id = nodeKey,
					IsBackOffice = nodeKey == CspManagerConstants.DefaultBackofficeId
				};
			}
			else if (contentKey != Guid.Empty)
			{
				// CspService refuses a new policy for a node without a hostname, but by throwing from
				// the save. Checked here so the item fails with a reason instead (e.g. a Settings import
				// before the Content import that brings the node and its Culture and Hostnames).
				if (!await _cspService.ContentNodeHasHostnameAsync(contentKey, CancellationToken.None))
				{
					const string reason = "Its content node doesn't exist on this site, is in the recycle bin, or has no hostname in Culture and Hostnames. Import the content (with its domains) first.";
					Log.DeserializeInvalid(logger, alias, reason);
					return SyncAttempt<CspDefinition>.Fail(alias, ChangeType.Fail, reason);
				}

				// Keeps the source id so later syncs match on key.
				definition = new CspDefinition
				{
					Id = nodeKey,
					ContentKey = contentKey,
					IsBackOffice = false
				};
			}
			else
			{
				// Only the two global definitions exist without a content node.
				return SyncAttempt<CspDefinition>.Fail(alias, ChangeType.Fail, "Cannot find CSPDefinition");
			}
		}
		else if (definition.ContentKey != (contentKey == Guid.Empty ? null : contentKey))
		{
			// An existing definition never changes between global and node policy, or between nodes.
			return SyncAttempt<CspDefinition>.Fail(alias, ChangeType.Fail, "The CSP definition's content node does not match the existing definition");
		}

		var details = new List<uSyncChange>();

		var enabled = infoNode.Element("Enabled").ValueOrDefault(false);
		details.AddIfUpdated(nameof(definition.Enabled), definition.Enabled, enabled);
		definition.Enabled = enabled;

		// Node policies are never backoffice policies, whatever the file says.
		var isBackOffice = definition.ContentKey is null && infoNode.Element("IsBackOffice").ValueOrDefault(false);
		details.AddIfUpdated(nameof(definition.IsBackOffice), definition.IsBackOffice, isBackOffice);
		definition.IsBackOffice = isBackOffice;

		var reportOnly = infoNode.Element("ReportOnly").ValueOrDefault(false);
		details.AddIfUpdated(nameof(definition.ReportOnly), definition.ReportOnly, reportOnly);
		definition.ReportOnly = reportOnly;

		var reportUri = infoNode.Element("ReportUri").ValueOrDefault(string.Empty);
		details.AddIfUpdated(nameof(definition.ReportUri), definition.ReportUri, reportUri);
		definition.ReportUri = reportUri;

		var reportingDirective = infoNode.Element("ReportingDirective").ValueOrDefault(string.Empty);
		details.AddIfUpdated(nameof(definition.ReportingDirective), definition.ReportingDirective, reportingDirective);
		definition.ReportingDirective = reportingDirective;

		var upgradeInsecureRequests = infoNode.Element("UpgradeInsecureRequests").ValueOrDefault(false);
		details.AddIfUpdated(nameof(definition.UpgradeInsecureRequests), definition.UpgradeInsecureRequests, upgradeInsecureRequests);
		definition.UpgradeInsecureRequests = upgradeInsecureRequests;

		definition.Sources = DeserializeSources(node, definition, details);

		// The same header rules CspService enforces on save, checked here so an invalid file (e.g. a
		// source that splices in another directive, or a line break) fails this one item with a
		// readable reason instead of an exception from the save. Blank sources are dropped on save,
		// so they don't count.
		var errors = CspDefinitionValidator.Validate(new CspDefinition
		{
			ReportingDirective = definition.ReportingDirective,
			ReportUri = definition.ReportUri,
			Sources = [.. definition.Sources.Where(s => !string.IsNullOrWhiteSpace(s.Source))]
		});
		if (errors.Count > 0)
		{
			var reason = string.Join(" ", errors.Select(e => e.ErrorMessage));
			Log.DeserializeInvalid(logger, alias, reason);
			return SyncAttempt<CspDefinition>.Fail(alias, ChangeType.Fail, $"Invalid CSP definition: {reason}");
		}

		Log.DeserializeComplete(logger, alias, details.Count);

		return SyncAttempt<CspDefinition>.Succeed(ItemAlias(definition), definition, ChangeType.Import, details);
	}

	private static List<CspDefinitionSource> DeserializeSources(XElement node, CspDefinition definition, List<uSyncChange> details)
	{
		var sources = new List<CspDefinitionSource>();
		var sourcesNode = node.Element("Sources");
		if (sourcesNode is null)
			return sources;

		foreach (var sourceNode in sourcesNode.Elements("Source"))
		{
			var definitionId = sourceNode.Attribute("definitionId").ValueOrDefault(Guid.Empty);
			if (definitionId == Guid.Empty) continue;

			var sourceValue = sourceNode.Attribute("value")?.Value
				?? sourceNode.Element("Value").ValueOrDefault(string.Empty);

			// Directives are serialized as a comma-separated string e.g. "script-src, default-src".
			// If you have uSync files from a different format, run a uSync export to regenerate them.
			var directivesElement = sourceNode.Element("Directives");
			var directives = directivesElement?.Value
				.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList() ?? [];

			var oldSource = definition.Sources.Find(s => s.DefinitionId == definitionId && s.Source == sourceValue);
			var source = oldSource ??
				new CspDefinitionSource()
				{
					DefinitionId = definitionId,
					Source = sourceValue
				};

			details.AddIfUpdated(nameof(CspDefinitionSource.Source), oldSource?.Directives, directives);
			source.Directives = directives;
			sources.Add(source);
		}
		return sources;
	}

	protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(CspDefinition item, SyncSerializerOptions options)
	{
		var alias = ItemAlias(item);
		Log.SerializeStart(logger, alias, item.Id);

		var node = new XElement(ItemType,
			new XAttribute(uSyncConstants.Xml.Key, ItemKey(item)),
			new XAttribute(uSyncConstants.Xml.Alias, alias));

		var info = new XElement("Info",
			new XElement("IsBackOffice", item.IsBackOffice),
			new XElement("Enabled", item.Enabled),
			new XElement("ReportOnly", item.ReportOnly),
			new XElement("ReportUri", item.ReportUri ?? string.Empty),
			new XElement("ReportingDirective", item.ReportingDirective ?? string.Empty),
			new XElement("UpgradeInsecureRequests", item.UpgradeInsecureRequests)
		);

		// Only node policies carry the element, so the global definitions' files are unchanged.
		if (item.ContentKey is { } contentKey)
		{
			info.Add(new XElement(nameof(CspDefinition.ContentKey), contentKey));
		}

		node.Add(info);
		node.Add(SerializeSources(item));

		return Task.FromResult(SyncAttempt<XElement>.Succeed(alias, node, ChangeType.Export, []));
	}

	private static XElement SerializeSources(CspDefinition item)
	{
		var sources = new XElement("Sources");
		foreach (CspDefinitionSource source in item.Sources)
		{
			var sourceNode = new XElement("Source",
				new XAttribute("definitionId", source.DefinitionId),
				new XAttribute("value", source.Source),
				new XElement("Directives", string.Join(", ", source.Directives)));

			sources.Add(sourceNode);
		}

		return sources;
	}
}