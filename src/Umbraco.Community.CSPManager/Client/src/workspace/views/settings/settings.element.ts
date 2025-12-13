import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { CspApiDefinition } from '@/api';
import {
	UmbCspManagerWorkspaceContext,
	UMB_CSP_MANAGER_WORKSPACE_CONTEXT,
	type WorkspaceState,
} from '../../context/workspace.context.js';
import { CspConstants, type PolicyType } from '@/constants';

@customElement('umb-csp-settings-view')
export class UmbCspSettingsViewElement extends UmbLitElement {
	@state()
	private _workspaceState: WorkspaceState = {
		definition: null,
		availableDirectives: [],
		loading: true,
		hasChanges: false,
	};

	@state()
	private _policyType: PolicyType = CspConstants.policyTypes.frontend;

	private _workspaceContext?: UmbCspManagerWorkspaceContext;

	constructor() {
		super();

		this.consumeContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, (context) => {
			this._workspaceContext = context;
			this._policyType = context?.getPolicyType() || CspConstants.policyTypes.frontend;

			if (context) {
				this.observe(context.state, (state) => {
					this._workspaceState = state;
				});
			}
		});
	}

	private _updateDefinitionSetting<K extends keyof CspApiDefinition>(field: K, value: CspApiDefinition[K]) {
		if (!this._workspaceState.definition) return;

		const updatedDefinition = {
			...this._workspaceState.definition,
			[field]: value,
		};

		this._workspaceContext?.updateDefinition(updatedDefinition);
	}

	render() {
		if (this._workspaceState.loading) {
			return html`<uui-loader></uui-loader>`;
		}

		if (!this._workspaceState.definition) {
			return html`<div>No CSP definition available</div>`;
		}

		return html`
			<uui-box headline="Settings">
				<div class="settings-intro">
					<p>
						Configure the Content Security Policy settings for
						<strong>${this._policyType === CspConstants.policyTypes.backoffice ? 'back office' : 'frontend'}</strong>
						content.
					</p>
					<div class="status-summary">
						<p>
							CSP is currently <strong>${this._workspaceState.definition.enabled ? 'enabled' : 'disabled'}</strong>
							${this._workspaceState.definition.enabled && this._workspaceState.definition.reportOnly
								? ' and running in report-only mode'
								: ''}
							${this._workspaceState.definition.enabled && !this._workspaceState.definition.reportOnly
								? ' and actively enforcing policies'
								: ''}.
						</p>
					</div>
				</div>

				<div class="settings-grid">
					<uui-form-layout-item>
						<uui-label slot="label">CSP Status</uui-label>
						<span slot="description">Enable or disable the Content Security Policy header</span>
						<div class="setting-control">
							<uui-toggle
								label="${this._workspaceState.definition.enabled ? 'Enabled' : 'Disabled'}"
								.checked=${this._workspaceState.definition.enabled}
								@change=${(e: Event) =>
									this._updateDefinitionSetting('enabled', (e.target as HTMLInputElement).checked)}>
								${this._workspaceState.definition.enabled ? 'Enabled' : 'Disabled'}
							</uui-toggle>
						</div>
					</uui-form-layout-item>

					<uui-form-layout-item>
						<uui-label slot="label">Report Only Mode</uui-label>
						<span slot="description">
							When enabled, violations are reported but not blocked. Use this to test policies before enforcing them.
						</span>
						<div class="setting-control">
							<uui-toggle
								label="Report Only Mode"
								.checked=${this._workspaceState.definition.reportOnly}
								.disabled=${!this._workspaceState.definition.enabled}
								@change=${(e: Event) =>
									this._updateDefinitionSetting('reportOnly', (e.target as HTMLInputElement).checked)}>
								${this._workspaceState.definition.reportOnly ? 'Report Only' : 'Enforced'}
							</uui-toggle>
						</div>
					</uui-form-layout-item>

					<uui-form-layout-item>
						<uui-label slot="label">Reporting Directive</uui-label>
						<span slot="description">
							Choose how CSP violations are reported.
							<a
								href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy/report-to"
								target="_blank">
								Learn more about CSP reporting
							</a>
						</span>
						<div class="setting-control">
							<uui-radio-group
								.value=${this._workspaceState.definition.reportingDirective || 'none'}
								@change=${(e: Event) =>
									this._updateDefinitionSetting('reportingDirective', (e.target as HTMLInputElement).value)}>
								<uui-radio value="none" label="No reporting"></uui-radio>
								<uui-radio value="report-to" label="report-to (recommended)"></uui-radio>
								<uui-radio value="report-uri" label="report-uri (deprecated)"></uui-radio>
							</uui-radio-group>
						</div>
					</uui-form-layout-item>

					<uui-form-layout-item>
						<uui-label slot="label">Report URI</uui-label>
						<span slot="description"> The endpoint where violation reports will be sent </span>
						<div class="setting-control">
							<uui-input
								label="Report URI"
								.value=${this._workspaceState.definition.reportUri || ''}
								placeholder="https://example.com/csp-report"
								.disabled=${!this._workspaceState.definition.reportingDirective ||
								this._workspaceState.definition.reportingDirective === 'none'}
								@input=${(e: Event) =>
									this._updateDefinitionSetting('reportUri', (e.target as HTMLInputElement).value)}>
							</uui-input>
						</div>
					</uui-form-layout-item>
				</div>

				<div class="settings-info">
					<uui-box headline="About Content Security Policy" look="placeholder">
						<p>
							Content Security Policy (CSP) helps prevent cross-site scripting (XSS), clickjacking, and other code
							injection attacks by controlling which resources can be loaded and executed.
						</p>
						<p>
							<strong>Best Practices:</strong>
						</p>
						<ul>
							<li>Start with report-only mode to test your policies</li>
							<li>Use 'self' for trusted first-party content</li>
							<li>Avoid 'unsafe-inline' and 'unsafe-eval' when possible</li>
							<li>Regularly review and update your CSP sources</li>
						</ul>
					</uui-box>
				</div>
			</uui-box>
		`;
	}

	static styles = [
		css`
			:host {
				display: block;
				padding: var(--uui-size-layout-1);
			}

			.settings-intro {
				margin-bottom: var(--uui-size-space-6);
			}

			.status-summary {
				padding: var(--uui-size-space-4);
				background-color: var(--uui-color-surface-alt);
				border-radius: var(--uui-border-radius);
				margin-top: var(--uui-size-space-3);
			}

			.status-summary p {
				margin: 0;
				font-weight: 500;
			}

			.settings-grid {
				display: grid;
				gap: var(--uui-size-space-5);
				margin-bottom: var(--uui-size-space-6);
			}

			.setting-control {
				margin-top: var(--uui-size-space-2);
			}

			.settings-info {
				margin-top: var(--uui-size-space-6);
			}

			.settings-info ul {
				margin: var(--uui-size-space-3) 0 0 var(--uui-size-space-5);
				padding: 0;
			}

			.settings-info li {
				margin-bottom: var(--uui-size-space-2);
			}

			a {
				color: var(--uui-color-current);
				text-decoration: none;
			}

			a:hover {
				text-decoration: underline;
			}
		`,
	];
}

export default UmbCspSettingsViewElement;

declare global {
  interface HTMLElementTagNameMap {
    'umb-csp-settings-view': UmbCspSettingsViewElement;
  }
}