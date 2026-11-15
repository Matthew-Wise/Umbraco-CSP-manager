import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import type { UmbConditionConfigBase, UmbConditionControllerArguments, UmbExtensionCondition } from '@umbraco-cms/backoffice/extension-api';
import { UmbConditionBase } from '@umbraco-cms/backoffice/extension-registry';
import { UMB_CSP_MANAGER_WORKSPACE_CONTEXT } from '../workspace/context/workspace.context.js';

/**
 * Permits an extension only while the CSP workspace shows a node policy (saved or a new draft), not
 * one of the two global policies. The workspace is reused when the user moves between policies, so
 * this follows its state.
 */
export class UmbCspNodePolicyCondition extends UmbConditionBase<UmbConditionConfigBase> implements UmbExtensionCondition {
	constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<UmbConditionConfigBase>) {
		super(host, args);

		this.consumeContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, (context) => {
			if (!context) {
				this.permitted = false;
				return;
			}

			this.observe(context.state, () => (this.permitted = context.isNodePolicy()), 'cspNodePolicyCondition');
		});
	}
}

export { UmbCspNodePolicyCondition as api };
