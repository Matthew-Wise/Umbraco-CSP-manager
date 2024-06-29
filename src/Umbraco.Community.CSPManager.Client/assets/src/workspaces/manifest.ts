import {
	ManifestWorkspace,
	ManifestWorkspaceContext,
	ManifestWorkspaceView,
} from '@umbraco-cms/backoffice/extension-registry';

var frontendWorkspace: ManifestWorkspace = {
	type: 'workspace',
	alias: 'csp.workspace.frontend',
	name: 'csp workspace',
	js: () => import('./workspace.element.js'),
	meta: {
		entityType: 'csp-frontend',
	},
};
var workspaceViews: Array<ManifestWorkspaceView> = [
	{
		type: 'workspaceView',
		alias: 'csp.workspace.frontend.manage',
		name: 'manage Frontend CSP Workspace',
		js: () => import('./views/default.element.js'),
		weight: 300,
		meta: {
			icon: 'icon-alarm-clock',
			pathname: 'manage',
			label: 'Front end',
		},
		conditions: [
			{
				alias: 'Umb.Condition.WorkspaceAlias',
				match: frontendWorkspace.alias,
			},
		],
	},
];

const context: ManifestWorkspaceContext = {
	type: 'workspaceContext',
	alias: 'csp.workspace.context',
	name: 'csp workspace context',
	js: () => import('./context.js'),
};

export const manifests = [context, frontendWorkspace, ...workspaceViews];
