import { expect, type Page } from "@playwright/test";

export class CspManagerTestHelpers {
	// URL constants
	static readonly BASE_URL = "/umbraco/section/csp-manager";
	static readonly FRONTEND_WORKSPACE_URL =
		"/umbraco/section/csp-manager/workspace/frontend";
	static readonly BACKOFFICE_WORKSPACE_URL =
		"/umbraco/section/csp-manager/workspace/backoffice";
	static readonly FRONTEND_SETTINGS_URL =
		"/umbraco/section/csp-manager/workspace/frontend/settings";
	static readonly BACKOFFICE_SETTINGS_URL =
		"/umbraco/section/csp-manager/workspace/backoffice/settings";

	// Selector constants
	static readonly DASHBOARD_SELECTOR = "umb-csp-section-dashboard";
	static readonly WORKSPACE_SELECTOR = "umb-csp-management-workspace";
	static readonly DEFAULT_VIEW_SELECTOR = "umb-csp-default-view";
	static readonly SETTINGS_VIEW_SELECTOR = "umb-csp-settings-view";
	static readonly FRONTEND_BUTTON_SELECTOR =
		'uui-button[href="/umbraco/section/csp-manager/workspace/frontend"]';
	static readonly BACKOFFICE_BUTTON_SELECTOR =
		'uui-button[href="/umbraco/section/csp-manager/workspace/backoffice"]';
	static readonly SAVE_BUTTON_SELECTOR =
		'uui-button[look="primary"][color="positive"]';
	static readonly ACTIVE_TAB_SELECTOR = 'uui-tab[aria-selected="true"]';

	/**
	 * Navigate to the CSP Manager dashboard
	 */
	static async goToDashboard(page: Page) {
		await page.goto(this.BASE_URL);
		await expect(page.locator(this.DASHBOARD_SELECTOR)).toBeVisible();
	}

	/**
	 * Navigate to the Frontend workspace
	 */
	static async goToFrontendWorkspace(page: Page) {
		await page.goto(this.FRONTEND_WORKSPACE_URL);
		await expect(page.locator(this.WORKSPACE_SELECTOR)).toBeVisible();
	}

	/**
	 * Navigate to the Backoffice workspace
	 */
	static async goToBackofficeWorkspace(page: Page) {
		await page.goto(this.BACKOFFICE_WORKSPACE_URL);
		await expect(page.locator(this.WORKSPACE_SELECTOR)).toBeVisible();
	}

	/**
	 * Navigate to the Frontend Settings view
	 */
	static async goToFrontendSettings(page: Page) {
		await page.goto(this.FRONTEND_SETTINGS_URL);
		await expect(page.locator(this.WORKSPACE_SELECTOR)).toBeVisible();
		await expect(page.locator(this.ACTIVE_TAB_SELECTOR)).toBeVisible();
		await expect(page.locator(this.SETTINGS_VIEW_SELECTOR)).toBeVisible();
	}

	/**
	 * Navigate to the Backoffice Settings view
	 */
	static async goToBackofficeSettings(page: Page) {
		await page.goto(this.BACKOFFICE_SETTINGS_URL);
		await expect(page.locator(this.WORKSPACE_SELECTOR)).toBeVisible();
		await expect(page.locator(this.ACTIVE_TAB_SELECTOR)).toBeVisible();
		await expect(page.locator(this.SETTINGS_VIEW_SELECTOR)).toBeVisible();
	}

	/**
	 * Verify that both workspace tabs are present
	 */
	static async verifyWorkspaceTabs(page: Page) {
		await expect(page.locator("uui-tab")).toHaveCount(2);
	}

	/**
	 * Verify that the Sources tab is active and shows content
	 */
	static async verifySourcesTabActive(page: Page) {
		await expect(page.locator(this.ACTIVE_TAB_SELECTOR)).toBeVisible();
		await expect(page.locator(this.DEFAULT_VIEW_SELECTOR)).toBeVisible();
	}

	/**
	 * Verify that the Settings tab is active and shows content
	 */
	static async verifySettingsTabActive(page: Page) {
		await expect(page.locator(this.ACTIVE_TAB_SELECTOR)).toBeVisible();
		await expect(page.locator(this.SETTINGS_VIEW_SELECTOR)).toBeVisible();
	}

	/**
	 * Click the Sources tab
	 */
	static async clickSourcesTab(page: Page) {
		await page.locator("uui-tab").first().click();
		await this.verifySourcesTabActive(page);
	}

	/**
	 * Click the Settings tab
	 */
	static async clickSettingsTab(page: Page) {
		await page.locator("uui-tab").nth(1).click();
		await this.verifySettingsTabActive(page);
	}

	/**
	 * Verify that the Save button is present and enabled
	 */
	static async verifySaveButtonPresent(page: Page) {
		const saveButton = page.locator(this.SAVE_BUTTON_SELECTOR);
		await expect(saveButton).toBeVisible();
		return saveButton;
	}

	/**
	 * Verify dashboard cards are present
	 */
	static async verifyDashboardCards(page: Page) {
		await expect(page.locator("uui-card")).toBeVisible();
		await expect(page.locator(this.FRONTEND_BUTTON_SELECTOR)).toBeVisible();
		await expect(page.locator(this.BACKOFFICE_BUTTON_SELECTOR)).toBeVisible();
	}

	/**
	 * Click the Frontend CSP button from dashboard
	 */
	static async clickFrontendCspButton(page: Page) {
		await page.locator(this.FRONTEND_BUTTON_SELECTOR).click();
		await expect(page.locator(this.WORKSPACE_SELECTOR)).toBeVisible();
		await expect(page).toHaveURL(/.*\/workspace\/frontend/);
	}

	/**
	 * Click the Backoffice CSP button from dashboard
	 */
	static async clickBackofficeCspButton(page: Page) {
		await page.locator(this.BACKOFFICE_BUTTON_SELECTOR).click();
		await expect(page.locator(this.WORKSPACE_SELECTOR)).toBeVisible();
		await expect(page).toHaveURL(/.*\/workspace\/backoffice/);
	}
}
