import { css, customElement, html, nothing, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UMB_APP_LANGUAGE_CONTEXT, type UmbLanguageDetailModel } from '@umbraco-cms/backoffice/language';
import type { CspApiDefinition } from '@/api';
import { CspConstants } from '@/constants';
import { languageLabel, nodePolicyContentName, nodePolicyLabel } from '@/node-policy-label';
import { cspEditPolicyPath } from '@/paths';
import { asCspHeaderValue, directivesOf } from '@/utils/csp-string';
import {
	UMB_CSP_MANAGER_WORKSPACE_CONTEXT,
	type UmbCspManagerWorkspaceContext,
	type WorkspaceState,
} from '../../context/workspace.context.js';

/**
 * A result that combines a policy with the policies extending it isn't one stored policy: the
 * server gives it the empty id, so that it can't be saved over one of them.
 */
const isCombined = (effective: CspApiDefinition): boolean => effective.id === CspConstants.nodePolicy.newId;

/**
 * Shows what a frontend request for the content node of a node policy gets today: the policy the
 * server resolves from the saved policies (the node's own, the ones it inherits and the Frontend
 * policy). Read-only. Content names are user content, so they are only rendered as text bindings.
 */
@customElement('umb-csp-effective-view')
export class UmbCspEffectiveViewElement extends UmbLitElement {
	#workspaceContext?: UmbCspManagerWorkspaceContext;
	// The request the shown result is for. The workspace hands out the same one until the answer can change.
	#request?: Promise<CspApiDefinition | undefined>;

	@state()
	private _workspaceState?: WorkspaceState;

	@state()
	private _languages: UmbLanguageDetailModel[] = [];

	/** The culture of the request to show. Starts as the policy's own, or the default language. */
	@state()
	private _culture: string | null = null;

	@state()
	private _effective?: CspApiDefinition;

	@state()
	private _loading = true;

	constructor() {
		super();

		this.consumeContext(UMB_CSP_MANAGER_WORKSPACE_CONTEXT, (context) => {
			this.#workspaceContext = context;
			if (!context) return;

			this.observe(context.state, (workspaceState) => {
				const previousKey = this._workspaceState?.definition?.contentKey;
				this._workspaceState = workspaceState;

				const definition = workspaceState.definition;
				if (definition?.contentKey !== previousKey) {
					// Another policy: start from its own culture again.
					this._culture = definition?.culture ?? null;
				}

				this.#loadEffective();
			});
		});

		// The languages the backoffice has already loaded.
		this.consumeContext(UMB_APP_LANGUAGE_CONTEXT, (context) => {
			this.observe(context?.languages, (languages) => (this._languages = languages ?? []));
		});
	}

	async #loadEffective() {
		// The answer only changes when a policy is saved, not with every edit. The workspace keeps
		// it until then, so getting the same request back means there is nothing new to show.
		const request = this.#workspaceContext?.getEffective(this._culture);
		if (!request || request === this.#request) return;

		this.#request = request;
		this._loading = true;

		const effective = await request;
		if (request !== this.#request) return;

		this._effective = effective;
		this._loading = false;
	}

	#onCultureChange(event: Event) {
		this._culture = (event.target as HTMLSelectElement).value || null;
		this.#loadEffective();
	}

	/** The selected culture, or the default language when the policy is for all cultures. */
	get #selectedCulture(): string | undefined {
		return this._culture ?? this._languages.find((language) => language.isDefault)?.unique;
	}

	override render() {
		const definition = this._workspaceState?.definition;
		if (!definition?.contentKey) {
			return html`<uui-loader></uui-loader>`;
		}

		return html`
			<uui-box headline=${this.localize.term('cspManagerEffective_headline')}>
				<p class="intro">
					${this.localize.term('cspManagerEffective_intro', nodePolicyContentName(this.localize, definition))}
				</p>
				${this.#renderNote()} ${this.#renderCulture()} ${this.#renderResult()}
			</uui-box>
		`;
	}

	// The result follows the saved policies, so say when what is being edited isn't saved.
	#renderNote() {
		if (!this.#workspaceContext?.hasUnsavedChanges()) {
			return nothing;
		}

		const term = this._workspaceState?.isNew ? 'cspManagerEffective_notSavedYet' : 'cspManagerEffective_unsaved';
		return html`<p class="note" data-mark="csp-effective-note">${this.localize.term(term)}</p>`;
	}

	#renderCulture() {
		if (this._languages.length < 2) {
			return nothing;
		}

		const selected = this.#selectedCulture?.toLowerCase();
		const options = this._languages.map((language) => ({
			name: language.isDefault
				? this.localize.term('cspManagerEffective_defaultLanguage', languageLabel(language))
				: languageLabel(language),
			value: language.unique,
			selected: language.unique.toLowerCase() === selected,
		}));

		return html`
			<uui-form-layout-item>
				<uui-label slot="label" for="culture">${this.localize.term('cspManagerNodePolicy_cultureLabel')}</uui-label>
				<span slot="description">${this.localize.term('cspManagerEffective_cultureDescription')}</span>
				<uui-select
					id="culture"
					data-mark="csp-effective-culture"
					label=${this.localize.term('cspManagerNodePolicy_cultureLabel')}
					.options=${options}
					@change=${this.#onCultureChange}></uui-select>
			</uui-form-layout-item>
		`;
	}

	#renderResult() {
		if (this._loading) {
			return html`<uui-loader></uui-loader>`;
		}

		const effective = this._effective;
		if (!effective) {
			return html`<p class="error">${this.localize.term('cspManagerEffective_loadFailed')}</p>`;
		}

		return html`
			<uui-form-layout-item>
				<uui-label slot="label">${this.localize.term('cspManagerEffective_fromLabel')}</uui-label>
				<div class="from" data-mark="csp-effective-from">${this.#renderFrom(effective)}</div>
			</uui-form-layout-item>
			${effective.enabled
				? this.#renderPolicy(effective)
				: html`<p data-mark="csp-effective-no-header">${this.localize.term('cspManagerEffective_noHeader')}</p>`}
		`;
	}

	// The server names the most specific policy that applies.
	#renderFrom(effective: CspApiDefinition) {
		if (!effective.contentKey) {
			return html`
				<span>${this.localize.term('cspManagerEffective_fromFrontend')}</span>
				${this.#renderOpenPolicy(CspConstants.policyTypes.frontend.value)}
			`;
		}

		const label = nodePolicyLabel(this.localize, effective);
		const combined = isCombined(effective);

		return html`
			<span>
				${this.localize.term(combined ? 'cspManagerEffective_fromCombined' : 'cspManagerEffective_fromNode', label)}
			</span>
			${combined ? nothing : this.#renderOpenPolicy(effective.id)}
		`;
	}

	#renderOpenPolicy(id: string) {
		// No link to the policy that is already open.
		if (id === this.#workspaceContext?.getUnique()) {
			return nothing;
		}

		return html`
			<uui-button
				look="secondary"
				compact
				href=${cspEditPolicyPath(id)}
				label=${this.localize.term('cspManagerEffective_openPolicy')}></uui-button>
		`;
	}

	#renderPolicy(effective: CspApiDefinition) {
		const headerValue = asCspHeaderValue(effective);
		if (!headerValue) {
			// A policy without sources or settings has nothing to send.
			return html`<p data-mark="csp-effective-no-header">${this.localize.term('cspManagerEffective_noHeaderEmpty')}</p>`;
		}

		const headerName = effective.reportOnly ? 'Content-Security-Policy-Report-Only' : 'Content-Security-Policy';

		return html`
			<uui-form-layout-item>
				<uui-label slot="label">${this.localize.term('cspManagerEffective_headerLabel')}</uui-label>
				<span slot="description">${this.localize.term('cspManagerEffective_headerNote')}</span>
				<div class="header" data-mark="csp-effective-header">
					<strong>${headerName}:</strong>
					<span>${headerValue}</span>
				</div>
			</uui-form-layout-item>
			<uui-form-layout-item>
				<uui-label slot="label">${this.localize.term('cspManagerEffective_directivesLabel')}</uui-label>
				<uui-table data-mark="csp-effective-directives">
					${[...directivesOf(effective)].map(
						([directive, sources]) => html`
							<uui-table-row>
								<uui-table-cell class="directive">${directive}</uui-table-cell>
								<uui-table-cell>${sources.join(' ')}</uui-table-cell>
							</uui-table-row>
						`,
					)}
				</uui-table>
			</uui-form-layout-item>
		`;
	}

	static override styles = [
		css`
			:host {
				display: block;
				padding: var(--uui-size-layout-1);
			}

			.intro,
			.note {
				margin-top: 0;
			}

			.note,
			.error {
				padding: var(--uui-size-space-3) var(--uui-size-space-4);
				border-radius: var(--uui-border-radius);
				background-color: var(--uui-color-surface-alt);
			}

			.error {
				color: var(--uui-color-danger);
			}

			.from {
				display: flex;
				align-items: center;
				gap: var(--uui-size-space-4);
			}

			.header {
				padding: var(--uui-size-space-4);
				border-radius: var(--uui-border-radius);
				background-color: var(--uui-color-surface-alt);
				font-family: monospace;
				overflow-wrap: anywhere;
			}

			.directive {
				white-space: nowrap;
				font-weight: 600;
			}

			uui-table-cell {
				overflow-wrap: anywhere;
			}

			uui-select {
				min-width: 20rem;
			}
		`,
	];
}

export default UmbCspEffectiveViewElement;

declare global {
	interface HTMLElementTagNameMap {
		'umb-csp-effective-view': UmbCspEffectiveViewElement;
	}
}
