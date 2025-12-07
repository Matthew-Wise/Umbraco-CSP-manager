import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import type { UmbTreeRootModel, UmbTreeRepository } from '@umbraco-cms/backoffice/tree';
import { UmbTreeRepositoryBase } from '@umbraco-cms/backoffice/tree';
import type { UmbApi } from '@umbraco-cms/backoffice/extension-api';
import { CspTreeDataSource, type CspTreeItemModel } from './tree-data-source.js';

export interface CspTreeRootModel extends UmbTreeRootModel {
	entityType: 'csp-manager-root';
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
			entityType: 'csp-manager-root',
			name: 'CSP Policies',
			hasChildren: true,
			isFolder: true,
		};

		return { data: root };
	}
}

export { CspTreeRepository as api };
