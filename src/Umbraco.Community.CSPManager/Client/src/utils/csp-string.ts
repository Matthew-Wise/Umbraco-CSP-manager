import type { CspApiDefinition } from '@/api';

type SourcesOnly = Pick<CspApiDefinition, 'sources'>;
type WithSettings = Pick<CspApiDefinition, 'sources' | 'upgradeInsecureRequests' | 'reportingDirective' | 'reportUri'>;

/** The sources of a definition per directive, both in the order the definition lists them. */
export function directivesOf(definition: SourcesOnly): Map<string, string[]> {
	const directives = new Map<string, string[]>();

	for (const { source, directives: names } of definition.sources) {
		for (const name of names) {
			const sources = directives.get(name);
			if (sources) {
				sources.push(source);
			} else {
				directives.set(name, [source]);
			}
		}
	}

	return directives;
}

const sourceLists = (definition: SourcesOnly): string[] =>
	[...directivesOf(definition)].map(([name, sources]) => `${name} ${sources.join(' ')}`);

/** The source lists of a definition as a policy value, e.g. `default-src 'self'; img-src data:;`. */
export function sourcesAsCspString(definition: SourcesOnly): string {
	return sourceLists(definition)
		.map((list) => `${list}; `)
		.join('');
}

/** Whether a definition reports violations: "No reporting" is stored as no directive, or as `none`. */
export function hasReportingDirective(definition: Pick<CspApiDefinition, 'reportingDirective'>): boolean {
	return !!definition.reportingDirective && definition.reportingDirective !== 'none';
}

/**
 * The policy value the server sends for a definition, apart from the nonces it adds to each
 * response: the source lists, then reporting and upgrade-insecure-requests.
 */
export function asCspHeaderValue(definition: WithSettings): string {
	const parts = sourceLists(definition);

	if (hasReportingDirective(definition) && definition.reportUri) {
		parts.push(`${definition.reportingDirective} ${definition.reportUri}`);
	}

	if (definition.upgradeInsecureRequests) {
		parts.push('upgrade-insecure-requests');
	}

	return parts.join('; ');
}
