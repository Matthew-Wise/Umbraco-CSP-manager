import { LitElement, css, html, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import type { UmbNotificationContext } from '@umbraco-cms/backoffice/notification';
import { UmbCspManagerWorkspaceContext, type WorkspaceState } from './context/workspace.context.js';
import { CspConstants, type PolicyType } from '@/constants';

@customElement('umb-csp-management-workspace')
export class UmbCspManagementWorkspaceElement extends UmbElementMixin(LitElement) {
	#workspaceContext: UmbCspManagerWorkspaceContext;
	#notificationContext?: UmbNotificationContext;

	@state()
	private _policyType: PolicyType = CspConstants.policyTypes.frontend;

	@state()
	private _workspaceState: WorkspaceState = {
		definition: null,
		availableDirectives: [],
		loading: true,
		hasChanges: false,
	};

	@state()
	private _saving: boolean = false;

	constructor() {
		super();
		this.#workspaceContext = new UmbCspManagerWorkspaceContext(this);

		this.observe(this.#workspaceContext.state, (state) => {
			this._workspaceState = state;
		});

		this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
			this.#notificationContext = context;
		});
	}

	connectedCallback() {
		super.connectedCallback();
		this._policyType = this.#workspaceContext.getPolicyType();
	}

	private async _handleSave() {
		if (this._saving) return;

		this._saving = true;

		try {
			const result = await this.#workspaceContext.save();

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
				alias="${this.#workspaceContext.workspaceAlias}">
				<div slot="actions">
					<uui-button
						label="Save"
						look="primary"
						color="positive"
						.disabled=${this._saving || !this._workspaceState.hasChanges}
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
