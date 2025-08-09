import { expect } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import { CspConstants } from "../src/constants";
import { CspManagerTestHelpers } from "./csp-manager-helpers";

test.beforeEach(async ({ umbracoUi }) => {
	await umbracoUi.goToBackOffice();
	await umbracoUi.login.goToSection(CspConstants.sectionLabel);
});

test.describe("CSP Settings Management", () => {
	const workspaceConfigs = [
		{
			name: "Frontend",
			workspaceTitle: "Front End CSP Management",
			description:
				"Configure the Content Security Policy settings for front-end content",
			clickButton: CspManagerTestHelpers.clickFrontendCspButton,
			testUri: "https://example.com/csp-report",
			reportingOption: "report-to (recommended)",
		},
		{
			name: "Backoffice",
			workspaceTitle: "Back Office CSP Management",
			description:
				"Configure the Content Security Policy settings for back-office content",
			clickButton: CspManagerTestHelpers.clickBackofficeCspButton,
			testUri: "https://umbraco.com/csp-report",
			reportingOption: "report-uri (deprecated)",
		},
	];

	for (const config of workspaceConfigs) {
		test.describe(`${config.name} Settings`, () => {
			test.beforeEach(async ({ umbracoUi }) => {
				await config.clickButton(umbracoUi.page);
				await CspManagerTestHelpers.clickSettingsTab(umbracoUi.page);
			});

			test("Shows correct workspace title", async ({ umbracoUi }) => {
				await expect(
					umbracoUi.page.locator(`text=${config.workspaceTitle}`)
				).toBeVisible();
			});

			test("Shows settings content", async ({ umbracoUi }) => {
				await expect(
					umbracoUi.page.locator('uui-box:has-text("Settings")')
				).toBeVisible();
				await expect(
					umbracoUi.page.locator(`text=${config.description}`)
				).toBeVisible();
			});

			test("Shows CSP status toggle", async ({ umbracoUi }) => {
				await expect(
					umbracoUi.page.locator('uui-label:has-text("CSP Status")')
				).toBeVisible();
				await expect(umbracoUi.page.locator("uui-toggle")).toBeVisible();
			});

			test("Shows report-only mode toggle", async ({ umbracoUi }) => {
				await expect(
					umbracoUi.page.locator('uui-label:has-text("Report Only Mode")')
				).toBeVisible();
				await expect(umbracoUi.page.locator("uui-toggle")).toBeVisible();
			});

			test("Shows reporting directive options", async ({ umbracoUi }) => {
				await expect(
					umbracoUi.page.locator('uui-label:has-text("Reporting Directive")')
				).toBeVisible();
				await expect(
					umbracoUi.page.locator('uui-radio:has-text("No reporting")')
				).toBeVisible();
				await expect(
					umbracoUi.page.locator(
						'uui-radio:has-text("report-to (recommended)")'
					)
				).toBeVisible();
				await expect(
					umbracoUi.page.locator(
						'uui-radio:has-text("report-uri (deprecated)")'
					)
				).toBeVisible();
			});

			test("Shows report URI input field", async ({ umbracoUi }) => {
				await expect(
					umbracoUi.page.locator('uui-label:has-text("Report URI")')
				).toBeVisible();
				await expect(
					umbracoUi.page.locator('uui-input[placeholder*="csp-report"]')
				).toBeVisible();
			});

			test("Can toggle CSP status", async ({ umbracoUi }) => {
				const cspToggle = umbracoUi.page.locator("uui-toggle").first();

				// Get initial state
				const initialChecked = await cspToggle.getAttribute("checked");

				// Toggle the switch
				await cspToggle.click();

				// Verify the state changed
				const newChecked = await cspToggle.getAttribute("checked");
				expect(newChecked).not.toBe(initialChecked);
			});

			test("Can toggle report-only mode", async ({ umbracoUi }) => {
				const reportOnlyToggle = umbracoUi.page.locator("uui-toggle").nth(1);

				// Get initial state
				const initialChecked = await reportOnlyToggle.getAttribute("checked");

				// Toggle the switch
				await reportOnlyToggle.click();

				// Verify the state changed
				const newChecked = await reportOnlyToggle.getAttribute("checked");
				expect(newChecked).not.toBe(initialChecked);
			});

			test("Can change reporting directive", async ({ umbracoUi }) => {
				// Click on the configured reporting option
				await umbracoUi.page
					.locator(`uui-radio:has-text("${config.reportingOption}")`)
					.click();

				// Verify it's selected
				await expect(
					umbracoUi.page.locator(
						`uui-radio[checked]:has-text("${config.reportingOption}")`
					)
				).toBeVisible();
			});

			test("Can enter report URI", async ({ umbracoUi }) => {
				const reportUriInput = umbracoUi.page.locator(
					'uui-input[placeholder*="csp-report"]'
				);

				// Enter a test URI
				await reportUriInput.fill(config.testUri);

				// Verify the value was entered
				await expect(reportUriInput).toHaveValue(config.testUri);
			});

			test("Shows help information", async ({ umbracoUi }) => {
				await expect(
					umbracoUi.page.locator("text=About Content Security Policy")
				).toBeVisible();
				await expect(
					umbracoUi.page.locator(
						"text=Content Security Policy (CSP) helps prevent cross-site scripting (XSS)"
					)
				).toBeVisible();
				await expect(
					umbracoUi.page.locator("text=Best Practices:")
				).toBeVisible();
			});

			test("Shows status summary", async ({ umbracoUi }) => {
				await expect(umbracoUi.page.locator(".status-summary")).toBeVisible();
				await expect(
					umbracoUi.page.locator("text=CSP is currently")
				).toBeVisible();
			});
		});
	}
});
