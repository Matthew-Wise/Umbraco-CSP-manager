import { CspConstants } from "../../src/constants";
import type { CspApiDefinition, CspApiDefinitionSource } from "../../src/api";

type PolicyKey = "frontend" | "backoffice";

/**
 * Fluent builder for constructing CspApiDefinition objects in tests.
 *
 * Usage:
 *   const def = CspDefinitionBuilder.frontend()
 *     .enabled()
 *     .withSource("'self'", ["default-src", "script-src"])
 *     .build();
 */
export class CspDefinitionBuilder {
	private _id: string;
	private _isBackOffice: boolean;
	private _enabled = false;
	private _reportOnly = false;
	private _upgradeInsecureRequests = false;
	private _reportingDirective: string | null = null;
	private _reportUri: string | null = null;
	private _sources: Array<{ source: string; directives: string[] }> = [];

	private constructor(policy: PolicyKey) {
		const policyType = CspConstants.policyTypes[policy];
		this._id = policyType.value;
		this._isBackOffice = policy === "backoffice";
	}

	static frontend(): CspDefinitionBuilder {
		return new CspDefinitionBuilder("frontend");
	}

	static backoffice(): CspDefinitionBuilder {
		return new CspDefinitionBuilder("backoffice");
	}

	static for(policy: PolicyKey): CspDefinitionBuilder {
		return new CspDefinitionBuilder(policy);
	}

	enabled(value = true): this {
		this._enabled = value;
		return this;
	}

	reportOnly(value = true): this {
		this._reportOnly = value;
		return this;
	}

	upgradeInsecureRequests(value = true): this {
		this._upgradeInsecureRequests = value;
		return this;
	}

	withReporting(directive: string, uri: string): this {
		this._reportingDirective = directive;
		this._reportUri = uri;
		return this;
	}

	withSource(source: string, directives: string[]): this {
		this._sources.push({ source, directives });
		return this;
	}

	withSources(sources: Array<{ source: string; directives: string[] }>): this {
		this._sources.push(...sources);
		return this;
	}

	build(): CspApiDefinition {
		const sources: CspApiDefinitionSource[] = this._sources.map((s) => ({
			definitionId: this._id,
			source: s.source,
			directives: s.directives,
		}));

		return {
			id: this._id,
			enabled: this._enabled,
			reportOnly: this._reportOnly,
			isBackOffice: this._isBackOffice,
			upgradeInsecureRequests: this._upgradeInsecureRequests,
			reportingDirective: this._reportingDirective,
			reportUri: this._reportUri,
			sources,
		};
	}
}
