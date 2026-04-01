export interface ParsedCspSource {
	source: string;
	directives: string[];
}

export interface CspParseResult {
	sources: ParsedCspSource[];
	upgradeInsecureRequests: boolean;
	reportingDirective: string | null;
	reportUri: string | null;
	warnings: string[];
}

/**
 * Parse a CSP policy value string (e.g. `default-src 'self'; script-src 'self' https://cdn.example.com`)
 * into the CSP Manager data model.
 *
 * The input should be the policy value only, not the full header line.
 */
export function parseCspString(cspString: string, knownDirectives: string[]): CspParseResult {
	const sources = new Map<string, Set<string>>();
	let upgradeInsecureRequests = false;
	let reportingDirective: string | null = null;
	let reportUri: string | null = null;
	const warnings: string[] = [];

	const knownDirectivesLower = new Set(knownDirectives.map((d) => d.toLowerCase()));

	const directives = cspString.split(';');

	for (const directive of directives) {
		const trimmed = directive.trim();
		if (!trimmed) continue;

		const tokens = trimmed.split(/\s+/);
		const directiveName = tokens[0].toLowerCase();
		const directiveSources = tokens.slice(1);

		if (directiveName === 'upgrade-insecure-requests') {
			upgradeInsecureRequests = true;
			continue;
		}

		if (directiveName === 'report-uri') {
			reportingDirective = 'report-uri';
			reportUri = directiveSources[0] ?? null;
			continue;
		}

		if (directiveName === 'report-to') {
			reportingDirective = 'report-to';
			reportUri = directiveSources[0] ?? null;
			continue;
		}

		if (!knownDirectivesLower.has(directiveName)) {
			warnings.push(`Unknown directive: "${directiveName}"`);
			continue;
		}

		for (const source of directiveSources) {
			if (!sources.has(source)) {
				sources.set(source, new Set());
			}
			sources.get(source)!.add(directiveName);
		}
	}

	const parsedSources: ParsedCspSource[] = Array.from(sources.entries()).map(([source, directiveSet]) => ({
		source,
		directives: Array.from(directiveSet),
	}));

	return {
		sources: parsedSources,
		upgradeInsecureRequests,
		reportingDirective,
		reportUri,
		warnings,
	};
}
