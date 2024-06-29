import type { ManifestSection } from '@umbraco-cms/backoffice/extension-registry';
import { cspConstants } from '../constants';

const sections: Array<ManifestSection> = [
	{
		type: 'section',
		alias: cspConstants.section.alias,
		name: cspConstants.name,
		weight: 10,
		meta: {
			label: cspConstants.name,
			pathname: cspConstants.path,
		},
	},
];

export const manifests = [...sections];
