namespace Umbraco.Community.CSPManager.Backoffice;

using Cms.Core;
using Cms.Core.Events;
using Cms.Core.Services;
using Cms.Core.Trees;
using Cms.Web.BackOffice.Trees;
using Cms.Web.Common.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[PluginController(CspConstants.PluginAlias)]
[Tree(CspConstants.PluginAlias, "manage-csp", TreeTitle = "CSP Manager")]
public class CspManagementTreeController : TreeController
{
	private readonly IMenuItemCollectionFactory _menuItemCollectionFactory;

	public CspManagementTreeController(ILocalizedTextService localizedTextService,
		UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection, IEventAggregator eventAggregator,
		IMenuItemCollectionFactory menuItemCollectionFactory) : base(localizedTextService,
		umbracoApiControllerTypeCollection, eventAggregator)
	{
		_menuItemCollectionFactory = menuItemCollectionFactory;
	}

	protected override ActionResult<TreeNodeCollection> GetTreeNodes(string id, FormCollection queryStrings)
	{
		return new TreeNodeCollection
		{
			CreateTreeNode("1", "-1", queryStrings, "Back Office", "icon-umbraco blue", false),
			CreateTreeNode("2", "-1", queryStrings, "Front end", "icon-globe blue", false)
		};
	}

	protected override ActionResult<MenuItemCollection> GetMenuForNode(string id, FormCollection queryStrings)
	{
		return _menuItemCollectionFactory.Create();
	}
}
