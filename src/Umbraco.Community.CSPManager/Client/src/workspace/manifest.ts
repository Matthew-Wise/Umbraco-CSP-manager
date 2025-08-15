import { CspConstants, type PolicyType } from '@/constants';

const createWorkspaceManifests = (policyType: PolicyType): Array<UmbExtensionManifest> => [
	{
		type: 'workspace',
		alias: `${CspConstants.workspace.alias}.${policyType.aliasPart}`,
		name: `CSP Manager ${policyType.label} Workspace`,
		js: () => import('./csp-management-workspace.element.js'),
		meta: {
			entityType: policyType.value,
		},
	},
	{
		type: 'workspaceContext',
		alias: `${CspConstants.workspace.alias}Context.${policyType.aliasPart}`,
		name: `CSP Manager ${policyType.label} Workspace Context`,
		js: () => import('./context/workspace.context.js'),
		conditions: [
			{
				alias: CspConstants.umbraco.conditions.workspaceAlias,
				match: `${CspConstants.workspace.alias}.${policyType.aliasPart}`,
			},
		],
	},
	{
		type: 'workspaceView',
		alias: `${CspConstants.workspace.alias}View.${policyType.aliasPart}.Default`,
		name: `CSP ${policyType.label} Sources View`,
		js: () => import('./views/default/default.element.js'),
		weight: CspConstants.weights.medium,
		meta: {
			label: 'Sources',
			pathname: 'sources',
			icon: CspConstants.icons.sources,
		},
		conditions: [
			{
				alias: CspConstants.umbraco.conditions.workspaceAlias,
				match: `${CspConstants.workspace.alias}.${policyType.aliasPart}`,
			},
		],
	},
	{
		type: 'workspaceView',
		alias: `${CspConstants.workspace.alias}View.${policyType.aliasPart}.Settings`,
		name: `CSP ${policyType.label} Settings View`,
		js: () => import('./views/settings/settings.element.js'),
		weight: CspConstants.weights.high,
		meta: {
			label: 'Settings',
			pathname: 'settings',
			icon: CspConstants.icons.settings,
		},
		conditions: [
			{
				alias: CspConstants.umbraco.conditions.workspaceAlias,
				match: `${CspConstants.workspace.alias}.${policyType.aliasPart}`,
			},
		],
	},
	{
		type: 'workspaceView',
		alias: `${CspConstants.workspace.alias}View.${policyType.aliasPart}.Evaluate`,
		name: `CSP ${policyType.label} Evaluate View`,
		js: () => import('./views/evaluate/evaluate.element.js'),
		weight: 50,
		meta: {
			label: 'Evaluate',
			pathname: 'evaluate',
			icon: CspConstants.icons.evaluate,
		},
		conditions: [
			{
				alias: CspConstants.umbraco.conditions.workspaceAlias,
				match: `${CspConstants.workspace.alias}.${policyType.aliasPart}`,
			},
		],
	},
];

export const manifests: Array<UmbExtensionManifest> = [
	...createWorkspaceManifests(CspConstants.policyTypes.frontend),
	...createWorkspaceManifests(CspConstants.policyTypes.backoffice),
];
