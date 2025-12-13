import { css, customElement, html, state, when } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_MODAL_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/modal';
import type { UmbModalManagerContext } from '@umbraco-cms/backoffice/modal';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import type { UmbNotificationContext } from '@umbraco-cms/backoffice/notification';
import type { CspApiDefinitionSource } from '@/api';
import {
	UmbCspManagerWorkspaceContext,
	UMB_CSP_MANAGER_WORKSPACE_CONTEXT,
	type WorkspaceState,
} from '../../context/workspace.context.js';
import type { UUIInputElement } from '@umbraco-cms/backoffice/external/uui';

@customElement('umb-csp-default-view')
export class UmbCspDefaultViewElement extends UmbLitElement {
	#workspaceContext?: UmbCspManagerWorkspaceContext;
	#modalManager?: UmbModalManagerContext;
	#notificationContext?: UmbNotificationContext;

	@state()
	private _workspaceState: WorkspaceState = {
		definition: null,
		persistedDefinition: null,
		availableDirectives: [],
		loading: true,
	};

	@state()
	private _invalidSources: Array<string> = [];

	@state()
	private _expandedSources = new Set<number>();

	@state()
	private _focusSourceIndex?: number;

	constructor() {
		super();

		this.consumeContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, (context) => {
			if (!context) return;

			this.#workspaceContext = context;

			this.observe(context.state, (state) => {
				this._workspaceState = state;
				if (state.error) {
					this._invalidSources = state.error.cause as string[];
				} else {
					this._invalidSources = [];
				}
			});
		});

		this.consumeContext(UMB_MODAL_MANAGER_CONTEXT, (context) => {
			this.#modalManager = context;
		});

		this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
			this.#notificationContext = context;
		});
	}

	private _toggleSourceExpansion(index: number) {
		if (this._expandedSources.has(index)) {
			this._expandedSources.delete(index);
		} else {
			this._expandedSources.add(index);
		}
		// Trigger reactive update
		this._expandedSources = new Set(this._expandedSources);
		this._focusSourceIndex = index;
	}

	override updated(changedProperties: Map<string | number | symbol, unknown>) {
		super.updated(changedProperties);
		if (changedProperties.has('_focusSourceIndex') && this._focusSourceIndex !== undefined) {
			const input = this.shadowRoot?.querySelector<UUIInputElement>(`#source-input-${this._focusSourceIndex}`);
			if (input) {
				input.focus();
			}
		}
	}

	private _toggleDirective(sourceIndex: number, directive: string) {
		if (!this._workspaceState.definition) return;

		const sources = [...this._workspaceState.definition.sources];
		const source = { ...sources[sourceIndex] };

		if (source.directives.includes(directive)) {
			source.directives = source.directives.filter((d) => d !== directive);
		} else {
			source.directives = [...source.directives, directive];
		}

		sources[sourceIndex] = source;

		const updatedDefinition = {
			...this._workspaceState.definition,
			sources,
		};

		this.#workspaceContext?.updateDefinition(updatedDefinition);
	}

	private _hasError(source: CspApiDefinitionSource): boolean {
		return this._invalidSources.includes(source.source);
	}

	private _updateSourceName(sourceIndex: number, newName: string) {
		if (!this._workspaceState.definition) return;

		const sources = [...this._workspaceState.definition.sources];
		sources[sourceIndex] = {
			...sources[sourceIndex],
			source: newName,
		};

		const updatedDefinition = {
			...this._workspaceState.definition,
			sources,
		};

		this.#workspaceContext?.updateDefinition(updatedDefinition);
	}

	private _handleCopySource(source: CspApiDefinitionSource, sourceIndex: number) {
		if (!this._workspaceState.definition) {
			return;
		}

		const newSource: CspApiDefinitionSource = {
			definitionId: this._workspaceState.definition.id,
			source: `${source.source}_copy`,
			directives: [...source.directives],
		};

		// Insert the copied source right after the current one
		const sources = [...this._workspaceState.definition.sources];
		sources.splice(sourceIndex + 1, 0, newSource);

		const updatedDefinition = {
			...this._workspaceState.definition,
			sources,
		};

		this.#workspaceContext?.updateDefinition(updatedDefinition);

		// Show success toast
		this.#notificationContext?.peek('positive', {
			data: {
				headline: 'Source Copied',
				message: `Source "${source.source || '(empty)'}" has been copied successfully.`,
			},
		});
	}

	private async _handleDeleteSource(source: CspApiDefinitionSource) {
		if (!this._workspaceState.definition || !this.#modalManager) return;

		const modalContext = this.#modalManager.open(this, 'Umb.Modal.Confirm', {
			data: {
				headline: 'Delete Source',
				content: `Are you sure you want to delete source "${source.source || '(empty)'}"?`,
				confirmLabel: 'Delete',
				color: 'danger',
			},
		});

		try {
			await modalContext?.onSubmit();

			// If we reach here, the user confirmed
			const updatedDefinition = {
				...this._workspaceState.definition,
				sources: this._workspaceState.definition.sources.filter((s) => s !== source),
			};

			this.#workspaceContext?.updateDefinition(updatedDefinition);

			// Show success toast
			this.#notificationContext?.peek('danger', {
				data: {
					headline: 'Source Deleted',
					message: `Source "${source.source || '(empty)'}" has been deleted successfully.`,
				},
			});
		} catch {
			// User cancelled or modal was closed
		}
	}

	private _handleAddSource() {
		if (!this._workspaceState.definition) return;

		const newSource: CspApiDefinitionSource = {
			definitionId: this._workspaceState.definition.id,
			source: '',
			directives: [],
		};

		const updatedDefinition = {
			...this._workspaceState.definition,
			sources: [...this._workspaceState.definition.sources, newSource],
		};

		this._toggleSourceExpansion(updatedDefinition.sources.length - 1);

		this.#workspaceContext?.updateDefinition(updatedDefinition);
	}

	private _updateUpgradeInsecureRequests(value: boolean) {
		if (!this._workspaceState.definition) return;

		const updatedDefinition = {
			...this._workspaceState.definition,
			upgradeInsecureRequests: value,
		};

		this.#workspaceContext?.updateDefinition(updatedDefinition);
	}

	override render() {
		if (this._workspaceState.loading) {
			return html`<uui-loader></uui-loader>`;
		}

		if (!this._workspaceState.definition) {
			return html`<div>No CSP definition available</div>`;
		}

		return html`
			<uui-box headline="Directives">
				<uui-form-layout-item>
					<uui-label slot="label" for="insecure-requests">Upgrade Insecure Requests</uui-label>
					<span slot="description">
						Automatically Converts urls from http to https for links, images, javascript, css, etc.
					</span>
					<div class="setting-control">
						<uui-toggle
							id="insecure-requests"
							.checked=${this._workspaceState.definition.upgradeInsecureRequests}
							.disabled=${!this._workspaceState.definition.enabled}
							label="${this._workspaceState.definition.upgradeInsecureRequests ? 'Enabled' : 'Disabled'}"
							@change=${(e: Event) => this._updateUpgradeInsecureRequests((e.target as HTMLInputElement).checked)}>
							${this._workspaceState.definition.upgradeInsecureRequests ? 'Enabled' : 'Disabled'}
						</uui-toggle>
					</div>
				</uui-form-layout-item>
			</uui-box>
			<uui-box headline="Sources">
				<p>
					Manage CSP sources and their directives. Each source defines what content can be loaded and which CSP
					directives apply to it.
				</p>
				${this._workspaceState.definition.sources.length === 0
					? html`
							<div class="empty-state">
								<p>No sources configured yet. Add a source to get started.</p>
							</div>
					  `
					: html`
							${this._workspaceState.definition.sources.map(
								(source, index) => html`
									<div class="source-item">
										<div class="source-header" @click=${() => this._toggleSourceExpansion(index)}>
											<div class="source-header-content">
												<h4>
													${when(this._hasError(source), () => html`<uui-icon name="icon-alert"></uui-icon>`)}
													${source.source || '(empty source)'}
												</h4>
												<span class="source-summary"
													>${source.directives.length} directive${source.directives.length !== 1 ? 's' : ''}</span
												>
											</div>
											<div class="source-header-actions">
												<uui-action-bar>
													<uui-button
														look="outline"
														color="default"
														type="button"
														label="Copy"
														@click=${(e: Event) => {
															e.stopPropagation();
															this._handleCopySource(source, index);
														}}>
														<uui-icon name="copy"></uui-icon>
													</uui-button>
													<uui-button
														look="outline"
														color="danger"
														type="button"
														label="Delete"
														@click=${(e: Event) => {
															e.stopPropagation();
															this._handleDeleteSource(source);
														}}>
														<uui-icon name="delete"></uui-icon>
													</uui-button>
												</uui-action-bar>
												<uui-symbol-expand .open=${this._expandedSources.has(index)} class="expand-icon">
												</uui-symbol-expand>
											</div>
										</div>

										<div class="source-content" ?hidden=${!this._expandedSources.has(index)}>
											<div class="source-fields">
												<uui-form-layout-item class="source-name-item">
													<uui-label slot="label" for="#source-input-${index}">Source</uui-label>
													<span slot="description"
														>The URL or pattern for this CSP source. e.g., 'self', https://example.com,
														*.example.com</span
													>
													<uui-input
														id="source-input-${index}"
														label="CSP Source"
														.value=${source.source || ''}
														@input=${(e: Event) => this._updateSourceName(index, (e.target as HTMLInputElement).value)}
														.error=${this._hasError(source)}
														error-message="Duplicate source name"
														placeholder="">
													</uui-input>
												</uui-form-layout-item>

												<uui-form-layout-item>
													<uui-label slot="label">CSP Directives</uui-label>
													<span slot="description"
														>Select which Content Security Policy directives this source applies to</span
													>
													<div class="directives-grid">
														${this._workspaceState.availableDirectives.map(
															(directive) => html`
																<uui-checkbox
																	.checked=${source.directives.includes(directive)}
																	@change=${() => this._toggleDirective(index, directive)}
																	label=${directive}>
																	${directive}
																</uui-checkbox>
															`
														)}
													</div>
												</uui-form-layout-item>
											</div>
										</div>
									</div>
								`
							)}
					  `}

				<uui-button
					label="Add Source"
					look="primary"
					style="margin-top: var(--uui-size-space-4)"
					@click=${this._handleAddSource}>
					<uui-icon name="add"></uui-icon>
					Add Source
				</uui-button>
			</uui-box>
		`;
	}

	static styles = [
		css`
			:host {
				display: block;
				padding: var(--uui-size-layout-1);
			}
			uui-box {
				margin-bottom: var(--uui-size-layout-1);
			}

			.empty-state {
				text-align: center;
				padding: var(--uui-size-space-6);
				color: var(--uui-color-text-alt);
			}

			.source-item {
				margin-bottom: var(--uui-size-space-4);
				border: 1px solid var(--uui-color-border);
				border-radius: var(--uui-border-radius);
				overflow: hidden;
			}

			.source-header {
				display: flex;
				justify-content: space-between;
				align-items: center;
				padding: var(--uui-size-space-4);
				background-color: var(--uui-color-surface-alt);
				cursor: pointer;
				transition: background-color 0.2s ease;
			}

			.source-header:hover {
				background-color: var(--uui-color-surface-emphasis);
			}

			.source-header-content {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-1);
				flex: 1;
			}

			.source-header h4 {
				margin: 0;
				font-size: 1rem;
				font-weight: 600;
				--uui-icon-color: var(--uui-color-danger);
			}

			.source-summary {
				font-size: 0.875rem;
				color: var(--uui-color-text-alt);
			}

			.source-header-actions {
				display: flex;
				align-items: center;
				gap: var(--uui-size-space-3);
			}

			uui-action-bar {
				--uui-action-bar-background: transparent;
				margin-right: var(--uui-size-space-2);
			}

			.expand-icon {
				--uui-symbol-expand-size: 1.25rem;
				color: currentColor;
			}

			.source-content {
				padding: var(--uui-size-space-4);
				border-top: 1px solid var(--uui-color-border);
				background-color: var(--uui-color-surface);
			}

			.source-content[hidden] {
				display: none !important;
			}

			.source-fields {
				display: grid;
				gap: var(--uui-size-space-5);
			}

			.directives-grid {
				display: grid;
				grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
				gap: var(--uui-size-space-3);
				margin-top: var(--uui-size-space-2);
			}

			.directives-grid uui-checkbox {
				white-space: nowrap;
			}
		`,
	];
}

export default UmbCspDefaultViewElement;

declare global {
	interface HTMLElementTagNameMap {
		'umb-csp-default-view': UmbCspDefaultViewElement;
	}
}
