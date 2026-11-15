import { expect } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import { CspApiHelpers, CspDefinitionBuilder, CspTestHelpers, TestNodes, WorkspaceTabs } from "./helpers";

// The API calls authenticate with the backoffice cookies, refreshed once the backoffice has loaded.
test.beforeEach(async ({ page, umbracoUi }) => {
	await new CspTestHelpers(umbracoUi).goToSection();
	const api = new CspApiHelpers(page.request);
	await api.deleteAllNodePolicies();
	await api.saveDefinition(
		CspDefinitionBuilder.frontend().withReporting("report-to", "csp-endpoint").withSource("'self'", ["default-src"]).build(),
	);
});

test.afterEach(async ({ page }) => {
	const api = new CspApiHelpers(page.request);
	await api.deleteAllNodePolicies();
	await api.resetDefinition("frontend");
});

test.describe("Reporting settings", () => {
	test("Switching report-to to No reporting hides the Report URI and saves", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const csp = new CspTestHelpers(umbracoUi);
		await csp.navigateToWorkspace("frontend");
		await csp.workspaceTab(WorkspaceTabs.settings).click();

		const reportUri = csp.workspace().locator('uui-input[label="Report URI"]');
		await expect(reportUri).toBeVisible();

		await csp.workspace().locator('uui-radio[value="none"]').click();
		await expect(reportUri).toHaveCount(0);

		await csp.workspace().getByRole("button", { name: "Save", exact: true }).click();
		await expect(umbracoUi.page.locator("uui-toast-notification").filter({ hasText: "Changes Saved" })).toBeVisible();

		const saved = await api.getDefinition("frontend");
		expect(saved.reportingDirective ?? null).toBeNull();
		expect(saved.reportUri ?? null).toBeNull();
	});

	test("A new node policy can be saved with No reporting", async ({ umbracoUi, page }) => {
		const api = new CspApiHelpers(page.request);
		const csp = new CspTestHelpers(umbracoUi);

		// The draft starts as a copy of the Frontend policy, report-to included.
		await csp.createDraftForSite(TestNodes.homepage);
		await csp.workspaceTab(WorkspaceTabs.settings).click();
		await csp.workspace().locator('uui-radio[value="none"]').click();

		await csp.workspace().getByRole("button", { name: "Save", exact: true }).click();
		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/edit\/[0-9a-f-]{36}/);

		const policies = await api.getNodePolicies();
		expect(policies).toHaveLength(1);
		expect((await api.getDefinitionById(policies[0].id)).reportingDirective ?? null).toBeNull();
	});
});
