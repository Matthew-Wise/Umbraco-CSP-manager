using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.uSync.Logging;
using Umbraco.Community.CSPManager.uSync.Serializers;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;

namespace Umbraco.Community.CSPManager.uSync.Handlers;

/// <summary>
/// Syncs node policies. They belong to a content node with a hostname, so they import in the
/// Content group, after uSync's Content and Domain handlers.
/// </summary>
[SyncHandler("CspNodePolicyHandler", "CSP Node Policies", "CspNodePolicies", 3010, Icon = "icon-shield", EntityType = CspItemNames.NodePolicyEntityType)]
public class CspNodePolicyHandler : CspHandlerBase<CspNodePolicySerializer>
{
	public CspNodePolicyHandler(ILogger<CspNodePolicyHandler> logger,
		AppCaches appCaches,
		IShortStringHelper shortStringHelper,
		ISyncFileService syncFileService,
		ISyncEventService mutexService,
		ISyncConfigService uSyncConfig,
		ISyncItemFactory itemFactory,
		ICspService cspService)
		: base(logger, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, itemFactory, cspService)
	{
	}

	public override string Group => "Content";

	protected override bool Handles(CspDefinition definition) => definition.ContentKey is not null;

	protected override async Task<IEnumerable<CspDefinition>> GetChildItemsAsync(CspDefinition? parent)
	{
		if (parent != null)
		{
			Log.NoChildItemsForDefinition(logger, parent.Id);
			return [];
		}

		var policies = await CspService.GetAllNodePoliciesAsync(CancellationToken.None);
		Log.GetNodePoliciesResult(logger, policies.Count);
		return policies;
	}
}
