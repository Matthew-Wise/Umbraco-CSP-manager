import {
	ManifestMenu,
	ManifestMenuItem,
	ManifestSectionSidebarAppMenuKind,
} from '@umbraco-cms/backoffice/extension-registry';
import { cspConstants } from '../constants';

const menu: ManifestMenu = {
	type: 'menu',
	alias: cspConstants.menuAlias,
	name: 'CSPManager menu',
	meta: {
		label: cspConstants.name,
	},
};

const sidebarKind: ManifestSectionSidebarAppMenuKind = {
	type: 'sectionSidebarApp',
	kind: 'menu',
	alias: 'csp.sidebar',
	name: 'CSP sidebar',
	meta: {
		label: 'Content Security Policies',
		menu: cspConstants.menuAlias,
	},
	conditions: [
		{
			alias: 'Umb.Condition.SectionAlias',
			match: cspConstants.section.alias,
		},
	],
};

const backofficeMenuItemManifest: ManifestMenuItem = {
	type: 'menuItem',
	alias: cspConstants.menuAlias + '.backoffice',
	name: 'Backoffice',
	meta: {
		label: 'Back Office',
		icon: 'icon-umbraco',
		entityType: 'csp-backoffice',
		menus: [cspConstants.menuAlias],
	},
};

const frontendMenuItemManifest: ManifestMenuItem = {
	type: 'menuItem',
	alias: cspConstants.menuAlias + '.frontend',
	name: 'Front end',
	meta: {
		label: 'Front end',
		icon: 'icon-globe',
		entityType: 'csp-frontend',
		menus: [cspConstants.menuAlias],
	},
};

export const manifests = [menu, sidebarKind, backofficeMenuItemManifest, frontendMenuItemManifest];
