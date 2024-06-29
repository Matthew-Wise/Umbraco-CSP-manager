import type { ManifestDashboard } from '@umbraco-cms/backoffice/extension-registry';
import { cspConstants } from '../constants';

const dashboards: Array<ManifestDashboard> = [
	{
		type: 'dashboard',
		alias: 'csp.dashboard',
		name: cspConstants.name,
		js: () => import('./dashboard.element.js'),
		weight: 10,
		meta: {
			label: cspConstants.name,
		},
		conditions: [
			{
				alias: 'Umb.Condition.SectionAlias',
				match: cspConstants.section.alias,
			},
		],
	},
];

export const manifests = [...dashboards];
