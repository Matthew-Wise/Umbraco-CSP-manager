using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.CSPManager.Tests.Helpers;

internal static class CspTestDomainHelper
{
	/// <summary>
	/// Assigns the given hostnames (and optionally a wildcard culture domain) to a content node and
	/// returns the resulting domains.
	/// </summary>
	public static async Task<IReadOnlyList<IDomain>> AssignDomainsAsync(
		IDomainService domainService,
		Guid contentKey,
		string[] domainNames,
		string defaultIsoCode = null)
	{
		var result = await domainService.UpdateDomainsAsync(contentKey, new DomainsUpdateModel
		{
			DefaultIsoCode = defaultIsoCode,
			Domains = domainNames.Select(name => new DomainModel { DomainName = name, IsoCode = "en-US" }),
		});
		Assert.That(result.Success, Is.True, $"Assigning domains failed: {result.Status}");

		return [.. await domainService.GetAssignedDomainsAsync(contentKey, includeWildcards: true)];
	}
}
