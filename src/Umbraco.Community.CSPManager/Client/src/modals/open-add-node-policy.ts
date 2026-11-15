import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { umbOpenModal } from '@umbraco-cms/backoffice/modal';
import { cspCreateNodePolicyPath } from '@/paths';
import { ADD_NODE_POLICY_MODAL } from './add-node-policy-modal.token.js';

/**
 * The "Add Node Policy" flow, shared by the Frontend tree action and the dashboard: pick a content
 * node, a culture and the kind of policy, then open an unsaved draft of it in the workspace. Nothing
 * is created until Save.
 */
export async function openAddNodePolicy(host: UmbControllerHost): Promise<void> {
	const result = await umbOpenModal(host, ADD_NODE_POLICY_MODAL, { data: {} }).catch(() => undefined);
	if (!result) return;

	history.pushState(null, '', cspCreateNodePolicyPath(result.contentKey, result.culture, result.inheritance));
}
