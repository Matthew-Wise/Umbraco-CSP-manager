const cspEntityTypes = [
  "csp-policy-root",
  "csp-policy",
];

const actions: Array<UmbExtensionManifest> = [
  {
    type: "entityAction",
    kind: "publisher-push",
    alias: "usync.cspmanager.push.action",
    name: "Push CSP Policy",
    forEntityTypes: cspEntityTypes,
  },
  {
    type: "entityAction",
    kind: "publisher-pull",
    alias: "usync.cspmanager.pull.action",
    name: "Pull CSP Policy",
    forEntityTypes: cspEntityTypes,
  },
];

export const manifests: Array<UmbExtensionManifest> = [...actions];
