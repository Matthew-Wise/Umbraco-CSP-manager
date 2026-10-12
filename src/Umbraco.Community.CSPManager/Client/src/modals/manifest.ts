import { CspConstants } from '@/constants';

export const manifests: Array<UmbExtensionManifest> = [
	{
		type: 'modal',
		alias: 'Umbraco.Community.CSPManager.Modal.ImportCsp',
		name: 'CSP Import Modal',
		js: () => import('./import-csp-modal.element.js'),
	},
	{
		type: 'modal',
		alias: CspConstants.domainPolicy.addModalAlias,
		name: 'CSP Add Domain Policy Modal',
		js: () => import('./add-domain-policy-modal.element.js'),
	},
];
