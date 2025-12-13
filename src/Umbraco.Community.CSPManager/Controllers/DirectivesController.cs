using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Umbraco.Community.CSPManager.Controllers;

/// <summary>
/// API controller for retrieving available CSP directive options.
/// </summary>
/// <remarks>
/// This controller provides a read-only endpoint to list all supported CSP directives
/// that can be used when configuring content security policies.
/// </remarks>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Directives")]
public class DirectivesController : CspManagerControllerBase
{
	/// <summary>
	/// Retrieves the list of all available CSP directive names.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token (unused).</param>
	/// <returns>
	/// A collection of CSP directive names such as "default-src", "script-src", "style-src", etc.
	/// </returns>
	/// <remarks>
	/// The returned directives follow the W3C Content Security Policy Level 3 specification.
	/// See <see href="https://www.w3.org/TR/CSP3/">CSP Level 3</see> for more information.
	/// </remarks>
	[HttpGet("Directives")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(ICollection<string>), 200)]
#pragma warning disable CA1822 // Mark members as static
	public ICollection<string> GetCspDirectiveOptions(CancellationToken cancellationToken = default) => Constants.AllDirectives;
#pragma warning restore CA1822 // Mark members as static
}