import { defineConfig, devices } from "@playwright/test";
import * as path from "path";
import { fileURLToPath } from "url";
import { dirname } from "path";
import dotenv from "dotenv";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
dotenv.config();

export const STORAGE_STATE = path.join(__dirname, "playwright/.auth/user.json");

export default defineConfig({
	testDir: "./tests/",
	timeout: 30 * 1000,
	expect: {
		timeout: 5000,
	},
	/* Fail the build on CI if you accidentally left test.only in the source code. */
	forbidOnly: !!process.env.CI,
	retries: 1,
	workers: 1, // We don't want to run parallel, as tests might differ in state
	/* Reporter to use. See https://playwright.dev/docs/test-reporters */
	reporter: process.env.CI
		? [["line"], ["junit", { outputFile: "results/results.xml" }]]
		: "html",
	outputDir: "./results",
	use: {
		actionTimeout: 0,
		// When working locally it can be a good idea to use trace: 'on-first-retry' instead of 'retain-on-failure', it can cut the local test times in half.
		trace: process.env.CI ? "retain-on-failure" : "on-first-retry",
		ignoreHTTPSErrors: true,
		testIdAttribute: "data-mark",
	},

	/* Configure projects for major browsers */
	projects: [
		{
			name: "setup",
			testMatch: "**/*.setup.ts",
		},
		{
			name: "chromium",
			use: {
				...devices["Desktop Chrome"],
				storageState: STORAGE_STATE,
			},
			dependencies: ["setup"],
		},
	],
});
