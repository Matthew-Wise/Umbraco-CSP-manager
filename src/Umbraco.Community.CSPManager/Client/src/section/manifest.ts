import { CspConstants, type PolicyType } from '@/constants';

const createMenuItemManifest = (policyType: PolicyType, weight: number): UmbExtensionManifest => ({
	type: 'menuItem',
	alias: `${CspConstants.alias}.MenuItems.${policyType.aliasPart}`,
	name: `CSP ${policyType.label} Menu Item`,
	weight,
	meta: {
		label: policyType.label,
		icon: policyType.icon,
		entityType: policyType.value,
		menus: [CspConstants.menu.alias],
	},
});

const menuItemsConfig = [
	{ policyType: CspConstants.policyTypes.frontend, weight: CspConstants.weights.high },
	{ policyType: CspConstants.policyTypes.backoffice, weight: CspConstants.weights.medium },
];

export const manifests: Array<UmbExtensionManifest> = [
	{
		type: 'section',
		alias: CspConstants.section.alias,
		name: 'CSP Management Section',
		weight: CspConstants.weights.high,
		meta: {
			label: CspConstants.section.label,
			pathname: 'csp-manager',
		},
	},
	{
		type: 'sectionSidebarApp',
		kind: 'menu',
		alias: `${CspConstants.alias}.Management`,
		name: 'CSP Manager Section Sidebar App',
		weight: CspConstants.weights.high,
		meta: {
			label: 'CSP Manager',
			menu: CspConstants.menu.alias,
		},
		conditions: [
			{
				alias: CspConstants.umbraco.conditions.sectionAlias,
				match: CspConstants.section.alias,
			},
		],
	},
	{
		type: 'menu',
		alias: CspConstants.menu.alias,
		name: 'CSP Manager Menu',
	},
	{
		type: 'sectionView',
		alias: `${CspConstants.section.alias}View.Dashboard`,
		name: 'CSP Manager Section Dashboard',
		js: () => import('./section-dashboard.element.js'),
		weight: CspConstants.weights.high,
		meta: {
			label: 'Dashboard',
			pathname: 'dashboard',
			icon: CspConstants.icons.dashboard,
		},
		conditions: [
			{
				alias: CspConstants.umbraco.conditions.sectionAlias,
				match: CspConstants.section.alias,
			},
		],
	},
	...menuItemsConfig.map(({ policyType, weight }) => createMenuItemManifest(policyType, weight)),
];
