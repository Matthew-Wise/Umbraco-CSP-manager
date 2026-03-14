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

	public const string HeaderName = "Content-Security-Policy";

	public const string ReportOnlyHeaderName = HeaderName + "-Report-Only";

	public static class EntityTypes
	{
		public const string CspPolicy = "csp-policy";

		public const string CspPolicyRoot = "csp-policy-root";
	}

	public static class TagHelper
	{

		public const string ScriptTag = "script";

		public const string StyleTag = "style";

		public const string ContextKey = "CspManagerContext";

		public const string CspManagerScriptNonceSet = "CspManagerScriptNonceSet";

		public const string CspManagerStyleNonceSet = "CspManagerStyleNonceSet";
	}

	public static class AuthorizationPolicies
	{
		public const string SectionAccess = "CspSectionAccess";
	}
}
