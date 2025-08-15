import { CspConstants } from '@/constants';

export const manifests: Array<UmbExtensionManifest> = [
	{
		name: 'Umbraco Community CSPManager Entrypoint',
		alias: `${CspConstants.alias}.Entrypoint`,
		type: 'backofficeEntryPoint',
		js: () => import('./entrypoint'),
	},
];
