import { CspConstants } from "../../src/constants";
import { CspPolicyInheritance, CspPolicyScope, type CspApiDefinition, type CspApiDefinitionSource } from "../../src/api";

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
	private _contentKey: string | null = null;
	private _culture: string | null = null;
	private _inheritance = CspPolicyInheritance.REPLACE;
	private _appliesTo = CspPolicyScope.DESCENDANTS;

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

	/**
	 * A new node policy for a content node, for one culture or (the default) all of them: posted
	 * with the empty id, so the server assigns one.
	 */
	static nodePolicy(contentKey: string, culture: string | null = null): CspDefinitionBuilder {
		const builder = new CspDefinitionBuilder("frontend");
		builder._id = CspConstants.nodePolicy.newId;
		builder._contentKey = contentKey;
		builder._culture = culture;
		return builder;
	}

	/** An "inherit and add" policy: its sources are added to the policy the node inherits. */
	extending(): this {
		this._inheritance = CspPolicyInheritance.EXTEND;
		return this;
	}

	/** For the node itself, not the pages below it. */
	nodeOnly(): this {
		this._appliesTo = CspPolicyScope.NODE_ONLY;
		return this;
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
			contentKey: this._contentKey,
			culture: this._culture,
			inheritance: this._inheritance,
			appliesTo: this._appliesTo,
			domains: [],
			isOrphaned: false,
			sources,
		};
	}
}
