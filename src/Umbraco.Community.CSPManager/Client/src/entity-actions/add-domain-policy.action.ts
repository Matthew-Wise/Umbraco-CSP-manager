import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { openAddDomainPolicy } from '../modals/open-add-domain-policy.js';

/** "Add Domain Policy" on the Frontend node. See `openAddDomainPolicy`. */
export class UmbAddDomainPolicyEntityAction extends UmbEntityActionBase<never> {
	override async execute() {
		await openAddDomainPolicy(this);
	}
}

export { UmbAddDomainPolicyEntityAction as api };
