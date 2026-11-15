import { CspConstants } from '@/constants';

export const manifests: Array<UmbExtensionManifest> = [
	{
		type: 'condition',
		name: 'CSP Node Policy Condition',
		alias: CspConstants.nodePolicy.conditionAlias,
		api: () => import('./node-policy.condition.js'),
	},
];
