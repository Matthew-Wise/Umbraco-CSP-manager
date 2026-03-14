const packageAlias = 'Umbraco.Community.CSPManager';
export const CspConstants = {
	alias: packageAlias,
	section: {
		label: 'CSP Manager',
		alias: `${packageAlias}.Section`,
	},
	menu: {
		alias: `${packageAlias}.Menu`,
	},
	tree: {
		alias: `${packageAlias}.Tree`,
		repositoryAlias: `${packageAlias}.Tree.Repository`,
		itemAlias: `${packageAlias}.TreeItem`,
		menuItemAlias: `${packageAlias}.MenuItem.Tree`,
	},
	entityTypes: {
		cspPolicy: 'csp-policy',
		cspPolicyRoot: 'csp-policy-root',
	},
	workspace: {
		alias: `${packageAlias}.Workspace`,
		entityType: 'csp-policy',
	},
	umbraco: {
		conditions: {
			sectionAlias: 'Umb.Condition.SectionAlias',
			workspaceAlias: 'Umb.Condition.WorkspaceAlias',
		},
	},
	icons: {
		dashboard: 'icon-home',
		sources: 'icon-list',
		settings: 'icon-settings',
		evaluate: 'icon-locate',
	},
	weights: {
		high: 100,
		medium: 200,
	},
	policyTypes: {
		backoffice: {
			value: '9cbfa28c-2b19-40f4-9f8e-bbc52bd8e780',
			label: 'Back Office',
			aliasPart: 'BackOffice',
			icon: 'icon-umbraco',
		},
		frontend: {
			value: 'fac780be-53af-41dc-b51d-1aa647100221',
			label: 'Frontend',
			aliasPart: 'Frontend',
			icon: 'icon-globe',
		},
	},
} as const;

export type PolicyType = (typeof CspConstants.policyTypes)[keyof typeof CspConstants.policyTypes];
