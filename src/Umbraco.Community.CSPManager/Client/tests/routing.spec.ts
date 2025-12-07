import { expect } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import { CspConstants } from "../src/constants";

test.beforeEach(async ({ umbracoUi }) => {
	await umbracoUi.goToBackOffice();
	await umbracoUi.login.goToSection(CspConstants.section.label);
});

test.describe("CSP Manager Tree Navigation", () => {
	test("Tree displays Back Office and Frontend items", async ({ umbracoUi }) => {
		// Wait for the section sidebar to load
		const sidebar = umbracoUi.page.locator("umb-section-sidebar");
		await expect(sidebar).toBeVisible();

		// Check both tree items are visible (tree items render as uui-menu-item)
		await expect(sidebar.locator("uui-menu-item").filter({ hasText: "Back Office" })).toBeVisible();
		await expect(sidebar.locator("uui-menu-item").filter({ hasText: "Frontend" })).toBeVisible();
	});

	test("Back Office tree item navigates to workspace", async ({ umbracoUi }) => {
		const sidebar = umbracoUi.page.locator("umb-section-sidebar");
		await expect(sidebar).toBeVisible();

		// Click the Back Office tree item
		await sidebar.locator("uui-menu-item").filter({ hasText: "Back Office" }).click();

		// Verify URL contains the correct path
		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/edit\/backoffice/);

		// Verify workspace loads
		await expect(umbracoUi.page.locator("umb-csp-management-workspace")).toBeVisible();
	});

	test("Frontend tree item navigates to workspace", async ({ umbracoUi }) => {
		const sidebar = umbracoUi.page.locator("umb-section-sidebar");
		await expect(sidebar).toBeVisible();

		// Click the Frontend tree item
		await sidebar.locator("uui-menu-item").filter({ hasText: "Frontend" }).click();

		// Verify URL contains the correct path
		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/edit\/frontend/);

		// Verify workspace loads
		await expect(umbracoUi.page.locator("umb-csp-management-workspace")).toBeVisible();
	});
});

test.describe("CSP Manager Dashboard Navigation", () => {
	test("Dashboard Front-end button navigates to workspace", async ({ umbracoUi }) => {
		// Wait for dashboard to load
		await expect(umbracoUi.page.locator("umb-csp-section-dashboard")).toBeVisible();

		// Click the Front-end CSP button on dashboard
		await umbracoUi.page.locator("uui-button").filter({ hasText: "Configure Front-end CSP" }).click();

		// Verify URL and workspace
		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/edit\/frontend/);
		await expect(umbracoUi.page.locator("umb-csp-management-workspace")).toBeVisible();
	});

	test("Dashboard Back-office button navigates to workspace", async ({ umbracoUi }) => {
		// Wait for dashboard to load
		await expect(umbracoUi.page.locator("umb-csp-section-dashboard")).toBeVisible();

		// Click the Back-office CSP button on dashboard
		await umbracoUi.page.locator("uui-button").filter({ hasText: "Configure Back-office CSP" }).click();

		// Verify URL and workspace
		await expect(umbracoUi.page).toHaveURL(/\/workspace\/csp-policy\/edit\/backoffice/);
		await expect(umbracoUi.page.locator("umb-csp-management-workspace")).toBeVisible();
	});
});

test.describe("CSP Manager Workspace Views", () => {
	test("Workspace has Sources, Settings, and Evaluate tabs", async ({ umbracoUi }) => {
		// Navigate to a workspace via sidebar
		const sidebar = umbracoUi.page.locator("umb-section-sidebar");
		await expect(sidebar).toBeVisible();
		await sidebar.locator("uui-menu-item").filter({ hasText: "Frontend" }).click();

		// Wait for workspace to load
		const workspace = umbracoUi.page.locator("umb-csp-management-workspace");
		await expect(workspace).toBeVisible();

		// Check workspace view tabs exist (scoped to workspace-editor)
		const workspaceEditor = umbracoUi.page.locator("umb-workspace-editor");
		await expect(workspaceEditor.locator("uui-tab").filter({ hasText: "Sources" })).toBeVisible();
		await expect(workspaceEditor.locator("uui-tab").filter({ hasText: "Settings" })).toBeVisible();
		await expect(workspaceEditor.locator("uui-tab").filter({ hasText: "Evaluate" })).toBeVisible();
	});

	test("Can switch between workspace tabs", async ({ umbracoUi }) => {
		// Navigate to workspace
		const sidebar = umbracoUi.page.locator("umb-section-sidebar");
		await expect(sidebar).toBeVisible();
		await sidebar.locator("uui-menu-item").filter({ hasText: "Back Office" }).click();

		const workspace = umbracoUi.page.locator("umb-csp-management-workspace");
		await expect(workspace).toBeVisible();

		// Scope tabs to workspace editor to avoid matching section tabs
		const workspaceEditor = umbracoUi.page.locator("umb-workspace-editor");

		// Click Settings tab
		await workspaceEditor.locator("uui-tab").filter({ hasText: "Settings" }).click();
		await expect(umbracoUi.page).toHaveURL(/\/settings$/);

		// Click Evaluate tab
		await workspaceEditor.locator("uui-tab").filter({ hasText: "Evaluate" }).click();
		await expect(umbracoUi.page).toHaveURL(/\/evaluate$/);

		// Click Sources tab
		await workspaceEditor.locator("uui-tab").filter({ hasText: "Sources" }).click();
		await expect(umbracoUi.page).toHaveURL(/\/sources$/);
	});
});
