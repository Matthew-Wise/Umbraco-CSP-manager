import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UmbSectionElement } from '@umbraco-cms/backoffice/extension-registry';
import { LitElement, html, customElement, property } from '@umbraco-cms/backoffice/external/lit';
import { cspConstants } from '../constants';

@customElement('csp-section')
export class Umbraco_Community_CSPManagerSection extends UmbElementMixin(LitElement) implements UmbSectionElement {
	constructor() {
		super();
	}

	@property()
	title = 'CSP Manager';

	render() {
		return html`
			<umb-split-panel lock="start" snap="300px">
				<umb-sidebarKind alias="${cspConstants.menuAlias}" slot="start"></umb-sidebarKind>
				<uui-box headline="${this.title}" slot="end">
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
			</umb-split-panel>
		`;
	}
}

export default Umbraco_Community_CSPManagerSection;

declare global {
	interface HtmlElementTagNameMap {
		'csp-section': Umbraco_Community_CSPManagerSection;
	}
}
