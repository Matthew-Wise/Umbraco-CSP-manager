import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { Definitions, Domains, type CspApiDefinition } from '../api';

export class UmbCspDefinitionRepository extends UmbRepositoryBase {
	constructor(host: UmbControllerHost) {
		super(host);
	}

	/**
	 * Get CSP definition by type (front-end or back-office)
	 */
	async get(isBackOffice: boolean) {
		const { data, error } = await tryExecute(
			this,
			Definitions.getUmbracoCspApiV1Definitions({
				query: { isBackOffice },
			}),
			{ disableNotifications: false }
		);

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Get domain-specific CSP definition by domain Guid Key
	 */
	async getByDomainKey(domainKey: string) {
		const { data, error } = await tryExecute(
			this,
			Definitions.getUmbracoCspApiV1Definitions({
				query: { domainKey },
			}),
			{ disableNotifications: false }
		);

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Get all domain-specific CSP policies
	 */
	async getDomainPolicies() {
		const { data, error } = await tryExecute(
			this,
			Definitions.getUmbracoCspApiV1DefinitionsDomainPolicies(),
			{ disableNotifications: false }
		);

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Save CSP definition
	 */
	async save(definition: CspApiDefinition) {
		const { data, error } = await tryExecute(
			this,
			Definitions.postUmbracoCspApiV1DefinitionsSave({
				body: definition,
			}),
			{ disableNotifications: false }
		);

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Create a new domain-specific CSP policy by copying from the global frontend policy
	 */
	async createFromFrontend(domainKey: string) {
		const { data, error } = await tryExecute(
			this,
			Definitions.postUmbracoCspApiV1DefinitionsCreateFromFrontend({
				query: { domainKey },
			}),
			{ disableNotifications: false }
		);

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Delete a domain-specific CSP policy
	 */
	async delete(id: string) {
		const { error } = await tryExecute(
			this,
			Definitions.deleteUmbracoCspApiV1DefinitionsById({
				path: { id },
			}),
			{ disableNotifications: false }
		);

		return { error };
	}

	/**
	 * Get all Umbraco domains with CSP policy status
	 */
	async getDomains() {
		const { data, error } = await tryExecute(
			this,
			Domains.getUmbracoCspApiV1Domains(),
			{ disableNotifications: false }
		);

		if (data) {
			return { data };
		}

		return { error };
	}
}

export { UmbCspDefinitionRepository as api };
