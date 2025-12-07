import { CspConstants } from '@/constants';
import type { UmbTreeItemModel, UmbTreeRootItemsRequestArgs, UmbTreeChildrenOfRequestArgs, UmbTreeAncestorsOfRequestArgs } from '@umbraco-cms/backoffice/tree';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';

export interface CspTreeItemModel extends UmbTreeItemModel {
	icon: string;
}

export class CspTreeDataSource {
	constructor(_host: UmbControllerHost) {
		// No initialization needed for static data
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
					entityType: 'csp-manager-root',
				},
			},
			{
				unique: CspConstants.policyTypes.frontend.value,
				entityType: CspConstants.workspace.entityType,
				name: CspConstants.policyTypes.frontend.label,
				hasChildren: false,
				icon: CspConstants.policyTypes.frontend.icon,
				isFolder: false,
				parent: {
					unique: null,
					entityType: 'csp-manager-root',
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

	async getChildrenOf(_args: UmbTreeChildrenOfRequestArgs) {
		// No children for now - flat structure
		return {
			data: {
				items: [],
				total: 0,
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
