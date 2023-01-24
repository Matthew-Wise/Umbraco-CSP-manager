namespace Umbraco.Community.CSPManager;

using Models;

public static class CspConstants
{
	public const string PackageAlias = "Umbraco.Community.CSPManager";
	
	public const string PluginAlias = "CspManager";

	public static readonly Guid DefaultBackofficeId = new("9cbfa28c-2b19-40f4-9f8e-bbc52bd8e780");
	
	public static readonly Guid DefaultFrontEndId = new("fac780be-53af-41dc-b51d-1aa647100221");

	public const string FrontEndCacheKey = "csp-frontend";
	
	public const string BackOfficeCacheKey = "csp-backoffice";

	public const string HeaderName = "Content-Security-Policy";
	
	public const string ReportOnlyHeaderName = HeaderName + "-Report-Only";
	
	public static readonly List<CspDefinitionSource> DefaultBackOfficeCsp = new()
	{
		new CspDefinitionSource
		{
			DefinitionId = DefaultBackofficeId,
			Source = "'self'",
			Directives = new List<string>
			{
				Directives.DefaultSource,
				Directives.ScriptSource,
				Directives.StyleSource,
				Directives.ImageSource,
				Directives.FontSource
			}
		},
		new CspDefinitionSource
		{
			DefinitionId = DefaultBackofficeId,
			Source = "packages.umbraco.com",
			Directives = new List<string> { Directives.DefaultSource }
		},
		new CspDefinitionSource
		{
			DefinitionId = DefaultBackofficeId,
			Source = "our.umbraco.com",
			Directives = new List<string> { Directives.DefaultSource, Directives.ImageSource }
		},
		new CspDefinitionSource
		{
			DefinitionId = DefaultBackofficeId,
			Source = "'unsafe-inline'",
			Directives = new List<string> { Directives.ScriptSource, Directives.StyleSource }
		},
		new CspDefinitionSource
		{
			DefinitionId = DefaultBackofficeId,
			Source = "'unsafe-eval'",
			Directives = new List<string> { Directives.ScriptSource }
		},
		new CspDefinitionSource
		{
			DefinitionId = DefaultBackofficeId, Source = "data:", Directives = new List<string> { Directives.ImageSource }
		},
		new CspDefinitionSource
		{
			DefinitionId = DefaultBackofficeId,
			Source = "dashboard.umbraco.com",
			Directives = new List<string> { Directives.ImageSource }
		},
		new CspDefinitionSource
		{
			DefinitionId = DefaultBackofficeId,
			Source = "www.gravatar.com",
			Directives = new List<string> { Directives.ImageSource }
		}
	};

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
	
	public static class ServerVariables {
		public const string BaseUrl = "cspManagerBaseUrl";
	}
}
