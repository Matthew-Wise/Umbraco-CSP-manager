import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import { UmbControllerHostElement } from '@umbraco-cms/backoffice/controller-api';
import { UMB_WORKSPACE_CONTEXT, type UmbWorkspaceContext } from '@umbraco-cms/backoffice/workspace';

export class CSPWorkspaceContext extends UmbControllerBase implements UmbWorkspaceContext {
	public readonly workspaceAlias: string = 'csp.workspace.frontend';

	constructor(host: UmbControllerHostElement) {
		super(host);
		this.provideContext(UMB_WORKSPACE_CONTEXT, this);
		this.provideContext(CSP_WORKSPACE_CONTEXT, this);
	}

	getEntityType(): string {
		return 'csp-frontend';
	}

	getUnique(): string | undefined {
		return undefined;
	}
}

export default CSPWorkspaceContext;

export const CSP_WORKSPACE_CONTEXT = new UmbContextToken<CSPWorkspaceContext>(CSPWorkspaceContext.name);
