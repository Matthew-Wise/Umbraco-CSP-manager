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
	/// Retrieves the node policy of a content node for all cultures from the database.
	/// </summary>
	/// <param name="contentKey">The key of the content node the policy belongs to.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The node policy, or <c>null</c> if the node has no policy for all cultures.</returns>
	Task<CspDefinition?> GetCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken);

	/// <summary>
	/// Retrieves the node policy of a content node for one culture from the database.
	/// </summary>
	/// <param name="contentKey">The key of the content node the policy belongs to.</param>
	/// <param name="culture">
	/// The ISO code of the culture (e.g. <c>fr-FR</c>), or <c>null</c> for the node's policy for all cultures.
	/// </param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>
	/// The policy stored for exactly that node and culture, or <c>null</c> if there is none. No
	/// fallback is applied: use <see cref="ResolveCspDefinitionAsync"/> for the policy a request gets.
	/// </returns>
	Task<CspDefinition?> GetCspDefinitionForNodeAsync(Guid contentKey, string? culture, CancellationToken cancellationToken);

	/// <summary>
	/// Retrieves the node policy of a content node for all cultures from the runtime cache, loading
	/// the node policies from the database if they aren't cached.
	/// </summary>
	/// <param name="contentKey">The key of the content node the policy belongs to.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>
	/// A defensive copy of the cached node policy, or <c>null</c> if the node has no policy for all
	/// cultures.
	/// </returns>
	Task<CspDefinition?> GetCachedCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken);

	/// <summary>
	/// Works out the node policy a frontend request gets, from the runtime cache.
	/// </summary>
	/// <param name="contentKey">The key of the requested page, or <c>null</c> when the request matched no content.</param>
	/// <param name="domainContentKey">
	/// The key of the content node of the hostname the request was routed through, or <c>null</c>.
	/// Its policy applies when the page isn't below that node (e.g. a "not found" page stored elsewhere).
	/// </param>
	/// <param name="culture">The ISO code of the request's culture, or <c>null</c>.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>
	/// <para>
	/// A new definition that is safe to change, or <c>null</c> when no node policy applies and the
	/// global frontend policy should be used.
	/// </para>
	/// <para>
	/// The nearest policy wins: the page's own, then each ancestor's that applies to its
	/// descendants; on each node the request's culture, then its fallback languages, then the
	/// policy for all cultures. Policies that extend
	/// (<see cref="CspPolicyInheritance.Extend"/>) add their sources to the first complete policy
	/// found beyond them, or to the global frontend policy; the result then has the identity of the
	/// most specific policy and the settings of that base.
	/// </para>
	/// <para>
	/// A disabled policy is skipped. With <see cref="DisabledNodePolicyBehavior.NoHeader"/>, a
	/// disabled complete policy is returned instead, so no header is sent.
	/// </para>
	/// </returns>
	Task<CspDefinition?> ResolveCspDefinitionAsync(Guid? contentKey, Guid? domainContentKey, string? culture, CancellationToken cancellationToken);

	/// <summary>
	/// Retrieves every node policy, including orphaned ones whose node was deleted or is in the recycle bin.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The node policies with their sources.</returns>
	Task<List<CspDefinition>> GetAllNodePoliciesAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Checks whether a content node exists, isn't in the recycle bin and has at least one hostname
	/// (non-wildcard domain) assigned.
	/// </summary>
	/// <param name="contentKey">The key of the content node.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns><c>true</c> when the node has a hostname.</returns>
	/// <remarks>A node doesn't need a hostname to have a policy: see <see cref="ContentNodeExistsAsync"/>.</remarks>
	Task<bool> ContentNodeHasHostnameAsync(Guid contentKey, CancellationToken cancellationToken);

	/// <summary>
	/// Checks whether a node policy can be created for a content node: the node exists and isn't
	/// in the recycle bin.
	/// </summary>
	/// <param name="contentKey">The key of the content node.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns><c>true</c> when the node can have a policy.</returns>
	Task<bool> ContentNodeExistsAsync(Guid contentKey, CancellationToken cancellationToken);

	/// <summary>
	/// Creates a complete node policy for all cultures of a content node and the pages below it, as
	/// an enabled copy of the policy the node gets today (a policy further up the content tree, or
	/// the global frontend policy).
	/// </summary>
	/// <param name="contentKey">The key of the content node.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The saved node policy.</returns>
	/// <exception cref="CspDefinitionValidationException">
	/// The node doesn't exist, is in the recycle bin, or already has a policy for all cultures.
	/// </exception>
	Task<CspDefinition> CreateCspDefinitionForNodeAsync(Guid contentKey, CancellationToken cancellationToken);

	/// <summary>
	/// Creates an enabled node policy for a content node.
	/// </summary>
	/// <param name="contentKey">The key of the content node.</param>
	/// <param name="culture">The ISO code of one of the site's languages, or <c>null</c> for all cultures.</param>
	/// <param name="inheritance">
	/// <see cref="CspPolicyInheritance.Replace"/> starts the policy as a copy of the policy the node
	/// gets today for that culture; <see cref="CspPolicyInheritance.Extend"/> starts it without
	/// sources, since it inherits them.
	/// </param>
	/// <param name="appliesTo">Whether the policy also applies to the pages below the node.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The saved node policy.</returns>
	/// <exception cref="CspDefinitionValidationException">
	/// The node doesn't exist, is in the recycle bin, the culture isn't a language, or the node
	/// already has a policy for that culture.
	/// </exception>
	Task<CspDefinition> CreateCspDefinitionForNodeAsync(
		Guid contentKey,
		string? culture,
		CspPolicyInheritance inheritance,
		CspPolicyScope appliesTo,
		CancellationToken cancellationToken);

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
	/// culture or inheritance, node policies require an existing content node (and, for a new policy
	/// with a culture, an existing language), and empty IDs receive a newly generated GUID.
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