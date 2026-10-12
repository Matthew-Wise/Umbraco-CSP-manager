import { expect, type Page } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import { CspApiHelpers, CspDefinitionBuilder, CspTestHelpers, EntityActions, TestDomains, TestNodes, WorkspaceTabs } from "./helpers";
import { CspConstants } from "../src/constants";

const baseUrl = process.env.URL ?? "https://localhost:44370";

// Every test starts with no domain policies and a disabled frontend policy.
// The API calls authenticate with the backoffice cookies, which are refreshed once the backoffice
// has loaded, so open the section first.
test.beforeEach(async ({ page, umbracoUi }) => {
	await new CspTestHelpers(umbracoUi).goToSection();

	const api = new CspApiHelpers(page.request);
	await api.deleteAllDomainPolicies();
	await api.resetDefinition("frontend");
});

test.afterEach(async ({ page }) => {
	const api = new CspApiHelpers(page.request);
	await api.deleteAllDomainPolicies();
	await api.resetDefinition("frontend");
});

// Domain policies belong to content nodes with a hostname (seeded by uSync).
async function contentKeyOf(api: CspApiHelpers, name: string): Promise<string> {
	const node = (await api.getNodes()).find((n) => n.contentName === name);
	expect(node, `test site node ${name} with a hostname (seeded by uSync)`).toBeDefined();
	return node!.contentKey;
}

const headerOn = async (page: Page, path: string) =>
	(await page.request.get(`${baseUrl}${path}/`, { ignoreHTTPSErrors: true })).headers()["content-security-policy"];

test.describe("Domain policies: tree and add action", () => {
	test("Add Domain Policy is offered on Frontend only", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);

		await csp.openEntityActionFromMenu(CspConstants.policyTypes.frontend.label, EntityActions.addDomainPolicy);
		await expect(csp.addDomainPolicyModal().locator("umb-body-layout")).toBeVisible();
		await csp.addDomainPolicyModal().getByRole("button", { name: "Cancel" }).click();
		await expect(csp.addDomainPolicyModal()).toHaveCount(0);

		const backOffice = csp.treeItem(CspConstants.policyTypes.backoffice.label);
		await backOffice.hover();
		await expect(backOffice.getByRole("button", { name: EntityActions.addDomainPolicy })).toHaveCount(0);
	});

	test("The modal lists only nodes without a policy, with all of their hostnames", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.createDomainPolicy(CspDefinitionBuilder.domainPolicy(await contentKeyOf(api, TestNodes.testPages)).build());
		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.reloadPage();

		await csp.openEntityActionFromMenu(CspConstants.policyTypes.frontend.label, EntityActions.addDomainPolicy);

		// The multilingual homepage is one entry, listing a hostname per culture.
		const modal = csp.addDomainPolicyModal();
		const homepage = modal.locator(`uui-ref-node[name="${TestNodes.homepage}"]`);
		await expect(homepage).toBeVisible();
		for (const hostname of [TestDomains.enGb, TestDomains.enUs, TestDomains.hostname]) {
			await expect(homepage).toHaveAttribute("detail", new RegExp(hostname));
		}
		await expect(modal.locator(`uui-ref-node[name="${TestNodes.testPages}"]`)).toHaveCount(0);
	});
});

test.describe("Domain policies: create, edit, delete", () => {
	test("Creating from the tree opens a draft that is only saved on Save", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const csp = new CspTestHelpers(umbracoUi);

		await csp.openEntityActionFromMenu(CspConstants.policyTypes.frontend.label, EntityActions.addDomainPolicy);
		await csp.addDomainPolicyModal().locator(`uui-ref-node[name="${TestNodes.testPages}"]`).click();

		// A draft: the create route, the draft notice, and nothing persisted yet.
		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/create\//);
		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${TestNodes.testPages} CSP Management`);
		// The hostnames the policy will cover and the link to the content node; no orphan notice.
		const domainInfo = umbracoUi.page.locator('[data-mark="csp-domain-info"]');
		await expect(domainInfo).toHaveAttribute("headline", "Linked content");
		await expect(domainInfo.locator('[data-mark="csp-domain-hostnames"] li')).toHaveText([TestDomains.testPages]);
		await expect(domainInfo.getByRole("link", { name: "Open content item" })).toBeVisible();
		await expect(domainInfo.locator("p")).toHaveCount(0);
		expect(await api.getDomainPolicies()).toHaveLength(0);

		await csp.workspace().getByRole("button", { name: "Save", exact: true }).click();

		// Saved: the server assigned the id, the route switched to edit, the tree shows the child.
		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/edit\/[0-9a-f-]{36}/);
		const policies = await api.getDomainPolicies();
		expect(policies).toHaveLength(1);
		expect(policies[0].contentName).toBe(TestNodes.testPages);
		expect(umbracoUi.page.url()).toContain(policies[0].id);
		await csp.expandTreeItem(CspConstants.policyTypes.frontend.label);
		// A copy of the (disabled) Frontend policy, but switched on: creating it means it should apply.
		await expect(csp.treeItemByLabel(TestNodes.testPages)).toBeVisible();
		expect(policies[0].enabled).toBe(true);
		await expect(csp.workspace().getByRole("button", { name: "Delete domain policy", exact: true })).toBeVisible();
	});

	test("Editing a domain policy changes the header on that node only", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(
			CspDefinitionBuilder.frontend().enabled().withSource("global.example.com", ["default-src"]).build(),
		);
		const policy = await api.createDomainPolicy(
			CspDefinitionBuilder.domainPolicy(await contentKeyOf(api, TestNodes.testPages))
				.enabled()
				.withSource("'self'", ["default-src"])
				.build(),
		);
		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.reloadPage();

		// Open it from the tree (child of Frontend) and add a source through the UI.
		await csp.expandTreeItem(CspConstants.policyTypes.frontend.label);
		await csp.treeItemByLabel(TestNodes.testPages).click();
		await expect(umbracoUi.page).toHaveURL(new RegExp(policy.id));
		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${TestNodes.testPages} CSP Management`);

		await csp.workspace().getByRole("button", { name: "Add Source", exact: true }).click();
		const newSource = csp.workspace().getByLabel("CSP Source").last();
		await newSource.fill("node-only.example.com");
		await csp.workspace().locator('uui-checkbox[label="script-src"]').last().click();
		await csp.workspace().getByRole("button", { name: "Save", exact: true }).click();
		await expect(umbracoUi.page.locator("uui-toast-notification").filter({ hasText: "Changes Saved" })).toBeVisible();

		// The nested site gets its own policy; the homepage's hostnames keep the Frontend policy.
		const onNode = await headerOn(page, TestDomains.testPages);
		const elsewhere = await headerOn(page, TestDomains.enGb);
		expect(onNode).toContain("node-only.example.com");
		expect(onNode).not.toContain("global.example.com");
		expect(elsewhere).toContain("global.example.com");
		expect(elsewhere).not.toContain("node-only.example.com");
	});

	// The reason policies belong to nodes: a multilingual site has a hostname per culture, and they
	// must all get the same policy without one policy per hostname.
	test("A node's policy applies to every one of its hostnames", async ({ page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(
			CspDefinitionBuilder.frontend().enabled().withSource("global.example.com", ["default-src"]).build(),
		);
		await api.createDomainPolicy(
			CspDefinitionBuilder.domainPolicy(await contentKeyOf(api, TestNodes.homepage))
				.enabled()
				.withSource("site.example.com", ["default-src"])
				.build(),
		);

		for (const hostname of [TestDomains.enGb, TestDomains.enUs]) {
			const header = await headerOn(page, hostname);
			expect(header, hostname).toContain("site.example.com");
			expect(header, hostname).not.toContain("global.example.com");
		}
		// Test Pages has its own hostname, so it's routed as its own site: no policy there, so Frontend.
		expect(await headerOn(page, TestDomains.testPages)).toContain("global.example.com");
	});

	test("Deleting a domain policy asks for confirmation and falls back to Frontend", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(
			CspDefinitionBuilder.frontend().enabled().withSource("global.example.com", ["default-src"]).build(),
		);
		const policy = await api.createDomainPolicy(
			CspDefinitionBuilder.domainPolicy(await contentKeyOf(api, TestNodes.homepage))
				.enabled()
				.withSource("domain-only.example.com", ["default-src"])
				.build(),
		);
		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.goToBackOffice();
		await umbracoUi.page.goto(`${baseUrl}/umbraco/section/csp-manager/workspace/csp-policy/edit/${policy.id}`);
		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${TestNodes.homepage} CSP Management`);

		// Cancelling the confirmation keeps the policy.
		await csp.workspace().getByRole("button", { name: "Delete domain policy", exact: true }).click();
		await umbracoUi.page.locator("umb-confirm-modal").getByRole("button", { name: "Cancel" }).click();
		expect(await api.getDomainPolicies()).toHaveLength(1);

		await csp.workspace().getByRole("button", { name: "Delete domain policy", exact: true }).click();
		await umbracoUi.page.locator("umb-confirm-modal").getByRole("button", { name: "Delete domain policy", exact: true }).click();

		await expect(umbracoUi.page).toHaveURL(new RegExp(CspConstants.policyTypes.frontend.value));
		expect(await api.getDomainPolicies()).toHaveLength(0);
		await expect(csp.treeItemByLabel(TestNodes.homepage)).toHaveCount(0);

		expect(await headerOn(page, TestDomains.enGb)).toContain("global.example.com");
	});

	// umbConfirmModal renders string content as HTML, so the content name (user content) must reach it
	// as text. The API response is rewritten so the name carries markup.
	test("The delete confirmation shows the content name as text, not HTML", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const policy = await api.createDomainPolicy(
			CspDefinitionBuilder.domainPolicy(await contentKeyOf(api, TestNodes.homepage)).build(),
		);
		const markup = '<img src="x" data-mark="csp-injected" onerror="window.__cspXss = true">';
		await umbracoUi.page.route(`**/umbraco/csp/api/v1/Definitions/${policy.id}`, async (route) => {
			const response = await route.fetch();
			const body = await response.json();
			await route.fulfill({ response, json: { ...body, contentName: markup } });
		});

		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.page.goto(`${baseUrl}/umbraco/section/csp-manager/workspace/csp-policy/edit/${policy.id}`);
		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${markup} CSP Management`);

		await csp.workspace().getByRole("button", { name: "Delete domain policy", exact: true }).click();
		const confirm = umbracoUi.page.locator("umb-confirm-modal");
		await expect(confirm).toContainText(markup);
		await expect(confirm.locator('[data-mark="csp-injected"]')).toHaveCount(0);
		expect(await umbracoUi.page.evaluate(() => (window as unknown as { __cspXss?: boolean }).__cspXss)).toBeUndefined();
		await confirm.getByRole("button", { name: "Cancel" }).click();
	});

	// A draft waits for the Frontend policy and the domain list. A response that arrives after the
	// user has opened another policy must not turn that policy into a draft: the draft counts as
	// unsaved changes, so Save would be enabled and would create a domain policy instead.
	test("Opening another policy while a draft is loading keeps that policy", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const contentKey = await contentKeyOf(api, TestNodes.homepage);

		let releaseDomains!: () => void;
		const domainsReleased = new Promise<void>((resolve) => (releaseDomains = resolve));
		let markDomainsRequested!: () => void;
		const domainsRequested = new Promise<void>((resolve) => (markDomainsRequested = resolve));
		await umbracoUi.page.route("**/umbraco/csp/api/v1/Nodes", async (route) => {
			markDomainsRequested();
			await domainsReleased;
			await route.continue();
		});

		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.page.goto(`${baseUrl}/umbraco/section/csp-manager/workspace/csp-policy/create/${contentKey}`);
		await domainsRequested;

		await csp.treeItemByLabel(CspConstants.policyTypes.frontend.label).click();
		await expect(umbracoUi.page).toHaveURL(new RegExp(`/edit/${CspConstants.policyTypes.frontend.value}$`));
		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${CspConstants.policyTypes.frontend.label} CSP Management`);

		// Let the draft's request finish now that the user has moved on.
		const domainsResponse = umbracoUi.page.waitForResponse("**/umbraco/csp/api/v1/Nodes");
		releaseDomains();
		await domainsResponse;
		// The response has arrived; give the page a moment to apply it before checking nothing changed.
		await umbracoUi.page.waitForTimeout(500);

		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${CspConstants.policyTypes.frontend.label} CSP Management`);
		await expect(umbracoUi.page.locator('[data-mark="csp-domain-info"]')).toHaveCount(0);
		await expect(csp.workspace().getByRole("button", { name: "Save", exact: true })).toBeDisabled();
		expect(await api.getDomainPolicies()).toHaveLength(0);
	});

	// A stale or hand-typed create URL: the node has no hostname (or doesn't exist). Reported on load,
	// not as a failed save.
	test("A create URL for a node without a hostname shows an error instead of a draft", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const csp = new CspTestHelpers(umbracoUi);

		await umbracoUi.page.goto(`${baseUrl}/umbraco/section/csp-manager/workspace/csp-policy/create/${crypto.randomUUID()}`);

		await expect(csp.workspace().getByText("A domain policy cannot be created for this content node.")).toBeVisible();
		await expect(umbracoUi.page.locator('[data-mark="csp-domain-info"]')).toHaveCount(0);
		await expect(csp.workspace().getByRole("button", { name: "Save", exact: true })).toBeDisabled();
		expect(await api.getDomainPolicies()).toHaveLength(0);
	});
});

test.describe("Domain policies: switching back to the Frontend policy", () => {
	test("Turning a domain policy off falls back to Frontend, turning it on overrides again", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(
			CspDefinitionBuilder.frontend().enabled().withSource("global.example.com", ["default-src"]).build(),
		);
		const policy = await api.createDomainPolicy(
			CspDefinitionBuilder.domainPolicy(await contentKeyOf(api, TestNodes.homepage))
				.enabled()
				.withSource("domain-only.example.com", ["default-src"])
				.build(),
		);
		const csp = new CspTestHelpers(umbracoUi);
		const headerOnDomain = () => headerOn(page, TestDomains.enGb);

		await umbracoUi.page.goto(`${baseUrl}/umbraco/section/csp-manager/workspace/csp-policy/edit/${policy.id}`);
		await csp.workspaceTab(WorkspaceTabs.settings).click();
		const summary = csp.workspace().locator('[data-mark="csp-status-summary"]');
		const toggle = csp.workspace().locator('[data-mark="csp-status-toggle"]');
		await expect(summary).toHaveText(`Every hostname of ${TestNodes.homepage} uses this policy.`);
		await expect(toggle).toHaveAttribute("label", "Use Domain Policy");

		// Off: the domain uses the Frontend policy, and the tree marks the policy inactive.
		await toggle.click();
		await expect(summary).toHaveText(`Every hostname of ${TestNodes.homepage} uses the Frontend policy.`);
		await expect(toggle).toHaveAttribute("label", "Use Frontend Policy");
		await csp.workspace().getByRole("button", { name: "Save", exact: true }).click();
		await expect(umbracoUi.page.locator("uui-toast-notification").filter({ hasText: "Changes Saved" })).toBeVisible();
		await csp.expandTreeItem(CspConstants.policyTypes.frontend.label);
		await expect(csp.treeItemByLabel(`${TestNodes.homepage} (inactive)`)).toBeVisible();
		expect(await headerOnDomain()).toContain("global.example.com");
		expect(await headerOnDomain()).not.toContain("domain-only.example.com");

		// On again: the domain policy overrides the Frontend policy.
		await toggle.click();
		await csp.workspace().getByRole("button", { name: "Save", exact: true }).click();
		await expect(csp.treeItemByLabel(TestNodes.homepage)).toBeVisible();
		expect(await headerOnDomain()).toContain("domain-only.example.com");
		expect(await headerOnDomain()).not.toContain("global.example.com");
	});
});
