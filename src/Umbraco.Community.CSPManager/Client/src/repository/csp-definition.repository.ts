import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { Definitions, type CspDefinition } from "../api";

export class UmbCspDefinitionRepository extends UmbRepositoryBase {
  constructor(host: UmbControllerHost) {
    super(host);
  }

  /**
   * Get CSP definition by type (front-end or back-office)
   */
  async get(isBackOffice: boolean) {
    const { data, error } = await tryExecute(
      this,
      Definitions.getDefinitions({
        query: { isBackOffice },
      }),
      { disableNotifications: false }
    );

    if (data) {
      return { data };
    }

    return { error };
  }

  /**
   * Save CSP definition
   */
  async save(definition: CspDefinition) {
    const { data, error } = await tryExecute(
      this,
      Definitions.postDefinitionsSave({
        body: definition,
      }),
      { disableNotifications: false }
    );

    if (data) {
      return { data };
    }

    return { error };
  }
}

export { UmbCspDefinitionRepository as api };
