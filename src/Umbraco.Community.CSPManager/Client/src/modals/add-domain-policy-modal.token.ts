import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import { CspConstants } from '@/constants';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type
export interface AddDomainPolicyModalData {}

export interface AddDomainPolicyModalValue {
	/** The content node the new domain policy belongs to. */
	contentKey: string;
}

export const ADD_DOMAIN_POLICY_MODAL = new UmbModalToken<AddDomainPolicyModalData, AddDomainPolicyModalValue>(
	CspConstants.domainPolicy.addModalAlias,
	{
		modal: {
			type: 'sidebar',
			size: 'small',
		},
	},
);
