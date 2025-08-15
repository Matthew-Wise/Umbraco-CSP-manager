import { CspConstants } from '@/constants';

export const manifests: Array<UmbExtensionManifest> = [
	{
		type: 'repository',
		alias: `${CspConstants.alias}.Repository.CspDefinition`,
		name: 'CSP Definition Repository',
		api: () => import('./csp-definition.repository.js'),
	},
	{
		type: 'repository',
		alias: `${CspConstants.alias}.Repository.CspDirectives`,
		name: 'CSP Directives Repository',
		api: () => import('./csp-directives.repository.js'),
	},
];
