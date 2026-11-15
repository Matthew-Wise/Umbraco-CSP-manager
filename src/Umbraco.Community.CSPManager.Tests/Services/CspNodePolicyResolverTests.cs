using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Tests.Services;

/// <summary>
/// Which node policies apply to a request, and how they are combined. Pure logic: no database.
/// </summary>
[TestFixture]
public class CspNodePolicyResolverTests
{
	// A small site: Home > Section > Page.
	private static readonly Guid Home = new("00000000-0000-0000-0000-00000000000a");
	private static readonly Guid Section = new("00000000-0000-0000-0000-00000000000b");
	private static readonly Guid Page = new("00000000-0000-0000-0000-00000000000c");

	private static readonly Guid[] PagePath = [Page, Section, Home];

	private static CspDefinition Policy(
		Guid node,
		string name,
		string culture = null,
		CspPolicyInheritance inheritance = CspPolicyInheritance.Replace,
		CspPolicyScope appliesTo = CspPolicyScope.Descendants,
		bool enabled = true,
		string directive = Constants.Directives.DefaultSource)
		=> new()
		{
			Id = Guid.NewGuid(),
			ContentKey = node,
			Culture = culture,
			Inheritance = inheritance,
			AppliesTo = appliesTo,
			Enabled = enabled,
			Sources = [new CspDefinitionSource { Source = name, Directives = [directive] }],
		};

	private static CspNodePolicyResolver.Resolution Resolve(
		IEnumerable<CspDefinition> policies,
		string[] cultures = null,
		Guid[] path = null,
		Guid? page = null,
		DisabledNodePolicyBehavior disabled = DisabledNodePolicyBehavior.FallbackToGlobal)
		=> CspNodePolicyResolver.Resolve(new CspNodePolicySnapshot(policies), path ?? PagePath, page ?? Page, cultures ?? [], disabled);

	private static string[] SourcesOf(CspDefinition definition) => [.. definition.Sources.Select(s => s.Source)];

	// ── Which policy ─────────────────────────────────────────────────────────

	[Test]
	public void Resolve_WithoutPolicies_IsEmpty()
	{
		var resolution = Resolve([]);

		Assert.Multiple(() =>
		{
			Assert.That(resolution.IsEmpty, Is.True);
			Assert.That(resolution.NoHeader, Is.False);
		});
	}

	[Test]
	public void Resolve_PageWithItsOwnPolicy_UsesIt()
	{
		var own = Policy(Page, "page");

		var resolution = Resolve([own, Policy(Home, "home")]);

		Assert.That(resolution.Base, Is.SameAs(own));
	}

	[Test]
	public void Resolve_PageWithoutAPolicy_UsesTheNearestAncestorsPolicy()
	{
		var section = Policy(Section, "section");

		var resolution = Resolve([Policy(Home, "home"), section]);

		Assert.That(resolution.Base, Is.SameAs(section));
	}

	[Test]
	public void Resolve_PolicyOnAnotherBranch_DoesNotApply()
	{
		var resolution = Resolve([Policy(Guid.NewGuid(), "elsewhere")]);

		Assert.That(resolution.IsEmpty, Is.True);
	}

	// ── Applies to ───────────────────────────────────────────────────────────

	[Test]
	public void Resolve_NodeOnlyPolicyOnThePage_Applies()
	{
		var own = Policy(Page, "page", appliesTo: CspPolicyScope.NodeOnly);

		var resolution = Resolve([own]);

		Assert.That(resolution.Base, Is.SameAs(own));
	}

	[Test]
	public void Resolve_NodeOnlyPolicyOnAnAncestor_IsSkipped()
	{
		var home = Policy(Home, "home");

		var resolution = Resolve([Policy(Section, "section", appliesTo: CspPolicyScope.NodeOnly), home]);

		Assert.That(resolution.Base, Is.SameAs(home));
	}

	// A request that matched no content (only a hostname): no node is "the page", so a policy for
	// one page alone can't apply.
	[Test]
	public void Resolve_WithoutAPage_SkipsNodeOnlyPolicies()
	{
		var resolution = CspNodePolicyResolver.Resolve(
			new CspNodePolicySnapshot([Policy(Home, "home", appliesTo: CspPolicyScope.NodeOnly)]),
			[Home],
			pageKey: null,
			[],
			DisabledNodePolicyBehavior.FallbackToGlobal);

		Assert.That(resolution.IsEmpty, Is.True);
	}

	// ── Cultures ─────────────────────────────────────────────────────────────

	[Test]
	public void Resolve_PolicyForTheRequestsCulture_WinsOverThePolicyForAllCultures()
	{
		var french = Policy(Home, "fr", culture: "fr-FR");

		var resolution = Resolve([Policy(Home, "all"), french], cultures: ["fr-FR"]);

		Assert.That(resolution.Base, Is.SameAs(french));
	}

	[Test]
	public void Resolve_CultureIsComparedIgnoringCase()
	{
		var french = Policy(Home, "fr", culture: "fr-FR");

		var resolution = Resolve([french], cultures: ["FR-fr"]);

		Assert.That(resolution.Base, Is.SameAs(french));
	}

	[Test]
	public void Resolve_NoPolicyForTheRequestsCulture_FollowsTheLanguageFallback()
	{
		var french = Policy(Home, "fr", culture: "fr-FR");

		var resolution = Resolve([Policy(Home, "all"), french, Policy(Home, "de", culture: "de-DE")], cultures: ["fr-CA", "fr-FR"]);

		Assert.That(resolution.Base, Is.SameAs(french));
	}

	[Test]
	public void Resolve_NoPolicyInTheFallbackChain_UsesThePolicyForAllCultures()
	{
		var all = Policy(Home, "all");

		var resolution = Resolve([all, Policy(Home, "de", culture: "de-DE")], cultures: ["fr-CA", "fr-FR"]);

		Assert.That(resolution.Base, Is.SameAs(all));
	}

	[Test]
	public void Resolve_PolicyForAnotherCultureOnly_DoesNotApply()
	{
		var resolution = Resolve([Policy(Home, "de", culture: "de-DE")], cultures: ["fr-FR"]);

		Assert.That(resolution.IsEmpty, Is.True);
	}

	[Test]
	public void Resolve_RequestWithoutACulture_OnlyUsesPoliciesForAllCultures()
	{
		var all = Policy(Home, "all");

		var resolution = Resolve([Policy(Home, "fr", culture: "fr-FR"), all]);

		Assert.That(resolution.Base, Is.SameAs(all));
	}

	// The node is more specific than the culture: a page's policy for all cultures wins over an
	// ancestor's policy for the request's culture.
	[Test]
	public void Resolve_NearerNodeWinsOverABetterCultureMatchFurtherUp()
	{
		var page = Policy(Page, "page-all");

		var resolution = Resolve([page, Policy(Home, "home-fr", culture: "fr-FR")], cultures: ["fr-FR"]);

		Assert.That(resolution.Base, Is.SameAs(page));
	}

	// ── Inherit and add ──────────────────────────────────────────────────────

	[Test]
	public void Resolve_ExtendingPolicy_IsAddedToTheNearestCompletePolicy()
	{
		var extra = Policy(Page, "maps", inheritance: CspPolicyInheritance.Extend);
		var home = Policy(Home, "home");

		var resolution = Resolve([extra, home]);

		Assert.Multiple(() =>
		{
			Assert.That(resolution.Base, Is.SameAs(home));
			Assert.That(resolution.Additions, Is.EqualTo(new[] { extra }));
		});
	}

	[Test]
	public void Resolve_ExtendingPolicyWithoutACompletePolicyAbove_ExtendsTheGlobalPolicy()
	{
		var extra = Policy(Page, "maps", inheritance: CspPolicyInheritance.Extend);

		var resolution = Resolve([extra]);

		Assert.Multiple(() =>
		{
			Assert.That(resolution.IsEmpty, Is.False);
			Assert.That(resolution.Base, Is.Null);
			Assert.That(resolution.Additions, Is.EqualTo(new[] { extra }));
		});
	}

	[Test]
	public void Resolve_SeveralExtendingPolicies_AreCollectedMostSpecificFirst()
	{
		var page = Policy(Page, "page", inheritance: CspPolicyInheritance.Extend);
		var section = Policy(Section, "section", inheritance: CspPolicyInheritance.Extend);
		var home = Policy(Home, "home");

		var resolution = Resolve([home, section, page]);

		Assert.Multiple(() =>
		{
			Assert.That(resolution.Base, Is.SameAs(home));
			Assert.That(resolution.Additions, Is.EqualTo(new[] { page, section }));
		});
	}

	// A culture's extending policy adds to the same node's policy for all cultures.
	[Test]
	public void Resolve_ExtendingPolicyForACulture_ExtendsTheNodesPolicyForAllCultures()
	{
		var french = Policy(Home, "fr-consent", culture: "fr-FR", inheritance: CspPolicyInheritance.Extend);
		var all = Policy(Home, "all");

		var resolution = Resolve([all, french], cultures: ["fr-FR"]);

		Assert.Multiple(() =>
		{
			Assert.That(resolution.Base, Is.SameAs(all));
			Assert.That(resolution.Additions, Is.EqualTo(new[] { french }));
		});
	}

	[Test]
	public void Resolve_PoliciesBeyondTheFirstCompletePolicy_AreIgnored()
	{
		var section = Policy(Section, "section");

		var resolution = Resolve([section, Policy(Home, "home-extra", inheritance: CspPolicyInheritance.Extend), Policy(Home, "home", culture: "fr-FR")], cultures: ["fr-FR"]);

		Assert.Multiple(() =>
		{
			Assert.That(resolution.Base, Is.SameAs(section));
			Assert.That(resolution.Additions, Is.Empty);
		});
	}

	// ── Disabled policies ────────────────────────────────────────────────────

	[Test]
	public void Resolve_DisabledPolicy_IsSkippedByDefault()
	{
		var home = Policy(Home, "home");

		var resolution = Resolve([Policy(Page, "page", enabled: false), home]);

		Assert.Multiple(() =>
		{
			Assert.That(resolution.Base, Is.SameAs(home));
			Assert.That(resolution.NoHeader, Is.False);
		});
	}

	[Test]
	public void Resolve_OnlyADisabledPolicy_IsEmptyByDefault()
	{
		var resolution = Resolve([Policy(Page, "page", enabled: false)]);

		Assert.That(resolution.IsEmpty, Is.True);
	}

	[Test]
	public void Resolve_DisabledCompletePolicy_WithNoHeader_EndsTheWalkWithoutAHeader()
	{
		var page = Policy(Page, "page", enabled: false);

		var resolution = Resolve([page, Policy(Home, "home")], disabled: DisabledNodePolicyBehavior.NoHeader);

		Assert.Multiple(() =>
		{
			Assert.That(resolution.NoHeader, Is.True);
			Assert.That(resolution.Base, Is.SameAs(page));
			Assert.That(resolution.Additions, Is.Empty);
		});
	}

	// A disabled extending policy adds nothing; it never suppresses the header of what it extends.
	[Test]
	[TestCase(DisabledNodePolicyBehavior.FallbackToGlobal)]
	[TestCase(DisabledNodePolicyBehavior.NoHeader)]
	public void Resolve_DisabledExtendingPolicy_IsSkippedWhateverTheBehaviour(DisabledNodePolicyBehavior behaviour)
	{
		var home = Policy(Home, "home");

		var resolution = Resolve([Policy(Page, "maps", inheritance: CspPolicyInheritance.Extend, enabled: false), home], disabled: behaviour);

		Assert.Multiple(() =>
		{
			Assert.That(resolution.Base, Is.SameAs(home));
			Assert.That(resolution.Additions, Is.Empty);
			Assert.That(resolution.NoHeader, Is.False);
		});
	}

	// ── Merge ────────────────────────────────────────────────────────────────

	[Test]
	public void Merge_TakesTheIdentityOfTheMostSpecificPolicyAndTheSettingsOfTheBase()
	{
		var baseDefinition = Policy(Home, "home");
		baseDefinition.ReportOnly = true;
		baseDefinition.ReportingDirective = Constants.ReportingDirectives.ReportUri;
		baseDefinition.ReportUri = "https://reports.example.com/csp";
		baseDefinition.UpgradeInsecureRequests = true;
		var page = Policy(Page, "maps", culture: "fr-FR", inheritance: CspPolicyInheritance.Extend, appliesTo: CspPolicyScope.NodeOnly);
		page.ReportOnly = false;

		var merged = CspNodePolicyResolver.Merge(baseDefinition, [page, Policy(Section, "section", inheritance: CspPolicyInheritance.Extend)]);

		Assert.Multiple(() =>
		{
			Assert.That(merged.Id, Is.EqualTo(page.Id));
			Assert.That(merged.ContentKey, Is.EqualTo(Page));
			Assert.That(merged.Culture, Is.EqualTo("fr-FR"));
			Assert.That(merged.Enabled, Is.True);
			Assert.That(merged.ReportOnly, Is.True);
			Assert.That(merged.ReportingDirective, Is.EqualTo(Constants.ReportingDirectives.ReportUri));
			Assert.That(merged.ReportUri, Is.EqualTo("https://reports.example.com/csp"));
			Assert.That(merged.UpgradeInsecureRequests, Is.True);
			Assert.That(merged.IsBackOffice, Is.False);
			Assert.That(merged.Sources.Select(s => s.DefinitionId), Is.All.EqualTo(page.Id));
		});
	}

	[Test]
	public void Merge_AddsSourcesFromTheBaseOutwards()
	{
		var merged = CspNodePolicyResolver.Merge(
			Policy(Home, "home"),
			[
				Policy(Page, "page", inheritance: CspPolicyInheritance.Extend),
				Policy(Section, "section", inheritance: CspPolicyInheritance.Extend),
			]);

		Assert.That(SourcesOf(merged), Is.EqualTo(new[] { "home", "section", "page" }));
	}

	[Test]
	public void Merge_SameSourceInSeveralPolicies_CombinesItsDirectivesOnce()
	{
		var baseDefinition = Policy(Home, "cdn.example.com", directive: Constants.Directives.ScriptSource);
		var page = Policy(Page, "cdn.example.com", inheritance: CspPolicyInheritance.Extend, directive: Constants.Directives.StyleSource);
		page.Sources[0].Directives.Add(Constants.Directives.ScriptSource);

		var merged = CspNodePolicyResolver.Merge(baseDefinition, [page]);

		Assert.Multiple(() =>
		{
			Assert.That(SourcesOf(merged), Is.EqualTo(new[] { "cdn.example.com" }));
			Assert.That(merged.Sources[0].Directives, Is.EqualTo(new[] { Constants.Directives.ScriptSource, Constants.Directives.StyleSource }));
		});
	}

	[Test]
	public void Merge_DoesNotChangeThePoliciesItCombines()
	{
		var baseDefinition = Policy(Home, "cdn.example.com", directive: Constants.Directives.ScriptSource);
		var page = Policy(Page, "cdn.example.com", inheritance: CspPolicyInheritance.Extend, directive: Constants.Directives.StyleSource);

		var merged = CspNodePolicyResolver.Merge(baseDefinition, [page]);
		merged.Sources[0].Directives.Add("mutated");

		Assert.Multiple(() =>
		{
			Assert.That(baseDefinition.Sources[0].Directives, Is.EqualTo(new[] { Constants.Directives.ScriptSource }));
			Assert.That(page.Sources[0].Directives, Is.EqualTo(new[] { Constants.Directives.StyleSource }));
		});
	}

	// frame-src 'none' in the site's policy, and a page that allows one frame origin: next to
	// another source a browser ignores 'none', so it has to go for that directive only.
	[Test]
	public void Merge_AddingASourceToADirectiveThatWasNone_DropsNoneForThatDirective()
	{
		var baseDefinition = Policy(Home, "'none'", directive: Constants.Directives.FrameSource);
		baseDefinition.Sources[0].Directives.Add(Constants.Directives.ObjectSource);
		var page = Policy(Page, "https://www.google.com", inheritance: CspPolicyInheritance.Extend, directive: Constants.Directives.FrameSource);

		var merged = CspNodePolicyResolver.Merge(baseDefinition, [page]);

		var none = merged.Sources.Single(s => s.Source == "'none'");
		Assert.Multiple(() =>
		{
			Assert.That(none.Directives, Is.EqualTo(new[] { Constants.Directives.ObjectSource }));
			Assert.That(merged.Sources.Single(s => s.Source == "https://www.google.com").Directives, Is.EqualTo(new[] { Constants.Directives.FrameSource }));
			Assert.That(baseDefinition.Sources[0].Directives, Has.Count.EqualTo(2), "the base policy itself is left alone");
		});
	}

	[Test]
	public void Merge_WhenNoneIsLeftWithoutDirectives_RemovesTheSource()
	{
		var baseDefinition = Policy(Home, "'none'", directive: Constants.Directives.FrameSource);
		var page = Policy(Page, "https://www.google.com", inheritance: CspPolicyInheritance.Extend, directive: Constants.Directives.FrameSource);

		var merged = CspNodePolicyResolver.Merge(baseDefinition, [page]);

		Assert.That(SourcesOf(merged), Is.EqualTo(new[] { "https://www.google.com" }));
	}

	[Test]
	public void Merge_NoneOnADirectiveNothingElseUses_IsKept()
	{
		var baseDefinition = Policy(Home, "'none'", directive: Constants.Directives.ObjectSource);
		var page = Policy(Page, "https://www.google.com", inheritance: CspPolicyInheritance.Extend, directive: Constants.Directives.FrameSource);

		var merged = CspNodePolicyResolver.Merge(baseDefinition, [page]);

		Assert.That(SourcesOf(merged), Is.EqualTo(new[] { "'none'", "https://www.google.com" }));
	}
}
