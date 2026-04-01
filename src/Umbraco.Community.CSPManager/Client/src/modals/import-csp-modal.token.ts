import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { ParsedCspSource } from '../utils/csp-parser.js';

export type { ParsedCspSource };

export interface ImportCspModalData {
	policyLabel: string;
	availableDirectives: string[];
}

export interface ImportCspModalValue {
	sources: ParsedCspSource[];
	upgradeInsecureRequests: boolean;
	reportingDirective: string | null;
	reportUri: string | null;
}

export const IMPORT_CSP_MODAL = new UmbModalToken<ImportCspModalData, ImportCspModalValue>(
	'Umbraco.Community.CSPManager.Modal.ImportCsp',
	{
		modal: {
			type: 'dialog',
			size: 'large',
		},
	},
);
