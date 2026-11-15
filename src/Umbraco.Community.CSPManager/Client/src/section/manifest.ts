import { CspConstants } from '@/constants';
import { CSP_MANAGER_SECTION_PATHNAME } from '@/paths';

export const manifests: Array<UmbExtensionManifest> = [
	{
		type: 'section',
		alias: CspConstants.section.alias,
		name: 'CSP Management Section',
		weight: CspConstants.weights.high,
		meta: {
			label: CspConstants.section.label,
			pathname: CSP_MANAGER_SECTION_PATHNAME,
		},
	},
	{
		type: 'sectionSidebarApp',
		kind: 'menuWithEntityActions',
		alias: `${CspConstants.alias}.Management`,
		name: 'CSP Manager Section Sidebar App',
		weight: CspConstants.weights.high,
		meta: {
			label: 'CSP Manager',
			menu: CspConstants.menu.alias,
			entityType: CspConstants.entityTypes.cspPolicyRoot,
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
];
