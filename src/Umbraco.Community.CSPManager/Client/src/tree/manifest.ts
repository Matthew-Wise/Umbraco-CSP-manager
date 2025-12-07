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
];
