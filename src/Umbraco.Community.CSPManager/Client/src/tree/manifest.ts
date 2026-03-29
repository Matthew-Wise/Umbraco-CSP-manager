import { CspConstants } from '@/constants';

export const manifests: Array<UmbExtensionManifest> = [
	// Tree Repository
	{
		type: 'repository',
		alias: CspConstants.tree.repositoryAlias,
		name: 'CSP Manager Tree Repository',
		api: () => import('./tree.repository.js'),
	},
	// Tree
	{
		type: 'tree',
		kind: 'default',
		alias: CspConstants.tree.alias,
		name: 'CSP Manager Tree',
		meta: {
			repositoryAlias: CspConstants.tree.repositoryAlias,
		},
	},
	// Tree Item for csp-policy entity type
	{
		type: 'treeItem',
		kind: 'default',
		alias: CspConstants.tree.itemAlias,
		name: 'CSP Policy Tree Item',
		forEntityTypes: [CspConstants.workspace.entityType],
	},
	// Menu Item that renders the tree in the sidebar
	{
		type: 'menuItem',
		kind: 'tree',
		alias: CspConstants.tree.menuItemAlias,
		name: 'CSP Manager Tree Menu Item',
		meta: {
			label: 'CSP Policies',
			treeAlias: CspConstants.tree.alias,
			menus: [CspConstants.menu.alias],
			hideTreeRoot: true,
		},
	},
	// Modal for selecting a domain when adding a domain policy
	{
		type: 'modal',
		alias: 'Umbraco.Community.CSPManager.Modal.AddDomainPolicy',
		name: 'Add Domain Policy Modal',
		element: () => import('./add-domain-policy-modal.element.js'),
	},
	// Entity action: Add Domain Policy — only visible on the Frontend policy node
	{
		type: 'entityAction',
		kind: 'default',
		alias: `${CspConstants.alias}.EntityAction.AddDomainPolicy`,
		name: 'Add Domain Policy',
		weight: 100,
		api: () => import('./add-domain-policy.action.js'),
		forEntityTypes: [CspConstants.workspace.entityType],
		meta: {
			icon: 'icon-add',
			label: 'Add Domain Policy',
		},
		conditions: [
			{
				alias: CspConstants.umbraco.conditions.entityUnique,
				match: CspConstants.policyTypes.frontend.value,
			},
		],
	},
];
