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
	{
		type: 'entityAction',
		kind: 'default',
		alias: CspConstants.domainPolicy.addEntityActionAlias,
		name: 'Add Domain Policy Entity Action',
		api: () => import('./add-domain-policy.action.js'),
		forEntityTypes: [CspConstants.entityTypes.cspPolicy],
		// Below Import (900), so Import stays the inline action and this one sits in the "..." menu.
		weight: 800,
		meta: {
			icon: 'icon-add',
			label: '#cspManagerDomainPolicy_addAction',
		},
		conditions: [
			{
				alias: CspConstants.umbraco.conditions.entityUnique,
				match: CspConstants.policyTypes.frontend.value,
			},
		],
	},
];
