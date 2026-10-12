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
			entityUnique: 'Umb.Condition.Entity.Unique',
		},
	},
	icons: {
		dashboard: 'icon-home',
		sources: 'icon-list',
		settings: 'icon-settings',
		evaluate: 'icon-locate',
		domainPolicy: 'icon-link',
		orphanedDomainPolicy: 'icon-alert',
	},
	domainPolicy: {
		addEntityActionAlias: `${packageAlias}.EntityAction.AddDomainPolicy`,
		addModalAlias: `${packageAlias}.Modal.AddDomainPolicy`,
		/** Id a new (unsaved) domain policy is posted with; the server assigns the real one. */
		newId: '00000000-0000-0000-0000-000000000000',
		/** DisabledNodePolicyBehavior values, as serialized by the API. */
		disabledBehavior: {
			fallbackToGlobal: 0,
			noHeader: 1,
		},
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

/** True for the two global policy ids; anything else is a domain policy. */
export const isGlobalPolicyId = (unique: string | null | undefined): boolean =>
	unique === CspConstants.policyTypes.frontend.value || unique === CspConstants.policyTypes.backoffice.value;
