using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.uSync.Logging;

using CspManagerConstants = Umbraco.Community.CSPManager.Constants;
using uSyncConstants = uSync.Core.uSyncConstants;

namespace Umbraco.Community.CSPManager.uSync.Serializers;

[SyncSerializer("f8c88eea-50c1-4146-95e9-a3c148339aea", "Csp Manager Serializer", CspManagerConstants.EntityTypes.CspPolicy)]
public class CspDefinitionSerializer : SyncSerializerRoot<CspDefinition>, ISyncSerializer<CspDefinition>
{
	private const string DomainAliasPrefix = "domain-";

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
	public override Task DeleteItemAsync(CspDefinition item) => Task.CompletedTask;

	public override async Task<CspDefinition?> FindItemAsync(Guid key)
	{
		Log.FindItemByKey(logger, key);
		return await _cspService.GetCspDefinitionAsync(key, CancellationToken.None);
	}

	public override async Task<CspDefinition?> FindItemAsync(string alias)
	{
		Log.FindItemByAlias(logger, alias);
		if (alias.Equals("backoffice", StringComparison.InvariantCultureIgnoreCase))
			return await _cspService.GetCspDefinitionAsync(true, CancellationToken.None);
		if (alias.Equals("front-end", StringComparison.InvariantCultureIgnoreCase))
			return await _cspService.GetCspDefinitionAsync(false, CancellationToken.None);
		if (alias.StartsWith(DomainAliasPrefix, StringComparison.InvariantCultureIgnoreCase))
		{
			var keyStr = alias[DomainAliasPrefix.Length..];
			if (Guid.TryParse(keyStr, out var domainKey))
				return await _cspService.GetCspDefinitionForDomainAsync(domainKey, CancellationToken.None);
		}

		return null;
	}

	public override string ItemAlias(CspDefinition item) =>
		item.DomainKey.HasValue
			? $"{DomainAliasPrefix}{item.DomainKey.Value}"
			: item.IsBackOffice ? "backoffice" : "front-end";

	public override Guid ItemKey(CspDefinition item) => item.Id;

	public override async Task SaveItemAsync(CspDefinition item) => await _cspService.SaveCspDefinitionAsync(item, CancellationToken.None);

	protected override async Task<SyncAttempt<CspDefinition>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
	{

		var nodeKey = node.GetKey();
		var alias = node.GetAlias();
		Log.DeserializeStart(logger, alias, nodeKey);
		var definition = await FindItemAsync(nodeKey);

		var infoNode = node.Element("Info");
		if (infoNode is null)
		{
			return SyncAttempt<CspDefinition>.Fail(alias, ChangeType.Fail, "No Info node");
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
			else
			{
				// Check if this is a domain policy (has a DomainKey in the Info node)
				var domainKeyStr = infoNode.Element("DomainKey").ValueOrDefault(string.Empty);
				if (!string.IsNullOrEmpty(domainKeyStr) && Guid.TryParse(domainKeyStr, out var domainKey))
				{
					// Check if a policy for this domain already exists (idempotency: prefer matching by DomainKey)
					var existingByDomain = await _cspService.GetCspDefinitionForDomainAsync(domainKey, CancellationToken.None);
					definition = existingByDomain ?? new CspDefinition
					{
						Id = nodeKey,
						DomainKey = domainKey,
						IsBackOffice = false
					};
				}
				else
				{
					return SyncAttempt<CspDefinition>.Fail(alias, ChangeType.Fail, "Cannot find CSPDefinition");
				}
			}
		}

		var details = new List<uSyncChange>();

		var enabled = infoNode.Element("Enabled").ValueOrDefault(false);
		details.AddIfUpdated(nameof(definition.Enabled), definition.Enabled, enabled);
		definition.Enabled = enabled;

		var isBackOffice = infoNode.Element("IsBackOffice").ValueOrDefault(false);
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

		var infoElements = new List<object>
		{
			new XElement("IsBackOffice", item.IsBackOffice),
			new XElement("Enabled", item.Enabled),
			new XElement("ReportOnly", item.ReportOnly),
			new XElement("ReportUri", item.ReportUri ?? string.Empty),
			new XElement("ReportingDirective", item.ReportingDirective ?? string.Empty),
			new XElement("UpgradeInsecureRequests", item.UpgradeInsecureRequests)
		};

		if (item.DomainKey.HasValue)
		{
			infoElements.Add(new XElement("DomainKey", item.DomainKey.Value));
		}

		var info = new XElement("Info", infoElements.ToArray());

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
