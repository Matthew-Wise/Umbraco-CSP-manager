using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.uSync.Logging;

using uSyncConstants = uSync.Core.uSyncConstants;

namespace Umbraco.Community.CSPManager.uSync.Serializers;

/// <summary>
/// The XML format shared by the global policies and node policies. Subclasses decide which
/// definition a file belongs to.
/// </summary>
public abstract class CspSerializerBase : SyncSerializerRoot<CspDefinition>, ISyncSerializer<CspDefinition>
{
	protected CspSerializerBase(ILogger<CspSerializerBase> logger, ICspService cspService) : base(logger)
	{
		CspService = cspService;
	}

	protected ICspService CspService { get; }

	public override string ItemAlias(CspDefinition item) => CspItemNames.Alias(item);

	public override Guid ItemKey(CspDefinition item) => CspItemNames.Key(item);

	public override async Task SaveItemAsync(CspDefinition item) => await CspService.SaveCspDefinitionAsync(item, CancellationToken.None);

	/// <summary>
	/// Finds the existing definition a file belongs to, or creates a new one.
	/// </summary>
	/// <param name="node">The file.</param>
	/// <param name="contentKey">The file's content key, or <c>null</c> when it has none.</param>
	/// <returns>The definition, or the reason the file can't be imported.</returns>
	protected abstract Task<(CspDefinition? Definition, string? Error)> ResolveDefinitionAsync(XElement node, Guid? contentKey);

	protected override async Task<SyncAttempt<CspDefinition>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options)
	{
		var alias = node.GetAlias();
		Log.DeserializeStart(logger, alias, node.GetKey());

		var infoNode = node.Element("Info");
		if (infoNode is null)
		{
			return SyncAttempt<CspDefinition>.Fail(alias, ChangeType.Fail, "No Info node");
		}

		var contentKeyValue = infoNode.Element(nameof(CspDefinition.ContentKey)).ValueOrDefault(Guid.Empty);
		var (definition, error) = await ResolveDefinitionAsync(node, contentKeyValue == Guid.Empty ? null : contentKeyValue);
		if (definition is null)
		{
			Log.DeserializeInvalid(logger, alias, error ?? "Cannot find CSP definition");
			return SyncAttempt<CspDefinition>.Fail(alias, ChangeType.Fail, error ?? "Cannot find CSP definition");
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
			var sourceValue = sourceNode.Attribute("value")?.Value
				?? sourceNode.Element("Value").ValueOrDefault(string.Empty);

			// Directives are serialized as a comma-separated string e.g. "script-src, default-src".
			// If you have uSync files from a different format, run a uSync export to regenerate them.
			var directivesElement = sourceNode.Element("Directives");
			var directives = directivesElement?.Value
				.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList() ?? [];

			// A source always belongs to the definition it's in. Older files also carry a
			// definitionId attribute, which is ignored: it's the id on the exporting environment.
			var oldSource = definition.Sources.Find(s => s.Source == sourceValue);
			var source = oldSource ??
				new CspDefinitionSource()
				{
					DefinitionId = definition.Id,
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
			// No definitionId: it's random per environment, so writing it would make every
			// environment's file differ for the same policy.
			var sourceNode = new XElement("Source",
				new XAttribute("value", source.Source),
				new XElement("Directives", string.Join(", ", source.Directives)));

			sources.Add(sourceNode);
		}

		return sources;
	}
}
