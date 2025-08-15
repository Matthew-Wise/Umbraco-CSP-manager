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
	workspace: {
		alias: `${packageAlias}.Workspace`,
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
			value: 'backoffice',
			label: 'Back Office',
			aliasPart: 'BackOffice',
			icon: 'icon-umbraco',
		},
		frontend: {
			value: 'frontend',
			label: 'Frontend',
			aliasPart: 'Frontend',
			icon: 'icon-globe',
		},
	},
} as const;

export type PolicyType = (typeof CspConstants.policyTypes)[keyof typeof CspConstants.policyTypes];
