import { expect } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import { CspTestHelpers, WorkspaceTabs } from "./helpers";
import { CspConstants } from "../src/constants";

test.beforeEach(async ({ umbracoUi }) => {
	const csp = new CspTestHelpers(umbracoUi);
	await csp.goToSection();
});

test.describe("CSP Manager Tree Navigation", () => {
	test("Tree displays Back Office and Frontend items", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await expect(csp.sidebar()).toBeVisible();

		await expect(csp.treeItem(CspConstants.policyTypes.backoffice.label)).toBeVisible();
		await expect(csp.treeItem(CspConstants.policyTypes.frontend.label)).toBeVisible();
	});

	test("Back Office tree item navigates to workspace", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await csp.navigateToWorkspace("backoffice");

		await expect(umbracoUi.page).toHaveURL(
			new RegExp(CspConstants.policyTypes.backoffice.value.slice(0, 8)),
		);
		await expect(csp.workspace()).toBeVisible();
	});

	test("Frontend tree item navigates to workspace", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await csp.navigateToWorkspace("frontend");

		await expect(umbracoUi.page).toHaveURL(
			new RegExp(CspConstants.policyTypes.frontend.value.slice(0, 8)),
		);
		await expect(csp.workspace()).toBeVisible();
	});
});

test.describe("CSP Manager Dashboard Navigation", () => {
	test("Dashboard Front-end button navigates to workspace", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await expect(csp.dashboard()).toBeVisible();

		await umbracoUi.page.locator("uui-button").filter({ hasText: "Configure Front-end CSP" }).click();

		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/edit\/frontend/);
		await expect(csp.workspace()).toBeVisible();
	});

	test("Dashboard Back-office button navigates to workspace", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await expect(csp.dashboard()).toBeVisible();

		await umbracoUi.page
			.locator("uui-button")
			.filter({ hasText: "Configure Back-office CSP" })
			.click();

		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/edit\/backoffice/);
		await expect(csp.workspace()).toBeVisible();
	});
});

test.describe("CSP Manager Workspace Views", () => {
	test("Workspace has Sources, Settings, and Evaluate tabs", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await csp.navigateToWorkspace("frontend");

		await expect(csp.workspaceTab(WorkspaceTabs.sources)).toBeVisible();
		await expect(csp.workspaceTab(WorkspaceTabs.settings)).toBeVisible();
		await expect(csp.workspaceTab(WorkspaceTabs.evaluate)).toBeVisible();
	});

	test("Can switch between workspace tabs", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await csp.navigateToWorkspace("backoffice");

		await csp.workspaceTab(WorkspaceTabs.settings).click();
		await expect(umbracoUi.page).toHaveURL(/\/settings$/);

		await csp.workspaceTab(WorkspaceTabs.evaluate).click();
		await expect(umbracoUi.page).toHaveURL(/\/evaluate$/);

		await csp.workspaceTab(WorkspaceTabs.sources).click();
		await expect(umbracoUi.page).toHaveURL(/\/sources$/);
	});
});
