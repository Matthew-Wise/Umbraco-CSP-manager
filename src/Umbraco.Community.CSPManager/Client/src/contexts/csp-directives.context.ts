import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbContextBase } from '@umbraco-cms/backoffice/class-api';
import { UmbCspDirectivesRepository } from '../repository/csp-directives.repository.js';

export class UmbCspDirectivesContext extends UmbContextBase {
	#repository: UmbCspDirectivesRepository;

	constructor(host: UmbControllerHost) {
		super(host, 'UmbCspDirectivesContext');
		this.#repository = new UmbCspDirectivesRepository(host);
	}

	async load() {
		return await this.#repository.getAll();
	}
}

export default UmbCspDirectivesContext;
