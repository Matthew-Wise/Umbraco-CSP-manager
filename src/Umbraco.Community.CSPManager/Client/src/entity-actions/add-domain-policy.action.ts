import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { umbOpenModal } from '@umbraco-cms/backoffice/modal';
import { ADD_DOMAIN_POLICY_MODAL } from '../modals/add-domain-policy-modal.token.js';

/**
 * "Add Domain Policy" on the Frontend node: pick a content node with a hostname, then open an
 * unsaved draft of its policy (a copy of the Frontend policy) in the workspace. Nothing is created
 * until Save.
 */
export class UmbAddDomainPolicyEntityAction extends UmbEntityActionBase<never> {
	override async execute() {
		const result = await umbOpenModal(this, ADD_DOMAIN_POLICY_MODAL, { data: {} }).catch(() => undefined);
		if (!result) return;

		history.pushState(null, '', `section/csp-manager/workspace/csp-policy/create/${encodeURIComponent(result.contentKey)}`);
	}
}

export { UmbAddDomainPolicyEntityAction as api };
