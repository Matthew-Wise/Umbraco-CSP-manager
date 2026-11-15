import { CspConstants } from '@/constants';

export const manifests: Array<UmbExtensionManifest> = [
	{
		type: 'workspace',
		kind: 'routable',
		alias: CspConstants.workspace.alias,
		name: 'CSP Manager Workspace',
		api: () => import('./context/workspace.context.js'),
		meta: {
			entityType: CspConstants.workspace.entityType,
		},
	},
	{
		type: 'workspaceView',
		alias: `${CspConstants.workspace.alias}View.Default`,
		name: 'CSP Sources View',
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
				match: CspConstants.workspace.alias,
			},
		],
	},
	{
		type: 'workspaceView',
		alias: `${CspConstants.workspace.alias}View.Settings`,
		name: 'CSP Settings View',
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
				match: CspConstants.workspace.alias,
			},
		],
	},
	{
		type: 'workspaceView',
		alias: `${CspConstants.workspace.alias}View.Effective`,
		name: 'CSP Effective Policy View',
		js: () => import('./views/effective/effective.element.js'),
		weight: 40,
		meta: {
			label: '#cspManagerEffective_viewLabel',
			pathname: 'effective',
			icon: CspConstants.icons.effective,
		},
		conditions: [
			{
				alias: CspConstants.umbraco.conditions.workspaceAlias,
				match: CspConstants.workspace.alias,
			},
			// Only a node policy inherits anything: the two global policies are what they are.
			{
				alias: CspConstants.nodePolicy.conditionAlias,
			},
		],
	},
	{
		type: 'workspaceView',
		alias: `${CspConstants.workspace.alias}View.Evaluate`,
		name: 'CSP Evaluate View',
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
				match: CspConstants.workspace.alias,
			},
		],
	},
];
