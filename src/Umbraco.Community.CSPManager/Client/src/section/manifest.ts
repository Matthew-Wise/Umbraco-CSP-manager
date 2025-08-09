import { CspConstants } from "../constants.js";

export const manifests: Array<UmbExtensionManifest> = [
	{
		type: "section",
		alias: "Umbraco.Community.CSPManager.Section",
		name: "CSP Management Section",
		weight: 100,
		meta: {
			label: CspConstants.sectionLabel,
			pathname: "csp-manager",
		},
	},
	{
		type: "sectionSidebarApp",
		kind: "menu",
		alias: "Umbraco.Community.CSPManager.Management",
		name: "CSP Manager Section Sidebar App",
		weight: 100,
		meta: {
			label: "CSP Manager",
			menu: "Umbraco.Community.CSPManager.Menu",
		},
		conditions: [
			{
				alias: "Umb.Condition.SectionAlias",
				match: "Umbraco.Community.CSPManager.Section",
			},
		],
	},
	{
		type: "menu",
		alias: "Umbraco.Community.CSPManager.Menu",
		name: "CSP Manager Menu",
	},
	{
		type: "sectionView",
		alias: "Umbraco.Community.CSPManager.SectionView.Dashboard",
		name: "CSP Manager Section Dashboard",
		js: () => import("./section-dashboard.element.js"),
		weight: 100,
		meta: {
			label: "Dashboard",
			pathname: "dashboard",
			icon: "icon-home",
		},
		conditions: [
			{
				alias: "Umb.Condition.SectionAlias",
				match: "Umbraco.Community.CSPManager.Section",
			},
		],
	},
	{
		type: "menuItem",
		alias: "Umbraco.Community.CSPManager.MenuItems.Backoffice",
		name: "CSP Backoffice Menu Item",
		weight: 200,
		meta: {
			label: "Back Office",
			icon: "icon-umbraco",
			entityType: "backoffice",
			menus: ["Umbraco.Community.CSPManager.Menu"],
		},
	},
	{
		type: "menuItem",
		alias: "Umbraco.Community.CSPManager.MenuItems.Frontend",
		name: "CSP Frontend Menu Item",
		weight: 100,
		meta: {
			label: "Front end",
			icon: "icon-globe",
			entityType: "frontend",
			menus: ["Umbraco.Community.CSPManager.Menu"],
		},
	},
];
