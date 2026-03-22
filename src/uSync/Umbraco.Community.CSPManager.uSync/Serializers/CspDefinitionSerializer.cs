using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

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
	///  delete - you can't delete the csp definitions, so we just ignore this.
	/// </summary>
	/// <param name="item"></param>
	public override Task DeleteItemAsync(CspDefinition item) => Task.CompletedTask;

	public override async Task<CspDefinition?> FindItemAsync(Guid key)
	{
		logger.LogWarning("FindItemAsync(Guid) called with key: {Key}", key);
		return await _cspService.GetCspDefinitionAsync(key, CancellationToken.None);
	}

	public override async Task<CspDefinition?> FindItemAsync(string alias)
	{
		logger.LogWarning("FindItemAsync(string) called with alias: {Alias}", alias);
		return await _cspService.GetCspDefinitionAsync(alias.Equals("backoffice", StringComparison.InvariantCultureIgnoreCase), CancellationToken.None);
	}

	public override string ItemAlias(CspDefinition item) => item.IsBackOffice ? "backoffice" : "front-end";

	public override Guid ItemKey(CspDefinition item) => item.Id;

	public override async Task SaveItemAsync(CspDefinition item) => await _cspService.SaveCspDefinitionAsync(item, CancellationToken.None);

	protected override async Task<SyncAttempt<CspDefinition>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
	{
		logger.LogWarning("DeserializeCoreAsync called for node alias: {Alias} key: {Key}", node.GetAlias(), node.GetKey());
		// find item is a base class method it will look for the item by key and alias.
		var nodeKey = node.GetKey();
		var definition = await FindItemAsync(nodeKey);
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
			else
			{
				// assuming the two CspDefinition's exist - as we can't create them here?
				return SyncAttempt<CspDefinition>.Fail(node.GetAlias(), ChangeType.Fail, "Cannot find CSPDefinition ?");
			}
		}

		var infoNode = node.Element("Info");
		if (infoNode is null)
		{
			return SyncAttempt<CspDefinition>.Fail(node.GetAlias(), ChangeType.Fail, "No Info node");
		}

		var details = new List<uSyncChange>();

		var enabled = infoNode.Element("Enabled").ValueOrDefault(false);
		details.AddIfUpdated(nameof(definition.Enabled), definition.Enabled, enabled);
		definition.Enabled = enabled;

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

		logger.LogWarning("Deserialization completed for {Alias} with {ChangeCount} changes", node.GetAlias(), details.Count);
		foreach (var detail in details)
		{
			logger.LogWarning("Change detected for {Alias} - {Property} changed from {OldValue} to {NewValue}",
				node.GetAlias(), detail.Name, detail.OldValue, detail.NewValue);
		}
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
			var definitiondId = sourceNode.Attribute("definitionId").ValueOrDefault(Guid.Empty);
			if (definitiondId == Guid.Empty) continue;

			var sourceValue = sourceNode.Attribute("value")?.Value
				?? sourceNode.Element("Value").ValueOrDefault(string.Empty);

			var directivesElement = sourceNode.Element("Directives");
			List<string> directives;
			if (directivesElement?.HasElements == true)
				directives = directivesElement.Elements("Directive").Select(x => x.Value).ToList();
			else
				directives = directivesElement?.Value
					.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList() ?? [];

			var oldSource = definition.Sources.Find(s => s.DefinitionId == definitiondId);
			var source = oldSource ??
				new CspDefinitionSource()
				{
					DefinitionId = definitiondId,
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
		logger.LogWarning("SerializeCoreAsync called for item alias: {Alias} key: {Key}", alias, item.Id);

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