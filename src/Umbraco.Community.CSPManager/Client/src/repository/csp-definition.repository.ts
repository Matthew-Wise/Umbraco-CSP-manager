import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { Definitions, Nodes, type CspApiDefinition, type CspPolicyInheritance } from '../api';
import { hasReportingDirective } from '../utils/csp-string';

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
	 * Get a CSP definition (global or node policy) by its id
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
	 * Get every node policy (including orphaned ones, whose content node or language is gone)
	 */
	async getNodePolicies() {
		const { data, error } = await tryExecute(this, Definitions.getDefinitionsNodePolicies(), {
			disableNotifications: false,
		});

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Get the content nodes with a hostname (the sites), the usual nodes to create a node policy for
	 */
	async getNodes() {
		const { data, error } = await tryExecute(this, Nodes.getNodes(), { disableNotifications: false });

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Get an unsaved node policy for a content node, as the server would create it: a complete
	 * policy is a copy of what the node gets today, an extending one has no sources. Fails when
	 * the node, or the culture, can't have a new policy.
	 */
	async getDraft(contentKey: string, culture: string | null, inheritance: CspPolicyInheritance) {
		const { data, error } = await tryExecute(
			this,
			Definitions.getDefinitionsDraft({ query: { contentKey, culture: culture ?? undefined, inheritance } }),
			{ disableNotifications: true },
		);

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Get the policy a frontend request for a content node gets today, from the saved policies.
	 * Without a culture the server answers for the default language.
	 */
	async getEffective(contentKey: string, culture?: string | null) {
		const { data, error } = await tryExecute(
			this,
			Definitions.getDefinitionsEffective({ query: { contentKey, culture: culture ?? undefined } }),
			{ disableNotifications: true },
		);

		if (data) {
			return { data };
		}

		return { error };
	}

	/**
	 * Delete a node policy
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
	 * Save CSP definition. A node policy posted with the empty id is created and gets its id from the server.
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
	if (hasReportingDirective(definition)) {
		return definition;
	}

	return { ...definition, reportingDirective: null, reportUri: null };
}

export { UmbCspDefinitionRepository as api };
