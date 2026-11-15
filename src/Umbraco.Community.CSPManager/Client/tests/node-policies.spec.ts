import { expect, type Page } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import {
	CspApiHelpers,
	CspDefinitionBuilder,
	CspTestHelpers,
	EntityActions,
	TestCultures,
	TestDomains,
	TestNodes,
	TestPages,
	WorkspaceTabs,
} from "./helpers";
import { CspConstants } from "../src/constants";

const baseUrl = process.env.URL ?? "https://localhost:44370";

// Every test starts with no node policies and a disabled frontend policy.
// The API calls authenticate with the backoffice cookies, which are refreshed once the backoffice
// has loaded, so open the section first.
test.beforeEach(async ({ page, umbracoUi }) => {
	await new CspTestHelpers(umbracoUi).goToSection();

	const api = new CspApiHelpers(page.request);
	await api.deleteAllNodePolicies();
	await api.resetDefinition("frontend");
});

test.afterEach(async ({ page }) => {
	const api = new CspApiHelpers(page.request);
	await api.deleteAllNodePolicies();
	await api.resetDefinition("frontend");
});

// The sites: the content nodes with a hostname (seeded by uSync).
async function contentKeyOf(api: CspApiHelpers, name: string): Promise<string> {
	const node = (await api.getNodes()).find((n) => n.contentName === name);
	expect(node, `test site node ${name} with a hostname (seeded by uSync)`).toBeDefined();
	return node!.contentKey;
}

const headerOn = async (page: Page, path: string) =>
	(await page.request.get(`${baseUrl}${path}/`, { ignoreHTTPSErrors: true })).headers()["content-security-policy"];

// The workspace routes (src/paths.ts, which needs the backoffice's modules, so not imported here).
const workspaceUrl = `${baseUrl}/umbraco/section/csp-manager/workspace/csp-policy`;
const editUrl = (id: string) => `${workspaceUrl}/edit/${id}`;
const createUrl = (contentKey: string, culture: string = CspConstants.nodePolicy.allCulturesSegment, inheritance = "Replace") =>
	`${workspaceUrl}/create/${contentKey}/${culture}/${inheritance}`;

const frontendWith = (source: string) =>
	CspDefinitionBuilder.frontend().enabled().withSource(source, ["default-src"]).build();

test.describe("Node policies: tree and add action", () => {
	test("Add Node Policy is offered on Frontend only", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);

		// Add Node Policy outweighs Import, so it's Frontend's inline action: visible on hover
		// without opening the "View actions for" dropdown.
		const frontendLabel = CspConstants.policyTypes.frontend.label;
		const frontend = csp.treeItemByLabel(frontendLabel);
		await frontend.hover();
		await expect(frontend.getByRole("button", { name: EntityActions.addNodePolicy, exact: true }).first()).toBeVisible();

		const modal = await csp.openAddNodePolicyModal();
		await modal.getByRole("button", { name: "Cancel" }).click();
		await expect(csp.addNodePolicyModal()).toHaveCount(0);

		// Back Office's only action is Import, so Umbraco renders it inline with no "View actions for"
		// dropdown. Waiting for Import proves the actions have rendered; a second action (such as Add
		// Node Policy) would add the dropdown.
		const backOfficeLabel = CspConstants.policyTypes.backoffice.label;
		const backOffice = csp.treeItemByLabel(backOfficeLabel);
		await backOffice.hover();
		await expect(backOffice.getByRole("button", { name: EntityActions.import, exact: true })).toBeVisible();
		await expect(backOffice.getByRole("button", { name: `View actions for '${backOfficeLabel}'` })).toHaveCount(0);
		await expect(backOffice.getByRole("button", { name: EntityActions.addNodePolicy })).toHaveCount(0);
	});

	test("The modal offers the sites with their hostnames, and one policy per content and culture", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const existing = await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(await contentKeyOf(api, TestNodes.testPages)).build(),
		);
		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.reloadPage();

		const modal = await csp.openAddNodePolicyModal();
		const continueButton = modal.locator('[data-mark="csp-add-node-policy-continue"]');
		await expect(continueButton).toHaveAttribute("disabled");

		// The multilingual homepage is one entry, listing a hostname per culture.
		const homepage = modal.locator(`uui-ref-node[name="${TestNodes.homepage}"]`);
		await expect(homepage).toBeVisible();
		for (const hostname of [TestDomains.enGb, TestDomains.enUs, TestDomains.hostname]) {
			await expect(homepage).toHaveAttribute("detail", new RegExp(hostname));
		}

		// Test Pages already has a policy for all cultures: it can't get a second one, but it can
		// get one for a single culture.
		await modal.locator(`uui-ref-node[name="${TestNodes.testPages}"]`).click();
		const exists = modal.locator('[data-mark="csp-node-policy-exists"]');
		await expect(exists).toContainText("already has a policy for All cultures");
		await expect(continueButton).toHaveAttribute("disabled");

		await csp.addNodePolicyCulture().selectOption(TestCultures.enGb);
		await expect(exists).toHaveCount(0);
		await expect(continueButton).not.toHaveAttribute("disabled");

		// Back to all cultures: the existing policy is one click away.
		await csp.addNodePolicyCulture().selectOption({ label: "All cultures" });
		await exists.getByRole("button", { name: "Open that policy" }).click();
		await expect(umbracoUi.page).toHaveURL(new RegExp(`/edit/${existing.id}`));
		await expect(csp.addNodePolicyModal()).toHaveCount(0);
	});
});

test.describe("Node policies: create, edit, delete", () => {
	test("Creating from the tree opens a draft that is only saved on Save", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const csp = new CspTestHelpers(umbracoUi);

		await csp.createDraftForSite(TestNodes.testPages);

		// A draft: the create route and nothing persisted yet.
		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${TestNodes.testPages} CSP Management`);
		// What the policy is for, its hostnames and the link to the content node; no orphan notice.
		const nodeInfo = umbracoUi.page.locator('[data-mark="csp-node-info"]');
		await expect(nodeInfo).toHaveAttribute("headline", "Linked content");
		await expect(nodeInfo.locator('[data-mark="csp-node-culture"]')).toHaveText("All cultures");
		await expect(nodeInfo.locator('[data-mark="csp-node-inheritance"]')).toHaveText("Complete policy");
		await expect(nodeInfo.locator('[data-mark="csp-node-applies-to"]')).toHaveText("This content and the pages below it");
		await expect(nodeInfo.locator('[data-mark="csp-node-hostnames"] li')).toHaveText([TestDomains.testPages]);
		await expect(nodeInfo.getByRole("link", { name: "Open content item" })).toBeVisible();
		await expect(nodeInfo.locator('[data-mark="csp-node-orphaned"]')).toHaveCount(0);
		expect(await api.getNodePolicies()).toHaveLength(0);

		await csp.save();

		// Saved: the server assigned the id, the route switched to edit, the tree shows the child.
		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/edit\/[0-9a-f-]{36}/);
		const policies = await api.getNodePolicies();
		expect(policies).toHaveLength(1);
		expect(policies[0].contentName).toBe(TestNodes.testPages);
		expect(umbracoUi.page.url()).toContain(policies[0].id);
		await csp.expandTreeItem(CspConstants.policyTypes.frontend.label);
		// A copy of the (disabled) Frontend policy, but switched on: creating it means it should apply.
		await expect(csp.treeItemByLabel(TestNodes.testPages)).toBeVisible();
		expect(policies[0].enabled).toBe(true);
		await expect(csp.workspace().getByRole("button", { name: "Delete node policy", exact: true })).toBeVisible();
	});

	// A complete policy must not change what the content gets until it is edited: it starts from
	// the policy the content gets today, which can be one further up the tree, not the Frontend one.
	test("A complete policy starts as a copy of what the content gets today", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(frontendWith("global.example.com"));
		await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(await contentKeyOf(api, TestNodes.homepage))
				.enabled()
				.reportOnly()
				.withSource("site.example.com", ["default-src"])
				.build(),
		);
		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.reloadPage();

		await csp.createDraftForSite(TestNodes.testPages);
		await expect(csp.workspace().getByLabel("CSP Source")).toHaveValue("site.example.com");
		await csp.save();
		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/edit\/[0-9a-f-]{36}/);

		const created = (await api.getNodePolicies()).find((p) => p.contentName === TestNodes.testPages)!;
		const saved = await api.getDefinitionById(created.id);
		expect(saved.reportOnly).toBe(true);
		expect(saved.sources.map((s) => s.source)).toEqual(["site.example.com"]);
	});

	// Any page can have a policy, not only the sites, and for one culture. An inherit-and-add
	// policy holds only the extra sources: the page keeps the policy it inherits.
	test("A page picked from the content tree gets an inherit-and-add policy for one culture", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(frontendWith("global.example.com"));
		const csp = new CspTestHelpers(umbracoUi);
		const withNonce = TestPages.withNonce;

		const modal = await csp.openAddNodePolicyModal();
		await modal.locator('[data-mark="csp-node-picker"]').getByRole("button", { name: "Choose" }).click();
		const picker = umbracoUi.page.locator("umb-tree-picker-modal");
		await picker.getByRole("button", { name: `Expand child items for ${TestNodes.homepage}` }).click();
		await picker.getByRole("button", { name: `Expand child items for ${TestNodes.testPages}` }).click();
		await picker.locator(`uui-menu-item[label="${withNonce.name}"]`).click();
		await picker.getByRole("button", { name: "Choose" }).click();
		await csp.continueToDraft({ culture: TestCultures.enGb, extending: true });

		await expect(umbracoUi.page).toHaveURL(new RegExp(`/create/${withNonce.contentKey}/${TestCultures.enGb}/Extend`));
		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${withNonce.name} (${TestCultures.enGb}) CSP Management`);
		const nodeInfo = umbracoUi.page.locator('[data-mark="csp-node-info"]');
		await expect(nodeInfo.locator('[data-mark="csp-node-culture"]')).toHaveText(TestCultures.enGb);
		await expect(nodeInfo.locator('[data-mark="csp-node-inheritance"]')).toHaveText("Inherit and add");
		// The page has no hostname of its own.
		await expect(nodeInfo.locator('[data-mark="csp-node-hostnames"]')).toHaveCount(0);
		// Only extra sources: nothing copied, and no settings that belong to the inherited policy.
		await expect(umbracoUi.page.locator('[data-mark="csp-sources-intro"]')).toContainText("Only the extra sources");
		await expect(csp.workspace().getByLabel("CSP Source")).toHaveCount(0);
		await expect(csp.workspace().locator('uui-box[headline="Directives"]')).toHaveCount(0);

		await csp.workspace().getByRole("button", { name: "Add Source", exact: true }).click();
		await csp.workspace().getByLabel("CSP Source").last().fill("extra.example.com");
		await csp.workspace().locator('uui-checkbox[label="default-src"]').last().click();

		// Evaluate checks what the page gets from the saved policies, and says the new source isn't in it yet.
		await csp.workspaceTab(WorkspaceTabs.evaluate).click();
		await expect(umbracoUi.page.locator('[data-mark="csp-evaluates-effective"]')).toBeVisible();
		await expect(umbracoUi.page.locator('[data-mark="csp-evaluate-unsaved"]')).toContainText("not saved yet");

		// The policy type is chosen when the policy is created; what it applies to can still change.
		await csp.workspaceTab(WorkspaceTabs.settings).click();
		await expect(umbracoUi.page.locator('[data-mark="csp-extend-settings-info"]')).toBeVisible();
		await expect(csp.workspace().locator('uui-toggle[label="Report Only Mode"]')).toHaveCount(0);
		await expect(csp.workspace().locator('[data-mark="csp-applies-to"]')).toBeVisible();
		await expect(csp.workspace().locator('[data-mark="csp-inheritance"]')).toHaveCount(0);

		await csp.save();
		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/edit\/[0-9a-f-]{36}/);
		await csp.expandTreeItem(CspConstants.policyTypes.frontend.label);
		await expect(csp.treeItemByLabel(`${withNonce.name} (${TestCultures.enGb})`)).toBeVisible();

		// The page, in that culture: the inherited policy and the extra source.
		const onPage = await headerOn(page, TestDomains.enGb + withNonce.path);
		expect(onPage).toContain("global.example.com");
		expect(onPage).toContain("extra.example.com");
		// The same page in the other culture, and the page next to it, are untouched.
		for (const path of [TestDomains.enUs + withNonce.path, TestDomains.enGb + TestPages.withoutNonce.path]) {
			const header = await headerOn(page, path);
			expect(header, path).toContain("global.example.com");
			expect(header, path).not.toContain("extra.example.com");
		}
	});

	test("Editing a node policy changes the header on that node only", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(frontendWith("global.example.com"));
		const policy = await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(await contentKeyOf(api, TestNodes.testPages))
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
		await csp.saveAndExpectSaved();

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
		await api.saveDefinition(frontendWith("global.example.com"));
		await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(await contentKeyOf(api, TestNodes.homepage))
				.enabled()
				.withSource("site.example.com", ["default-src"])
				.build(),
		);

		for (const hostname of [TestDomains.enGb, TestDomains.enUs]) {
			const header = await headerOn(page, hostname);
			expect(header, hostname).toContain("site.example.com");
			expect(header, hostname).not.toContain("global.example.com");
		}
		// Test Pages is below the homepage and has no policy of its own, so it gets the homepage's
		// policy, even though it has its own hostname.
		const testPagesHeader = await headerOn(page, TestDomains.testPages);
		expect(testPagesHeader).toContain("site.example.com");
		expect(testPagesHeader).not.toContain("global.example.com");
	});

	test("A policy for one culture wins over the all-cultures policy, on that culture only", async ({ page }) => {
		const api = new CspApiHelpers(page.request);
		const homepage = await contentKeyOf(api, TestNodes.homepage);
		await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(homepage).enabled().withSource("site.example.com", ["default-src"]).build(),
		);
		await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(homepage, TestCultures.enGb)
				.enabled()
				.withSource("gb.example.com", ["default-src"])
				.build(),
		);

		const enGb = await headerOn(page, TestDomains.enGb);
		expect(enGb).toContain("gb.example.com");
		expect(enGb).not.toContain("site.example.com");
		const enUs = await headerOn(page, TestDomains.enUs);
		expect(enUs).toContain("site.example.com");
		expect(enUs).not.toContain("gb.example.com");
	});

	test("A policy for this content only doesn't reach the pages below it", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(frontendWith("global.example.com"));
		const policy = await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(await contentKeyOf(api, TestNodes.testPages))
				.enabled()
				.withSource("section.example.com", ["default-src"])
				.build(),
		);
		const below = TestDomains.enGb + TestPages.withNonce.path;
		expect(await headerOn(page, below)).toContain("section.example.com");

		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.page.goto(editUrl(policy.id));
		await csp.workspaceTab(WorkspaceTabs.settings).click();
		const summary = csp.workspace().locator('[data-mark="csp-status-summary"]');
		await expect(summary).toHaveText(`This policy applies to ${TestNodes.testPages} and the pages below it.`);
		await csp.workspace().locator('[data-mark="csp-applies-to"] uui-radio[value="NodeOnly"]').click();
		await expect(summary).toHaveText(`This policy applies to ${TestNodes.testPages}.`);
		await csp.saveAndExpectSaved();

		expect(await headerOn(page, TestDomains.enGb + TestDomains.testPages)).toContain("section.example.com");
		const belowAfter = await headerOn(page, below);
		expect(belowAfter).toContain("global.example.com");
		expect(belowAfter).not.toContain("section.example.com");
	});

	test("Deleting a node policy asks for confirmation and falls back to Frontend", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(frontendWith("global.example.com"));
		const policy = await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(await contentKeyOf(api, TestNodes.homepage))
				.enabled()
				.withSource("domain-only.example.com", ["default-src"])
				.build(),
		);
		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.goToBackOffice();
		await umbracoUi.page.goto(editUrl(policy.id));
		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${TestNodes.homepage} CSP Management`);

		// Cancelling the confirmation keeps the policy.
		await csp.workspace().getByRole("button", { name: "Delete node policy", exact: true }).click();
		await umbracoUi.page.locator("umb-confirm-modal").getByRole("button", { name: "Cancel" }).click();
		expect(await api.getNodePolicies()).toHaveLength(1);

		await csp.workspace().getByRole("button", { name: "Delete node policy", exact: true }).click();
		await umbracoUi.page.locator("umb-confirm-modal").getByRole("button", { name: "Delete node policy", exact: true }).click();

		await expect(umbracoUi.page).toHaveURL(new RegExp(CspConstants.policyTypes.frontend.value));
		expect(await api.getNodePolicies()).toHaveLength(0);
		await expect(csp.treeItemByLabel(TestNodes.homepage)).toHaveCount(0);

		expect(await headerOn(page, TestDomains.enGb)).toContain("global.example.com");
	});

	// umbConfirmModal renders string content as HTML, so the content name (user content) must reach it
	// as text. The API response is rewritten so the name carries markup.
	test("The delete confirmation shows the content name as text, not HTML", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const policy = await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(await contentKeyOf(api, TestNodes.homepage)).build(),
		);
		const markup = '<img src="x" data-mark="csp-injected" onerror="window.__cspXss = true">';
		await umbracoUi.page.route(`**/umbraco/csp/api/v1/Definitions/${policy.id}`, async (route) => {
			const response = await route.fetch();
			const body = await response.json();
			await route.fulfill({ response, json: { ...body, contentName: markup } });
		});

		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.page.goto(editUrl(policy.id));
		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${markup} CSP Management`);

		await csp.workspace().getByRole("button", { name: "Delete node policy", exact: true }).click();
		const confirm = umbracoUi.page.locator("umb-confirm-modal");
		await expect(confirm).toContainText(markup);
		await expect(confirm.locator('[data-mark="csp-injected"]')).toHaveCount(0);
		expect(await umbracoUi.page.evaluate(() => (window as unknown as { __cspXss?: boolean }).__cspXss)).toBeUndefined();
		await confirm.getByRole("button", { name: "Cancel" }).click();
	});

	// A draft is built by the server. A response that arrives after the
	// user has opened another policy must not turn that policy into a draft: the draft counts as
	// unsaved changes, so Save would be enabled and would create a node policy instead.
	test("Opening another policy while a draft is loading keeps that policy", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const contentKey = await contentKeyOf(api, TestNodes.homepage);

		const draftRequest = "**/umbraco/csp/api/v1/Definitions/draft?*";
		let releaseDraft!: () => void;
		const draftReleased = new Promise<void>((resolve) => (releaseDraft = resolve));
		let markDraftRequested!: () => void;
		const draftRequested = new Promise<void>((resolve) => (markDraftRequested = resolve));
		await umbracoUi.page.route(draftRequest, async (route) => {
			markDraftRequested();
			await draftReleased;
			await route.continue();
		});

		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.page.goto(createUrl(contentKey));
		await draftRequested;

		await csp.treeItemByLabel(CspConstants.policyTypes.frontend.label).click();
		await expect(umbracoUi.page).toHaveURL(new RegExp(`/edit/${CspConstants.policyTypes.frontend.value}$`));
		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${CspConstants.policyTypes.frontend.label} CSP Management`);

		// Let the draft's request finish now that the user has moved on. Wait until the page has
		// received the whole body, then let it run the task that hands the body to the workspace,
		// so the checks below see the result of applying it rather than the state before it.
		const draftResponse = umbracoUi.page.waitForResponse(draftRequest);
		releaseDraft();
		await (await draftResponse).finished();
		await umbracoUi.page.evaluate(() => new Promise<void>((resolve) => setTimeout(resolve, 0)));

		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${CspConstants.policyTypes.frontend.label} CSP Management`);
		await expect(umbracoUi.page.locator('[data-mark="csp-node-info"]')).toHaveCount(0);
		await expect(csp.workspace().getByRole("button", { name: "Save", exact: true })).toBeDisabled();
		expect(await api.getNodePolicies()).toHaveLength(0);
	});

	// A stale or hand-typed create URL: the content doesn't exist. Reported on load, not as a failed save.
	test("A create URL for content that doesn't exist shows an error instead of a draft", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const csp = new CspTestHelpers(umbracoUi);

		await umbracoUi.page.goto(createUrl(crypto.randomUUID()));

		const unavailable = "A node policy cannot be created here.";
		await expect(csp.workspace().getByText(unavailable)).toBeVisible();
		// The same for a culture that isn't one of the site's languages.
		await umbracoUi.page.goto(createUrl(await contentKeyOf(api, TestNodes.homepage), "nl-NL"));
		await expect(csp.workspace().getByText(unavailable)).toBeVisible();
		await expect(umbracoUi.page.locator('[data-mark="csp-node-info"]')).toHaveCount(0);
		// Every view reports the same error, not a generic "no definition" message.
		for (const [tab, view] of [
			[WorkspaceTabs.settings, "umb-csp-settings-view"],
			[WorkspaceTabs.effective, "umb-csp-effective-view"],
		]) {
			await csp.workspaceTab(tab).click();
			await expect(umbracoUi.page.locator(view)).toBeVisible();
			await expect(csp.workspace().getByText(unavailable)).toBeVisible();
		}
		await expect(csp.workspace().getByRole("button", { name: "Save", exact: true })).toBeDisabled();
		expect(await api.getNodePolicies()).toHaveLength(0);
	});

	// "Cannot be created here" is for a draft the server refuses. A request that fails for another
	// reason must not send the editor looking for a problem with the content.
	test("A draft that fails to load reports the failure, not a problem with the content", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const contentKey = await contentKeyOf(api, TestNodes.homepage);
		await umbracoUi.page.route("**/umbraco/csp/api/v1/Definitions/draft?*", (route) =>
			route.fulfill({ status: 500, json: { title: "The server is having a bad day", status: 500 } }),
		);
		const csp = new CspTestHelpers(umbracoUi);

		await umbracoUi.page.goto(createUrl(contentKey));

		await expect(csp.workspace().getByLabel("CSP Source")).toHaveCount(0);
		await expect(csp.workspace().getByRole("button", { name: "Save", exact: true })).toBeDisabled();
		await expect(csp.workspace().getByText("A node policy cannot be created here.")).toHaveCount(0);
		await expect(csp.workspace().getByText("The new policy could not be loaded.")).toBeVisible();
	});

	// The same for a create URL of content that already has a policy for that culture: there can
	// only be one, and Save would be refused.
	test("A create URL for content that already has that policy shows an error instead of a draft", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const contentKey = await contentKeyOf(api, TestNodes.homepage);
		await api.createNodePolicy(CspDefinitionBuilder.nodePolicy(contentKey, TestCultures.enGb).build());
		const csp = new CspTestHelpers(umbracoUi);

		await umbracoUi.page.goto(createUrl(contentKey, TestCultures.enGb));
		await expect(csp.workspace().getByText("A node policy cannot be created here.")).toBeVisible();
		await expect(csp.workspace().getByRole("button", { name: "Save", exact: true })).toBeDisabled();

		// Another culture of the same content is free.
		await umbracoUi.page.goto(createUrl(contentKey, TestCultures.enUs));
		await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${TestNodes.homepage} (${TestCultures.enUs}) CSP Management`);
	});

	// A policy whose content or language is gone is kept but applies to nothing. The API responses
	// are rewritten, so no content has to be deleted from the test site.
	test("An orphaned policy is marked as not in use", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const policy = await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(await contentKeyOf(api, TestNodes.homepage)).enabled().build(),
		);
		await umbracoUi.page.route(`**/umbraco/csp/api/v1/Definitions/${policy.id}`, async (route) => {
			const response = await route.fetch();
			await route.fulfill({ response, json: { ...(await response.json()), isOrphaned: true } });
		});
		await umbracoUi.page.route("**/umbraco/csp/api/v1/Definitions/node-policies", async (route) => {
			const response = await route.fetch();
			const policies = (await response.json()) as Array<{ id: string }>;
			await route.fulfill({ response, json: policies.map((p) => ({ ...p, isOrphaned: true })) });
		});

		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.page.goto(editUrl(policy.id));
		const nodeInfo = umbracoUi.page.locator('[data-mark="csp-node-info"]');
		await expect(nodeInfo.locator('[data-mark="csp-node-orphaned"]')).toContainText("This policy applies to no requests");
		await expect(nodeInfo.locator('[data-mark="csp-node-culture"]')).toHaveCount(0);
		await csp.expandTreeItem(CspConstants.policyTypes.frontend.label);
		await expect(csp.treeItemByLabel(`${TestNodes.homepage} (not in use)`)).toBeVisible();

		// No request gets it, so there is no effective policy to show.
		await csp.workspaceTab(WorkspaceTabs.effective).click();
		await expect(umbracoUi.page.locator('[data-mark="csp-effective-orphaned"]')).toContainText("This policy applies to no requests");
		await expect(umbracoUi.page.locator('[data-mark="csp-effective-from"]')).toHaveCount(0);
	});

	// The link is a route of Umbraco's content section, so follow it to see that it still opens the content.
	test("Open content item opens the content of the policy", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const contentKey = await contentKeyOf(api, TestNodes.homepage);
		const policy = await api.createNodePolicy(CspDefinitionBuilder.nodePolicy(contentKey).build());

		await umbracoUi.page.goto(editUrl(policy.id));
		await umbracoUi.page.locator('[data-mark="csp-node-info"]').getByRole("link", { name: "Open content item" }).click();

		await expect(umbracoUi.page).toHaveURL(new RegExp(`/section/content/workspace/document/edit/${contentKey}`));
		await expect(umbracoUi.page.locator("umb-document-workspace-editor")).toBeVisible();
		await expect(umbracoUi.page.locator("umb-document-workspace-editor #name-input input")).toHaveValue(TestNodes.homepage);
	});
});

test.describe("Node policies: what a request gets", () => {
	test("The Effective view shows the header, where it comes from, and follows the culture", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(frontendWith("global.example.com"));
		const homepage = await contentKeyOf(api, TestNodes.homepage);
		const gbPolicy = await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(homepage, TestCultures.enGb)
				.enabled()
				.withSource("gb.example.com", ["default-src"])
				.build(),
		);
		const extraPolicy = await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(TestPages.withNonce.contentKey)
				.enabled()
				.extending()
				.withSource("extra.example.com", ["default-src"])
				.build(),
		);
		const csp = new CspTestHelpers(umbracoUi);
		const from = umbracoUi.page.locator('[data-mark="csp-effective-from"]');
		const header = umbracoUi.page.locator('[data-mark="csp-effective-header"]');
		const culture = umbracoUi.page.locator('[data-mark="csp-effective-culture"] select');

		// The homepage's own policy, for its culture. Nothing to open: it is this policy.
		await umbracoUi.page.goto(editUrl(gbPolicy.id));
		await csp.workspaceTab(WorkspaceTabs.effective).click();
		await expect(from).toContainText(`The policy of ${TestNodes.homepage} (${TestCultures.enGb})`);
		await expect(from.locator("uui-button")).toHaveCount(0);
		await expect(header).toContainText("Content-Security-Policy:");
		await expect(header).toContainText("default-src gb.example.com");
		await expect(umbracoUi.page.locator('[data-mark="csp-effective-directives"] uui-table-row')).toHaveCount(1);

		// In the other culture the homepage has no policy, so it gets the Frontend policy.
		await culture.selectOption(TestCultures.enUs);
		await expect(from).toContainText("The Frontend policy");
		await expect(header).toContainText("default-src global.example.com");
		await expect(from.locator("uui-button")).toHaveAttribute("href", new RegExp(CspConstants.policyTypes.frontend.value));

		// An inherit-and-add policy: its sources on top of what the page inherits, per culture.
		await umbracoUi.page.goto(editUrl(extraPolicy.id));
		await csp.workspaceTab(WorkspaceTabs.effective).click();
		await expect(from).toContainText(`The policy of ${TestPages.withNonce.name}, added to the policy it inherits`);
		await expect(header).toContainText("global.example.com");
		await expect(header).toContainText("extra.example.com");
		await culture.selectOption(TestCultures.enGb);
		await expect(header).toContainText("gb.example.com");
		await expect(header).toContainText("extra.example.com");
		await expect(header).not.toContainText("global.example.com");

		// Evaluate checks what the page gets, since the extra sources are not a policy on their own.
		await csp.workspaceTab(WorkspaceTabs.evaluate).click();
		await expect(umbracoUi.page.locator('[data-mark="csp-evaluates-effective"]')).toBeVisible();
		await expect(umbracoUi.page.locator("umb-csp-evaluate-view")).toContainText("extra.example.com");
	});

	test("A draft shows what the content gets without it, and the global policies have no Effective view", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(frontendWith("global.example.com"));
		const sitePolicy = await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(await contentKeyOf(api, TestNodes.homepage))
				.enabled()
				.withSource("site.example.com", ["default-src"])
				.build(),
		);
		const csp = new CspTestHelpers(umbracoUi);
		await umbracoUi.reloadPage();

		await csp.navigateToWorkspace("frontend");
		await expect(csp.workspaceTab(WorkspaceTabs.settings)).toBeVisible();
		await expect(csp.workspaceTab(WorkspaceTabs.effective)).toHaveCount(0);

		await csp.createDraftForSite(TestNodes.testPages);
		await csp.workspaceTab(WorkspaceTabs.effective).click();
		await expect(umbracoUi.page.locator('[data-mark="csp-effective-note"]')).toContainText("not saved yet");
		const from = umbracoUi.page.locator('[data-mark="csp-effective-from"]');
		await expect(from).toContainText(`The policy of ${TestNodes.homepage}`);
		await expect(from.locator("uui-button")).toHaveAttribute("href", new RegExp(sitePolicy.id));
		await expect(umbracoUi.page.locator('[data-mark="csp-effective-header"]')).toContainText("default-src site.example.com");
	});

	test("A policy that is switched off or has no sources sends no header, and the view says so", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const policy = await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(await contentKeyOf(api, TestNodes.homepage)).enabled().build(),
		);
		const csp = new CspTestHelpers(umbracoUi);

		await umbracoUi.page.goto(editUrl(policy.id));
		await csp.workspaceTab(WorkspaceTabs.effective).click();
		const noHeader = umbracoUi.page.locator('[data-mark="csp-effective-no-header"]');
		await expect(noHeader).toContainText("has no sources yet");
		await expect(umbracoUi.page.locator('[data-mark="csp-effective-header"]')).toHaveCount(0);
		expect(await headerOn(page, TestDomains.enGb)).toBeUndefined();
	});
});

test.describe("Node policies: switching back to the inherited policy", () => {
	test("Turning a node policy off falls back to Frontend, turning it on overrides again", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		await api.saveDefinition(frontendWith("global.example.com"));
		const policy = await api.createNodePolicy(
			CspDefinitionBuilder.nodePolicy(await contentKeyOf(api, TestNodes.homepage))
				.enabled()
				.withSource("domain-only.example.com", ["default-src"])
				.build(),
		);
		const csp = new CspTestHelpers(umbracoUi);
		const headerOnSite = () => headerOn(page, TestDomains.enGb);

		await umbracoUi.page.goto(editUrl(policy.id));
		await csp.workspaceTab(WorkspaceTabs.settings).click();
		const summary = csp.workspace().locator('[data-mark="csp-status-summary"]');
		const toggle = csp.workspace().locator('[data-mark="csp-status-toggle"]');
		await expect(summary).toHaveText(`This policy applies to ${TestNodes.homepage} and the pages below it.`);
		await expect(toggle).toHaveAttribute("label", "Use this policy");

		// Off: the content gets what it inherits (here the Frontend policy), and the tree marks the policy inactive.
		await toggle.click();
		await expect(summary).toContainText(`Requests for ${TestNodes.homepage} and the pages below it get the inherited policy`);
		await expect(toggle).toHaveAttribute("label", "Use the inherited policy");
		await csp.saveAndExpectSaved();
		await csp.expandTreeItem(CspConstants.policyTypes.frontend.label);
		await expect(csp.treeItemByLabel(`${TestNodes.homepage} (inactive)`)).toBeVisible();
		expect(await headerOnSite()).toContain("global.example.com");
		expect(await headerOnSite()).not.toContain("domain-only.example.com");

		// On again: the node policy overrides the Frontend policy.
		await toggle.click();
		await csp.save();
		await expect(csp.treeItemByLabel(TestNodes.homepage)).toBeVisible();
		expect(await headerOnSite()).toContain("domain-only.example.com");
		expect(await headerOnSite()).not.toContain("global.example.com");
	});
});
