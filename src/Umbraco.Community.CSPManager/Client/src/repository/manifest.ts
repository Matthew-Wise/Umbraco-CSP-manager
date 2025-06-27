export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'repository',
    alias: 'Umbraco.Community.CSPManager.Repository.CspDefinition',
    name: 'CSP Definition Repository',
    api: () => import('./csp-definition.repository.js'),
  },
  {
    type: 'repository',
    alias: 'Umbraco.Community.CSPManager.Repository.CspDirectives',
    name: 'CSP Directives Repository',
    api: () => import('./csp-directives.repository.js'),
  },
];