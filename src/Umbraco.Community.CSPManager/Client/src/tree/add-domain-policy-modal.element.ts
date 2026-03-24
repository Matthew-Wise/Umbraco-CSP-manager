import { css, html, customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement, UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { CspDomainInfo } from '@/api';
import { UmbCspDefinitionRepository } from '@/repository/csp-definition.repository.js';

export interface AddDomainPolicyModalData {
	_unused?: never;
}

export interface AddDomainPolicyModalValue {
	domainKey: string;
}

export const UMB_ADD_DOMAIN_POLICY_MODAL = new UmbModalToken<AddDomainPolicyModalData, AddDomainPolicyModalValue>(
	'Umbraco.Community.CSPManager.Modal.AddDomainPolicy',
	{
		modal: {
			type: 'sidebar',
			size: 'small',
		},
	},
);

@customElement('umb-csp-add-domain-policy-modal')
export class UmbCspAddDomainPolicyModalElement extends UmbModalBaseElement<
	AddDomainPolicyModalData,
	AddDomainPolicyModalValue
> {
	@state()
	private _domains: CspDomainInfo[] = [];

	@state()
	private _loading = true;

	@state()
	private _error: string | null = null;

	override connectedCallback() {
		super.connectedCallback();
		this.#loadDomains();
	}

	async #loadDomains() {
		const repository = new UmbCspDefinitionRepository(this);
		const { data, error } = await repository.getDomains();
		this._loading = false;

		if (error) {
			this._error = 'Failed to load domains.';
			return;
		}

		if (data) {
			this._domains = data.filter((d) => !d.hasCspPolicy);
		}
	}

	#submit(domainKey: string) {
		this.modalContext?.setValue({ domainKey });
		this.modalContext?.submit();
	}

	#reject() {
		this.modalContext?.reject();
	}

	render() {
		return html`
			<umb-body-layout headline="Add Domain Policy">
				<div id="main">
					${this._loading
						? html`<div class="loader"><uui-loader></uui-loader></div>`
						: this._error
							? html`<p class="error">${this._error}</p>`
							: this._domains.length === 0
								? html`
										<p>
											No domains available. Either all domains already have a CSP policy, or no
											domains have been configured in Umbraco.
										</p>
									`
								: html`
										<p>
											Select a domain to create a CSP policy for. It will be copied from the
											Frontend policy as a starting point.
										</p>
										<uui-ref-list>
											${this._domains.map(
												(domain) => html`
													<uui-ref-node
														name="${domain.name ?? domain.key ?? 'Unknown domain'}"
														@open="${() => this.#submit(domain.key!)}">
														<uui-icon slot="icon" name="icon-home"></uui-icon>
													</uui-ref-node>
												`,
											)}
										</uui-ref-list>
									`}
				</div>
				<div slot="actions">
					<uui-button label="Cancel" @click="${this.#reject}"></uui-button>
				</div>
			</umb-body-layout>
		`;
	}

	static override styles = [
		css`
			#main {
				padding: var(--uui-size-space-5);
			}
			.loader {
				display: flex;
				justify-content: center;
				padding: var(--uui-size-space-5);
			}
			p {
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
