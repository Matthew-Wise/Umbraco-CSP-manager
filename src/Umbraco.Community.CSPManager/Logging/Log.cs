using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Umbraco.Community.CSPManager.Logging;

/// <summary>
/// High-performance logging methods for CSP Manager using source generation.
/// </summary>
/// <remarks>
/// Uses <see cref="LoggerMessageAttribute"/> for compile-time log message generation,
/// providing better performance than runtime string interpolation.
/// </remarks>
internal static partial class Log
{
	// ===========================================
	// Middleware Events (1-99)
	// ===========================================

	[LoggerMessage(
		EventId = 1,
		Level = LogLevel.Error,
		Message = "Failed to construct CSP header for {Path}")]
	public static partial void CspHeaderConstructionFailed(ILogger logger, PathString path, Exception ex);

	// ===========================================
	// Service Events (100-199)
	// ===========================================

	[LoggerMessage(
		EventId = 100,
		Level = LogLevel.Information,
		Message = "Saving CSP definition {DefinitionId} for {Context}")]
	public static partial void SavingCspDefinition(ILogger logger, Guid definitionId, string context);

	[LoggerMessage(
		EventId = 101,
		Level = LogLevel.Information,
		Message = "Saved CSP definition {DefinitionId} with {SourceCount} sources")]
	public static partial void CspDefinitionSaved(ILogger logger, Guid definitionId, int sourceCount);

	[LoggerMessage(
		EventId = 102,
		Level = LogLevel.Error,
		Message = "Failed to save CSP definition {DefinitionId}")]
	public static partial void CspDefinitionSaveFailed(ILogger logger, Guid definitionId, Exception ex);

	[LoggerMessage(
		EventId = 103,
		Level = LogLevel.Debug,
		Message = "Retrieved CSP definition {DefinitionId} from cache for {Context}")]
	public static partial void CspDefinitionRetrievedFromCache(ILogger logger, Guid definitionId, string context);

	[LoggerMessage(
		EventId = 104,
		Level = LogLevel.Debug,
		Message = "Loading CSP definition from database for {Context}")]
	public static partial void LoadingCspDefinitionFromDatabase(ILogger logger, string context);

	// ===========================================
	// Cache Events (200-299)
	// ===========================================

	[LoggerMessage(
		EventId = 200,
		Level = LogLevel.Debug,
		Message = "Clearing CSP cache for {CacheKey}")]
	public static partial void ClearingCspCache(ILogger logger, string cacheKey);

	[LoggerMessage(
		EventId = 201,
		Level = LogLevel.Debug,
		Message = "Clearing all CSP caches")]
	public static partial void ClearingAllCspCaches(ILogger logger);
}