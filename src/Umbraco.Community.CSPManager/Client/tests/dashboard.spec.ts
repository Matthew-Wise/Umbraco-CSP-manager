import { expect } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import { CspConstants } from "../src/constants";

test.beforeEach(async ({ umbracoUi }) => {
	await umbracoUi.goToBackOffice();
	await umbracoUi.login.goToSection(CspConstants.sectionLabel);
});

test.describe("CSP Manager Section & Dashboard", () => {
	test("Can access CSP Manager Section", async ({ umbracoUi }) => {
		// Verify we're on the CSP Manager dashboard
		await expect(
			umbracoUi.page.locator("umb-csp-section-dashboard")
		).toBeVisible();
		await expect(
			umbracoUi.page.locator('uui-box[headline="CSP Manager"]')
		).toBeVisible();
	});

	// Parameterized test for both frontend and backoffice workspace navigation
	test.describe("Workspace Navigation", () => {
		const workspaceConfigs = [
			{
				name: "Frontend",
				workspace: "frontend",
				isBackOffice: false,
				buttonSelector:
					'uui-button[href="/umbraco/section/csp-manager/workspace/frontend"]',
			},
			{
				name: "Backoffice",
				workspace: "backoffice",
				isBackOffice: true,
				buttonSelector:
					'uui-button[href="/umbraco/section/csp-manager/workspace/backoffice"]',
			},
		];

		for (const config of workspaceConfigs) {
			test(`${config.name} CSP button navigates to ${config.name} workspace with isBackOffice=${config.isBackOffice}`, async ({
				umbracoUi,
			}) => {
				// Click the workspace button
				await umbracoUi.page.locator(config.buttonSelector).click();

				// Verify we're in the workspace
				await expect(
					umbracoUi.page.locator("umb-csp-management-workspace")
				).toBeVisible();

				// Verify the URL contains the correct workspace
				await expect(umbracoUi.page).toHaveURL(
					new RegExp(`.*\\/workspace\\/${config.workspace}`)
				);

				// Wait for network request to Definitions endpoint
				const responsePromise = umbracoUi.page.waitForResponse(
					(response) =>
						response.url().includes("/Definitions") &&
						response.url().includes(`isBackOffice=${config.isBackOffice}`)
				);

				// Trigger a page reload or action that would call the API
				await umbracoUi.page.reload();

				// Wait for the response
				const response = await responsePromise;
				expect(response.status()).toBe(200);
			});
		}
	});
});
