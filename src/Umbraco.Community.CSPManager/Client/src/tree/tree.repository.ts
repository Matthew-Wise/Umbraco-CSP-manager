import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import type { UmbTreeRootModel, UmbTreeRepository } from '@umbraco-cms/backoffice/tree';
import { UmbTreeRepositoryBase } from '@umbraco-cms/backoffice/tree';
import type { UmbApi } from '@umbraco-cms/backoffice/extension-api';
import { CspTreeDataSource, type CspTreeItemModel } from './tree-data-source.js';
import { CspConstants } from '@/constants.js';

export interface CspTreeRootModel extends UmbTreeRootModel {
	entityType: typeof CspConstants.entityTypes.cspPolicyRoot;
}

export class CspTreeRepository
	extends UmbTreeRepositoryBase<CspTreeItemModel, CspTreeRootModel>
	implements UmbTreeRepository, UmbApi
{
	constructor(host: UmbControllerHost) {
		super(host, CspTreeDataSource);
	}

	async requestTreeRoot() {
		const root: CspTreeRootModel = {
			unique: null,
			entityType: CspConstants.entityTypes.cspPolicyRoot,
			name: 'CSP Policies',
			hasChildren: true,
			isFolder: true,
		};

		return { data: root };
	}
}

export { CspTreeRepository as api };
