using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using uSync.Core.Dependency;

namespace Umbraco.Community.CSPManager.uSync.Logging;

/// <summary>
/// High-performance logging methods for CSP Manager uSync using source generation.
/// </summary>
/// <remarks>
/// Uses <see cref="LoggerMessageAttribute"/> for compile-time log message generation,
/// providing better performance than runtime string interpolation.
/// </remarks>
internal static partial class Log
{
	// ===========================================
	// Handler Events (1-19)
	// ===========================================

	[LoggerMessage(
		EventId = 1,
		Level = LogLevel.Debug,
		Message = "Exported CSP definition {ItemId} with change type {ChangeType}: {Message}")]
	public static partial void CspExportAttempt(ILogger logger, Guid itemId, ChangeType changeType, string? message);

	[LoggerMessage(
		EventId = 2,
		Level = LogLevel.Warning,
		Message = "Failed to create uSync export file")]
	public static partial void CspExportFailed(ILogger logger, Exception ex);

	[LoggerMessage(
		EventId = 4,
		Level = LogLevel.Debug,
		Message = "No child items for CSP definition {ParentId}")]
	public static partial void NoChildItemsForDefinition(ILogger logger, Guid parentId);

	[LoggerMessage(
		EventId = 5,
		Level = LogLevel.Debug,
		Message = "Returning CSP definitions: backoffice={BackofficeId} frontend={FrontendId}")]
	public static partial void GetChildItemsResult(ILogger logger, Guid backofficeId, Guid frontendId);

	// ===========================================
	// Serializer Events (20-39)
	// ===========================================

	[LoggerMessage(
		EventId = 20,
		Level = LogLevel.Debug,
		Message = "Finding CSP definition by key {Key}")]
	public static partial void FindItemByKey(ILogger logger, Guid key);

	[LoggerMessage(
		EventId = 21,
		Level = LogLevel.Debug,
		Message = "Finding CSP definition by alias '{Alias}'")]
	public static partial void FindItemByAlias(ILogger logger, string alias);

	[LoggerMessage(
		EventId = 22,
		Level = LogLevel.Debug,
		Message = "Deserializing CSP definition alias='{Alias}' key={Key}")]
	public static partial void DeserializeStart(ILogger logger, string alias, Guid key);

	[LoggerMessage(
		EventId = 23,
		Level = LogLevel.Debug,
		Message = "Deserialization complete for '{Alias}': {ChangeCount} change(s)")]
	public static partial void DeserializeComplete(ILogger logger, string alias, int changeCount);

	[LoggerMessage(
		EventId = 25,
		Level = LogLevel.Debug,
		Message = "Serializing CSP definition alias='{Alias}' key={Key}")]
	public static partial void SerializeStart(ILogger logger, string alias, Guid key);

	// ===========================================
	// SyncItemManager Events (40-59)
	// ===========================================

	[LoggerMessage(
		EventId = 40,
		Level = LogLevel.Debug,
		Message = "GetTreeType: treeItem={TreeItemId} section={SectionAlias}")]
	public static partial void GetTreeType(ILogger logger, string? treeItemId, string? sectionAlias);

	[LoggerMessage(
		EventId = 41,
		Level = LogLevel.Warning,
		Message = "Cannot get CSP definition: invalid key '{Key}'")]
	public static partial void InvalidCspDefinitionKey(ILogger logger, string key);

	[LoggerMessage(
		EventId = 42,
		Level = LogLevel.Warning,
		Message = "No CSP definition found for key '{Key}'")]
	public static partial void CspDefinitionNotFound(ILogger logger, string key);

	[LoggerMessage(
		EventId = 43,
		Level = LogLevel.Debug,
		Message = "GetItems for {ItemUdi}: returned {ItemCount} item(s)")]
	public static partial void GetItemsResult(ILogger logger, Udi itemUdi, int itemCount);

	[LoggerMessage(
		EventId = 44,
		Level = LogLevel.Debug,
		Message = "Included item {ItemUdi} name='{Name}' flags={Flags} change={Change}")]
	public static partial void IncludedSyncItem(ILogger logger, Udi itemUdi, string name, DependencyFlags flags, ChangeType change);

	[LoggerMessage(
		EventId = 45,
		Level = LogLevel.Debug,
		Message = "GetDescendants: item={ItemUdi} flags={Flags} isRoot={IsRoot}")]
	public static partial void GetDescendants(ILogger logger, Udi itemUdi, DependencyFlags flags, bool isRoot);
}