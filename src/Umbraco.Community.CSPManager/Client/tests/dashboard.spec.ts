import { expect } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import { CspTestHelpers } from "./helpers";

test.beforeEach(async ({ umbracoUi }) => {
	const csp = new CspTestHelpers(umbracoUi);
	await csp.goToSection();
});

test.describe("CSP Manager Section & Dashboard", () => {
	test("Can access CSP Manager Section", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);

		await expect(csp.dashboard()).toBeVisible();
		await expect(umbracoUi.page.locator('uui-box[headline="CSP Manager"]')).toBeVisible();
	});
});
