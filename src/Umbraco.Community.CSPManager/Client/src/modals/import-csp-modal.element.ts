import { css, customElement, html, state, when } from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { parseCspString, type CspParseResult } from '../utils/csp-parser.js';
import type { ImportCspModalData, ImportCspModalValue } from './import-csp-modal.token.js';

@customElement('umb-csp-import-modal')
export class UmbCspImportModalElement extends UmbModalBaseElement<ImportCspModalData, ImportCspModalValue> {
	@state()
	private _input = '';

	@state()
	private _parseResult: CspParseResult | null = null;

	private _handleInput(e: Event) {
		this._input = (e.target as HTMLTextAreaElement).value;
		this._parseResult = null;
	}

	private _handleParse() {
		if (!this._input.trim()) return;
		this._parseResult = parseCspString(this._input, this.data?.availableDirectives ?? []);
	}

	private _handleImport() {
		if (!this._parseResult) return;

		this.value = {
			sources: this._parseResult.sources,
			upgradeInsecureRequests: this._parseResult.upgradeInsecureRequests,
			reportingDirective: this._parseResult.reportingDirective,
			reportUri: this._parseResult.reportUri,
		};
		this.modalContext?.submit();
	}

	private _handleClose() {
		this.modalContext?.reject();
	}

	override render() {
		const canImport = this._parseResult !== null && this._parseResult.sources.length > 0;

		return html`
			<uui-dialog-layout headline="Import ${this.data?.policyLabel ?? ''} CSP Policy">
				<p class="description">
					Paste your Content Security Policy value below. This will <strong>replace</strong> all existing sources in the
					${this.data?.policyLabel ?? ''} policy.
				</p>

				<uui-form-layout-item>
					<uui-label slot="label">CSP Policy Value</uui-label>
					<uui-textarea
						.value=${this._input}
						@input=${this._handleInput}
						placeholder="default-src 'self'; script-src 'self' https://cdn.example.com; style-src 'self' 'unsafe-inline';"
						rows="5"></uui-textarea>
				</uui-form-layout-item>

				<uui-button
					look="secondary"
					label="Parse"
					.disabled=${!this._input.trim()}
					@click=${this._handleParse}>
					Parse
				</uui-button>

				${when(this._parseResult !== null, () => this._renderPreview())}

				<uui-button slot="actions" label="Cancel" @click=${this._handleClose}></uui-button>
				<uui-button
					slot="actions"
					look="primary"
					color="positive"
					label="Import"
					.disabled=${!canImport}
					@click=${this._handleImport}>
					Import
				</uui-button>
			</uui-dialog-layout>
		`;
	}

	private _renderPreview() {
		const result = this._parseResult!;

		return html`
			<div class="preview">
				<h4>Preview</h4>

				${when(
					result.warnings.length > 0,
					() => html`
						<uui-tag color="warning" look="outline" class="warning-tag">
							<strong>Warnings</strong>
						</uui-tag>
						<ul class="warnings">
							${result.warnings.map((w) => html`<li>${w}</li>`)}
						</ul>
					`,
				)}
				${when(
					result.sources.length === 0,
					() => html`<p class="no-sources">No sources found. Check the CSP value and try again.</p>`,
					() => html`
						<uui-box>
							${result.sources.map(
								(s) => html`
									<div class="source-row">
										<code class="source-name">${s.source}</code>
										<div class="directives">
											${s.directives.map((d) => html`<uui-tag look="outline" size="s">${d}</uui-tag>`)}
										</div>
									</div>
								`,
							)}
						</uui-box>
					`,
				)}

				<div class="special-flags">
					${when(
						result.upgradeInsecureRequests,
						() => html`<div class="flag"><uui-icon name="icon-lock"></uui-icon> upgrade-insecure-requests: enabled</div>`,
					)}
					${when(
						result.reportingDirective !== null,
						() =>
							html`<div class="flag">
								<uui-icon name="icon-alert"></uui-icon> ${result.reportingDirective}: ${result.reportUri}
							</div>`,
					)}
				</div>
			</div>
		`;
	}

	static override styles = [
		css`
			:host {
				display: contents;
			}

			.description {
				margin: 0 0 var(--uui-size-space-4);
				color: var(--uui-color-text-alt);
			}

			uui-textarea {
				width: 100%;
				font-family: monospace;
			}

			uui-button[label='Parse'] {
				margin-top: var(--uui-size-space-3);
			}

			.preview {
				margin-top: var(--uui-size-space-5);
				border-top: 1px solid var(--uui-color-border);
				padding-top: var(--uui-size-space-4);
			}

			.preview h4 {
				margin: 0 0 var(--uui-size-space-3);
				font-size: var(--uui-size-4);
				font-weight: 600;
			}

			.warning-tag {
				margin-bottom: var(--uui-size-space-2);
			}

			.warnings {
				margin: 0 0 var(--uui-size-space-3);
				padding-left: var(--uui-size-space-5);
				color: var(--uui-color-warning-emphasis);
			}

			.no-sources {
				color: var(--uui-color-danger);
			}

			.source-row {
				display: flex;
				align-items: center;
				gap: var(--uui-size-space-3);
				padding: var(--uui-size-space-2) 0;
				border-bottom: 1px solid var(--uui-color-border);
			}

			.source-row:last-child {
				border-bottom: none;
			}

			.source-name {
				min-width: 200px;
				font-size: 0.85em;
				background: var(--uui-color-surface-alt);
				padding: 2px 6px;
				border-radius: 3px;
			}

			.directives {
				display: flex;
				flex-wrap: wrap;
				gap: var(--uui-size-space-2);
			}

			.special-flags {
				margin-top: var(--uui-size-space-3);
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-2);
			}

			.flag {
				display: flex;
				align-items: center;
				gap: var(--uui-size-space-2);
				font-size: 0.9em;
				color: var(--uui-color-text-alt);
			}
		`,
	];
}

export default UmbCspImportModalElement;

declare global {
	interface HTMLElementTagNameMap {
		'umb-csp-import-modal': UmbCspImportModalElement;
	}
}
