import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import { UMB_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/workspace';
import type { UmbWorkspaceContext, UmbRoutableWorkspaceContext } from '@umbraco-cms/backoffice/workspace';
import { UmbWorkspaceRouteManager } from '@umbraco-cms/backoffice/workspace';
import { UmbObjectState } from '@umbraco-cms/backoffice/observable-api';
import { UMB_DISCARD_CHANGES_MODAL, umbOpenModal } from '@umbraco-cms/backoffice/modal';
import { UMB_ACTION_EVENT_CONTEXT } from '@umbraco-cms/backoffice/action';
import { UmbRequestReloadChildrenOfEntityEvent } from '@umbraco-cms/backoffice/entity-action';
import type { CspApiDefinition } from '@/api';
import { UmbCspDefinitionContext, UmbCspDirectivesContext } from '@/contexts/index';
import { UmbError, type UmbApiError, type UmbCancelError } from '@umbraco-cms/backoffice/resources';
import { CspConstants, type PolicyType } from '@/constants';

export interface WorkspaceState {
	definition: CspApiDefinition | null;
	persistedDefinition: CspApiDefinition | null;
	availableDirectives: string[];
	loading: boolean;
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

	#state = new UmbObjectState<WorkspaceState>({
		definition: null,
		persistedDefinition: null,
		availableDirectives: [],
		loading: true,
	});

	#cspDefinitionContext: UmbCspDefinitionContext;
	#cspDirectivesContext: UmbCspDirectivesContext;

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
		return CspConstants.policyTypes.frontend;
	}

	isDomainPolicy(): boolean {
		return (
			this.#state.getValue().isNew === true ||
			(this.#policyId != null && !ID_TO_POLICY_TYPE[this.#policyId])
		);
	}

	getDomainName(): string | null {
		return this.#state.getValue().definition?.domainName ?? null;
	}

	constructor(host: UmbControllerHost) {
		super(host);

		this.#cspDefinitionContext = new UmbCspDefinitionContext(this);
		this.#cspDirectivesContext = new UmbCspDirectivesContext(this);

		this.provideContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, this);
		this.provideContext(UMB_WORKSPACE_CONTEXT, this);

		// Listen for navigation events to show discard changes modal
		window.addEventListener('willchangestate', this.#onWillNavigate);

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
				path: 'create/:domainKey',
				component: () => import('../csp-management-workspace.element.js'),
				setup: (_component, info) => {
					const domainKey = info.match.params.domainKey;
					this.#loadNew(domainKey);
				},
			},
		]);
	}

	async #load(unique: string) {
		this.#policyId = unique;
		await Promise.all([this.loadDefinition(), this.loadDirectives()]);
	}

	async #loadNew(domainKey: string) {
		this.#policyId = null;
		this.#state.update({ loading: true, error: undefined, isNew: true });
		this.#allowNavigateAway = false;

		// Load frontend policy as template and domain name in parallel
		const [definitionResult, domainsResult] = await Promise.all([
			this.#cspDefinitionContext.load(false),
			this.#cspDefinitionContext.getDomains(),
			this.loadDirectives(),
		]);

		if (definitionResult.error) {
			this.#state.update({ loading: false, error: definitionResult.error });
			return;
		}

		const frontendDef = definitionResult.data;
		if (!frontendDef) {
			this.#state.update({ loading: false });
			return;
		}

		// Resolve domain name for display
		const domains = domainsResult.data ?? [];
		const domain = domains.find((d) => d.key === domainKey);

		// Build a new definition using the frontend policy as a template
		const newId = crypto.randomUUID();
		const newDefinition: CspApiDefinition = {
			...frontendDef,
			id: newId,
			domainKey,
			domainName: domain?.name ?? null,
			// Fix up source DefinitionId references to point to the new definition
			sources: (frontendDef.sources ?? []).map((s) => ({ ...s, definitionId: newId })),
		};

		this.#state.update({
			definition: newDefinition,
			persistedDefinition: null, // null = new, not yet saved
			loading: false,
			isNew: true,
			error: undefined,
		});
	}

	getIsBackOffice(): boolean {
		return this.getPolicyType() === CspConstants.policyTypes.backoffice;
	}

	async loadDefinition() {
		this.#state.update({ loading: true, error: undefined });
		this.#allowNavigateAway = false;

		let result: Awaited<ReturnType<UmbCspDefinitionContext['load']>>;

		if (this.isDomainPolicy() && this.#policyId) {
			const { data: policies, error } = await this.#cspDefinitionContext.getDomainPolicies();
			if (error) {
				this.#state.update({ loading: false, error });
				return;
			}
			const policy = policies?.find((p) => p.id === this.#policyId);
			if (policy && policy.domainKey) {
				const { data, error: loadError } = await this.#cspDefinitionContext.loadByDomainKey(policy.domainKey);
				if (loadError) {
					this.#state.update({ loading: false, error: loadError });
					return;
				}
				if (data) {
					this.#state.update({
						definition: data,
						persistedDefinition: structuredClone(data),
						loading: false,
						error: undefined,
					});
				} else {
					this.#state.update({ loading: false, error: undefined });
				}
			} else {
				this.#state.update({ loading: false, error: undefined });
			}
			return;
		}

		const isBackOffice = this.getIsBackOffice();
		result = await this.#cspDefinitionContext.load(isBackOffice);

		const { data, error } = result;

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
		const set = new Set<string>(definition.sources.map((s) => s.source));
		let duplicates: string[] = [];
		set.forEach((name) => {
			const matches = definition.sources.filter((s) => s.source === name);
			if (matches.length > 1) {
				duplicates.push(name);
			}
		});

		if (duplicates.length > 0) {
			return new UmbError('Duplicate source names found', { cause: duplicates });
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

		const { error } = await this.#cspDefinitionContext.save(currentState.definition);

		if (error) {
			return { success: false, error };
		}

		// Update persisted to match current after successful save
		this.#state.update({
			persistedDefinition: structuredClone(currentState.definition),
			error: undefined,
		});

		// If this was a new domain policy, refresh the tree and navigate to the edit route
		if (currentState.isNew && currentState.definition.id) {
			this.#policyId = currentState.definition.id;
			this.#state.update({ isNew: false });
			this.#allowNavigateAway = true;

			const actionEventContext = await this.getContext(UMB_ACTION_EVENT_CONTEXT);
			actionEventContext?.dispatchEvent(
				new UmbRequestReloadChildrenOfEntityEvent({
					entityType: CspConstants.workspace.entityType,
					unique: CspConstants.policyTypes.frontend.value,
				}),
			);

			history.pushState(
				{},
				'',
				`section/csp-manager/workspace/csp-policy/edit/${currentState.definition.id}`,
			);
		}

		return { success: true };
	}

	async deleteDomainPolicy(): Promise<{ success: boolean; error?: Error }> {
		const id = this.#policyId;
		if (!id || !this.isDomainPolicy()) {
			return { success: false, error: new Error('Not a domain policy') };
		}

		const { error } = await this.#cspDefinitionContext.deleteDomainPolicy(id);
		if (error) {
			return { success: false, error: error as Error };
		}

		return { success: true };
	}

	getDefinition(): CspApiDefinition | null {
		return this.#state.getValue().definition;
	}

	isLoading(): boolean {
		return this.#state.getValue().loading;
	}

	/**
	 * Check if there are unsaved changes by comparing current and persisted definitions.
	 * New (unsaved) domain policies always return true.
	 */
	hasUnsavedChanges(): boolean {
		const state = this.#state.getValue();
		if (state.isNew) return true;
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

	override destroy(): void {
		window.removeEventListener('willchangestate', this.#onWillNavigate);
		super.destroy();
	}
}

export const UMB_CSP_MANAGER_WORKSPACE_CONTEXT = new UmbContextToken<UmbCspManagerWorkspaceContext>(
	'UmbWorkspaceContext',
	'csp-manager.workspace'
);

export { UmbCspManagerWorkspaceContext as api };
export default UmbCspManagerWorkspaceContext;
