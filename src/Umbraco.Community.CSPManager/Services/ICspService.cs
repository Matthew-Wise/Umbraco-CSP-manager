using Microsoft.AspNetCore.Http;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Service for managing Content Security Policy (CSP) definitions and nonce generation.
/// </summary>
/// <remarks>
/// This service provides methods to retrieve, save, and cache CSP definitions for both
/// frontend and backoffice contexts. It also handles cryptographically secure nonce
/// generation for script and style elements.
/// </remarks>
public interface ICspService
{
	/// <summary>
	/// Retrieves the CSP definition from the database for the specified context.
	/// </summary>
	/// <param name="isBackOfficeRequest">
	/// <c>true</c> to retrieve the backoffice CSP policy; <c>false</c> for the frontend policy.
	/// </param>
	/// <returns>
	/// The <see cref="CspDefinition"/> for the specified context, or a default disabled
	/// definition if none exists in the database.
	/// </returns>
	CspDefinition GetCspDefinition(bool isBackOfficeRequest);

	/// <summary>
	/// Retrieves the CSP definition from the runtime cache, loading from the database if not cached.
	/// </summary>
	/// <param name="isBackOfficeRequest">
	/// <c>true</c> to retrieve the backoffice CSP policy; <c>false</c> for the frontend policy.
	/// </param>
	/// <returns>
	/// The cached <see cref="CspDefinition"/> for the specified context, or <c>null</c> if
	/// the cache factory returns null.
	/// </returns>
	/// <remarks>
	/// This method uses separate cache keys for frontend and backoffice policies to ensure
	/// isolation. The cache is automatically invalidated when definitions are saved.
	/// </remarks>
	CspDefinition? GetCachedCspDefinition(bool isBackOfficeRequest);

	/// <summary>
	/// Saves a CSP definition to the database and publishes a <see cref="Notifications.CspSavedNotification"/>.
	/// </summary>
	/// <param name="definition">The CSP definition to save.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The saved <see cref="CspDefinition"/> with any modifications applied during save.</returns>
	/// <remarks>
	/// This method removes empty or whitespace-only sources before saving. It also deletes
	/// any sources that were removed from the definition and publishes a notification
	/// that can be handled by other components.
	/// </remarks>
	Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets or creates a cryptographically secure nonce for script elements.
	/// </summary>
	/// <param name="context">The current HTTP context.</param>
	/// <returns>
	/// A Base64-encoded 128-bit nonce value, or an empty string if the context is unavailable.
	/// </returns>
	/// <remarks>
	/// The nonce is generated using <see cref="System.Security.Cryptography.RandomNumberGenerator"/>
	/// and is stored in the HTTP context for reuse within the same request. This ensures
	/// all script elements in a single request use the same nonce value.
	/// </remarks>
	string GetOrCreateCspScriptNonce(HttpContext context);

	/// <summary>
	/// Gets or creates a cryptographically secure nonce for style elements.
	/// </summary>
	/// <param name="context">The current HTTP context.</param>
	/// <returns>
	/// A Base64-encoded 128-bit nonce value, or an empty string if the context is unavailable.
	/// </returns>
	/// <remarks>
	/// The nonce is generated using <see cref="System.Security.Cryptography.RandomNumberGenerator"/>
	/// and is stored in the HTTP context for reuse within the same request. This ensures
	/// all style elements in a single request use the same nonce value.
	/// </remarks>
	string GetOrCreateCspStyleNonce(HttpContext context);
}