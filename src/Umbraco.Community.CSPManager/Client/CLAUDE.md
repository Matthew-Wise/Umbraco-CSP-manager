# CSP Manager - Frontend Client

## Build Commands

- `npm run build:ts` - TypeScript checking only (fast, use during development)
- `npm run build` - Full build (TypeScript + Vite bundling)
- `npm run dev` - Dev server with hot reload
- `npm run generate-client` - Regenerate OpenAPI client from the backend OpenAPI document (test site
  running on 44370). `@hey-api/openapi-ts` 0.99 can't run on TypeScript 7: until it can, run
  `npm i --no-save typescript@5.9.3` first, then `npx prettier --write src/api` and `npm ci` after.

## Testing

- `npm test` - Run Playwright E2E tests
- `npm run test:ui` - Playwright UI mode for debugging
- `npm run test:headed` - See browser during tests
- Playwright needs the test site running on 44370 and a `.env` in this folder (gitignored) with
  `URL=https://localhost:44370`, `UMBRACO_USER_LOGIN=admin@example.com` and
  `UMBRACO_USER_PASSWORD=1234567890`. In a cloud session also set
  `NO_PROXY=localhost,127.0.0.1,csp-test.localhost` and run `npx playwright test --trace off`.
- `domain-policies.spec.ts` expects these hostnames on the test site: Homepage `/en-gb`, `/en-us`
  and `csp-test.localhost:44370`, Test Pages `/test-pages`. A fresh test site doesn't have them
  all, so set them under Culture and Hostnames first.
- `CspApiHelpers` sets up auth cookies and bearer placeholder headers in E2E specs
- Custom test ID attribute: `data-mark`

## Architecture

- Lit 3.x web components extending `UmbLitElement`
- Manifest-based extension registration (all manifests aggregated in `bundle.manifests.ts`)
- Observable state via `UmbObjectState` with `observe()` subscriptions
- Repository pattern: repositories handle API calls, contexts manage state
- Domain policies: child nodes under Frontend sharing `csp-policy` entity type and workspace (`isGlobalPolicyId()` distinguishes them). "Add Domain Policy" picks a content node with a hostname and copies the frontend policy to draft `create/{contentKey}`; policies are labelled with the node name (`domainPolicyLabel`).
- Backoffice links and redirects come from `src/paths.ts` (`cspEditPolicyPath`, `cspCreateDomainPolicyPath`), built on Umbraco's `UmbPathPattern`; the workspace routes use the same patterns. Never hand-write `section/csp-manager/...` URLs. `tests/dashboard.spec.ts` clicks the dashboard links to catch a broken route.
- `src/api/` is auto-generated from OpenAPI spec - do not edit manually. The API enums are string enums
  (`DisabledNodePolicyBehavior.NO_HEADER` is `'NoHeader'`): compare with the generated enum, not a number.

## Umbraco v18 Patterns

### Context Tokens - always use `UmbContextToken` with two parameters:

```typescript
new UmbContextToken<MyContext>('ContextAlias', 'unique.api.alias')
```

- Workspace contexts use `'UmbWorkspaceContext'` as the context alias
- Import `UmbContextToken` from `@umbraco-cms/backoffice/context-api`
- Import `UmbContextBase` from `@umbraco-cms/backoffice/class-api`
- Import `UmbRepositoryBase` from `@umbraco-cms/backoffice/repository`

### HTTP Client - must include credentials for cookie auth:

```typescript
credentials: 'include'  // Required for v18 authentication
```

## Key Files

- `src/constants.ts` - Package aliases, policy type UUIDs
- `src/workspace/context/workspace.context.ts` - Main workspace state and logic
- `vite.config.ts` - Build config, output to `wwwroot/App_Plugins/UmbracoCommunityCSPManager/`
