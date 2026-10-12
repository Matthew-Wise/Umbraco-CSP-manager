import { css, customElement, html, repeat, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import type { CspApiNodeInfo } from '@/api';
import { CspConstants } from '@/constants';
import { UmbCspDefinitionRepository } from '../repository/csp-definition.repository.js';
import type { AddDomainPolicyModalData, AddDomainPolicyModalValue } from './add-domain-policy-modal.token.js';

/**
 * Lists the content nodes with a hostname that don't have a domain policy yet, with their hostnames
 * (a multilingual site shows one entry for all its cultures). Picking one submits it straight away.
 * Content names and hostnames are user content, so they are only ever rendered as text bindings.
 */
@customElement('umb-csp-add-domain-policy-modal')
export class UmbCspAddDomainPolicyModalElement extends UmbModalBaseElement<
	AddDomainPolicyModalData,
	AddDomainPolicyModalValue
> {
	@state()
	private _nodes: CspApiNodeInfo[] = [];

	@state()
	private _loading = true;

	@state()
	private _failed = false;

	override connectedCallback() {
		super.connectedCallback();
		this.#loadDomains();
	}

	async #loadDomains() {
		const { data, error } = await new UmbCspDefinitionRepository(this).getNodes();
		this._loading = false;

		if (error || !data) {
			this._failed = true;
			return;
		}

		this._nodes = data.filter((node) => !node.hasCspPolicy);
	}

	#select(node: CspApiNodeInfo) {
		this.value = { contentKey: node.contentKey };
		this.modalContext?.submit();
	}

	#cancel() {
		this.modalContext?.reject();
	}

	#detail(node: CspApiNodeInfo) {
		return node.domains.map((domain) => domain.name).join(', ');
	}

	override render() {
		return html`
			<umb-body-layout headline=${this.localize.term('cspManagerDomainPolicy_addModalHeadline')}>
				<uui-box>${this.#renderContent()}</uui-box>
				<div slot="actions">
					<uui-button label=${this.localize.term('general_cancel')} @click=${this.#cancel}></uui-button>
				</div>
			</umb-body-layout>
		`;
	}

	#renderContent() {
		if (this._loading) {
			return html`<div class="loader"><uui-loader></uui-loader></div>`;
		}

		if (this._failed) {
			return html`<p class="error">${this.localize.term('cspManagerDomainPolicy_addModalLoadFailed')}</p>`;
		}

		if (this._nodes.length === 0) {
			return html`<p class="empty" data-mark="csp-no-domains">${this.localize.term('cspManagerDomainPolicy_addModalEmpty')}</p>`;
		}

		return html`
			<uui-ref-list>
				${repeat(
					this._nodes,
					(node) => node.contentKey,
					(node) => html`
						<uui-ref-node
							name=${node.contentName}
							detail=${this.#detail(node)}
							data-mark="csp-domain-node:${node.contentName}"
							@open=${() => this.#select(node)}>
							<uui-icon slot="icon" name=${CspConstants.icons.domainPolicy}></uui-icon>
						</uui-ref-node>
					`,
				)}
			</uui-ref-list>
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
				color: var(--uui-color-text-alt);
			}

			.error {
				color: var(--uui-color-danger);
			}
		`,
	];
}

export default UmbCspAddDomainPolicyModalElement;

declare global {
	interface HTMLElementTagNameMap {
		'umb-csp-add-domain-policy-modal': UmbCspAddDomainPolicyModalElement;
	}
}
