using System.Collections.Frozen;
using Umbraco.Community.CSPManager.Models;

namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Every node policy, grouped by content node. Immutable, so one instance can be cached and shared
/// by every request.
/// </summary>
internal sealed class CspNodePolicySnapshot
{
	private readonly Dictionary<Guid, CspDefinition[]> _byNode;

	public CspNodePolicySnapshot(IEnumerable<CspDefinition> policies)
	{
		_byNode = policies
			.Where(p => p.ContentKey is not null)
			.GroupBy(p => p.ContentKey!.Value)
			.ToDictionary(g => g.Key, g => g.ToArray());

		HasCulturePolicies = _byNode.Values.Any(nodePolicies => nodePolicies.Any(p => !string.IsNullOrEmpty(p.Culture)));
	}

	/// <summary>
	/// Whether there are no node policies at all, so a request can skip resolution entirely.
	/// </summary>
	public bool IsEmpty => _byNode.Count == 0;

	/// <summary>
	/// Whether any policy is for one culture. When none is, a request's culture and its fallback
	/// languages can't change the outcome, so there is no need to look them up.
	/// </summary>
	public bool HasCulturePolicies { get; }

	/// <summary>
	/// Gets the policies of a content node (one per culture, plus one for all cultures), or none.
	/// The instances are the cached ones: never hand them out or change them.
	/// </summary>
	public IReadOnlyList<CspDefinition> ForNode(Guid contentKey)
		=> _byNode.TryGetValue(contentKey, out var policies) ? policies : [];
}

/// <summary>
/// Decides which node policies apply to a request and combines them. Pure logic over a
/// <see cref="CspNodePolicySnapshot"/>: no database, cache or Umbraco services.
/// </summary>
/// <remarks>
/// <para>
/// Candidates are visited from most to least specific: the requested page first, then each
/// ancestor up to the root; and on each node the request's culture first, then its fallback
/// languages, then the node's policy for all cultures. A policy on an ancestor only counts when it
/// applies to its descendants (<see cref="CspPolicyScope.Descendants"/>).
/// </para>
/// <para>
/// Every <see cref="CspPolicyInheritance.Extend"/> policy met on the way is collected, and the walk
/// ends at the first <see cref="CspPolicyInheritance.Replace"/> policy, which is the base the
/// collected sources are added to. With no such policy the base is the global frontend policy.
/// </para>
/// <para>
/// A disabled policy is skipped as if it didn't exist, with one exception: a disabled
/// <see cref="CspPolicyInheritance.Replace"/> policy under
/// <see cref="DisabledNodePolicyBehavior.NoHeader"/> ends the walk with no header at all.
/// </para>
/// </remarks>
internal static class CspNodePolicyResolver
{
	private const string NoneSource = "'none'";

	// What a browser uses for a fetch directive a policy doesn't have, nearest first (CSP Level 3,
	// "directive fallback list"). Directives that aren't listed have no fallback. child-src is
	// simplified to default-src: for workers a browser would look at script-src first.
	private static readonly FrozenDictionary<string, string[]> FallbackDirectives = new Dictionary<string, string[]>
	{
		[Constants.Directives.ScriptSourceElement] = [Constants.Directives.ScriptSource, Constants.Directives.DefaultSource],
		[Constants.Directives.ScriptSourceAttribute] = [Constants.Directives.ScriptSource, Constants.Directives.DefaultSource],
		[Constants.Directives.StyleSourceElement] = [Constants.Directives.StyleSource, Constants.Directives.DefaultSource],
		[Constants.Directives.StyleSourceAttribute] = [Constants.Directives.StyleSource, Constants.Directives.DefaultSource],
		[Constants.Directives.WorkerSource] = [Constants.Directives.ChildSource, Constants.Directives.ScriptSource, Constants.Directives.DefaultSource],
		[Constants.Directives.FrameSource] = [Constants.Directives.ChildSource, Constants.Directives.DefaultSource],
		[Constants.Directives.ChildSource] = [Constants.Directives.DefaultSource],
		[Constants.Directives.ConnectSource] = [Constants.Directives.DefaultSource],
		[Constants.Directives.FontSource] = [Constants.Directives.DefaultSource],
		[Constants.Directives.ImageSource] = [Constants.Directives.DefaultSource],
		[Constants.Directives.ManifestSource] = [Constants.Directives.DefaultSource],
		[Constants.Directives.MediaSource] = [Constants.Directives.DefaultSource],
		[Constants.Directives.ObjectSource] = [Constants.Directives.DefaultSource],
		[Constants.Directives.PreFetchSource] = [Constants.Directives.DefaultSource],
		[Constants.Directives.ScriptSource] = [Constants.Directives.DefaultSource],
		[Constants.Directives.StyleSource] = [Constants.Directives.DefaultSource],
	}.ToFrozenDictionary(StringComparer.Ordinal);

	/// <summary>
	/// The outcome of a walk.
	/// </summary>
	/// <param name="Base">
	/// The complete policy the walk ended at, or <c>null</c> when it reached the root, in which
	/// case the global frontend policy is the base. The cached instance: copy before handing out.
	/// </param>
	/// <param name="Additions">
	/// The <see cref="CspPolicyInheritance.Extend"/> policies that apply, most specific first.
	/// </param>
	internal readonly record struct Resolution(CspDefinition? Base, IReadOnlyList<CspDefinition> Additions)
	{
		/// <summary>Whether no node policy applies at all, so the global frontend policy is used as is.</summary>
		public bool IsEmpty => Base is null && Additions.Count == 0;

		/// <summary>
		/// Whether <see cref="Base"/> is a disabled policy that suppresses the header. The walk only
		/// ends at a disabled policy for that reason, and then without additions.
		/// </summary>
		public bool NoHeader => Base is { Enabled: false };
	}

	/// <summary>
	/// Walks the candidates for a request.
	/// </summary>
	/// <param name="snapshot">Every node policy.</param>
	/// <param name="path">The requested page followed by its ancestors, nearest first.</param>
	/// <param name="pageKey">
	/// The requested page, or <c>null</c> when the request has none. Policies that only apply to
	/// their own node (<see cref="CspPolicyScope.NodeOnly"/>) are used for this node alone.
	/// </param>
	/// <param name="cultures">The request's culture followed by its fallback languages; empty for no culture.</param>
	/// <param name="disabledBehavior">What a disabled complete policy does.</param>
	public static Resolution Resolve(
		CspNodePolicySnapshot snapshot,
		IReadOnlyList<Guid> path,
		Guid? pageKey,
		IReadOnlyList<string> cultures,
		DisabledNodePolicyBehavior disabledBehavior)
	{
		List<CspDefinition>? additions = null;

		// Indexed, like Find: foreach over the interface would box an enumerator on every request.
		for (var n = 0; n < path.Count; n++)
		{
			var nodeKey = path[n];
			var policies = snapshot.ForNode(nodeKey);
			if (policies.Count == 0)
			{
				continue;
			}

			var isPage = nodeKey == pageKey;

			// One past the last culture is the node's policy for all cultures (a null culture).
			for (var i = 0; i <= cultures.Count; i++)
			{
				var policy = Find(policies, i < cultures.Count ? cultures[i] : null, isPage);
				if (policy is null)
				{
					continue;
				}

				if (!policy.Enabled)
				{
					if (policy.Inheritance == CspPolicyInheritance.Replace && disabledBehavior == DisabledNodePolicyBehavior.NoHeader)
					{
						return new Resolution(policy, []);
					}

					continue;
				}

				if (policy.Inheritance == CspPolicyInheritance.Extend)
				{
					(additions ??= []).Add(policy);
					continue;
				}

				return new Resolution(policy, additions ?? []);
			}
		}

		return new Resolution(null, additions ?? []);
	}

	/// <summary>
	/// Builds the policy a request gets from a base policy and the policies that extend it.
	/// </summary>
	/// <param name="baseDefinition">The complete policy. Not changed.</param>
	/// <param name="additions">The extending policies, most specific first; at least one. Not changed.</param>
	/// <returns>
	/// A new definition with the identity (id, content node, culture) of the most specific policy,
	/// the settings of <paramref name="baseDefinition"/> and the sources of all of them.
	/// </returns>
	/// <remarks>
	/// An extending policy only ever allows more. A fetch directive it is the first to use (say
	/// <c>script-src</c> on top of a policy with only <c>default-src</c>) would otherwise stop
	/// falling back and lose everything it allowed so far, so that directive starts from the sources
	/// of the directive it fell back to. When it fell back to nothing, the policy so far doesn't
	/// restrict it and the extra sources are already allowed: the directive is left out.
	/// </remarks>
	public static CspDefinition Merge(CspDefinition baseDefinition, IReadOnlyList<CspDefinition> additions)
	{
		var mostSpecific = additions[0];
		var merged = new CspDefinition
		{
			Id = mostSpecific.Id,
			ContentKey = mostSpecific.ContentKey,
			Culture = mostSpecific.Culture,
			Inheritance = mostSpecific.Inheritance,
			AppliesTo = mostSpecific.AppliesTo,
			IsBackOffice = false,
			Enabled = baseDefinition.Enabled,
			ReportOnly = baseDefinition.ReportOnly,
			ReportingDirective = baseDefinition.ReportingDirective,
			ReportUri = baseDefinition.ReportUri,
			UpgradeInsecureRequests = baseDefinition.UpgradeInsecureRequests,
		};

		var bySource = new Dictionary<string, CspDefinitionSource>(StringComparer.Ordinal);
		var directivesInUse = new HashSet<string>(StringComparer.Ordinal);

		void AddSources(CspDefinition definition, HashSet<string>? leftOut)
		{
			foreach (var source in definition.Sources)
			{
				bySource.TryGetValue(source.Source, out var target);

				foreach (var directive in source.Directives)
				{
					if (leftOut is not null && leftOut.Contains(directive))
					{
						continue;
					}

					if (target is null)
					{
						target = new CspDefinitionSource { DefinitionId = merged.Id, Source = source.Source, Directives = [] };
						bySource.Add(source.Source, target);
						merged.Sources.Add(target);
					}

					if (!target.Directives.Contains(directive))
					{
						target.Directives.Add(directive);
					}

					directivesInUse.Add(directive);
				}
			}
		}

		// Least specific first, so the header reads base, site-wide additions, page additions.
		AddSources(baseDefinition, leftOut: null);
		for (var i = additions.Count - 1; i >= 0; i--)
		{
			var unrestricted = StartNewFetchDirectives(additions[i], merged.Sources, directivesInUse);
			AddSources(additions[i], unrestricted);
		}

		RemoveNoneWhereSourcesWereAdded(merged.Sources);

		return merged;
	}

	// Prepares the merged sources for the fetch directives that an extending policy is the first to
	// use: each starts from the sources of the directive it fell back to until now. Returns the
	// ones that fell back to nothing, which the policy must not add (it would restrict what the
	// policy so far allows), or null when there are none.
	private static HashSet<string>? StartNewFetchDirectives(
		CspDefinition addition,
		List<CspDefinitionSource> mergedSources,
		HashSet<string> directivesInUse)
	{
		// The fallback of every new directive is decided before any of them is started, so that
		// one new directive never starts from another one of the same policy.
		List<(string Directive, string? FellBackTo)>? started = null;
		foreach (var source in addition.Sources)
		{
			foreach (var directive in source.Directives)
			{
				if (!directivesInUse.Contains(directive)
					&& FallbackDirectives.TryGetValue(directive, out var fallbacks)
					&& started?.Exists(s => s.Directive == directive) != true)
				{
					(started ??= []).Add((directive, Array.Find(fallbacks, directivesInUse.Contains)));
				}
			}
		}

		if (started is null)
		{
			return null;
		}

		HashSet<string>? unrestricted = null;
		foreach (var (directive, fellBackTo) in started)
		{
			if (fellBackTo is null)
			{
				(unrestricted ??= new HashSet<string>(StringComparer.Ordinal)).Add(directive);
				continue;
			}

			foreach (var source in mergedSources)
			{
				if (source.Directives.Contains(fellBackTo))
				{
					source.Directives.Add(directive);
				}
			}

			directivesInUse.Add(directive);
		}

		return unrestricted;
	}

	// 'none' only means something as the only value of a directive: next to another source a
	// browser ignores it (and reports the policy as invalid). A base policy with frame-src 'none'
	// extended by a page that allows one frame origin must therefore lose 'none' for frame-src.
	private static void RemoveNoneWhereSourcesWereAdded(List<CspDefinitionSource> sources)
	{
		var none = sources.Find(s => string.Equals(s.Source.Trim(), NoneSource, StringComparison.OrdinalIgnoreCase));
		if (none is null)
		{
			return;
		}

		var usedByOthers = new HashSet<string>(StringComparer.Ordinal);
		foreach (var source in sources)
		{
			if (!ReferenceEquals(source, none))
			{
				usedByOthers.UnionWith(source.Directives);
			}
		}

		none.Directives.RemoveAll(usedByOthers.Contains);
		if (none.Directives.Count == 0)
		{
			sources.Remove(none);
		}
	}

	private static CspDefinition? Find(IReadOnlyList<CspDefinition> policies, string? culture, bool isPage)
	{
		// A plain loop: this runs for every frontend request on a site with node policies.
		for (var i = 0; i < policies.Count; i++)
		{
			var policy = policies[i];
			if ((isPage || policy.AppliesTo == CspPolicyScope.Descendants) && CultureEquals(policy.Culture, culture))
			{
				return policy;
			}
		}

		return null;
	}

	internal static bool CultureEquals(string? left, string? right)
		=> string.IsNullOrEmpty(left)
			? string.IsNullOrEmpty(right)
			: string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
