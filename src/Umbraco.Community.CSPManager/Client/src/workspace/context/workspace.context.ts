import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UMB_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/workspace";
import type { UmbWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import type { CspDefinition } from '@/api';
import { UmbCspDefinitionContext, UmbCspDirectivesContext } from '@/contexts/index';
import type { UmbApiError, UmbCancelError, UmbError } from '@umbraco-cms/backoffice/resources';
import { CspConstants, type PolicyType } from '@/constants';

export interface WorkspaceState {
	definition: CspDefinition | null;
	availableDirectives: string[];
	loading: boolean;
	hasChanges: boolean;
	error?: UmbError | UmbApiError | UmbCancelError | Error | undefined;
}

const URL_TO_POLICY_TYPE = {
	frontend: CspConstants.policyTypes.frontend,
	backoffice: CspConstants.policyTypes.backoffice,
} as const;

export class UmbCspManagerWorkspaceContext extends UmbControllerBase implements UmbWorkspaceContext {
	public readonly workspaceAlias: string;

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
		const policyType = this.getPolicyType();
		return policyType.value;
	}

	getPolicyType(): PolicyType {
		try {
			const url = window.location.pathname;
			const matches = url.match(/\/umbraco\/section\/csp-manager\/workspace\/(frontend|backoffice)/);

			if (matches && matches[1]) {
				const urlSegment = matches[1] as keyof typeof URL_TO_POLICY_TYPE;
				return URL_TO_POLICY_TYPE[urlSegment] || CspConstants.policyTypes.frontend;
			}

			console.warn('Invalid CSP policy type in URL, defaulting to frontend');
			return CspConstants.policyTypes.frontend;
		} catch (error) {
			console.error('Error parsing policy type from URL:', error);
			return CspConstants.policyTypes.frontend;
		}
	}

	constructor(host: UmbControllerHost) {
		super(host);

		// Set workspace alias based on current URL
		const policyType = this.getPolicyType();
		this.workspaceAlias = `${CspConstants.workspace.alias}.${policyType.aliasPart}`;

		this.#cspDefinitionContext = new UmbCspDefinitionContext(this);
		this.#cspDirectivesContext = new UmbCspDirectivesContext(this);

		this.provideContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, this);
		this.provideContext(UMB_WORKSPACE_CONTEXT, this);

		this.loadDefinition();
		this.loadDirectives();
	}

	getIsBackOffice(): boolean {
		return this.getPolicyType() === CspConstants.policyTypes.backoffice;
	}

	async loadDefinition() {
		this.#state.update({ loading: true });
		const isBackOffice = this.getIsBackOffice();
		const { data, error } = await this.#cspDefinitionContext.load(isBackOffice);

		if (error) {
			this.#state.update({ loading: false, error });
		} else if (data) {
			this.#state.update({
				definition: data,
				loading: false,
				hasChanges: false,
			});
		} else {
			this.#state.update({ loading: false });
		}
	}

	async loadDirectives() {
		const { data, error } = await this.#cspDirectivesContext.load();

		if (error) {
			this.#state.update({ error });
		} else if (data) {
			this.#state.update({ availableDirectives: data });
		}
	}

	updateDefinition(definition: CspDefinition) {
		this.#state.update({
			definition,
			hasChanges: true,
		});
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

		this.#state.update({ hasChanges: false });
		await this.loadDefinition();

		return { success: true };
	}

	getDefinition(): CspDefinition | null {
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

export const UMB_CSP_MANAGER_WORKSPACE_CONTEXT =
  new UmbContextToken<UmbCspManagerWorkspaceContext>(
    "UmbCspManagerWorkspaceContext"
  );

export { UmbCspManagerWorkspaceContext as api };
export default UmbCspManagerWorkspaceContext;
