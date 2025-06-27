import type { ManifestGlobalContext } from '@umbraco-cms/backoffice/extension-registry';

const contexts: Array<ManifestGlobalContext> = [
  {
    type: 'globalContext',
    alias: 'UmbracoCommunity.CSPManager.CspDefinitionContext',
    name: 'CSP Definition Context',
    api: () => import('./csp-definition.context.js'),
  },
  {
    type: 'globalContext',
    alias: 'UmbracoCommunity.CSPManager.CspDirectivesContext',
    name: 'CSP Directives Context',
    api: () => import('./csp-directives.context.js'),
  },
];

export const manifests = contexts;