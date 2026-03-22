using System.Collections.Frozen;

namespace Umbraco.Community.CSPManager;

public static partial class Constants
{
	public static class ReportingDirectives
	{
		/// <summary>
		/// The deprecated report-uri directive. Value should be a valid URI.
		/// </summary>
		public const string ReportUri = "report-uri";

		/// <summary>
		/// The report-to directive. Value should be an endpoint name defined in the Reporting-Endpoints header.
		/// </summary>
		public const string ReportTo = "report-to";
	}

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

		public const string RequireTrustedTypes = "require-trusted-types-for";

		public const string ScriptSourceAttribute = "script-src-attr";

		public const string ScriptSourceElement = "script-src-elem";

		public const string ScriptSource = "script-src";

		public const string StyleSourceAttribute = "style-src-attr";

		public const string StyleSourceElement = "style-src-elem";

		public const string StyleSource = "style-src";

		public const string TrustedTypes = "trusted-types";

		public const string UpgradeInsecureRequests = "upgrade-insecure-requests";

		public const string WorkerSource = "worker-src";
	}

	public static readonly FrozenSet<string> AllDirectives =
	[
		Directives.BaseUri, Directives.ChildSource, Directives.ConnectSource, Directives.DefaultSource,
		Directives.FontSource, Directives.FormAction, Directives.FrameAncestors, Directives.FrameSource,
		Directives.ImageSource, Directives.ManifestSource, Directives.MediaSource, Directives.NavigateTo,
		Directives.ObjectSource, Directives.PreFetchSource,
		Directives.RequireTrustedTypes,
		Directives.ScriptSourceAttribute, Directives.ScriptSourceElement, Directives.ScriptSource,
		Directives.StyleSourceAttribute, Directives.StyleSourceElement, Directives.StyleSource,
		Directives.TrustedTypes,
		Directives.UpgradeInsecureRequests,
		Directives.WorkerSource
	];
}