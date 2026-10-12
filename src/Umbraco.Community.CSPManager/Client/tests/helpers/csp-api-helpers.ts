import type { APIRequestContext } from "@playwright/test";
import type { CspApiDefinition, CspApiNodePolicy, CspApiNodeInfo } from "../../src/api";
import { CspDefinitionBuilder } from "./csp-definition-builder";

type PolicyKey = "frontend" | "backoffice";

/**
 * Helpers for direct API calls in tests — for setup and teardown that
 * should not go through the UI.
 *
 * Takes `page.request`, so calls are authenticated with the backoffice cookies from the stored
 * login. With cookie auth the backoffice sends the placeholder `Bearer [redacted]` and the server
 * reads the real token from the httpOnly cookie, so these calls do the same. (The testhelpers'
 * umbracoApi fixture expects a bearer token in localStorage, which Umbraco 17+ doesn't store.)
 */
export class CspApiHelpers {
	private readonly baseUrl = process.env.URL ?? "https://localhost:44370";

	constructor(private request: APIRequestContext) {}

	private readonly options = {
		ignoreHTTPSErrors: true,
		headers: { Authorization: "Bearer [redacted]" },
	};

	private url(path: string): string {
		return `${this.baseUrl}/umbraco/csp/api/v1/${path}`;
	}

	/** Save a CSP definition via the management API; returns the saved definition. */
	async saveDefinition(definition: CspApiDefinition): Promise<CspApiDefinition> {
		const response = await this.request.post(this.url("Definitions/save"), { ...this.options, data: definition });
		if (!response.ok()) {
			throw new Error(`Failed to save definition: ${response.status()} ${await response.text()}`);
		}
		return await response.json();
	}

	/** Get a global CSP definition via the management API. */
	async getDefinition(policy: PolicyKey): Promise<CspApiDefinition> {
		const response = await this.request.get(this.url(`Definitions?isBackOffice=${policy === "backoffice"}`), this.options);
		if (!response.ok()) {
			throw new Error(`Failed to get definition: ${response.status()}`);
		}
		return await response.json();
	}

	/**
	 * Reset a CSP definition to its initial empty placeholder state.
	 *
	 * The frontend definition is not persisted from migration — it is created
	 * lazily in memory by CspService. Resetting it to empty sources + disabled
	 * restores the state a fresh test site would have.
	 */
	async resetDefinition(policy: PolicyKey): Promise<void> {
		await this.saveDefinition(CspDefinitionBuilder.for(policy).build());
	}

	/** Every content node with a hostname, with its hostnames and whether it has a domain policy. */
	async getNodes(): Promise<CspApiNodeInfo[]> {
		const response = await this.request.get(this.url("Nodes"), this.options);
		if (!response.ok()) {
			throw new Error(`Failed to list domains: ${response.status()}`);
		}
		return await response.json();
	}

	/** Every domain policy, including orphaned ones. */
	async getDomainPolicies(): Promise<CspApiNodePolicy[]> {
		const response = await this.request.get(this.url("Definitions/node-policies"), this.options);
		if (!response.ok()) {
			throw new Error(`Failed to list domain policies: ${response.status()}`);
		}
		return await response.json();
	}

	/** Creates a domain policy: saved with the empty id, so the server assigns one. */
	async createDomainPolicy(definition: CspApiDefinition): Promise<CspApiDefinition> {
		return await this.saveDefinition(definition);
	}

	/** Deletes every domain policy, so each test starts from the global policies only. */
	async deleteAllDomainPolicies(): Promise<void> {
		for (const policy of await this.getDomainPolicies()) {
			const response = await this.request.delete(this.url(`Definitions/${policy.id}`), this.options);
			if (!response.ok()) {
				throw new Error(`Failed to delete domain policy ${policy.id}: ${response.status()}`);
			}
		}
	}
}
