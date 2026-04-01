export const manifests: Array<UmbExtensionManifest> = [
	{
		type: 'modal',
		alias: 'Umbraco.Community.CSPManager.Modal.ImportCsp',
		name: 'CSP Import Modal',
		js: () => import('./import-csp-modal.element.js'),
	},
];
