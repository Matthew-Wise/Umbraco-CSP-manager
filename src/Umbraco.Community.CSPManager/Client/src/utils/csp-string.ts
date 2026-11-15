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

/** The source lists of a definition as a policy value, e.g. `default-src 'self'; img-src data:;`. */
export function sourcesAsCspString(definition: SourcesOnly): string {
	return [...directivesOf(definition)].map(([name, sources]) => `${name} ${sources.join(' ')}; `).join('');
}

/**
 * The policy value the server sends for a definition, apart from the nonces it adds to each
 * response: the source lists, then reporting and upgrade-insecure-requests.
 */
export function asCspHeaderValue(definition: WithSettings): string {
	const parts = [...directivesOf(definition)].map(([name, sources]) => `${name} ${sources.join(' ')}`);

	if (definition.reportingDirective && definition.reportingDirective !== 'none' && definition.reportUri) {
		parts.push(`${definition.reportingDirective} ${definition.reportUri}`);
	}

	if (definition.upgradeInsecureRequests) {
		parts.push('upgrade-insecure-requests');
	}

	return parts.join('; ');
}
