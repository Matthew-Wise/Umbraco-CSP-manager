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
		alias: CspConstants.nodePolicy.addModalAlias,
		name: 'CSP Add Node Policy Modal',
		js: () => import('./add-node-policy-modal.element.js'),
	},
];
