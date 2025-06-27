import { UmbRepositoryBase } from '@umbraco-cms/backoffice/repository';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { tryExecuteAndNotify } from '@umbraco-cms/backoffice/resources';
import { Definitions, type CspDefinition } from '../api';

export class UmbCspDefinitionRepository extends UmbRepositoryBase {
  constructor(host: UmbControllerHost) {
    super(host);
  }

  /**
   * Get CSP definition by type (front-end or back-office)
   */
  async get(isBackOffice: boolean) {
    console.log('CSP Manager Repository: Getting CSP definition for isBackOffice:', isBackOffice);
    
    const { data, error } = await tryExecuteAndNotify(
      this,
      Definitions.getDefinitions({
        query: { isBackOffice }
      })
    );

    console.log('CSP Manager Repository: API response:', { data, error });

    if (data) {
      return { data };
    }

    return { error };
  }

  /**
   * Save CSP definition
   */
  async save(definition: CspDefinition) {
    const { data, error } = await tryExecuteAndNotify(
      this,
      Definitions.postDefinitionsSave({
        body: definition
      })
    );

    if (data) {
      return { data };
    }

    return { error };
  }
}

export { UmbCspDefinitionRepository as api };