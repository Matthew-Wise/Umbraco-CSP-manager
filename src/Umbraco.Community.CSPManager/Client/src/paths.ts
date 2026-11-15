import { UmbPathPattern } from '@umbraco-cms/backoffice/router';
import { UMB_WORKSPACE_PATH_PATTERN } from '@umbraco-cms/backoffice/workspace';
import type { CspPolicyInheritance } from './api';
import { CspConstants } from './constants';

/**
 * Backoffice routes for CSP Manager, built from Umbraco's path patterns so every link, redirect and
 * the workspace's own routes share one definition. Generate links from these instead of writing
 * `section/csp-manager/...` strings by hand.
 */
export const CSP_MANAGER_SECTION_PATHNAME = 'csp-manager';

export const CSP_POLICY_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
	sectionName: CSP_MANAGER_SECTION_PATHNAME,
	entityType: CspConstants.workspace.entityType,
});

/** A saved policy (global or node policy), by its id. */
export const CSP_EDIT_POLICY_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ unique: string }>(
	'edit/:unique',
	CSP_POLICY_WORKSPACE_PATH,
);

/**
 * An unsaved draft node policy: the content node's key, the culture (`invariant` for all cultures)
 * and whether it is a complete policy or one that extends what it inherits.
 */
export const CSP_CREATE_NODE_POLICY_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{
	contentKey: string;
	culture: string;
	inheritance: string;
}>('create/:contentKey/:culture/:inheritance', CSP_POLICY_WORKSPACE_PATH);

export const cspEditPolicyPath = (unique: string) =>
	CSP_EDIT_POLICY_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: encodeURIComponent(unique) });

export const cspCreateNodePolicyPath = (contentKey: string, culture: string | null, inheritance: CspPolicyInheritance) =>
	CSP_CREATE_NODE_POLICY_WORKSPACE_PATH_PATTERN.generateAbsolute({
		contentKey: encodeURIComponent(contentKey),
		culture: encodeURIComponent(culture ?? CspConstants.nodePolicy.allCulturesSegment),
		inheritance,
	});

/** The culture of a create route's `culture` segment: `null` for all cultures. */
export const cultureFromCreatePathSegment = (segment: string): string | null =>
	segment === CspConstants.nodePolicy.allCulturesSegment ? null : decodeURIComponent(segment);

/** The content workspace of a document, for "open content item" links. */
export const documentEditPath = (contentKey: string) =>
	`section/content/workspace/document/edit/${encodeURIComponent(contentKey)}`;
