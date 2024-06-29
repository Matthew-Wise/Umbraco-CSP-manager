import { UmbEntryPointOnInit } from '@umbraco-cms/backoffice/extension-api';
import { ManifestTypes } from '@umbraco-cms/backoffice/extension-registry';

// load up the manifests here.
import { manifests as sectionManifests } from './sections/manifest.ts';
import { manifests as sidebarsManifests } from './sidebars/manifest.ts';
import { manifests as workspacesManifests } from './workspaces/manifest.ts';
import { manifests as dashboardsManifests } from './dashboards/manifest.ts';

const manifests: Array<ManifestTypes> = [
	...sectionManifests,
	...sidebarsManifests,
	...workspacesManifests,
	...dashboardsManifests,
];

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {
	// register them here.
	extensionRegistry.registerMany(manifests);
};
