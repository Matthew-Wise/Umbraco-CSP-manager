import { LitElement, css, customElement, html, state } from '@umbraco-cms/backoffice/external/lit';
import { getUmbracoManagementApiV1CspManageGetByIsBackOffice } from '../../api/services.gen.ts';

@customElement('csp-workspace-default-view')
export class CSPDefaultWorkspaceElement extends LitElement {
	render() {
		getUmbracoManagementApiV1CspManageGetByIsBackOffice({ isBackOffice: false }).then((resp) => {
			console.log(resp);
		});
		return html`<umb-body-layout header-transparent header-fit-height>
			<uui-box headline="Sources" class="umb-group-panel">
				<div ng-repeat="(sourceIndex, source) in vm.definition.Sources" class="mb2">
					<button
						type="button"
						class="csp-accordion-btn"
						ng-class="{'active': vm.expanded.includes(sourceIndex) ? true : false}"
						ng-click="vm.expandAccordion($event, sourceIndex)">
						Source: {{source.Source}}
						<span class="ml-auto">
							<uui-icon-registry-essential>
								<uui-action-bar>
									<uui-button label="copy" look="secondary" color="default" ng-click="vm.copySource(sourceIndex)">
										<uui-icon name="copy"></uui-icon>
									</uui-button>
									<uui-button label="remove" look="secondary" color="default" ng-click="vm.deleteSource(sourceIndex)">
										<uui-icon name="remove"></uui-icon>
									</uui-button>
								</uui-action-bar>
							</uui-icon-registry-essential>
							<uui-symbol-expand
								ng-attr-open="{{vm.expanded.includes(sourceIndex) ? true : undefined}}"></uui-symbol-expand>
						</span>
					</button>
					<div class="csp-accordion" ng-class="{'show': vm.expanded.includes(sourceIndex) ? true : false}">
						<div class="control-group umb-control-group">
							<div class="umb-el-wrap">
								<div class="control-header">
									<label class="control-label" for="source-{{sourceIndex}}">Source Url</label>
								</div>
								<div class="controls">
									<input
										id="source-{{sourceIndex}}"
										type="text"
										ng-model="vm.definition.Sources[sourceIndex].Source"
										class="umb-property-editor umb-textstring textstring" />
								</div>
							</div>
						</div>

						<div class="control-group umb-control-group">
							<div class="umb-el-wrap">
								<div class="control-header">
									<label class="control-label">Directives Set</label>
								</div>
								<div
									class="controls"
									style="
						display: grid;
						grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
						grid-template-rows: repeat(auto-fill);">
									<label ng-repeat="(index, directive) in vm.cspDirectiveOptions">
										<umb-checkbox
											name="directives"
											value="{{directive.value}}"
											model="source.Directives.includes(directive.value)"
											text="{{directive.value}}"
											on-change="vm.updateDirectiveOnSource(source, directive.value)">
										</umb-checkbox>
									</label>
								</div>
							</div>
						</div>
					</div>
				</div>

				<uui-button
					look="outline"
					color="default"
					type="button"
					label="Add Source"
					ng-click="vm.addSource()"
					class="mt3"></uui-button>
			</uui-box>
		</umb-body-layout>`;
	}
}

export default CSPDefaultWorkspaceElement;

declare global {
	interface HTMLElementTagNameMap {
		'csp-workspace-default-view': CSPDefaultWorkspaceElement;
	}
}
