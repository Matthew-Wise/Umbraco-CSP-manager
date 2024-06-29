import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { LitElement, html, customElement, property } from '@umbraco-cms/backoffice/external/lit';
import { UmbDashboardElement } from '@umbraco-cms/backoffice/extension-registry';

@customElement('csp-dasboard')
export class Umbraco_Community_CSPDasboard extends UmbElementMixin(LitElement) implements UmbDashboardElement {
	constructor() {
		super();
	}

	@property()
	title = 'CSP Manager';

	render() {
		return html`<umb-body-layout header-transparent header-fit-height>
			<uui-box headline="${this.title}">
				<p>
					Within this section you are able to manage the Content-Security Policy (CSP) for the front end and the back
					office.
				</p>
				<p>
					If you want to learn more about CSP vist the
					<a
						class="btn-link -underline"
						target="_blank"
						rel="nofollow noopener noreferrer external"
						href="https://content-security-policy.com/"
						>Content Security Policy website</a
					>.
				</p>
			</uui-box>
		</umb-body-layout>`;
	}
}

export default Umbraco_Community_CSPDasboard;

declare global {
	interface HtmlElementTagNameMap {
		'csp-dashboard': Umbraco_Community_CSPDasboard;
	}
}
