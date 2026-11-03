import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { CspApiDefinition } from '@/api';
import {
	UmbCspManagerWorkspaceContext,
	UMB_CSP_MANAGER_WORKSPACE_CONTEXT,
	type WorkspaceState,
} from '../../context/workspace.context.js';
import { CspConstants, type PolicyType } from '@/constants';
import { domainPolicyLabel } from '@/domain-policy-label';

@customElement('umb-csp-settings-view')
export class UmbCspSettingsViewElement extends UmbLitElement {
	@state()
	private _workspaceState: WorkspaceState = {
		definition: null,
		persistedDefinition: null,
		availableDirectives: [],
		loading: true,
	};

	@state()
	private _policyType: PolicyType = CspConstants.policyTypes.frontend;

	@state()
	private _isDomainPolicy = false;

	private _workspaceContext?: UmbCspManagerWorkspaceContext;

	constructor() {
		super();

		this.consumeContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, (context) => {
			this._workspaceContext = context;
			this._policyType = context?.getPolicyType() || CspConstants.policyTypes.frontend;

			if (context) {
				this.observe(context.state, (state) => {
					this._workspaceState = state;
					this._isDomainPolicy = context.isDomainPolicy();
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

	private _scopeLabel(): string {
		if (this._isDomainPolicy) {
			return domainPolicyLabel(this.localize, this._workspaceState.definition ?? {});
		}

		return this._policyType === CspConstants.policyTypes.backoffice ? 'back office' : 'frontend';
	}

	private get _disabledMeansNoHeader(): boolean {
		return (
			this._workspaceState.definition?.disabledNodePolicyBehavior ===
			CspConstants.domainPolicy.disabledBehavior.noHeader
		);
	}

	// A domain policy is a switch between its own policy and the Frontend one (or, when the server is
	// configured with DisabledNodePolicyBehavior NoHeader, no header). Text bindings only: the
	// domain name is user content.
	private _renderDomainStatusSummary(definition: CspApiDefinition) {
		const domain = this._scopeLabel();
		if (!definition.enabled) {
			return this._disabledMeansNoHeader
				? this.localize.term('cspManagerDomainPolicy_summaryNoHeader', domain)
				: this.localize.term('cspManagerDomainPolicy_summaryFallback', domain);
		}

		return definition.reportOnly
			? this.localize.term('cspManagerDomainPolicy_summaryActiveReportOnly', domain)
			: this.localize.term('cspManagerDomainPolicy_summaryActive', domain);
	}

	private _renderGlobalStatusSummary(definition: CspApiDefinition) {
		return html`CSP is currently <strong>${definition.enabled ? 'enabled' : 'disabled'}</strong>${definition.enabled &&
			definition.reportOnly
				? ' and running in report-only mode'
				: ''}${definition.enabled && !definition.reportOnly ? ' and actively enforcing policies' : ''}.`;
	}

	private _statusToggleText(enabled: boolean): string {
		if (!this._isDomainPolicy) {
			return enabled ? 'Enabled' : 'Disabled';
		}

		if (enabled) {
			return this.localize.term('cspManagerDomainPolicy_statusActive');
		}

		return this._disabledMeansNoHeader
			? this.localize.term('cspManagerDomainPolicy_statusInactiveNoHeader')
			: this.localize.term('cspManagerDomainPolicy_statusInactiveFallback');
	}

	private _hasReportingDirective(definition: CspApiDefinition): boolean {
		return !!definition.reportingDirective && definition.reportingDirective !== 'none';
	}

	render() {
		if (this._workspaceState.loading) {
			return html`<uui-loader></uui-loader>`;
		}

		if (!this._workspaceState.definition) {
			return html`<div>${this._workspaceState.error?.message ?? this.localize.term('cspManager_noDefinition')}</div>`;
		}

		const definition = this._workspaceState.definition;

		return html`
			<uui-box headline="Settings">
				<div class="settings-intro">
					<p>
						Configure the Content Security Policy settings for
						<strong>${this._scopeLabel()}</strong>
						content.
					</p>
					<div class="status-summary" data-mark="csp-status-summary">
						<p>
							${this._isDomainPolicy
								? this._renderDomainStatusSummary(definition)
								: this._renderGlobalStatusSummary(definition)}
						</p>
					</div>
				</div>

				<div class="settings-grid">
					<uui-form-layout-item>
						<uui-label slot="label">
							${this._isDomainPolicy ? this.localize.term('cspManagerDomainPolicy_statusLabel') : 'CSP Status'}
						</uui-label>
						${this._isDomainPolicy
							? ''
							: html`<span slot="description">Enable or disable the Content Security Policy header</span>`}
						<div class="setting-control">
							<uui-toggle
								data-mark="csp-status-toggle"
								label=${this._statusToggleText(definition.enabled)}
								.checked=${definition.enabled}
								@change=${(e: Event) =>
									this._updateDefinitionSetting('enabled', (e.target as HTMLInputElement).checked)}>
								${this._statusToggleText(definition.enabled)}
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
								.checked=${definition.reportOnly}
								.disabled=${!definition.enabled}
								@change=${(e: Event) =>
									this._updateDefinitionSetting('reportOnly', (e.target as HTMLInputElement).checked)}>
								${definition.reportOnly ? 'Report Only' : 'Enforced'}
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
								.value=${definition.reportingDirective || 'none'}
								@change=${(e: Event) => {
									const value = (e.target as HTMLInputElement).value;
									this._updateDefinitionSetting('reportingDirective', value === 'none' ? null : value);
								}}>
								<uui-radio value="none" label="No reporting"></uui-radio>
								<uui-radio value="report-to" label="report-to (recommended)"></uui-radio>
								<uui-radio value="report-uri" label="report-uri (deprecated)"></uui-radio>
							</uui-radio-group>
						</div>
					</uui-form-layout-item>

					${this._hasReportingDirective(definition)
						? html`
								<uui-form-layout-item>
									<uui-label slot="label">Report URI</uui-label>
									<span slot="description"> The endpoint where violation reports will be sent </span>
									<div class="setting-control">
										<uui-input
											label="Report URI"
											.value=${definition.reportUri || ''}
											placeholder="https://example.com/csp-report"
											@input=${(e: Event) =>
												this._updateDefinitionSetting('reportUri', (e.target as HTMLInputElement).value)}>
										</uui-input>
									</div>
								</uui-form-layout-item>
							`
						: ''}
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