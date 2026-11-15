using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Strings;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.uSync.Logging;
using Umbraco.Community.CSPManager.uSync.Serializers;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;

using CspManagerConstants = Umbraco.Community.CSPManager.Constants;

namespace Umbraco.Community.CSPManager.uSync.Handlers;

/// <summary>
/// Syncs the two global policies (Frontend and Backoffice) in the Settings group. Node policies
/// depend on content, so <see cref="CspNodePolicyHandler"/> syncs them in the Content group.
/// </summary>
[SyncHandler("CspDefinitionHandler", "CSP", "CspDefinitions", 3000, Icon = "icon-shield", EntityType = CspManagerConstants.EntityTypes.CspPolicy)]
public class CspDefinitionHandler : CspHandlerBase<CspDefinitionSerializer>
{
	public CspDefinitionHandler(ILogger<CspDefinitionHandler> logger,
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

	public override string Group => "Settings";

	protected override bool Handles(CspDefinition definition) => definition.ContentKey is null;

	/// <summary>
	///  return child items - if there are none, return an empty list
	/// </summary>
	protected override async Task<IEnumerable<CspDefinition>> GetChildItemsAsync(CspDefinition? parent)
	{
		if (parent != null)
		{
			Log.NoChildItemsForDefinition(logger, parent.Id);
			return [];
		}

		var backoffice = await CspService.GetCspDefinitionAsync(true, CancellationToken.None);
		var frontend = await CspService.GetCspDefinitionAsync(false, CancellationToken.None);
		Log.GetChildItemsResult(logger, backoffice.Id, frontend.Id);
		return [backoffice, frontend];
	}
}
