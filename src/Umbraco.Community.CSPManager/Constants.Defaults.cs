using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager;

public static partial class Constants
{
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
}
