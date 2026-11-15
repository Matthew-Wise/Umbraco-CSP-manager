import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbContextBase } from '@umbraco-cms/backoffice/class-api';
import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import type { CspApiDefinition, CspPolicyInheritance } from '@/api';
import { UmbCspDefinitionRepository } from '../repository/csp-definition.repository.js';

export const UMB_CSP_DEFINITION_CONTEXT = new UmbContextToken<UmbCspDefinitionContext>(
	'UmbCspDefinitionContext',
	'csp-manager.definition'
);

export class UmbCspDefinitionContext extends UmbContextBase {
	#repository: UmbCspDefinitionRepository;

	constructor(host: UmbControllerHost) {
		super(host, UMB_CSP_DEFINITION_CONTEXT);
		this.#repository = new UmbCspDefinitionRepository(host);
	}

	async load(isBackOffice: boolean) {
		return await this.#repository.get(isBackOffice);
	}

	async loadById(id: string) {
		return await this.#repository.getById(id);
	}

	async save(definition: CspApiDefinition) {
		return await this.#repository.save(definition);
	}

	async getNodePolicies() {
		return await this.#repository.getNodePolicies();
	}

	async getDraft(contentKey: string, culture: string | null, inheritance: CspPolicyInheritance) {
		return await this.#repository.getDraft(contentKey, culture, inheritance);
	}

	async getEffective(contentKey: string, culture?: string | null) {
		return await this.#repository.getEffective(contentKey, culture);
	}

	async deleteNodePolicy(id: string) {
		return await this.#repository.delete(id);
	}
}

export default UmbCspDefinitionContext;
