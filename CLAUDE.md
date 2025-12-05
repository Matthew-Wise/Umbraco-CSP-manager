Please also reference the following documents as needed:

@.claude/memories/csharp-standards.md description: "C# coding standards and best practices for CSP Manager backend" globs: "src/**/*.cs,!src/**/Client/**/*"
@.claude/memories/security-practices.md description: "Security practices specific to CSP management and web security" globs: "src/**/*,!**/node_modules/**/*"
@.claude/memories/testing-guidelines.md description: "Testing standards for both backend and frontend code" globs: "**/*test*,**/*spec*,**/Tests/**/*"
@.claude/memories/typescript-standards.md description: "TypeScript coding standards for Umbraco backoffice UI components" globs: "src/**/Client/**/*.ts,!src/**/Client/node_modules/**/*"

# Umbraco Community CSP Manager - Project Overview

## Purpose & Mission

The Umbraco Community CSP Manager is a comprehensive Content Security Policy management solution for Umbraco CMS that enables developers and content editors to manage security policies through an intuitive backoffice interface. This project transforms complex CSP configuration from a developer-only task into something manageable like content within Umbraco.

## Project Structure

```
src/
├── Umbraco.Community.CSPManager/          # Main package
│   ├── Controllers/                       # API controllers for CSP management
│   ├── Services/                          # Core CSP business logic
│   ├── Middleware/                        # HTTP middleware for CSP header injection
│   ├── Models/                           # Data models and API DTOs
│   ├── Client/                           # TypeScript/Lit frontend components
│   ├── Notifications/                    # Event system for extensibility
│   └── TagHelpers/                      # Razor tag helpers for nonce support
├── Umbraco.Community.CSPManager.TestSite/ # Development/testing site
└── Umbraco.Community.CSPManager.Tests/   # Unit and integration tests
```

## Technology Stack

### Backend (.NET)

- **Framework**: .NET 9 with Umbraco CMS 17+
- **Database**: Uses Umbraco's database abstraction (NPoco ORM)
- **Testing**: XUnit with Umbraco's testing framework
- **Patterns**: Repository pattern, notification pattern, middleware pattern

### Frontend (TypeScript)

- **Framework**: Lit Web Components (v3.x) for Umbraco 17+ backoffice
- **Build Tools**: Vite 7.x for development and bundling
- **Testing**: Playwright for E2E testing
- **API**: OpenAPI code generation via @hey-api/openapi-ts
- **Node**: Requires Node.js 22+

## Umbraco v17 Patterns

### Context Token Pattern

All contexts must use `UmbContextToken` with two parameters:

```typescript
// Define token with context alias and unique API alias
export const UMB_MY_CONTEXT = new UmbContextToken<MyContext>(
  'MyContextType',        // Context alias (for grouping)
  'my-package.context'    // API alias (unique identifier)
);

// Use token in constructor (not a string)
constructor(host: UmbControllerHost) {
  super(host, UMB_MY_CONTEXT);
}
```

### Workspace Context Token

Workspace contexts should use `'UmbWorkspaceContext'` as the context alias:

```typescript
export const UMB_CSP_MANAGER_WORKSPACE_CONTEXT = new UmbContextToken<UmbCspManagerWorkspaceContext>(
  'UmbWorkspaceContext',      // Shared workspace context alias
  'csp-manager.workspace'     // Unique API alias
);
```

### HTTP Client Configuration

API clients must include credentials for cookie-based authentication:

```typescript
export const client = createClient(createConfig<ClientOptions>({
  baseUrl: 'https://localhost:44370',
  throwOnError: true,
  credentials: 'include'  // Required for v17 authentication
}));
```

### Import Paths

- `UmbContextToken`: Import from `@umbraco-cms/backoffice/context-api`
- `UmbContextBase`: Import from `@umbraco-cms/backoffice/class-api`
- `UmbRepositoryBase`: Import from `@umbraco-cms/backoffice/repository`

## Core Concepts

- **Content Security Policy (CSP)**: Web security standard preventing XSS attacks
- **Dual Context Management**: Separate policies for frontend and Umbraco backoffice
- **Nonce Support**: Automatic generation and injection of cryptographic nonces
- **Policy Evaluation**: Built-in tools for testing CSP policies before deployment
- **Report-Only Mode**: Safe testing without breaking functionality

## Development Principles

- **Security by Design**: All code should enhance security, never compromise it
- **Content-Like Management**: CSP policies should be as easy to manage as Umbraco content
- **Developer Experience**: APIs should be intuitive and well-documented
- **Performance**: Minimal impact on request processing and page load times
- **Extensibility**: Support for custom integrations via notification system
