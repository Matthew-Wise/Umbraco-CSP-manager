import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { CspPolicyInheritance, CspPolicyScope, DisabledNodePolicyBehavior, type CspApiDefinition } from '@/api';
import { hasReportingDirective } from '@/utils/csp-string';
import {
	UmbCspManagerWorkspaceContext,
	UMB_CSP_MANAGER_WORKSPACE_CONTEXT,
	type WorkspaceState,
} from '../../context/workspace.context.js';
import { CspConstants, type PolicyType } from '@/constants';
import { nodePolicyLabel } from '@/node-policy-label';

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
	private _isNodePolicy = false;

	/** An "inherit and add" policy takes everything but its sources and its switch from the policy it adds to. */
	@state()
	private _isExtending = false;

	private _workspaceContext?: UmbCspManagerWorkspaceContext;

	constructor() {
		super();

		this.consumeContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, (context) => {
			this._workspaceContext = context;
			this._policyType = context?.getPolicyType() || CspConstants.policyTypes.frontend;

			if (context) {
				this.observe(context.state, (state) => {
					this._workspaceState = state;
					this._isNodePolicy = context.isNodePolicy();
					this._isExtending = context.isExtending();
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
		if (this._isNodePolicy) {
			return nodePolicyLabel(this.localize, this._workspaceState.definition ?? {});
		}

		return this._policyType === CspConstants.policyTypes.backoffice ? 'back office' : 'frontend';
	}

	private get _disabledMeansNoHeader(): boolean {
		return this._workspaceState.definition?.disabledNodePolicyBehavior === DisabledNodePolicyBehavior.NO_HEADER;
	}

	// A node policy is a switch between itself and what its content inherits (or, for a complete
	// policy when the server is configured with DisabledNodePolicyBehavior NoHeader, no header).
	// Text bindings only: the content name is user content.
	private _renderNodeStatusSummary(definition: CspApiDefinition) {
		const extending = this._isExtending;
		const label = this._scopeLabel();
		const reach =
			definition.appliesTo === CspPolicyScope.NODE_ONLY
				? label
				: this.localize.term('cspManagerNodePolicy_scopeDescendants', label);

		if (!definition.enabled) {
			// Switching an extending policy off only takes its sources away again.
			return this._disabledMeansNoHeader && !extending
				? this.localize.term('cspManagerNodePolicy_summaryNoHeader', reach)
				: this.localize.term('cspManagerNodePolicy_summaryFallback', reach);
		}

		if (extending) {
			return this.localize.term('cspManagerNodePolicy_summaryExtendActive', reach);
		}

		return definition.reportOnly
			? this.localize.term('cspManagerNodePolicy_summaryActiveReportOnly', reach)
			: this.localize.term('cspManagerNodePolicy_summaryActive', reach);
	}

	private _renderGlobalStatusSummary(definition: CspApiDefinition) {
		return html`CSP is currently <strong>${definition.enabled ? 'enabled' : 'disabled'}</strong>${definition.enabled &&
			definition.reportOnly
				? ' and running in report-only mode'
				: ''}${definition.enabled && !definition.reportOnly ? ' and actively enforcing policies' : ''}.`;
	}

	private _statusToggleText(enabled: boolean): string {
		if (!this._isNodePolicy) {
			return enabled ? 'Enabled' : 'Disabled';
		}

		if (enabled) {
			return this.localize.term('cspManagerNodePolicy_statusActive');
		}

		return this._disabledMeansNoHeader && !this._isExtending
			? this.localize.term('cspManagerNodePolicy_statusInactiveNoHeader')
			: this.localize.term('cspManagerNodePolicy_statusInactiveFallback');
	}

	// What kind of policy this is and how far it reaches. Its content and culture can't change.
	private _renderNodePolicyOptions(definition: CspApiDefinition) {
		return html`
			<uui-form-layout-item>
				<uui-label slot="label">${this.localize.term('cspManagerNodePolicy_inheritanceLabel')}</uui-label>
				<span slot="description">
					${this.localize.term(
						this._isExtending
							? 'cspManagerNodePolicy_inheritanceExtendDescription'
							: 'cspManagerNodePolicy_inheritanceReplaceDescription',
					)}
				</span>
				<div class="setting-control">
					<uui-radio-group
						data-mark="csp-inheritance"
						.value=${definition.inheritance}
						@change=${(e: Event) =>
							this._updateDefinitionSetting('inheritance', (e.target as HTMLInputElement).value as CspPolicyInheritance)}>
						<uui-radio
							value=${CspPolicyInheritance.REPLACE}
							label=${this.localize.term('cspManagerNodePolicy_inheritanceReplace')}></uui-radio>
						<uui-radio
							value=${CspPolicyInheritance.EXTEND}
							label=${this.localize.term('cspManagerNodePolicy_inheritanceExtend')}></uui-radio>
					</uui-radio-group>
				</div>
			</uui-form-layout-item>

			<uui-form-layout-item>
				<uui-label slot="label">${this.localize.term('cspManagerNodePolicy_appliesToLabel')}</uui-label>
				<span slot="description">${this.localize.term('cspManagerNodePolicy_appliesToDescription')}</span>
				<div class="setting-control">
					<uui-radio-group
						data-mark="csp-applies-to"
						.value=${definition.appliesTo}
						@change=${(e: Event) =>
							this._updateDefinitionSetting('appliesTo', (e.target as HTMLInputElement).value as CspPolicyScope)}>
						<uui-radio
							value=${CspPolicyScope.DESCENDANTS}
							label=${this.localize.term('cspManagerNodePolicy_appliesToDescendants')}></uui-radio>
						<uui-radio
							value=${CspPolicyScope.NODE_ONLY}
							label=${this.localize.term('cspManagerNodePolicy_appliesToNodeOnly')}></uui-radio>
					</uui-radio-group>
				</div>
			</uui-form-layout-item>
		`;
	}

	// Report-only mode and reporting belong to a complete policy: an extending one takes them from
	// the policy it adds to, so there is nothing to set.
	private _renderReportingSettings(definition: CspApiDefinition) {
		if (this._isExtending) {
			return html`<p class="extend-info" data-mark="csp-extend-settings-info">
				${this.localize.term('cspManagerNodePolicy_extendSettingsInfo')}
			</p>`;
		}

		return html`
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
						@change=${(e: Event) => this._updateDefinitionSetting('reportOnly', (e.target as HTMLInputElement).checked)}>
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

			${hasReportingDirective(definition)
				? html`
						<uui-form-layout-item>
							<uui-label slot="label">Report URI</uui-label>
							<span slot="description"> The endpoint where violation reports will be sent </span>
							<div class="setting-control">
								<uui-input
									label="Report URI"
									.value=${definition.reportUri || ''}
									placeholder="https://example.com/csp-report"
									@input=${(e: Event) => this._updateDefinitionSetting('reportUri', (e.target as HTMLInputElement).value)}>
								</uui-input>
							</div>
						</uui-form-layout-item>
					`
				: ''}
		`;
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
							${this._isNodePolicy
								? this._renderNodeStatusSummary(definition)
								: this._renderGlobalStatusSummary(definition)}
						</p>
					</div>
				</div>

				<div class="settings-grid">
					<uui-form-layout-item>
						<uui-label slot="label">
							${this._isNodePolicy ? this.localize.term('cspManagerNodePolicy_statusLabel') : 'CSP Status'}
						</uui-label>
						${this._isNodePolicy
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

					${this._isNodePolicy ? this._renderNodePolicyOptions(definition) : ''}
					${this._renderReportingSettings(definition)}
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

			.extend-info {
				margin: 0;
				color: var(--uui-color-text-alt);
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