import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { umbOpenModal } from '@umbraco-cms/backoffice/modal';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import type { UmbEntityActionArgs } from '@umbraco-cms/backoffice/entity-action';
import { UMB_ADD_DOMAIN_POLICY_MODAL } from './add-domain-policy-modal.element.js';

export class AddDomainPolicyAction extends UmbEntityActionBase<never> {
	constructor(host: UmbControllerHost, args: UmbEntityActionArgs<never>) {
		super(host, args);
	}

	override async execute() {
		let domainKey: string;

		try {
			const result = await umbOpenModal(this, UMB_ADD_DOMAIN_POLICY_MODAL, { data: {} });
			domainKey = result.domainKey;
		} catch {
			// User cancelled the modal
			return;
		}

		history.pushState({}, '', `section/csp-manager/workspace/csp-policy/create/${domainKey}`);
	}
}

export default AddDomainPolicyAction;
