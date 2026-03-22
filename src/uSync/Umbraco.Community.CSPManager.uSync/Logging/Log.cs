using Microsoft.Extensions.Logging;

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
}