export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "workspace",
    alias: "Umbraco.Community.CSPManager.Workspace.Frontend",
    name: "CSP Manager Frontend Workspace",
    js: () => import("./csp-management-workspace.element.js"),
    meta: {
      entityType: "frontend",
    },
  },
  {
    type: "workspace",
    alias: "Umbraco.Community.CSPManager.Workspace.Backoffice",
    name: "CSP Manager Backoffice Workspace",
    js: () => import("./csp-management-workspace.element.js"),
    meta: {
      entityType: "backoffice",
    },
  },
  {
    type: "workspaceContext",
    alias: "Umbraco.Community.CSPManager.WorkspaceContext.Frontend",
    name: "CSP Manager Frontend Workspace Context",
    js: () => import("./context/workspace.context.js"),
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        match: "Umbraco.Community.CSPManager.Workspace.Frontend",
      },
    ],
  },
  {
    type: "workspaceContext",
    alias: "Umbraco.Community.CSPManager.WorkspaceContext.Backoffice",
    name: "CSP Manager Backoffice Workspace Context",
    js: () => import("./context/workspace.context.js"),
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        match: "Umbraco.Community.CSPManager.Workspace.Backoffice",
      },
    ],
  },
  // Frontend Workspace Views (appear as tabs)
  {
    type: "workspaceView",
    alias: "Umbraco.Community.CSPManager.WorkspaceView.Frontend.Default",
    name: "CSP Frontend Sources View",
    js: () => import("./views/default/default.element.js"),
    weight: 200,
    meta: {
      label: "Sources",
      pathname: "sources",
      icon: "icon-list",
    },
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        match: "Umbraco.Community.CSPManager.Workspace.Frontend",
      },
    ],
  },
  {
    type: "workspaceView",
    alias: "Umbraco.Community.CSPManager.WorkspaceView.Frontend.Settings",
    name: "CSP Frontend Settings View",
    js: () => import("./views/settings/settings.element.js"),
    weight: 100,
    meta: {
      label: "Settings",
      pathname: "settings",
      icon: "icon-settings",
    },
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        match: "Umbraco.Community.CSPManager.Workspace.Frontend",
      },
    ],
  },
  // Backoffice Workspace Views (appear as tabs)
  {
    type: "workspaceView",
    alias: "Umbraco.Community.CSPManager.WorkspaceView.Backoffice.Default",
    name: "CSP Backoffice Sources View",
    js: () => import("./views/default/default.element.js"),
    weight: 200,
    meta: {
      label: "Sources",
      pathname: "sources",
      icon: "icon-list",
    },
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        match: "Umbraco.Community.CSPManager.Workspace.Backoffice",
      },
    ],
  },
  {
    type: "workspaceView",
    alias: "Umbraco.Community.CSPManager.WorkspaceView.Backoffice.Settings",
    name: "CSP Backoffice Settings View",
    js: () => import("./views/settings/settings.element.js"),
    weight: 100,
    meta: {
      label: "Settings",
      pathname: "settings",
      icon: "icon-settings",
    },
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        match: "Umbraco.Community.CSPManager.Workspace.Backoffice",
      },
    ],
  },
];
