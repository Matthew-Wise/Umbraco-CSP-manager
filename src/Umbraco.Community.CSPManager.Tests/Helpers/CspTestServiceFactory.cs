using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Tests.Helpers;

internal static class CspTestServiceFactory
{
	/// <summary>
	/// Creates a <see cref="CspService"/> from the test host's services, with its own caches and,
	/// when given, its own event aggregator or options. The one place that knows the constructor.
	/// </summary>
	public static CspService Create(
		IServiceProvider services,
		AppCaches caches,
		IEventAggregator eventAggregator = null,
		IOptionsMonitor<CspManagerOptions> options = null)
		=> new(
			eventAggregator ?? services.GetRequiredService<IEventAggregator>(),
			services.GetRequiredService<IScopeProvider>(),
			caches,
			NullLogger<CspService>.Instance,
			services.GetRequiredService<IDomainService>(),
			services.GetRequiredService<IEntityService>(),
			services.GetRequiredService<ILanguageService>(),
			services.GetRequiredService<IDocumentNavigationQueryService>(),
			options ?? services.GetRequiredService<IOptionsMonitor<CspManagerOptions>>());
}
