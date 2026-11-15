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
		// Above Import (900), so on Frontend this is the inline action and Import sits in the "..." menu.
		// Back Office doesn't get this action, so Import stays inline there.
		weight: 1000,
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
