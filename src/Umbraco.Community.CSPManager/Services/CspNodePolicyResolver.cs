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
	}

	/// <summary>
	/// Whether there are no node policies at all, so a request can skip resolution entirely.
	/// </summary>
	public bool IsEmpty => _byNode.Count == 0;

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
	/// <param name="NoHeader">
	/// <c>true</c> when <paramref name="Base"/> is a disabled policy that suppresses the header.
	/// </param>
	internal readonly record struct Resolution(CspDefinition? Base, IReadOnlyList<CspDefinition> Additions, bool NoHeader)
	{
		/// <summary>Whether no node policy applies at all, so the global frontend policy is used as is.</summary>
		public bool IsEmpty => Base is null && Additions.Count == 0;
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

		foreach (var nodeKey in path)
		{
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
						return new Resolution(policy, [], NoHeader: true);
					}

					continue;
				}

				if (policy.Inheritance == CspPolicyInheritance.Extend)
				{
					(additions ??= []).Add(policy);
					continue;
				}

				return new Resolution(policy, additions ?? [], NoHeader: false);
			}
		}

		return new Resolution(null, additions ?? [], NoHeader: false);
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

		void AddSources(CspDefinition definition)
		{
			foreach (var source in definition.Sources)
			{
				if (!bySource.TryGetValue(source.Source, out var target))
				{
					target = new CspDefinitionSource { DefinitionId = merged.Id, Source = source.Source, Directives = [] };
					bySource.Add(source.Source, target);
					merged.Sources.Add(target);
				}

				foreach (var directive in source.Directives)
				{
					if (!target.Directives.Contains(directive))
					{
						target.Directives.Add(directive);
					}
				}
			}
		}

		// Least specific first, so the header reads base, site-wide additions, page additions.
		AddSources(baseDefinition);
		for (var i = additions.Count - 1; i >= 0; i--)
		{
			AddSources(additions[i]);
		}

		RemoveNoneWhereSourcesWereAdded(merged.Sources);

		return merged;
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
