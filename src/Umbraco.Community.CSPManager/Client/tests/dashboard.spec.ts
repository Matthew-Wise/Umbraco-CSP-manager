import { expect } from "@playwright/test";
import { test } from "@umbraco/playwright-testhelpers";
import { CspTestHelpers, EntityActions } from "./helpers";
import { CspConstants } from "../src/constants";

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

	// The dashboard links are the only way into a workspace that isn't the tree, so check they still
	// land on the right policy when the workspace routes change.
	for (const { button, policy } of [
		{ button: "Configure Front-end CSP", policy: CspConstants.policyTypes.frontend },
		{ button: "Configure Back-office CSP", policy: CspConstants.policyTypes.backoffice },
	]) {
		test(`${button} opens the ${policy.label} policy`, async ({ umbracoUi }) => {
			const csp = new CspTestHelpers(umbracoUi);

			await csp.dashboard().getByRole("link", { name: button }).or(csp.dashboard().getByRole("button", { name: button })).first().click();

			await expect(umbracoUi.page).toHaveURL(new RegExp(`/workspace/csp-policy/edit/${policy.value}$`));
			await expect(csp.workspaceEditor()).toHaveAttribute("headline", `${policy.label} CSP Management`);
		});
	}

	test("Add Domain Policy opens the content picker", async ({ umbracoUi }) => {
		const csp = new CspTestHelpers(umbracoUi);

		await csp.dashboard().getByRole("button", { name: EntityActions.addDomainPolicy, exact: true }).click();

		await expect(csp.addDomainPolicyModal().locator("umb-body-layout")).toBeVisible();
		await csp.addDomainPolicyModal().getByRole("button", { name: "Cancel" }).click();
		await expect(csp.addDomainPolicyModal()).toHaveCount(0);
	});
});
