import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import { UMB_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/workspace';
import type { UmbWorkspaceContext, UmbRoutableWorkspaceContext } from '@umbraco-cms/backoffice/workspace';
import { UmbWorkspaceRouteManager } from '@umbraco-cms/backoffice/workspace';
import { UmbObjectState } from '@umbraco-cms/backoffice/observable-api';
import { UMB_DISCARD_CHANGES_MODAL, umbOpenModal } from '@umbraco-cms/backoffice/modal';
import { UMB_ACTION_EVENT_CONTEXT } from '@umbraco-cms/backoffice/action';
import { UmbRequestReloadChildrenOfEntityEvent, UmbRequestReloadStructureForEntityEvent } from '@umbraco-cms/backoffice/entity-action';
import type { CspApiDefinition } from '@/api';
import { UmbCspDefinitionContext, UmbCspDirectivesContext } from '@/contexts/index';
import { UmbError, type UmbApiError, type UmbCancelError } from '@umbraco-cms/backoffice/resources';
import { UmbLocalizationController } from '@umbraco-cms/backoffice/localization-api';
import { CspConstants, isGlobalPolicyId, type PolicyType } from '@/constants';

export interface WorkspaceState {
	definition: CspApiDefinition | null;
	persistedDefinition: CspApiDefinition | null;
	availableDirectives: string[];
	loading: boolean;
	/** True for an unsaved domain policy draft (create route); it is created on the first save. */
	isNew?: boolean;
	error?: UmbError | UmbApiError | UmbCancelError | Error | undefined;
}

const ID_TO_POLICY_TYPE: Record<string, PolicyType> = {
	[CspConstants.policyTypes.frontend.value]: CspConstants.policyTypes.frontend,
	[CspConstants.policyTypes.backoffice.value]: CspConstants.policyTypes.backoffice,
};

export class UmbCspManagerWorkspaceContext
	extends UmbControllerBase
	implements UmbWorkspaceContext, UmbRoutableWorkspaceContext
{
	public readonly workspaceAlias = CspConstants.workspace.alias;
	public readonly routes = new UmbWorkspaceRouteManager(this);

	#policyId: string | null = null;
	#allowNavigateAway = false;
	// Bumped by every route load. A response that arrives after the route has changed again (e.g. the
	// user clicks another policy while a draft is still loading) must not overwrite the newer state.
	#loadVersion = 0;

	#state = new UmbObjectState<WorkspaceState>({
		definition: null,
		persistedDefinition: null,
		availableDirectives: [],
		loading: true,
	});

	#cspDefinitionContext: UmbCspDefinitionContext;
	#cspDirectivesContext: UmbCspDirectivesContext;
	#localize = new UmbLocalizationController(this);

	readonly state = this.#state.asObservable();

	getEntityType(): string {
		return CspConstants.workspace.entityType;
	}

	getUnique(): string | null {
		return this.#policyId;
	}

	getPolicyType(): PolicyType {
		if (this.#policyId && ID_TO_POLICY_TYPE[this.#policyId]) {
			return ID_TO_POLICY_TYPE[this.#policyId];
		}
		// Domain policies override the frontend policy.
		return CspConstants.policyTypes.frontend;
	}

	/** True when this workspace edits a domain policy (saved or a new draft) rather than a global one. */
	isDomainPolicy(): boolean {
		return this.#state.getValue().isNew === true || (this.#policyId !== null && !isGlobalPolicyId(this.#policyId));
	}

	constructor(host: UmbControllerHost) {
		super(host);

		this.#cspDefinitionContext = new UmbCspDefinitionContext(this);
		this.#cspDirectivesContext = new UmbCspDirectivesContext(this);

		this.provideContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, this);
		this.provideContext(UMB_WORKSPACE_CONTEXT, this);

		// Listen for navigation events to show discard changes modal
		window.addEventListener('willchangestate', this.#onWillNavigate);
		// Listen for external saves (e.g. import action) to reload definition
		window.addEventListener('csp:definition-saved', this.#onDefinitionSaved as EventListener);

		this.routes.setRoutes([
			{
				path: 'edit/:unique',
				component: () => import('../csp-management-workspace.element.js'),
				setup: (_component, info) => {
					const unique = info.match.params.unique;
					this.#load(unique);
				},
			},
			{
				path: 'create/:contentKey',
				component: () => import('../csp-management-workspace.element.js'),
				setup: (_component, info) => {
					this.#loadNew(decodeURIComponent(info.match.params.contentKey));
				},
			},
		]);
	}

	async #load(unique: string) {
		const version = ++this.#loadVersion;
		this.#policyId = unique;
		this.#state.update({ isNew: false });
		// Load both in parallel but await completion to avoid race conditions
		await Promise.all([this.loadDefinition(version), this.loadDirectives()]);
	}

	/**
	 * Builds an unsaved draft for a new domain policy from the Frontend policy. Nothing is persisted
	 * until save(); the id stays empty so the server assigns it.
	 */
	async #loadNew(contentKey: string) {
		const version = ++this.#loadVersion;
		this.#policyId = null;
		this.#allowNavigateAway = false;
		this.#state.update({ loading: true, error: undefined, isNew: true, definition: null, persistedDefinition: null });

		const [frontendResult, domainsResult] = await Promise.all([
			this.#cspDefinitionContext.load(false),
			this.#cspDefinitionContext.getNodes(),
			this.loadDirectives(),
		]);

		if (version !== this.#loadVersion) {
			return;
		}

		const error = frontendResult.error ?? domainsResult.error;
		if (error || !frontendResult.data) {
			this.#state.update({ loading: false, error });
			return;
		}

		// A stale or hand-typed URL: the node lost its hostname, was deleted or trashed, or already
		// has a policy. Shown now rather than as a 400 on save.
		const node = domainsResult.data?.find((n) => n.contentKey === contentKey);
		if (!node || node.hasCspPolicy) {
			this.#state.update({
				loading: false,
				error: new UmbError(this.#localize.term('cspManagerDomainPolicy_createUnavailable')),
			});
			return;
		}

		const newId = CspConstants.domainPolicy.newId;
		const draft: CspApiDefinition = {
			...structuredClone(frontendResult.data),
			id: newId,
			// Always on: a new domain policy should apply, whether or not the Frontend policy is on.
			enabled: true,
			isBackOffice: false,
			contentKey,
			contentName: node.contentName,
			domains: node.domains,
			disabledNodePolicyBehavior: null,
			sources: frontendResult.data.sources.map((s) => ({ ...s, definitionId: newId, directives: [...s.directives] })),
		};

		this.#state.update({
			definition: draft,
			persistedDefinition: null,
			loading: false,
			isNew: true,
			error: undefined,
		});
	}

	getIsBackOffice(): boolean {
		return this.getPolicyType() === CspConstants.policyTypes.backoffice;
	}

	async loadDefinition(version = this.#loadVersion) {
		this.#state.update({ loading: true, error: undefined });
		this.#allowNavigateAway = false;
		const { data, error } =
			this.#policyId && !isGlobalPolicyId(this.#policyId)
				? await this.#cspDefinitionContext.loadById(this.#policyId)
				: await this.#cspDefinitionContext.load(this.getIsBackOffice());

		if (version !== this.#loadVersion) {
			return;
		}

		if (error) {
			this.#state.update({ loading: false, error });
		} else if (data) {
			this.#state.update({
				definition: data,
				persistedDefinition: structuredClone(data),
				loading: false,
				error: undefined,
			});
		} else {
			this.#state.update({ loading: false, error: undefined });
		}
	}

	async loadDirectives() {
		const { data, error } = await this.#cspDirectivesContext.load();

		if (error) {
			this.#state.update({ error });
		} else if (data) {
			this.#state.update({ availableDirectives: data, error: undefined });
		}
	}

	private _validateDefinition(definition: CspApiDefinition): UmbError | undefined {
		// CSP host and keyword matching is case-insensitive, and the server rejects case
		// variants as duplicates, so compare case-insensitively here too. The cause carries the
		// sources exactly as typed so each offending row can be highlighted.
		const counts = new Map<string, number>();
		for (const { source } of definition.sources) {
			const key = source.toLowerCase();
			counts.set(key, (counts.get(key) ?? 0) + 1);
		}

		const duplicates = definition.sources
			.map(({ source }) => source)
			.filter((source) => (counts.get(source.toLowerCase()) ?? 0) > 1);

		if (duplicates.length > 0) {
			return new UmbError('Duplicate source names found', { cause: [...new Set(duplicates)] });
		}
	}

	updateDefinition(definition: CspApiDefinition) {
		const error = this._validateDefinition(definition);
		if (error) {
			this.#state.update({ definition, error });
		} else {
			this.#state.update({
				definition,
				error: undefined,
			});
		}
	}

	async save(): Promise<{
		success: boolean;
		error?: UmbError | UmbApiError | UmbCancelError | Error | undefined;
	}> {
		const currentState = this.#state.getValue();

		if (!currentState.definition) {
			return { success: false, error: new Error('No definition to save') };
		}

		const { data, error } = await this.#cspDefinitionContext.save(currentState.definition);

		if (error) {
			return { success: false, error };
		}

		if (currentState.isNew && data) {
			// First save of a draft: the server created the policy and assigned its id. Show it in
			// the tree and swap the create route for its edit route.
			this.#policyId = data.id;
			this.#allowNavigateAway = true;
			this.#state.update({
				definition: data,
				persistedDefinition: structuredClone(data),
				isNew: false,
				error: undefined,
			});
			await this.#reloadDomainPoliciesInTree();
			history.replaceState(null, '', `section/csp-manager/workspace/csp-policy/edit/${data.id}`);
			return { success: true };
		}

		// Update persisted to match current after successful save
		this.#state.update({
			persistedDefinition: structuredClone(currentState.definition),
			error: undefined,
		});

		// The tree marks inactive domain policies, so it has to follow an Enabled change.
		if (this.isDomainPolicy()) {
			await this.#reloadDomainPoliciesInTree();
		}

		return { success: true };
	}

	/** Deletes the saved domain policy this workspace shows. The global policies can't be deleted. */
	async deleteDomainPolicy(): Promise<{ success: boolean; error?: UmbError | UmbApiError | UmbCancelError | Error }> {
		const id = this.#policyId;
		if (!id || !this.isDomainPolicy() || this.#state.getValue().isNew) {
			return { success: false, error: new Error(this.#localize.term('cspManagerDomainPolicy_deleteUnsaved')) };
		}

		const { error } = await this.#cspDefinitionContext.deleteDomainPolicy(id);
		if (error) {
			return { success: false, error };
		}

		this.#allowNavigateAway = true;
		await this.#reloadDomainPoliciesInTree();
		return { success: true };
	}

	async #reloadDomainPoliciesInTree() {
		const actionEventContext = await this.getContext(UMB_ACTION_EVENT_CONTEXT);
		const frontend = {
			entityType: CspConstants.workspace.entityType,
			unique: CspConstants.policyTypes.frontend.value,
		};
		// Structure too, so Frontend gets its expand arrow when its first domain policy appears.
		actionEventContext?.dispatchEvent(new UmbRequestReloadStructureForEntityEvent(frontend));
		actionEventContext?.dispatchEvent(new UmbRequestReloadChildrenOfEntityEvent(frontend));
	}

	getDefinition(): CspApiDefinition | null {
		return this.#state.getValue().definition;
	}

	isLoading(): boolean {
		return this.#state.getValue().loading;
	}

	/**
	 * Check if there are unsaved changes by comparing current and persisted definitions
	 * Uses JSON string comparison for deep equality check
	 */
	hasUnsavedChanges(): boolean {
		const state = this.#state.getValue();
		// An unsaved draft is all unsaved changes.
		if (state.isNew) {
			return state.definition !== null;
		}
		if (!state.definition || !state.persistedDefinition) {
			return false;
		}
		return JSON.stringify(state.definition) !== JSON.stringify(state.persistedDefinition);
	}

	getAvailableDirectives(): string[] {
		return this.#state.getValue().availableDirectives;
	}

	/**
	 * Check if the workspace is about to navigate away from the current route
	 */
	#checkWillNavigateAway(newUrl: string | URL): boolean {
		if (newUrl instanceof URL) {
			newUrl = newUrl.href;
		}
		return !newUrl.includes(this.routes.getActiveLocalPath());
	}

	/**
	 * Handle navigation events to show discard changes modal
	 */
	#onWillNavigate = async (e: CustomEvent) => {
		const newUrl = e.detail.url;

		if (this.#allowNavigateAway) {
			return true;
		}

		if (this.#checkWillNavigateAway(newUrl) && this.hasUnsavedChanges()) {
			e.preventDefault();

			try {
				await umbOpenModal(this, UMB_DISCARD_CHANGES_MODAL);
				this.#allowNavigateAway = true;
				history.pushState({}, '', e.detail.url);
				return true;
			} catch {
				return false;
			}
		}

		return true;
	};

	#onDefinitionSaved = (e: CustomEvent<{ unique: string }>) => {
		if (e.detail.unique === this.#policyId) {
			this.loadDefinition();
		}
	};

	override destroy(): void {
		window.removeEventListener('willchangestate', this.#onWillNavigate);
		window.removeEventListener('csp:definition-saved', this.#onDefinitionSaved as EventListener);
		super.destroy();
	}
}

export const UMB_CSP_MANAGER_WORKSPACE_CONTEXT = new UmbContextToken<UmbCspManagerWorkspaceContext>(
	'UmbWorkspaceContext',
	'csp-manager.workspace'
);

export { UmbCspManagerWorkspaceContext as api };
export default UmbCspManagerWorkspaceContext;
