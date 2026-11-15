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
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>
	/// The <see cref="CspDefinition"/> for the specified context, or a default disabled
	/// definition if none exists in the database.
	/// </returns>
	/// <remarks>Only returns a global policy; node policies are never returned here.</remarks>
	Task<CspDefinition> GetCspDefinitionAsync(bool isBackOfficeRequest, CancellationToken cancellationToken);

	/// <summary>
	/// Retrieves a CSP definition (global or node policy) from the database by its id.
	/// </summary>
	/// <param name="key">The definition id.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The definition, or <c>null</c> if there is no definition with that id.</returns>
	Task<CspDefinition?> GetCspDefinitionAsync(Guid key, CancellationToken cancellationToken);

	/// <summary>
	/// Retrieves the node policy of a content node from the database.
	/// </summary>
	/// <param name="contentKey">The key of the content node the policy belongs to.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The node policy, or <c>null</c> if the node has none.</returns>
	Task<CspDefinition?> GetCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken);

	/// <summary>
	/// Retrieves the node policy of a content node from the runtime cache, loading it from the
	/// database if it isn't cached.
	/// </summary>
	/// <param name="contentKey">The key of the content node the policy belongs to.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>
	/// A defensive copy of the cached node policy, or <c>null</c> if the node has none. The
	/// absence of a policy is cached too, until a policy for the node is saved.
	/// </returns>
	Task<CspDefinition?> GetCachedCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken);

	/// <summary>
	/// Retrieves every node policy, including orphaned ones whose node was deleted or lost its hostnames.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The node policies with their sources.</returns>
	Task<List<CspDefinition>> GetAllNodePoliciesAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Checks whether a node policy can apply to a content node: the node exists, isn't in the
	/// recycle bin and has at least one hostname (non-wildcard domain) assigned.
	/// </summary>
	/// <param name="contentKey">The key of the content node.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns><c>true</c> when a node policy for the node applies to requests on its hostnames.</returns>
	Task<bool> ContentNodeHasHostnameAsync(Guid contentKey, CancellationToken cancellationToken);

	/// <summary>
	/// Creates a node policy for a content node as an enabled copy of the global frontend policy.
	/// </summary>
	/// <param name="contentKey">The key of a content node with at least one hostname (non-wildcard domain) assigned.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The saved node policy.</returns>
	/// <exception cref="CspDefinitionValidationException">
	/// The node doesn't exist, has no hostname, or already has a policy.
	/// </exception>
	Task<CspDefinition> CreateCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken);

	/// <summary>
	/// Deletes a node policy and publishes a <see cref="Notifications.CspDeletedNotification"/>.
	/// Does nothing if there is no definition with that id.
	/// </summary>
	/// <param name="id">The id of the node policy.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <exception cref="CspDefinitionValidationException">The id is one of the two global policies.</exception>
	Task DeleteCspDefinitionAsync(Guid id, CancellationToken cancellationToken);

	/// <summary>
	/// Retrieves the CSP definition from the runtime cache, loading from the database if not cached.
	/// </summary>
	/// <param name="isBackOfficeRequest">
	/// <c>true</c> to retrieve the backoffice CSP policy; <c>false</c> for the frontend policy.
	/// </param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>
	/// The cached <see cref="CspDefinition"/> for the specified context, or <c>null</c> if
	/// the cache factory returns null.
	/// </returns>
	/// <remarks>
	/// This method uses separate cache keys for frontend and backoffice policies to ensure
	/// isolation. The cache is automatically invalidated when definitions are saved.
	/// </remarks>
	Task<CspDefinition?> GetCachedCspDefinitionAsync(bool isBackOfficeRequest, CancellationToken cancellationToken);

	/// <summary>
	/// Saves a CSP definition to the database and publishes a <see cref="Notifications.CspSavedNotification"/>.
	/// </summary>
	/// <param name="definition">The CSP definition to save.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The saved <see cref="CspDefinition"/> with any modifications applied during save.</returns>
	/// <remarks>
	/// Strips whitespace sources and validates identity: global policies cannot carry a content key,
	/// node policies require a content node with a hostname, and empty IDs receive a newly generated GUID.
	/// </remarks>
	/// <exception cref="CspDefinitionValidationException">The definition fails identity or content validation.</exception>
	Task<CspDefinition> SaveCspDefinitionAsync(CspDefinition definition, CancellationToken cancellationToken);

	/// <summary>
	/// Gets or creates a cryptographically secure nonce for use in CSP directives.
	/// </summary>
	/// <param name="context">The current HTTP context.</param>
	/// <returns>
	/// A Base64-encoded 128-bit nonce value, or an empty string if the context is unavailable.
	/// </returns>
	/// <remarks>
	/// The nonce is generated using <see cref="System.Security.Cryptography.RandomNumberGenerator"/>
	/// and is stored in the HTTP context for reuse within the same request. The same nonce value
	/// is used for both script-src and style-src directives.
	/// </remarks>
	string GetOrCreateCspNonce(HttpContext context);
}