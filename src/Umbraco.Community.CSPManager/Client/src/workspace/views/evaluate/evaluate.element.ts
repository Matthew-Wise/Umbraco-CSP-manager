import { css, html, customElement, nothing, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_CSP_MANAGER_WORKSPACE_CONTEXT, type WorkspaceState } from '../../context/workspace.context.js';
import { CspPolicyInheritance, type CspApiDefinition } from '@/api';
import { nodePolicyLabel } from '@/node-policy-label';
import { UmbCspDefinitionRepository } from '@/repository/csp-definition.repository.js';
import { sourcesAsCspString } from '@/utils/csp-string';
import { CspEvaluator } from 'csp_evaluator/dist/evaluator.js';
import { CspParser } from 'csp_evaluator/dist/parser.js';
import type { Finding, Severity } from 'csp_evaluator/dist/finding.js';

interface GroupedFindings {
	[directive: string]: {
		findings: Finding[];
		highestSeverity: Severity;
	};
}

// Google CSP Evaluator severity mapping to match https://csp-evaluator.withgoogle.com/
const getSeverityInfo = (severity: Severity) => {
	const severityMap: Record<number, { text: string; icon: string; level: string; color: string }> = {
		10: { text: 'High severity finding', icon: 'icon-block', level: 'high', color: 'red' }, // HIGH
		20: { text: 'Syntax Error', icon: 'icon-code', level: 'syntax', color: 'red' }, // SYNTAX
		30: { text: 'Medium severity finding', icon: 'icon-alert', level: 'medium', color: 'orange' }, // MEDIUM
		40: { text: 'Possible high severity finding', icon: 'icon-alert', level: 'high-possible', color: 'orange' }, // HIGH_MAYBE
		45: { text: 'Strict CSP', icon: 'icon-info', level: 'strict-csp', color: 'blue' }, // STRICT_CSP
		50: { text: 'Possible medium severity finding', icon: 'icon-alert', level: 'medium-possible', color: 'yellow' }, // MEDIUM_MAYBE
		60: { text: 'Information', icon: 'icon-info', level: 'info', color: 'blue' }, // INFO
		100: { text: 'All Good', icon: 'icon-check', level: 'none', color: 'green' }, // NONE
	};

	const severityNum = Number(severity);
	return severityMap[severityNum] || { text: 'Unknown', icon: 'help', level: 'unknown', color: 'gray' };
};

@customElement('umb-csp-evaluate-view')
export class UmbCspEvaluateViewElement extends UmbLitElement {
	@state()
	private _workspaceState: WorkspaceState = {
		definition: null,
		persistedDefinition: null,
		availableDirectives: [],
		loading: true,
	};

	@state()
	private _expandedDirectives = new Set<string>();

	// An "inherit and add" policy is only a list of extra sources. Evaluating those on their own
	// says nothing, so for such a policy this is what a request for its content gets instead.
	@state()
	private _effective?: CspApiDefinition;

	#repository = new UmbCspDefinitionRepository(this);
	#effectiveFor?: CspApiDefinition | null;

	constructor() {
		super();

		this.consumeContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, (context) => {
			if (context) {
				this.observe(context.state, (state) => {
					this._workspaceState = state;
					this.#loadEffective();
				});
			}
		});
	}

	get #isExtending(): boolean {
		return this._workspaceState.definition?.inheritance === CspPolicyInheritance.EXTEND;
	}

	async #loadEffective() {
		const definition = this._workspaceState.definition;
		if (!this.#isExtending || !definition?.contentKey) {
			this._effective = undefined;
			this.#effectiveFor = undefined;
			return;
		}

		// The server works it out from the saved policies, so it only changes when this one is saved.
		const saved = this._workspaceState.persistedDefinition;
		if (this.#effectiveFor === saved) return;
		this.#effectiveFor = saved;

		const { data } = await this.#repository.getEffective(definition.contentKey, definition.culture);
		if (this.#effectiveFor === saved) {
			this._effective = data;
		}
	}

	private _toggleDirectiveExpansion(directive: string) {
		if (this._expandedDirectives.has(directive)) {
			this._expandedDirectives.delete(directive);
		} else {
			this._expandedDirectives.add(directive);
		}
		this._expandedDirectives = new Set(this._expandedDirectives);
	}

	/** The policy to evaluate: this one, or for an extending policy what its content gets. */
	get #evaluated(): Pick<CspApiDefinition, 'sources'> | undefined {
		return this.#isExtending ? this._effective : (this._workspaceState.definition ?? undefined);
	}

	#getFindings(csp: string): GroupedFindings {
		const parsed = new CspParser(csp).csp;
		const results = new CspEvaluator(parsed).evaluate();

		// Deduplicate findings - csp_evaluator triplicates entries when
		// script-src-attr/elem fall back to script-src
		const uniqueResults = results.filter(
			(finding: Finding, index: number, arr: Finding[]) =>
				arr.findIndex((f: Finding) => f.equals(finding)) === index
		);

		return uniqueResults.reduce((acc: GroupedFindings, finding: Finding) => {
			const directive = finding.directive;

			if (!acc[directive]) {
				acc[directive] = {
					findings: [],
					highestSeverity: finding.severity,
				};
			}

			acc[directive].findings.push(finding);

			// Update highest severity if current finding is more severe (lower number = higher severity)
			const currentSeverityValue = Number(finding.severity);
			const highestSeverityValue = Number(acc[directive].highestSeverity);

			if (currentSeverityValue < highestSeverityValue) {
				acc[directive].highestSeverity = finding.severity;
			}

			return acc;
		}, {});
	}

	render() {
		if (this._workspaceState.loading) {
			return html`<uui-loader></uui-loader>`;
		}

		const evaluated = this.#evaluated;
		if (this.#isExtending && !evaluated) {
			return html`<uui-loader></uui-loader>`;
		}

		// An inherit-and-add policy is evaluated as what the request gets, so say so.
		const note = this.#isExtending
			? html`<p data-mark="csp-evaluates-effective">
					${this.localize.term(
						'cspManagerEffective_evaluatesEffective',
						nodePolicyLabel(this.localize, { ...this._workspaceState.definition, culture: null }),
					)}
				</p>`
			: nothing;

		if (!evaluated || evaluated.sources.length == 0) {
			return html`<uui-box headline="Evaluate">
				${note}
				<div>
					<div>No sources configured yet</div>
				</div>
			</uui-box>`;
		}

		const rawCsp = sourcesAsCspString(evaluated);
		const findings = this.#getFindings(rawCsp);
		const hasFindings = Object.keys(findings).length > 0;
		return html`
			<uui-box headline="Evaluate">
				${note}
				<div class="csp-container">
					<span>${rawCsp}</span>
				</div>
				<div>
					${!hasFindings
						? html`<div class="no-findings">
								<uui-icon name="icon-check"></uui-icon>
								<span>No issues found</span>
							</div>`
						: Object.entries(findings).map(([directive, group]) => {
						const highestSeverityInfo = getSeverityInfo(group.highestSeverity);
						return html`
							<div class="directive-item">
								<div class="directive-header" @click=${() => this._toggleDirectiveExpansion(directive)}>
									<div class="directive-header-content">
										<h4>${directive}</h4>
										<span class="directive-summary">
											${group.findings.length} finding${group.findings.length !== 1 ? 's' : ''}
										</span>
									</div>
									<div class="directive-header-actions">
										<span class="severity-badge severity-${highestSeverityInfo.level}">
											<uui-icon name="${highestSeverityInfo.icon}"></uui-icon>
											${highestSeverityInfo.text}
										</span>
										<uui-symbol-expand .open=${this._expandedDirectives.has(directive)} class="expand-icon">
										</uui-symbol-expand>
									</div>
								</div>

								<div class="directive-content" ?hidden=${!this._expandedDirectives.has(directive)}>
									${group.findings.map((finding) => {
										const severityInfo = getSeverityInfo(finding.severity);
										return html`
											<div class="finding-item severity-${severityInfo.level}">
												<div class="finding-header">
													<uui-icon name="${severityInfo.icon}"></uui-icon>
													<span class="severity-text">${severityInfo.text}</span>
												</div>
												<div class="finding-description">${finding.description}</div>
												${finding.value ? html`<div class="finding-value">${finding.value}</div>` : ''}
											</div>
										`;
									})}
								</div>
							</div>
						`;
					})}
				</div>
			</uui-box>
		`;
	}

	static styles = [
		css`
			.no-findings {
					display: flex;
					align-items: center;
					gap: 0.5rem;
					padding: var(--uui-size-4);
					color: var(--uui-color-positive);
					--uui-icon-color: var(--uui-color-positive);
				}

				.csp-container {
				justify-content: center;
				display: flex;

				span {
					width: 80%;
					margin-bottom: 1rem;
					font-family: var(--uui-font-family-monospace);
					padding: 0.75rem;
					border: 1px solid var(--uui-color-border);
					border-radius: 4px;
					resize: vertical;
				}
			}
			.directive-item {
				margin-bottom: 1rem;
				border: 1px solid var(--uui-color-border);
				border-radius: var(--uui-border-radius);
				background-color: var(--uui-color-surface);
			}

			.directive-header {
				display: flex;
				align-items: center;
				justify-content: space-between;
				padding: var(--uui-size-4);
				cursor: pointer;
				border-radius: var(--uui-border-radius);
				transition: background-color 0.15s ease;
			}

			.directive-header:hover {
				background-color: var(--uui-color-surface-alt);
			}

			.directive-header-content {
				display: flex;
				flex-direction: column;
				gap: 0.25rem;
			}

			.directive-header-content h4 {
				margin: 0;
				font-size: 1rem;
				font-weight: 600;
				color: var(--uui-color-text);
			}

			.directive-summary {
				font-size: 0.875rem;
				color: var(--uui-color-text-alt);
			}

			.directive-header-actions {
				display: flex;
				align-items: center;
				gap: var(--uui-size-3);
			}

			.expand-icon {
				transition: transform 0.15s ease;
			}

			.directive-content {
				padding: 0 var(--uui-size-4) var(--uui-size-4);
			}

			.severity-badge {
				padding: 0.25rem 0.5rem;
				border-radius: 3px;
				font-size: 0.8rem;
				font-weight: normal;
				display: flex;
				align-items: center;
				gap: 0.25rem;
			}

			/* Severity colours come from the backoffice theme tokens so they hold up in both light and dark themes. */
			.severity-high,
			.severity-syntax {
				background-color: var(--uui-color-danger);
				color: var(--uui-color-danger-contrast);
			}
			.severity-medium,
			.severity-high-possible {
				background-color: var(--uui-color-warning);
				color: var(--uui-color-warning-contrast);
			}
			.severity-strict-csp,
			.severity-info {
				background-color: var(--uui-color-default);
				color: var(--uui-color-default-contrast);
			}

			.severity-medium-possible {
				background-color: var(--uui-color-warning-emphasis);
				color: var(--uui-color-warning-standalone);
			}
			.severity-none {
				background-color: var(--uui-color-positive);
				color: var(--uui-color-positive-contrast);
			}

			.severity-unknown {
				background-color: var(--uui-color-border-standalone);
				color: var(--uui-color-text);
			}

			.finding-item {
				padding: var(--uui-size-3);
				margin-bottom: var(--uui-size-2);
				border-left: 4px solid transparent;
				background-color: var(--uui-color-surface-alt);
				border-radius: var(--uui-border-radius);
				color: var(--uui-color-text);
			}

			.finding-item:last-child {
				margin-bottom: 0;
			}

			.finding-item.severity-high,
			.finding-item.severity-syntax {
				border-left-color: var(--uui-color-danger);
				--uui-icon-color: var(--uui-color-danger);
			}
			.finding-item.severity-medium,
			.finding-item.severity-high-possible {
				border-left-color: var(--uui-color-warning);
				--uui-icon-color: var(--uui-color-warning);
			}
			.finding-item.severity-strict-csp,
			.finding-item.severity-info {
				border-left-color: var(--uui-color-default);
				--uui-icon-color: var(--uui-color-default);
			}

			.finding-item.severity-medium-possible {
				border-left-color: var(--uui-color-warning-standalone);
				--uui-icon-color: var(--uui-color-warning-standalone);
			}
			.finding-item.severity-none {
				border-left-color: var(--uui-color-positive);
				--uui-icon-color: var(--uui-color-positive);
			}

			.finding-item.severity-unknown {
				border-left-color: var(--uui-color-border-standalone);
				--uui-icon-color: var(--uui-color-border-standalone);
			}

			.finding-header {
				display: flex;
				align-items: center;
				gap: 0.5rem;
				margin-bottom: 0.5rem;
				font-weight: 600;
			}

			.severity-text {
				font-size: 0.9rem;
			}

			.finding-description {
				font-weight: 500;
				margin-bottom: 0.5rem;
			}

			.finding-value {
				font-family: var(--uui-font-family-monospace);
				background-color: var(--uui-color-surface);
				padding: var(--uui-size-1) var(--uui-size-2);
				border-radius: var(--uui-border-radius);
				margin-top: var(--uui-size-2);
				font-size: 0.875rem;
				color: var(--uui-color-text-alt);
			}

			.finding-meta {
				display: flex;
				gap: 1rem;
				font-size: 0.9rem;
				color: var(--uui-color-text-alt);
			}
		`,
	];
}

export default UmbCspEvaluateViewElement;

declare global {
	interface HTMLElementTagNameMap {
		'umb-csp-evaluate-view': UmbCspEvaluateViewElement;
	}
}
