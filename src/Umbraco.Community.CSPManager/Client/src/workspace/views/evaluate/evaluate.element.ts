import { css, html, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_CSP_MANAGER_WORKSPACE_CONTEXT, type WorkspaceState } from '../../context/workspace.context.js';
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

	constructor() {
		super();

		this.consumeContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, (context) => {
			if (context) {
				this.observe(context.state, (state) => {
					this._workspaceState = state;
				});
			}
		});
	}

	private _toggleDirectiveExpansion(directive: string) {
		if (this._expandedDirectives.has(directive)) {
			this._expandedDirectives.delete(directive);
		} else {
			this._expandedDirectives.add(directive);
		}
		this._expandedDirectives = new Set(this._expandedDirectives);
	}

	#getCspString = () => {
		let cspDirectives: Record<string, Array<string>> = {};

		this._workspaceState.definition?.sources.forEach(({ source, directives }) => {
			if (directives) {
				directives.forEach(
					(name) => (cspDirectives[name] = cspDirectives[name] ? [source, ...cspDirectives[name]] : [source])
				);
			}
		});

		var cspValue = '';
		for (var key in cspDirectives) {
			cspValue += key + ' ' + cspDirectives[key].join(' ') + '; ';
		}

		return cspValue;
	};

	#getFindings(csp: string): GroupedFindings {
		const parsed = new CspParser(csp).csp;
		const results = new CspEvaluator(parsed).evaluate();

		return results.reduce((acc: GroupedFindings, finding: Finding) => {
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

		if (this._workspaceState.definition?.sources.length == 0) {
			return html`<uui-box headline="Evaluate">
				<div>
					<div>No sources configured yet</div>
				</div>
			</uui-box>`;
		}

		const rawCsp = this.#getCspString();
		const findings = this.#getFindings(rawCsp);
		return html`
			<uui-box headline="Evaluate">
				<div class="csp-container">
					<span>${rawCsp}</span>
				</div>
				<div>
					${Object.entries(findings).map(([directive, group]) => {
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

			.severity-high,
			.severity-syntax {
				background-color: #dc3545;
				color: white;
			}
			.severity-medium,
			.severity-high-possible {
				background-color: #fd7e14;
				color: white;
			}
			.severity-strict-csp,
			.severity-info {
				background-color: #17a2b8;
				color: white;
			}

			.severity-medium-possible {
				background-color: #ffc107;
				color: black;
			}
			.severity-none {
				background-color: #28a745;
				color: white;
			}

			.severity-unknown {
				background-color: #6c757d;
				color: white;
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
				border-left-color: #dc3545;
				--uui-icon-color: #dc3545;
			}
			.finding-item.severity-medium-possible,
			.finding-item.severity-high-possible {
				border-left-color: #fd7e14;
				--uui-icon-color: #fd7e14;
			}
			.finding-item.severity-strict-csp,
			.finding-item.severity-info {
				border-left-color: #17a2b8;
				--uui-icon-color: #17a2b8;
			}

			.finding-item.severity-medium-possible {
				border-left-color: #ffc107;
				--uui-icon-color: #ffc107;
			}
			.finding-item.severity-none {
				border-left-color: #28a745;
				--uui-icon-color: #28a745;
			}

			.finding-item.severity-unknown {
				border-left-color: #6c757d;
				--uui-icon-color: #6c757d;
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
