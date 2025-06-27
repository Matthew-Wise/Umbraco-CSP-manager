import { type UmbEntryPointOnInit, type UmbEntryPointOnUnload } from '@umbraco-cms/backoffice/extension-api';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { client } from '../api';

// load up the manifests here
export const onInit: UmbEntryPointOnInit = (_host, _extensionRegistry) => {

  console.log('CSP Manager Extension Loading 🎉');
  console.log('Extension Registry:', _extensionRegistry);
  console.log('Host:', _host);
  // Will use only to add in Open API config with generated TS OpenAPI HTTPS Client
  // Do the OAuth token handshake stuff
  _host.consumeContext(UMB_AUTH_CONTEXT, async (authContext) => {
    if (!authContext) {
      console.warn('CSP Manager: Auth context not available');
      return;
    }

    // Get the token info from Umbraco
    const config = authContext.getOpenApiConfiguration();
    console.log('CSP Manager: API config from auth context:', {
      base: config.base,
      credentials: config.credentials
    });

    client.setConfig({
      baseUrl: config.base,
      credentials: config.credentials
    });

    // For every request being made, add the token to the headers
    // Can't use the setConfig approach above as its set only once and
    // tokens expire and get refreshed
    client.interceptors.request.use(async (request, _options) => {
      const token = await config.token();
      console.log('CSP Manager: Making API request to:', request.url, 'with token length:', token?.length || 0);
      request.headers.set('Authorization', `Bearer ${token}`);
      return request;
    });

    console.log('CSP Manager: API client configured successfully');
  });
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
  console.log('Goodbye from my extension 👋');
};
