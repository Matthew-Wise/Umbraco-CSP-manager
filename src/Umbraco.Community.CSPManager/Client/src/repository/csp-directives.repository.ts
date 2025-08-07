import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { Directives } from "../api";

export class UmbCspDirectivesRepository extends UmbRepositoryBase {
  constructor(host: UmbControllerHost) {
    super(host);
  }

  /**
   * Get all available CSP directives
   */
  async getAll() {
    const { data, error } = await tryExecute(this, Directives.getDirectives(), {
      disableNotifications: false,
    });

    if (data) {
      return { data };
    }

    return { error };
  }
}

export { UmbCspDirectivesRepository as api };
