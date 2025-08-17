import { css, customElement, html, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_MODAL_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/modal';
import type { UmbModalManagerContext } from '@umbraco-cms/backoffice/modal';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import type { UmbNotificationContext } from '@umbraco-cms/backoffice/notification';
import type { CspDefinitionSource } from '@/api';
import {
	UmbCspManagerWorkspaceContext,
	UMB_CSP_MANAGER_WORKSPACE_CONTEXT,
	type WorkspaceState,
} from '../../context/workspace.context.js';

@customElement('umb-csp-default-view')
export class UmbCspDefaultViewElement extends UmbLitElement {
	#workspaceContext?: UmbCspManagerWorkspaceContext;
	#modalManager?: UmbModalManagerContext;
	#notificationContext?: UmbNotificationContext;

	@state()
	private _workspaceState: WorkspaceState = {
		definition: null,
		availableDirectives: [],
		loading: true,
		hasChanges: false,
	};

	@state()
	private _expandedSources = new Set<number>();

	constructor() {
		super();

		this.consumeContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, (context) => {
			if (!context) return;

			this.#workspaceContext = context;

			this.observe(context.state, (state) => {
				this._workspaceState = state;
			});
		});

		this.consumeContext(UMB_MODAL_MANAGER_CONTEXT, (context) => {
			this.#modalManager = context;
		});

		this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
			this.#notificationContext = context;
		});
	}

	override connectedCallback() {
		super.connectedCallback();
	}

	private _toggleSourceExpansion(index: number) {
		if (this._expandedSources.has(index)) {
			this._expandedSources.delete(index);
		} else {
			this._expandedSources.add(index);
		}

		// Trigger reactive update
		this._expandedSources = new Set(this._expandedSources);
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

	private _handleCopySource(source: CspDefinitionSource, sourceIndex: number) {
		if (!this._workspaceState.definition) {
			return;
		}

		const newSource: CspDefinitionSource = {
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

	private async _handleDeleteSource(source: CspDefinitionSource) {
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
		if (!this._workspaceState.definition) {
			return;
		}

		const newSource: CspDefinitionSource = {
			definitionId: this._workspaceState.definition.id,
			source: 'new-source',
			directives: ['script-src', 'style-src'],
		};

		const updatedDefinition = {
			...this._workspaceState.definition,
			sources: [...this._workspaceState.definition.sources, newSource],
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
												<h4>${source.source || '(empty source)'}</h4>
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
												<uui-form-layout-item>
													<uui-label slot="label">Source</uui-label>
													<span slot="description">The URL or pattern for this CSP source</span>
													<uui-input
														.value=${source.source || ''}
														@input=${(e: Event) => this._updateSourceName(index, (e.target as HTMLInputElement).value)}
														placeholder="e.g., 'self', https://example.com, *.example.com">
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
