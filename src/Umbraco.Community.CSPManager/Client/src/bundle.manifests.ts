import { manifests as entrypoints } from './entrypoints/manifest';
import { manifests as workspace } from './workspace/manifest';
import { manifests as section } from './section/manifest';
import { manifests as repository } from './repository/manifest';
import { manifests as contexts } from './contexts/manifest';
import { manifests as tree } from './tree/manifest';

// Job of the bundle is to collate all the manifests from different parts of the extension and load other manifests
// We load this bundle from umbraco-package.json
export const manifests: Array<UmbExtensionManifest> = [
  ...entrypoints,
  ...workspace,
  ...section,
  ...repository,
  ...contexts,
  ...tree,
];
