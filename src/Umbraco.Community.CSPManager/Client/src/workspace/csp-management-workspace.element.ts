import { css, html, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import type { UmbNotificationContext } from '@umbraco-cms/backoffice/notification';
import { UMB_CSP_MANAGER_WORKSPACE_CONTEXT, type WorkspaceState } from './context/workspace.context.js';
import { CspConstants, type PolicyType } from '@/constants';

@customElement('umb-csp-management-workspace')
export class UmbCspManagementWorkspaceElement extends UmbLitElement {
	#notificationContext?: UmbNotificationContext;

	@state()
	private _policyType: PolicyType = CspConstants.policyTypes.frontend;

	@state()
	private _workspaceState: WorkspaceState = {
		definition: null,
		persistedDefinition: null,
		availableDirectives: [],
		loading: true,
	};

	@state()
	private _saving: boolean = false;

	@state()
	private _hasChanges: boolean = false;

	constructor() {
		super();

		this.consumeContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, (context) => {
			if (!context) return;

			this._policyType = context.getPolicyType();

			this.observe(context.state, (state) => {
				this._workspaceState = state;
				this._hasChanges = context.hasUnsavedChanges();
			});
		});

		this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
			this.#notificationContext = context;
		});
	}

	private async _handleSave() {
		if (this._saving) return;

		this._saving = true;

		try {
			const context = await this.getContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT);
			if (!context) {
				throw new Error('Workspace context not available');
			}
			const result = await context.save();

			if (result.success) {
				this.#notificationContext?.peek('positive', {
					data: {
						headline: 'Changes Saved',
						message: `CSP ${this._policyType.label} configuration has been saved successfully.`,
					},
				});
			} else {
				this.#notificationContext?.peek('danger', {
					data: {
						headline: 'Save Failed',
						message: result.error?.message || 'An error occurred while saving the CSP configuration.',
					},
				});
			}
		} catch (error) {
			this.#notificationContext?.peek('danger', {
				data: {
					headline: 'Save Failed',
					message: 'An unexpected error occurred while saving the CSP configuration.',
				},
			});
		} finally {
			this._saving = false;
		}
	}

	render() {
		return html`
			<umb-workspace-editor
				headline="${this._policyType.label} CSP Management"
				alias="${CspConstants.workspace.alias}">
				<div slot="actions">
					<uui-button
						label="Save"
						look="primary"
						color="positive"
						.disabled=${this._saving || !this._hasChanges || this._workspaceState.error !== undefined}
						@click=${this._handleSave}>
						${this._saving ? 'Saving...' : 'Save'}
					</uui-button>
				</div>
			</umb-workspace-editor>
		`;
	}

	static override styles = [
		css`
			:host {
				display: block;
				height: 100%;
			}
		`,
	];
}

export default UmbCspManagementWorkspaceElement;

declare global {
	interface HTMLElementTagNameMap {
		'umb-csp-management-workspace': UmbCspManagementWorkspaceElement;
	}
}
