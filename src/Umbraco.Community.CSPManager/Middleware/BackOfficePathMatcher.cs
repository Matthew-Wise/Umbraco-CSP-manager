using Microsoft.AspNetCore.Http;
using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Middleware;

/// <summary>
/// Decides whether a request path gets the backoffice CSP definition or the frontend one.
/// </summary>
/// <remarks>
/// <para>
/// Everything under <c>/umbraco</c> is backoffice, apart from the areas Umbraco uses to serve the
/// public site. Umbraco's own <c>HttpRequest.IsBackOfficeRequest()</c> is deliberately not used: it
/// matches on a string prefix, so content URLs such as <c>/umbraco-partners</c> count as backoffice,
/// and it treats any path of three or more segments as front end, which misses deep links into the
/// backoffice shell such as <c>/umbraco/section/content/...</c>.
/// </para>
/// <para>
/// <see cref="HttpRequest.Path"/> already excludes <see cref="HttpRequest.PathBase"/>, so this also
/// works when the site runs in a virtual directory.
/// </para>
/// </remarks>
internal static class BackOfficePathMatcher
{
	private static readonly PathString UmbracoRoot = new("/" + UmbracoConstants.System.UmbracoPathSegment);

	// Umbraco-owned routes under /umbraco that serve the public site, not the backoffice.
	// PluginController routes (/umbraco/{area}/{controller}/{action}) are not listed, so they keep the
	// backoffice definition.
	private static readonly PathString[] FrontEndAreas =
	[
		UmbracoRoot.Add("/surface"),
		UmbracoRoot.Add("/api"),
		UmbracoRoot.Add(UmbracoConstants.Web.DeliveryApiPath.TrimEnd('/')),
	];

	/// <summary>
	/// Returns <c>true</c> if <paramref name="path"/> should get the backoffice CSP definition.
	/// </summary>
	public static bool IsBackOfficeRequest(PathString path)
		=> path.StartsWithSegments(UmbracoRoot, StringComparison.OrdinalIgnoreCase)
			&& !FrontEndAreas.Any(area => path.StartsWithSegments(area, StringComparison.OrdinalIgnoreCase));
}
