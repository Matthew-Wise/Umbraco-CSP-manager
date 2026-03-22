import { CspConstants } from '@/constants';

export const manifests: Array<UmbExtensionManifest> = [
	{
		 "type": "localization",
      "alias": `${CspConstants.alias}.Localize.EnUS`,
      "name": "English",
      "meta": {
        "culture": "en"
      },
			"js": () => import('./en.ts'),
	},
];
