import { CspConstants } from '@/constants';

export const manifests: Array<UmbExtensionManifest> = [
	{
		type: 'entityAction',
		kind: 'default',
		alias: 'Umbraco.Community.CSPManager.EntityAction.Import',
		name: 'Import CSP Entity Action',
		api: () => import('./import-csp.action.js'),
		forEntityTypes: [CspConstants.entityTypes.cspPolicy],
		weight: 900,
		meta: {
			icon: 'icon-download-alt',
			label: 'Import...',
		},
	},
];
