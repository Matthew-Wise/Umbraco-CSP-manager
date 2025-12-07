import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import { UMB_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/workspace';
import type { UmbWorkspaceContext, UmbRoutableWorkspaceContext } from '@umbraco-cms/backoffice/workspace';
import { UmbWorkspaceRouteManager } from '@umbraco-cms/backoffice/workspace';
import { UmbObjectState } from '@umbraco-cms/backoffice/observable-api';
import type { CspApiDefinition } from '@/api';
import { UmbCspDefinitionContext, UmbCspDirectivesContext } from '@/contexts/index';
import { UmbError, type UmbApiError, type UmbCancelError } from '@umbraco-cms/backoffice/resources';
import { CspConstants, type PolicyType } from '@/constants';

export interface WorkspaceState {
	definition: CspApiDefinition | null;
	availableDirectives: string[];
	loading: boolean;
	hasChanges: boolean;
	error?: UmbError | UmbApiError | UmbCancelError | Error | undefined;
}

const ID_TO_POLICY_TYPE: Record<string, PolicyType> = {
	frontend: CspConstants.policyTypes.frontend,
	backoffice: CspConstants.policyTypes.backoffice,
};

export class UmbCspManagerWorkspaceContext
	extends UmbControllerBase
	implements UmbWorkspaceContext, UmbRoutableWorkspaceContext
{
	public readonly workspaceAlias = CspConstants.workspace.alias;
	public readonly routes = new UmbWorkspaceRouteManager(this);

	#policyId: string | null = null;

	#state = new UmbObjectState<WorkspaceState>({
		definition: null,
		availableDirectives: [],
		loading: true,
		hasChanges: false,
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
		console.warn('Invalid CSP policy ID, defaulting to frontend');
		return CspConstants.policyTypes.frontend;
	}

	constructor(host: UmbControllerHost) {
		super(host);

		this.#cspDefinitionContext = new UmbCspDefinitionContext(this);
		this.#cspDirectivesContext = new UmbCspDirectivesContext(this);

		this.provideContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, this);
		this.provideContext(UMB_WORKSPACE_CONTEXT, this);

		this.routes.setRoutes([
			{
				path: 'edit/:unique',
				component: () => import('../csp-management-workspace.element.js'),
				setup: (_component, info) => {
					const unique = info.match.params.unique;
					this.#load(unique);
				},
			},
		]);
	}

	#load(unique: string) {
		this.#policyId = unique;
		this.loadDefinition();
		this.loadDirectives();
	}

	getIsBackOffice(): boolean {
		return this.getPolicyType() === CspConstants.policyTypes.backoffice;
	}

	async loadDefinition() {
		this.#state.update({ loading: true, error: undefined });
		const isBackOffice = this.getIsBackOffice();
		const { data, error } = await this.#cspDefinitionContext.load(isBackOffice);

		if (error) {
			this.#state.update({ loading: false, error });
		} else if (data) {
			this.#state.update({
				definition: data,
				loading: false,
				hasChanges: false,
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
			this.#state.update({ definition, hasChanges: true, error });
		} else {
			this.#state.update({
				definition,
				hasChanges: true,
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

		this.#state.update({ hasChanges: false, error: undefined });
		await this.loadDefinition();

		return { success: true };
	}

	getDefinition(): CspApiDefinition | null {
		return this.#state.getValue().definition;
	}

	isLoading(): boolean {
		return this.#state.getValue().loading;
	}

	hasUnsavedChanges(): boolean {
		return this.#state.getValue().hasChanges;
	}

	getAvailableDirectives(): string[] {
		return this.#state.getValue().availableDirectives;
	}
}

export const UMB_CSP_MANAGER_WORKSPACE_CONTEXT = new UmbContextToken<UmbCspManagerWorkspaceContext>(
	'UmbWorkspaceContext',
	'csp-manager.workspace'
);

export { UmbCspManagerWorkspaceContext as api };
export default UmbCspManagerWorkspaceContext;
