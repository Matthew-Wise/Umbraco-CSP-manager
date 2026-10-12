import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { Definitions, Nodes, type CspApiDefinition } from '../api';

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
			Definitions.getDefinitions({
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
	 * Get a CSP definition (global or domain policy) by its id
	 */
	async getById(id: string) {
		const { data, error } = await tryExecute(
			this,
			Definitions.getDefinitionsById({
				path: { id },
			}),
			{ disableNotifications: false }
		);

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Get every domain policy (including orphaned ones whose content node lost its hostnames)
	 */
	async getDomainPolicies() {
		const { data, error } = await tryExecute(this, Definitions.getDefinitionsNodePolicies(), {
			disableNotifications: false,
		});

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Get the content nodes (with their hostnames) a domain policy can be created for
	 */
	async getNodes() {
		const { data, error } = await tryExecute(this, Nodes.getNodes(), { disableNotifications: false });

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Delete a domain policy
	 */
	async delete(id: string) {
		const { error } = await tryExecute(
			this,
			Definitions.deleteDefinitionsById({
				path: { id },
			}),
			{ disableNotifications: false }
		);

		return { error };
	}

	/**
	 * Save CSP definition. A domain policy posted with the empty id is created and gets its id from the server.
	 */
	async save(definition: CspApiDefinition) {
		const { data, error } = await tryExecute(
			this,
			Definitions.postDefinitionsSave({
				body: withoutUnusedReporting(definition),
			}),
			{ disableNotifications: false }
		);

		if (data) {
			return { data };
		}

		return { error };
	}
}

/**
 * "No reporting" is no directive at all: the server only accepts report-uri or report-to. A URI
 * left over from a directive the user switched away from isn't sent either, so a stale value
 * can't fail validation for a setting that is off.
 */
export function withoutUnusedReporting(definition: CspApiDefinition): CspApiDefinition {
	const directive = definition.reportingDirective;
	if (directive && directive !== 'none') {
		return definition;
	}

	return { ...definition, reportingDirective: null, reportUri: null };
}

export { UmbCspDefinitionRepository as api };
