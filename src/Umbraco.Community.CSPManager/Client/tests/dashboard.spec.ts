import { expect } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import { CspConstants } from "../src/constants";

test.beforeEach(async ({ umbracoUi }) => {
	await umbracoUi.goToBackOffice();
	await umbracoUi.login.goToSection(CspConstants.section.label);
});

test.describe("CSP Manager Section & Dashboard", () => {
	test("Can access CSP Manager Section", async ({ umbracoUi }) => {
		await expect(
			umbracoUi.page.locator("umb-csp-section-dashboard")
		).toBeVisible();

		await expect(
			umbracoUi.page.locator('uui-box[headline="CSP Manager"]')
		).toBeVisible();
	});
});
