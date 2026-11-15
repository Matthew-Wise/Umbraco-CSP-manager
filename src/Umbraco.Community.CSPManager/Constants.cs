namespace Umbraco.Community.CSPManager;

public static partial class Constants
{
	public const string ApiName = "csp";

	public const string PackageAlias = "Umbraco.Community.CSPManager";

	public const string OptionsName = "CspManager";

	public const string ManagementApiPath = "/csp/api";

	public const string SectionAlias = "Umbraco.Community.CSPManager.Section";

	public static readonly Guid DefaultBackofficeId = new("9cbfa28c-2b19-40f4-9f8e-bbc52bd8e780");

	public static readonly Guid DefaultFrontEndId = new("fac780be-53af-41dc-b51d-1aa647100221");

	public const string FrontEndCacheKey = "csp-frontend";

	public const string BackOfficeCacheKey = "csp-backoffice";

	/// <summary>
	/// Prefix shared by every cache entry used to resolve node policies, so they can all be cleared
	/// at once (<c>IAppCache.ClearByKey</c> matches keys that start with the value it's given).
	/// </summary>
	public const string NodeCacheKeyPrefix = "csp-node-";

	/// <summary>
	/// The cache key of the snapshot of every node policy. One entry rather than one per node: a
	/// request has to check its page and each ancestor, and most sites have a handful of policies.
	/// </summary>
	public const string NodePoliciesCacheKey = NodeCacheKeyPrefix + "policies";

	/// <summary>
	/// The cache key of the language fallback chains used to pick a node policy for a culture.
	/// </summary>
	public const string LanguageFallbackCacheKey = NodeCacheKeyPrefix + "language-fallbacks";

	public const string HeaderName = "Content-Security-Policy";

	public const string ReportOnlyHeaderName = HeaderName + "-Report-Only";

	/// <summary>
	/// The minimal, same-origin-only policy applied when CSP header construction throws and
	/// <see cref="CspFailureBehavior.FailClosed"/> is configured.
	/// </summary>
	public const string FailClosedFallbackPolicy = "default-src 'self'";

	/// <summary>
	/// Rows this package adds to Umbraco's <c>umbracoLock</c> table, taken as distributed write locks.
	/// </summary>
	/// <remarks>
	/// There is no registry of lock ids. Umbraco CMS uses <c>-1000</c> (MainDom) and a block that
	/// starts at <c>-331</c> and grows downwards by one or two per release (<c>-349</c> in v18);
	/// Umbraco Deploy uses <c>-800</c>. <c>-2776</c> ("CSPM" on a phone keypad) stays well clear of
	/// all of those.
	/// </remarks>
	public static class Locks
	{
		/// <summary>
		/// Serialises every write to the CSP definition tables (save, create, delete).
		/// </summary>
		public const int Definitions = -2776;

		/// <summary>
		/// The <c>umbracoLock.name</c> of <see cref="Definitions"/>.
		/// </summary>
		public const string DefinitionsName = "CspManagerDefinitions";
	}

	public static class EntityTypes
	{
		public const string CspPolicy = "csp-policy";
	}

	public static class TagHelper
	{

		public const string ScriptTag = "script";

		public const string StyleTag = "style";

		public const string LinkTag = "link";

		public const string ContextKey = "CspManagerContext";

		public const string CspManagerScriptNonceSet = "CspManagerScriptNonceSet";

		public const string CspManagerStyleNonceSet = "CspManagerStyleNonceSet";
	}

	public static class AuthorizationPolicies
	{
		public const string SectionAccess = "CspSectionAccess";
	}
}