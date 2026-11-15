import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { umbOpenModal } from '@umbraco-cms/backoffice/modal';
import { cspCreateDomainPolicyPath } from '@/paths';
import { ADD_DOMAIN_POLICY_MODAL } from './add-domain-policy-modal.token.js';

/**
 * The "Add Domain Policy" flow, shared by the Frontend tree action and the dashboard: pick a content
 * node with a hostname, then open an unsaved draft of its policy (a copy of the Frontend policy) in
 * the workspace. Nothing is created until Save.
 */
export async function openAddDomainPolicy(host: UmbControllerHost): Promise<void> {
	const result = await umbOpenModal(host, ADD_DOMAIN_POLICY_MODAL, { data: {} }).catch(() => undefined);
	if (!result) return;

	history.pushState(null, '', cspCreateDomainPolicyPath(result.contentKey));
}
