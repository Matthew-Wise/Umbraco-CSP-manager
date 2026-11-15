using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Helpers;

internal static class CspTestLanguageHelper
{
	/// <summary>
	/// Creates a language, optionally falling back to another, and asserts it was created.
	/// </summary>
	public static async Task CreateLanguageAsync(ILanguageService languageService, string isoCode, string fallback = null)
	{
		var result = await languageService.CreateAsync(
			new Language(isoCode, isoCode) { FallbackIsoCode = fallback },
			UmbConstants.Security.SuperUserKey);
		Assert.That(result.Success, Is.True, $"Creating language {isoCode} failed: {result.Status}");
	}
}
