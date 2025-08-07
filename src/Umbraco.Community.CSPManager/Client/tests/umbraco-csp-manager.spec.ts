import { test, expect } from "@playwright/test";
import * as umbraco from "@umbraco/playwright-testhelpers";

test("Can access CSP Manager Section", async ({ page }) => {
	var uiHelper = new umbraco.UiHelpers(page);
	await uiHelper.gotToSection("Umbraco.Community.CSPManager.Section");
});
