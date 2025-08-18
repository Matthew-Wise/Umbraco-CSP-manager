using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager;

public static class Constants
{
	public const string ApiName = "csp";

	public const string PackageAlias = "Umbraco.Community.CSPManager";

	public const string PluginAlias = "CspManager";

	public const string OptionsName = "CspManager";

	public const string ManagementApiPath = "/csp/api";

	public const string SectionAlias = "Umbraco.Community.CSPManager.Section";

	public static readonly Guid DefaultBackofficeId = new("9cbfa28c-2b19-40f4-9f8e-bbc52bd8e780");

	public static readonly Guid DefaultFrontEndId = new("fac780be-53af-41dc-b51d-1aa647100221");

	public const string FrontEndCacheKey = "csp-frontend";

	public const string BackOfficeCacheKey = "csp-backoffice";

	public const string HeaderName = "Content-Security-Policy";

	public const string ReportOnlyHeaderName = HeaderName + "-Report-Only";

	public static readonly List<CspDefinitionSource> DefaultBackOfficeCsp =
	[
		new()
		{
			DefinitionId = DefaultBackofficeId,
			Source = "'self'",
			Directives = [
				Directives.DefaultSource,
				Directives.ScriptSource,
				Directives.StyleSource,
				Directives.ImageSource,
				Directives.FontSource
			]
		},
		new()
		{
			DefinitionId = DefaultBackofficeId,
			Source = "marketplace.umbraco.com",
			Directives = [Directives.DefaultSource]
		},
		new()
		{
			DefinitionId = DefaultBackofficeId,
			Source = "our.umbraco.com",
			Directives = [Directives.DefaultSource, Directives.ImageSource]
		},
		new()
		{
			DefinitionId = DefaultBackofficeId,
			Source = "'unsafe-inline'",
			Directives = [Directives.ScriptSource, Directives.StyleSource]
		},
		new()
		{
			DefinitionId = DefaultBackofficeId,
			Source = "'unsafe-eval'",
			Directives = [Directives.ScriptSource]
		},
		new()
		{
			DefinitionId = DefaultBackofficeId, Source = "data:", Directives = [Directives.ImageSource]
		},
		new()
		{
			DefinitionId = DefaultBackofficeId,
			Source = "dashboard.umbraco.com",
			Directives = [Directives.ImageSource]
		}
	];

	public static class Directives
	{
		public const string BaseUri = "base-uri";

		public const string ChildSource = "child-src";

		public const string ConnectSource = "connect-src";

		public const string DefaultSource = "default-src";

		public const string FontSource = "font-src";

		public const string FormAction = "form-action";

		public const string FrameAncestors = "frame-ancestors";

		public const string FrameSource = "frame-src";

		public const string ImageSource = "img-src";

		public const string ManifestSource = "manifest-src";

		public const string MediaSource = "media-src";

		public const string NavigateTo = "navigate-to";

		public const string ObjectSource = "object-src";

		public const string PreFetchSource = "prefetch-src";

		//TODO: public const string RequireTrustedTypes = "require-trusted-types-for";

		//TODO: public const string Sandbox = "sandbox";

		public const string ScriptSourceAttribute = "script-src-attr";

		public const string ScriptSourceElement = "script-src-elem";

		public const string ScriptSource = "script-src";

		public const string StyleSourceAttribute = "style-src-attr";

		public const string StyleSourceElement = "style-src-elem";

		public const string StyleSource = "style-src";

		//TODO: public const string TrustedTypes = "trusted-types";

		//TODO: public const string UpgradeInsecureRequests = "upgrade-insecure-requests";

		public const string WorkerSource = "worker-src";
	}

	public static ReadOnlySpan<string> AllDirectives => new[]
	{
		Directives.BaseUri, Directives.ChildSource, Directives.ConnectSource, Directives.DefaultSource,
		Directives.FontSource, Directives.FormAction, Directives.FrameAncestors, Directives.FrameSource,
		Directives.ImageSource, Directives.ManifestSource, Directives.MediaSource, Directives.NavigateTo,
		Directives.ObjectSource, Directives.PreFetchSource,
		// Directives.RequireTrustedTypes,
		// Directives.Sandbox,
		Directives.ScriptSourceAttribute, Directives.ScriptSourceElement, Directives.ScriptSource,
		Directives.StyleSourceAttribute, Directives.StyleSourceElement, Directives.StyleSource,
		// Directives.TrustedTypes,
		// Directives.UpgradeInsecureRequests,
		Directives.WorkerSource
	};

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
