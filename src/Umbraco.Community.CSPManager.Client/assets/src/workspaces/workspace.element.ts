import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { LitElement, html, customElement, css } from '@umbraco-cms/backoffice/external/lit';
import CSPWorkspaceContext from './context';

@customElement('csp-workspace-root')
export class CSPWorkspaceElement extends UmbElementMixin(LitElement) {
	#workspaceContext = new CSPWorkspaceContext(this);

	constructor() {
		super();

		this.#workspaceContext._host;
	}

	render() {
		return html`
			<umb-workspace-editor headline="CSP Manager" alias="csp.workspace.root" .enforceNoFooter=${true}>
			</umb-workspace-editor>
		`;
	}

	static styles = css`
		uui-box {
			display: block;
			margin: 20px;
		}
	`;
}

export default CSPWorkspaceElement;
