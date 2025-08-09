import { expect } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import { CspConstants } from "../src/constants";
import { CspManagerTestHelpers } from "./csp-manager-helpers";

test.beforeEach(async ({ umbracoUi }) => {
	await umbracoUi.goToBackOffice();
	await umbracoUi.login.goToSection(CspConstants.sectionLabel);
});

test.describe("CSP Sources Management", () => {
	const workspaceConfigs = [
		{
			name: "Frontend",
			clickButton: CspManagerTestHelpers.clickFrontendCspButton,
			testSourceUrl: "https://example.com",
			testSourceName: "test-source",
			directiveToToggle: "script-src",
		},
		{
			name: "Backoffice",
			clickButton: CspManagerTestHelpers.clickBackofficeCspButton,
			testSourceUrl: "https://umbraco.com",
			testSourceName: "backoffice-source",
			directiveToToggle: "style-src",
		},
	];

	for (const config of workspaceConfigs) {
		test.describe(`${config.name} Sources`, () => {
			test.beforeEach(async ({ umbracoUi }) => {
				await config.clickButton(umbracoUi.page);
			});

			test("Can add a new source", async ({ umbracoUi }) => {
				// Click Add Source button
				await umbracoUi.page
					.locator('uui-button:has-text("Add Source")')
					.click();

				// Verify a new source item appears
				await expect(umbracoUi.page.locator(".source-item")).toBeVisible();

				// Verify the new source has default values
				await expect(umbracoUi.page.locator("text=new-source")).toBeVisible();
				await expect(umbracoUi.page.locator("text=2 directives")).toBeVisible();
			});

			test("Can edit source name", async ({ umbracoUi }) => {
				// Add a source first
				await umbracoUi.page
					.locator('uui-button:has-text("Add Source")')
					.click();

				// Expand the source to edit it
				await umbracoUi.page.locator(".source-header").click();

				// Find the source URL input and edit it
				const sourceInput = umbracoUi.page.locator(
					'uui-input[placeholder*="self"]'
				);
				await sourceInput.fill(config.testSourceUrl);

				// Verify the change is reflected in the header
				await expect(
					umbracoUi.page.locator(`text=${config.testSourceUrl}`)
				).toBeVisible();
			});

			test("Can toggle CSP directives", async ({ umbracoUi }) => {
				// Add a source first
				await umbracoUi.page
					.locator('uui-button:has-text("Add Source")')
					.click();

				// Expand the source
				await umbracoUi.page.locator(".source-header").click();

				// Find and toggle a directive checkbox
				const directiveCheckbox = umbracoUi.page.locator(
					`uui-checkbox:has-text("${config.directiveToToggle}")`
				);
				await directiveCheckbox.click();

				// Verify the directive count changes
				await expect(umbracoUi.page.locator("text=1 directive")).toBeVisible();
			});

			test("Can copy a source", async ({ umbracoUi }) => {
				// Add a source first
				await umbracoUi.page
					.locator('uui-button:has-text("Add Source")')
					.click();

				// Edit the source name to make it identifiable
				await umbracoUi.page.locator(".source-header").click();
				const sourceInput = umbracoUi.page.locator(
					'uui-input[placeholder*="self"]'
				);
				await sourceInput.fill(config.testSourceName);

				// Click the copy button
				await umbracoUi.page.locator('uui-button[label="Copy"]').click();

				// Verify a copy is created with "_copy" suffix
				await expect(
					umbracoUi.page.locator(`text=${config.testSourceName}_copy`)
				).toBeVisible();

				// Verify success notification appears
				await expect(
					umbracoUi.page.locator("text=Source Copied")
				).toBeVisible();
			});

			test("Can delete a source", async ({ umbracoUi }) => {
				// Add a source first
				await umbracoUi.page
					.locator('uui-button:has-text("Add Source")')
					.click();

				// Verify source exists
				await expect(umbracoUi.page.locator(".source-item")).toBeVisible();

				// Click the delete button
				await umbracoUi.page.locator('uui-button[label="Delete"]').click();

				// Handle the confirmation modal
				await umbracoUi.page.locator('uui-button:has-text("Delete")').click();

				// Verify the source is removed
				await expect(umbracoUi.page.locator(".source-item")).not.toBeVisible();

				// Verify success notification appears
				await expect(
					umbracoUi.page.locator("text=Source Deleted")
				).toBeVisible();
			});

			test("Shows empty state when no sources exist", async ({ umbracoUi }) => {
				// Verify empty state message is shown
				await expect(
					umbracoUi.page.locator(
						"text=No sources configured yet. Add a source to get started."
					)
				).toBeVisible();
			});

			// Only test expand/collapse for frontend since it's the same functionality
			if (config.name === "Frontend") {
				test("Can expand and collapse source items", async ({ umbracoUi }) => {
					// Add a source first
					await umbracoUi.page
						.locator('uui-button:has-text("Add Source")')
						.click();

					// Initially collapsed
					await expect(
						umbracoUi.page.locator(".source-content[hidden]")
					).toBeVisible();

					// Click to expand
					await umbracoUi.page.locator(".source-header").click();
					await expect(
						umbracoUi.page.locator(".source-content:not([hidden])")
					).toBeVisible();

					// Click to collapse
					await umbracoUi.page.locator(".source-header").click();
					await expect(
						umbracoUi.page.locator(".source-content[hidden]")
					).toBeVisible();
				});
			}
		});
	}
});
