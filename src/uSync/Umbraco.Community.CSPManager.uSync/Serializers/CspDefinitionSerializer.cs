using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

using CspManagerConstants = Umbraco.Community.CSPManager.Constants;

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
		if (key.Equals(CspManagerConstants.DefaultBackofficeId))
		{
			logger.LogWarning("FindItemAsync(Guid): matched backoffice key");
			return await _cspService.GetCspDefinitionAsync(true, CancellationToken.None);
		}

		if (key.Equals(CspManagerConstants.DefaultFrontEndId))
		{
			logger.LogWarning("FindItemAsync(Guid): matched frontend key");
			return await _cspService.GetCspDefinitionAsync(false, CancellationToken.None);
		}

		logger.LogWarning("FindItemAsync(Guid): no match for key {Key}", key);
		return null;
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
		var item = await FindItemAsync(node);
		if (item is null)
		{
			// assuming the two CspDefinition's exist - as we can't create them here?
			return SyncAttempt<CspDefinition>.Fail(node.GetAlias(), ChangeType.Fail, "Cannot find CSPDefinition ?");
		}

		var infoNode = node.Element("Info");
		if (infoNode is null)
		{
			return SyncAttempt<CspDefinition>.Fail(node.GetAlias(), ChangeType.Fail, "No Info node");
		}
		item.Enabled = infoNode.Element("Enabled").ValueOrDefault(false); // default to false
		item.ReportOnly = infoNode.Element("ReportOnly").ValueOrDefault(false); // default to false
		item.ReportUri = infoNode.Element("ReportUri").ValueOrDefault(string.Empty);
		item.ReportUri = infoNode.Element("ReportUri").ValueOrDefault(string.Empty);
		item.ReportingDirective = infoNode.Element("ReportingDirective").ValueOrDefault(string.Empty);
		item.UpgradeInsecureRequests = infoNode.Element("UpgradeInsecureRequests").ValueOrDefault(false);

		item.Sources = DeserializeSources(node, options);

		return SyncAttempt<CspDefinition>.Succeed(ItemAlias(item), item, ChangeType.Import, []);
	}

	private static List<CspDefinitionSource> DeserializeSources(XElement node, SyncSerializerOptions options)
	{
		var sources = new List<CspDefinitionSource>();
		var sourcesNode = node.Element("Sources");
		if (sourcesNode is null)
			return sources;

		foreach (var sourceNode in sourcesNode.Elements("Source"))
		{
			var source = new CspDefinitionSource
			{
				DefinitionId = sourceNode.Attribute("definitionId").ValueOrDefault(Guid.Empty),
				Source = sourceNode.Element("Value").ValueOrDefault(string.Empty),
				Directives = sourceNode.Element("Directives")?.Elements("Directive").Select(x => x.Value).ToList() ?? []
			};
			sources.Add(source);
		}
		return sources;
	}

	protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(CspDefinition item, SyncSerializerOptions options)
	{
		var alias = ItemAlias(item);
		logger.LogWarning("SerializeCoreAsync called for item alias: {Alias} key: {Key}", alias, item.Id);

		var node = new XElement(ItemType,
			new XAttribute("Key", item.Id),
			new XAttribute("Alias", alias));

		var info = new XElement("Info",
			new XElement("IsBackOffice", item.IsBackOffice),
			new XElement("Enabled", item.Enabled),
			new XElement("ReportOnly", item.ReportOnly),
			new XElement("ReportUri", item.ReportUri ?? string.Empty),
			new XElement("ReportingDirective", item.ReportingDirective ?? string.Empty),
			new XElement("UpgradeInsecureRequests", item.UpgradeInsecureRequests)
		);

		node.Add(info);
		node.Add(SerializeSources(item, options));

		return Task.FromResult(SyncAttempt<XElement>.Succeed(alias, node, ChangeType.Export, []));
	}

	private static XElement SerializeSources(CspDefinition item, SyncSerializerOptions options)
	{
		var sources = new XElement("Sources");
		foreach (CspDefinitionSource source in item.Sources)
		{
			var sourceNode = new XElement("Source",
				new XAttribute("definitionId", source.DefinitionId),
				new XElement("Value", source.Source));

			var directives = new XElement("Directives");

			foreach (var directive in source.Directives)
			{
				directives.Add(new XElement("Directive", directive));
			}

			sourceNode.Add(directives);
			sources.Add(sourceNode);
		}
		return sources;
	}
}
