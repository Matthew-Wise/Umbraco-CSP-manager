import { UmbEntityActionBase } from '@umbraco-cms/backoffice/entity-action';
import { openAddNodePolicy } from '../modals/open-add-node-policy.js';

/** "Add Node Policy" on the Frontend node. See `openAddNodePolicy`. */
export class UmbAddNodePolicyEntityAction extends UmbEntityActionBase<never> {
	override async execute() {
		await openAddNodePolicy(this);
	}
}

export { UmbAddNodePolicyEntityAction as api };
