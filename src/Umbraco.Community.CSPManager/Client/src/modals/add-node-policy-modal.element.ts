import { css, customElement, html, nothing, repeat, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { UMB_APP_LANGUAGE_CONTEXT, type UmbLanguageDetailModel } from '@umbraco-cms/backoffice/language';
import type { UmbInputDocumentElement } from '@umbraco-cms/backoffice/document';
// Registers <umb-input-document>, which the backoffice only loads once the Content section has been opened.
import '@umbraco-cms/backoffice/document';
import { CspPolicyInheritance, type CspApiNodeInfo, type CspApiNodePolicy } from '@/api';
import { cspEditPolicyPath } from '@/paths';
import { cultureLabel, languageLabel } from '@/node-policy-label';
import { UmbCspDefinitionRepository } from '../repository/csp-definition.repository.js';
import type { AddNodePolicyModalData, AddNodePolicyModalValue } from './add-node-policy-modal.token.js';

// uui-select keeps the empty value for its placeholder, so "all cultures" needs a value of its own.
const ALL_CULTURES_OPTION = 'all-cultures';

/**
 * Chooses what a new node policy is for: a content node (any node; the sites are offered as
 * shortcuts), one culture or all of them, and whether it is a complete policy or one that adds to
 * what the node inherits. Content names and hostnames are user content, so they are only ever
 * rendered as text bindings.
 */
@customElement('umb-csp-add-node-policy-modal')
export class UmbCspAddNodePolicyModalElement extends UmbModalBaseElement<
	AddNodePolicyModalData,
	AddNodePolicyModalValue
> {
	@state()
	private _sites: CspApiNodeInfo[] = [];

	@state()
	private _policies: CspApiNodePolicy[] = [];

	@state()
	private _languages: UmbLanguageDetailModel[] = [];

	@state()
	private _loading = true;

	@state()
	private _failed = false;

	@state()
	private _contentKey?: string;

	@state()
	private _culture: string | null = null;

	@state()
	private _inheritance: CspPolicyInheritance = CspPolicyInheritance.REPLACE;

	constructor() {
		super();

		// The languages the backoffice has already loaded.
		this.consumeContext(UMB_APP_LANGUAGE_CONTEXT, (context) => {
			this.observe(context?.languages, (languages) => (this._languages = languages ?? []));
		});
	}

	override connectedCallback() {
		super.connectedCallback();
		this.#load();
	}

	async #load() {
		const repository = new UmbCspDefinitionRepository(this);
		const [sites, policies] = await Promise.all([repository.getNodes(), repository.getNodePolicies()]);
		this._loading = false;

		if (!sites.data || !policies.data) {
			this._failed = true;
			return;
		}

		this._sites = sites.data;
		this._policies = policies.data;
	}

	/** The policy the chosen node already has for the chosen culture, if any: there can only be one. */
	get #existingPolicy(): CspApiNodePolicy | undefined {
		return this._policies.find(
			(policy) =>
				policy.contentKey === this._contentKey &&
				(policy.culture ?? '').toLowerCase() === (this._culture ?? '').toLowerCase(),
		);
	}

	#onContentChange(event: Event) {
		this._contentKey = (event.target as UmbInputDocumentElement).selection[0];
	}

	#submit() {
		if (!this._contentKey || this.#existingPolicy) return;

		this.value = { contentKey: this._contentKey, culture: this._culture, inheritance: this._inheritance };
		this.modalContext?.submit();
	}

	#openExisting(policy: CspApiNodePolicy) {
		this.modalContext?.reject();
		history.pushState(null, '', cspEditPolicyPath(policy.id));
	}

	#cancel() {
		this.modalContext?.reject();
	}

	override render() {
		return html`
			<umb-body-layout headline=${this.localize.term('cspManagerNodePolicy_addModalHeadline')}>
				<uui-box>${this.#renderContent()}</uui-box>
				<div slot="actions">
					<uui-button label=${this.localize.term('general_cancel')} @click=${this.#cancel}></uui-button>
					<uui-button
						look="primary"
						color="positive"
						data-mark="csp-add-node-policy-continue"
						label=${this.localize.term('cspManagerNodePolicy_addModalContinue')}
						.disabled=${!this._contentKey || !!this.#existingPolicy}
						@click=${this.#submit}></uui-button>
				</div>
			</umb-body-layout>
		`;
	}

	#renderContent() {
		if (this._loading) {
			return html`<div class="loader"><uui-loader></uui-loader></div>`;
		}

		if (this._failed) {
			return html`<p class="error">${this.localize.term('cspManagerNodePolicy_addModalLoadFailed')}</p>`;
		}

		return html`
			<uui-form-layout-item>
				<uui-label slot="label">${this.localize.term('cspManagerNodePolicy_addModalContentLabel')}</uui-label>
				<span slot="description">${this.localize.term('cspManagerNodePolicy_addModalContentDescription')}</span>
				<umb-input-document
					data-mark="csp-node-picker"
					.max=${1}
					.selection=${this._contentKey ? [this._contentKey] : []}
					@change=${this.#onContentChange}></umb-input-document>
				${this.#renderSites()}
			</uui-form-layout-item>
			${this.#renderCulture()}
			<uui-form-layout-item>
				<uui-label slot="label">${this.localize.term('cspManagerNodePolicy_inheritanceLabel')}</uui-label>
				<uui-radio-group
					data-mark="csp-node-policy-inheritance"
					.value=${this._inheritance}
					@change=${(e: Event) => (this._inheritance = (e.target as HTMLInputElement).value as CspPolicyInheritance)}>
					<uui-radio
						value=${CspPolicyInheritance.REPLACE}
						label=${this.localize.term('cspManagerNodePolicy_inheritanceReplace')}></uui-radio>
					<uui-radio
						value=${CspPolicyInheritance.EXTEND}
						label=${this.localize.term('cspManagerNodePolicy_inheritanceExtend')}></uui-radio>
				</uui-radio-group>
				<p class="hint">
					${this._inheritance === CspPolicyInheritance.EXTEND
						? this.localize.term('cspManagerNodePolicy_inheritanceExtendDescription')
						: `${this.localize.term('cspManagerNodePolicy_inheritanceReplaceDescription')} ${this.localize.term('cspManagerNodePolicy_addModalReplaceStart')}`}
				</p>
			</uui-form-layout-item>
			${this.#renderExisting()}
		`;
	}

	// The sites (nodes with a hostname) are the usual nodes to give a policy, so they are one click away.
	#renderSites() {
		if (this._contentKey || this._sites.length === 0) {
			return nothing;
		}

		return html`
			<p class="hint">${this.localize.term('cspManagerNodePolicy_addModalSites')}</p>
			<uui-ref-list>
				${repeat(
					this._sites,
					(site) => site.contentKey,
					(site) => html`
						<uui-ref-node
							name=${site.contentName}
							detail=${site.domains.map((domain) => domain.name).join(', ')}
							data-mark="csp-node:${site.contentName}"
							@open=${() => (this._contentKey = site.contentKey)}>
							<uui-icon slot="icon" name="icon-globe"></uui-icon>
						</uui-ref-node>
					`,
				)}
			</uui-ref-list>
		`;
	}

	// A site with one language has nothing to choose: its policies are for all cultures.
	#renderCulture() {
		if (this._languages.length < 2) {
			return nothing;
		}

		const options = [
			{ name: cultureLabel(this.localize, null), value: ALL_CULTURES_OPTION, selected: this._culture === null },
			...this._languages.map((language) => ({
				name: languageLabel(language),
				value: language.unique,
				selected: this._culture === language.unique,
			})),
		];

		return html`
			<uui-form-layout-item>
				<uui-label slot="label" for="culture">${this.localize.term('cspManagerNodePolicy_cultureLabel')}</uui-label>
				<span slot="description">${this.localize.term('cspManagerNodePolicy_addModalCultureDescription')}</span>
				<uui-select
					id="culture"
					data-mark="csp-node-policy-culture"
					label=${this.localize.term('cspManagerNodePolicy_cultureLabel')}
					.options=${options}
					@change=${(e: Event) => {
						const value = (e.target as HTMLSelectElement).value;
						this._culture = value === ALL_CULTURES_OPTION ? null : value;
					}}></uui-select>
			</uui-form-layout-item>
		`;
	}

	#renderExisting() {
		const existing = this.#existingPolicy;
		if (!existing) {
			return nothing;
		}

		return html`
			<div class="existing" data-mark="csp-node-policy-exists">
				<p>${this.localize.term('cspManagerNodePolicy_addModalExists', cultureLabel(this.localize, existing.culture))}</p>
				<uui-button
					look="secondary"
					label=${this.localize.term('cspManagerNodePolicy_addModalOpenExisting')}
					@click=${() => this.#openExisting(existing)}></uui-button>
			</div>
		`;
	}

	static override styles = [
		css`
			.loader {
				display: flex;
				justify-content: center;
				padding: var(--uui-size-space-5);
			}

			p {
				margin-top: 0;
			}

			.hint {
				margin: var(--uui-size-space-3) 0 var(--uui-size-space-2);
				color: var(--uui-color-text-alt);
			}

			.error {
				color: var(--uui-color-danger);
			}

			.existing {
				padding: var(--uui-size-space-4);
				border: 1px solid var(--uui-color-warning-standalone);
				border-radius: var(--uui-border-radius);
			}

			uui-select {
				width: 100%;
			}
		`,
	];
}

export default UmbCspAddNodePolicyModalElement;

declare global {
	interface HTMLElementTagNameMap {
		'umb-csp-add-node-policy-modal': UmbCspAddNodePolicyModalElement;
	}
}
