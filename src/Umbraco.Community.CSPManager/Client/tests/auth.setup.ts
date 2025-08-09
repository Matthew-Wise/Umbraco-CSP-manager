import { test as setup } from "@playwright/test";
import { STORAGE_STATE } from "../playwright.config";
import { UiHelpers } from "@umbraco/playwright-testhelpers";
import { CspConstants } from "../src/constants";

setup("authenticate", async ({ page }) => {
	const umbracoUi = new UiHelpers(page);

	await umbracoUi.goToBackOffice();
	await umbracoUi.login.enterEmail(process.env.UMBRACO_USER_LOGIN!);
	await umbracoUi.login.enterPassword(process.env.UMBRACO_USER_PASSWORD!);
	await umbracoUi.login.clickLoginButton();
	await umbracoUi.login.goToSection(CspConstants.sectionLabel);
	await umbracoUi.page.context().storageState({ path: STORAGE_STATE });
});
