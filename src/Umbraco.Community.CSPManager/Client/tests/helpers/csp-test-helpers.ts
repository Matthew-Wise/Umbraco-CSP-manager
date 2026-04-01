import { expect, type Locator } from "@playwright/test";
import { CspConstants } from "../../src/constants";
import { Selectors, EntityActions } from "./test-constants";

type PolicyKey = "frontend" | "backoffice";

const POLICY_LABELS: Record<PolicyKey, string> = {
	frontend: CspConstants.policyTypes.frontend.label,
	backoffice: CspConstants.policyTypes.backoffice.label,
};

/**
 * UI helpers for CSP Manager E2E tests.
 *
 * Encodes hard-won learnings:
 * - `umb-csp-import-modal` uses `:host { display: contents }` — use `waitForModalOpen()`,
 *   not `toBeVisible()` on the modal host element.
 * - Entity actions render directly on tree items — hover first, then click by button label.
 * - Workspace URLs use policy type UUIDs, not string aliases.
 */
export class CspTestHelpers {
	constructor(private umbracoUi: any) {}

	// ── Navigation ────────────────────────────────────────────────────────────

	/** Navigate to the CSP Manager section. */
	async goToSection(): Promise<void> {
		await this.umbracoUi.goToBackOffice();
		await this.umbracoUi.login.goToSection(CspConstants.section.label);
	}

	/**
	 * Navigate to a CSP policy workspace by clicking the tree item.
	 * Waits for the workspace element to be visible before returning.
	 */
	async navigateToWorkspace(policy: PolicyKey): Promise<void> {
		const label = POLICY_LABELS[policy];
		await expect(this.sidebar()).toBeVisible();
		await this.treeItem(label).click();
		await expect(this.workspace()).toBeVisible();
	}

	// ── Locators ──────────────────────────────────────────────────────────────

	/** The section sidebar element. */
	sidebar(): Locator {
		return this.umbracoUi.page.locator(Selectors.sidebar);
	}

	/**
	 * A tree item (uui-menu-item) filtered by its visible label text.
	 * Scoped to the sidebar to avoid ambiguity with other menu items.
	 */
	treeItem(label: string): Locator {
		return this.sidebar().locator(Selectors.menuItem).filter({ hasText: label });
	}

	/** The CSP management workspace element. */
	workspace(): Locator {
		return this.umbracoUi.page.locator(Selectors.workspace);
	}

	/** The generic workspace editor wrapper element. */
	workspaceEditor(): Locator {
		return this.umbracoUi.page.locator(Selectors.workspaceEditor);
	}

	/**
	 * A workspace view tab by its label text.
	 * Scoped to the workspace editor to avoid matching section-level tabs.
	 */
	workspaceTab(tabLabel: string): Locator {
		return this.workspaceEditor().locator(Selectors.tab).filter({ hasText: tabLabel });
	}

	/** The CSP Manager section dashboard. */
	dashboard(): Locator {
		return this.umbracoUi.page.locator(Selectors.dashboard);
	}

	/** The import CSP modal element. */
	importModal(): Locator {
		return this.umbracoUi.page.locator(Selectors.importModal);
	}

	// ── Entity Actions ────────────────────────────────────────────────────────

	/**
	 * Open an entity action on a tree item.
	 *
	 * Entity actions render as buttons directly alongside the tree item — hover
	 * to make them visible, then click by label.
	 */
	async openEntityAction(policyLabel: string, actionLabel: string): Promise<void> {
		const item = this.treeItem(policyLabel);
		await item.hover();
		await item.getByRole("button", { name: actionLabel }).click();
	}

	// ── Import Modal ──────────────────────────────────────────────────────────

	/**
	 * Open the Import CSP modal for the given policy.
	 * Waits for the modal to be ready before returning.
	 */
	async openImportModal(policy: PolicyKey): Promise<void> {
		await expect(this.sidebar()).toBeVisible();
		await this.openEntityAction(POLICY_LABELS[policy], EntityActions.import);
		await this.waitForModalOpen();
	}

	/**
	 * Wait for the import modal to be open and ready for interaction.
	 *
	 * `umb-csp-import-modal` uses `:host { display: contents }` so Playwright
	 * considers the host element itself "hidden". Checking `uui-dialog-layout`
	 * (the first visible interior element) confirms the modal is truly open.
	 */
	async waitForModalOpen(): Promise<void> {
		await expect(this.umbracoUi.page.locator(Selectors.dialogLayout)).toBeVisible({
			timeout: 10000,
		});
	}
}
