import { css, html, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import type { UmbNotificationContext } from '@umbraco-cms/backoffice/notification';
import { umbConfirmModal } from '@umbraco-cms/backoffice/modal';
import { UMB_CSP_MANAGER_WORKSPACE_CONTEXT, type WorkspaceState } from './context/workspace.context.js';
import { CspConstants, type PolicyType } from '@/constants';
import { nodePolicyLabel } from '@/node-policy-label';
import { cspEditPolicyPath } from '@/paths';

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
	private _deleting: boolean = false;

	@state()
	private _isNodePolicy: boolean = false;

	@state()
	private _hasChanges: boolean = false;

	constructor() {
		super();

		this.consumeContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, (context) => {
			if (!context) return;

			this._policyType = context.getPolicyType();

			this.observe(context.state, (state) => {
				this._workspaceState = state;
				this._isNodePolicy = context.isNodePolicy();
				this._hasChanges = context.hasUnsavedChanges();
			});
		});

		this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
			this.#notificationContext = context;
		});
	}

	/** The content node's name (or a "deleted content" label) and the policy's culture. Rendered as text only. */
	private get _nodeLabel(): string {
		return nodePolicyLabel(this.localize, this._workspaceState.definition ?? {});
	}

	private get _headline(): string {
		return this._isNodePolicy
			? this.localize.term('cspManagerNodePolicy_workspaceHeadline', this._nodeLabel)
			: `${this._policyType.label} CSP Management`;
	}

	private async _handleDelete() {
		if (this._deleting) return;

		const confirmed = await umbConfirmModal(this, {
			headline: this.localize.term('cspManagerNodePolicy_deleteConfirmHeadline'),
			// A TemplateResult, not a string: umbConfirmModal renders string content as HTML, and the
			// content name is user content. The text binding escapes it.
			content: html`${this.localize.term('cspManagerNodePolicy_deleteConfirmContent', this._nodeLabel)}`,
			color: 'danger',
			confirmLabel: this.localize.term('cspManagerNodePolicy_deleteAction'),
		})
			.then(() => true)
			.catch(() => false);
		if (!confirmed) return;

		this._deleting = true;
		const nodeLabel = this._nodeLabel;

		try {
			const context = await this.getContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT);
			const result = await context!.deleteNodePolicy();

			if (result.success) {
				this.#notificationContext?.peek('positive', {
					data: {
						headline: this.localize.term('cspManagerNodePolicy_deletedHeadline'),
						message: this.localize.term('cspManagerNodePolicy_deletedMessage', nodeLabel),
					},
				});
				history.pushState(null, '', cspEditPolicyPath(CspConstants.policyTypes.frontend.value));
			} else {
				this.#notificationContext?.peek('danger', {
					data: {
						headline: this.localize.term('cspManagerNodePolicy_deleteFailedHeadline'),
						message: result.error?.message || this.localize.term('cspManagerNodePolicy_deleteFailedMessage'),
					},
				});
			}
		} finally {
			this._deleting = false;
		}
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
						message: `CSP ${this._isNodePolicy ? this._nodeLabel : this._policyType.label} configuration has been saved successfully.`,
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
				headline=${this._headline}
				alias="${CspConstants.workspace.alias}">
				<div slot="actions">
					${this._isNodePolicy && !this._workspaceState.isNew && this._workspaceState.definition
						? html`
								<uui-button
									label=${this.localize.term('cspManagerNodePolicy_deleteAction')}
									look="secondary"
									color="danger"
									.disabled=${this._deleting || this._saving}
									@click=${this._handleDelete}>
									${this._deleting
										? this.localize.term('cspManagerNodePolicy_deleting')
										: this.localize.term('cspManagerNodePolicy_deleteAction')}
								</uui-button>
							`
						: ''}
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
