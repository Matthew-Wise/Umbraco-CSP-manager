/** Custom element selectors used in Playwright locators */
export const Selectors = {
	sidebar: "umb-section-sidebar",
	menuItem: "uui-menu-item",
	workspace: "umb-csp-management-workspace",
	workspaceEditor: "umb-workspace-editor",
	dashboard: "umb-csp-section-dashboard",
	importModal: "umb-csp-import-modal",
	addNodePolicyModal: "umb-csp-add-node-policy-modal",
	dialogLayout: "uui-dialog-layout",
	tab: "uui-tab",
} as const;

/** Workspace view tab labels */
export const WorkspaceTabs = {
	sources: "Sources",
	settings: "Settings",
	evaluate: "Evaluate",
	/** Node policies only: what a request for the content gets today. */
	effective: "Effective",
} as const;

/** Dashboard button labels */
export const DashboardButtons = {
	configureFrontend: "Configure Front-end CSP",
	configureBackoffice: "Configure Back-office CSP",
} as const;

/** Entity action labels */
export const EntityActions = {
	import: "Import...",
	addNodePolicy: "Add Node Policy",
} as const;

/**
 * Hostnames seeded on the test site (uSync/v18/Domains). The homepage is a multilingual site with a
 * hostname per culture; Test Pages, below it, has its own hostname (a nested site).
 */
export const TestDomains = {
	enGb: "/en-gb",
	enUs: "/en-us",
	hostname: "csp-test.localhost:44370",
	testPages: "/test-pages",
} as const;

/** The content nodes those hostnames are assigned to (the sites). */
export const TestNodes = {
	homepage: "Homepage",
	testPages: "Test Pages",
} as const;

/**
 * Pages below Test Pages without a hostname of their own (uSync/v18/Content). `path` is below a
 * hostname of the homepage, e.g. `/en-gb/test-pages/with-nonce`.
 */
export const TestPages = {
	withNonce: { name: "With Nonce", contentKey: "bc5af91c-f263-4703-8a26-5c94b44518da", path: "/test-pages/with-nonce" },
	withoutNonce: { name: "Without Nonce", path: "/test-pages/without-nonce" },
} as const;

/** The languages of the test site (uSync/v18/Languages). */
export const TestCultures = {
	enGb: "en-GB",
	enUs: "en-US",
} as const;

/**
 * Well-known CSP strings for import tests.
 * All directives used are in Constants.AllDirectives on the backend.
 */
export const TestCspStrings = {
	/**
	 * Produces 3 distinct sources:
	 *   'self'              → default-src, script-src, style-src
	 *   https://example.com → script-src
	 *   'unsafe-inline'     → style-src
	 */
	threeSourceImport:
		"default-src 'self'; script-src 'self' https://example.com; style-src 'self' 'unsafe-inline';",
} as const;
