import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { CspPolicyInheritance } from '@/api';
import { CspConstants } from '@/constants';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type
export interface AddNodePolicyModalData {}

export interface AddNodePolicyModalValue {
	/** The content node the new node policy belongs to. */
	contentKey: string;
	/** The ISO code of the language the policy is for, or null for all cultures. */
	culture: string | null;
	/** A complete policy, or one that adds sources to the policy the node inherits. */
	inheritance: CspPolicyInheritance;
}

export const ADD_NODE_POLICY_MODAL = new UmbModalToken<AddNodePolicyModalData, AddNodePolicyModalValue>(
	CspConstants.nodePolicy.addModalAlias,
	{
		modal: {
			type: 'sidebar',
			size: 'small',
		},
	},
);
