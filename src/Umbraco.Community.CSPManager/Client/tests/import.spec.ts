import { expect } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import { CspTestHelpers, CspApiHelpers, TestCspStrings, EntityActions } from "./helpers";
import { CspConstants } from "../src/constants";

test.beforeEach(async ({ umbracoUi }) => {
	const csp = new CspTestHelpers(umbracoUi);
	await csp.goToSection();
});

test.describe("Import Action Availability", () => {
	test("Import action is shown on the Frontend tree item", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await expect(csp.sidebar()).toBeVisible();

		const frontendItem = csp.treeItem(CspConstants.policyTypes.frontend.label);
		await frontendItem.hover();

		await expect(frontendItem.getByRole("button", { name: EntityActions.import })).toBeVisible();
	});

	test("Import action is shown on the Back Office tree item", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await expect(csp.sidebar()).toBeVisible();

		const backOfficeItem = csp.treeItem(CspConstants.policyTypes.backoffice.label);
		await backOfficeItem.hover();

		await expect(
			backOfficeItem.getByRole("button", { name: EntityActions.import }),
		).toBeVisible();
	});
});

test.describe("Import Modal UI", () => {
	test("Import modal opens with correct headline for Frontend policy", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await csp.openImportModal("frontend");

		await expect(umbracoUi.page.locator("uui-dialog-layout")).toHaveAttribute(
			"headline",
			`Import ${CspConstants.policyTypes.frontend.label} CSP Policy`,
		);
	});

	test("Parse button is disabled when textarea is empty", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await csp.openImportModal("frontend");

		await expect(csp.importModal().getByRole("button", { name: "Parse" })).toBeDisabled();
	});

	test("Import button is disabled before parsing", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await csp.openImportModal("frontend");

		await expect(csp.importModal().getByRole("button", { name: "Import" })).toBeDisabled();
	});

	test("Cancel closes modal without saving", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await csp.openImportModal("frontend");

		await csp.importModal().getByRole("button", { name: "Cancel" }).click();

		await expect(csp.importModal()).not.toBeVisible();
	});

	test("Parsing a valid CSP string shows preview with correct source rows", async ({
		umbracoUi,
	}) => {
		const csp = new CspTestHelpers(umbracoUi);
		await csp.openImportModal("frontend");
		const modal = csp.importModal();

		await modal.locator("uui-textarea").locator("textarea").fill(TestCspStrings.threeSourceImport);
		await modal.getByRole("button", { name: "Parse" }).click();

		// threeSourceImport produces 3 distinct sources: 'self', https://example.com, 'unsafe-inline'
		await expect(modal.locator(".source-row")).toHaveCount(3);
		await expect(modal.locator(".source-name").filter({ hasText: "'self'" })).toBeVisible();
		await expect(
			modal.locator(".source-name").filter({ hasText: "https://example.com" }),
		).toBeVisible();
		await expect(
			modal.locator(".source-name").filter({ hasText: "'unsafe-inline'" }),
		).toBeVisible();
	});

	test("Import button is enabled after parsing a valid CSP with sources", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);
		await csp.openImportModal("frontend");
		const modal = csp.importModal();

		await modal.locator("uui-textarea").locator("textarea").fill(TestCspStrings.threeSourceImport);
		await modal.getByRole("button", { name: "Parse" }).click();

		await expect(modal.getByRole("button", { name: "Import" })).toBeEnabled();
	});
});

test.describe("Import Full Flow", () => {
	test.afterEach(async ({ umbracoApi }) => {
		await new CspApiHelpers(umbracoApi).resetDefinition("frontend");
	});

	test("Importing a CSP policy closes modal and navigates to the workspace", async ({
		umbracoUi,
	}) => {
		const csp = new CspTestHelpers(umbracoUi);
		await csp.openImportModal("frontend");
		const modal = csp.importModal();

		await modal.locator("uui-textarea").locator("textarea").fill(TestCspStrings.threeSourceImport);
		await modal.getByRole("button", { name: "Parse" }).click();
		await modal.getByRole("button", { name: "Import" }).click();

		await expect(modal).not.toBeVisible();
		await expect(csp.workspace()).toBeVisible();
		await expect(umbracoUi.page).toHaveURL(
			new RegExp(CspConstants.policyTypes.frontend.value),
		);
	});
});
