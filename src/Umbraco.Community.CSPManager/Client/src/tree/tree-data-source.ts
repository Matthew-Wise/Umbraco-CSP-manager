import { CspConstants } from '@/constants';
import type { UmbTreeItemModel, UmbTreeRootItemsRequestArgs, UmbTreeChildrenOfRequestArgs, UmbTreeAncestorsOfRequestArgs } from '@umbraco-cms/backoffice/tree';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbCspDefinitionRepository } from '@/repository/csp-definition.repository';

export interface CspTreeItemModel extends UmbTreeItemModel {
	icon: string;
}

export class CspTreeDataSource {
	#repository: UmbCspDefinitionRepository;

	constructor(host: UmbControllerHost) {
		this.#repository = new UmbCspDefinitionRepository(host);
	}

	async getRootItems(_args: UmbTreeRootItemsRequestArgs) {
		const items: CspTreeItemModel[] = [
			{
				unique: CspConstants.policyTypes.backoffice.value,
				entityType: CspConstants.workspace.entityType,
				name: CspConstants.policyTypes.backoffice.label,
				hasChildren: false,
				icon: CspConstants.policyTypes.backoffice.icon,
				isFolder: false,
				parent: {
					unique: null,
					entityType: CspConstants.entityTypes.cspPolicyRoot,
				},
			},
			{
				unique: CspConstants.policyTypes.frontend.value,
				entityType: CspConstants.workspace.entityType,
				name: CspConstants.policyTypes.frontend.label,
				hasChildren: true,
				icon: CspConstants.policyTypes.frontend.icon,
				isFolder: false,
				parent: {
					unique: null,
					entityType: CspConstants.entityTypes.cspPolicyRoot,
				},
			},
		];

		return {
			data: {
				items,
				total: items.length,
			},
		};
	}

	async getChildrenOf(args: UmbTreeChildrenOfRequestArgs) {
		// Only the Frontend node has children (domain-specific policies)
		if (args.parent.unique !== CspConstants.policyTypes.frontend.value) {
			return {
				data: {
					items: [],
					total: 0,
				},
			};
		}

		const { data: policies, error } = await this.#repository.getDomainPolicies();

		if (error || !policies) {
			return {
				data: {
					items: [],
					total: 0,
				},
			};
		}

		const items: CspTreeItemModel[] = policies.map((policy) => ({
			unique: policy.id,
			entityType: CspConstants.workspace.entityType,
			name: policy.domainName ?? 'Unknown domain',
			hasChildren: false,
			icon: 'icon-home',
			isFolder: false,
			parent: {
				unique: CspConstants.policyTypes.frontend.value,
				entityType: CspConstants.workspace.entityType,
			},
		}));

		return {
			data: {
				items,
				total: items.length,
			},
		};
	}

	async getAncestorsOf(_args: UmbTreeAncestorsOfRequestArgs) {
		// No ancestors for root items
		return {
			data: [],
		};
	}
}
