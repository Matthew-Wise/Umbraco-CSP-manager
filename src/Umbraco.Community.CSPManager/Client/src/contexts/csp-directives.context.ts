import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbContextBase } from '@umbraco-cms/backoffice/class-api';
import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import { UmbCspDirectivesRepository } from '../repository/csp-directives.repository.js';

export const UMB_CSP_DIRECTIVES_CONTEXT = new UmbContextToken<UmbCspDirectivesContext>(
	'UmbCspDirectivesContext',
	'csp-manager.directives'
);

export class UmbCspDirectivesContext extends UmbContextBase {
	#repository: UmbCspDirectivesRepository;

	constructor(host: UmbControllerHost) {
		super(host, UMB_CSP_DIRECTIVES_CONTEXT);
		this.#repository = new UmbCspDirectivesRepository(host);
	}

	async load() {
		return await this.#repository.getAll();
	}
}

export default UmbCspDirectivesContext;
