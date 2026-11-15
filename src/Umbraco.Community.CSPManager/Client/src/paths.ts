import { UmbPathPattern } from '@umbraco-cms/backoffice/router';
import { UMB_WORKSPACE_PATH_PATTERN } from '@umbraco-cms/backoffice/workspace';
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

/** A saved policy (global or domain), by its id. */
export const CSP_EDIT_POLICY_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ unique: string }>(
	'edit/:unique',
	CSP_POLICY_WORKSPACE_PATH,
);

/** An unsaved draft domain policy for a content node, by the node's key. */
export const CSP_CREATE_DOMAIN_POLICY_WORKSPACE_PATH_PATTERN = new UmbPathPattern<{ contentKey: string }>(
	'create/:contentKey',
	CSP_POLICY_WORKSPACE_PATH,
);

export const cspEditPolicyPath = (unique: string) =>
	CSP_EDIT_POLICY_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: encodeURIComponent(unique) });

export const cspCreateDomainPolicyPath = (contentKey: string) =>
	CSP_CREATE_DOMAIN_POLICY_WORKSPACE_PATH_PATTERN.generateAbsolute({ contentKey: encodeURIComponent(contentKey) });
