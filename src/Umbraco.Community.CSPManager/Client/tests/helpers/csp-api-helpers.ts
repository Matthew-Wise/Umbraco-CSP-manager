import type { CspApiDefinition } from "../../src/api";
import { CspDefinitionBuilder } from "./csp-definition-builder";

type PolicyKey = "frontend" | "backoffice";

/**
 * Helpers for direct API calls in tests — for setup and teardown that
 * should not go through the UI.
 *
 * Uses umbracoApi from the testhelpers fixture, which handles Bearer token
 * auth via ApiHelpers.getHeaders().
 */
export class CspApiHelpers {
	constructor(private umbracoApi: any) {}

	/** Save a CSP definition via the management API. */
	async saveDefinition(definition: CspApiDefinition): Promise<void> {
		const url = `${this.umbracoApi.baseUrl}/umbraco/csp/api/v1/Definitions/save`;
		const response = await this.umbracoApi.post(url, definition);
		if (!response.ok()) {
			throw new Error(`Failed to save definition: ${response.status()} ${response.statusText()}`);
		}
	}

	/**
	 * Reset a CSP definition to its initial empty placeholder state.
	 *
	 * The frontend definition is not persisted from migration — it is created
	 * lazily in memory by CspService. Resetting it to empty sources + disabled
	 * restores the state a fresh test site would have.
	 */
	async resetDefinition(policy: PolicyKey): Promise<void> {
		const definition = CspDefinitionBuilder.for(policy).build();
		await this.saveDefinition(definition);
	}
}
