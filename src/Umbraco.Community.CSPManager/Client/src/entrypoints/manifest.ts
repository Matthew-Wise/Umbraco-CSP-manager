export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Umbraco Community CSPManager Entrypoint",
    alias: "Umbraco.Community.CSPManager.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint"),
  }
];
