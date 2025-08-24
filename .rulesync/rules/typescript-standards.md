---
targets: ["*"]
description: "TypeScript coding standards for Umbraco backoffice UI components"
globs: ["src/**/Client/**/*.ts", "!src/**/Client/node_modules/**/*"]
---

# TypeScript Standards

## Code Style

### Formatting

- Follow the .editorconfig rules

```typescript
const definition: CspDefinition = {
  id: "test-id",
  enabled: true,
  reportOnly: false,
  sources: [
    { directive: "script-src", source: "'self'" },
    { directive: "style-src", source: "'self' 'unsafe-inline'" },
  ],
};
```

## Lit Component Patterns

### Component Structure

```typescript
@customElement("csp-management-workspace")
export class CspManagementWorkspaceElement extends UmbLitElement {
  @property({ type: Boolean })
  isBackOffice = false;

  @state()
  private _definition?: CspDefinition;

  @state()
  private _loading = false;

  static styles = css`
    :host {
      display: block;
      padding: var(--uui-size-space-6);
    }
  `;

  render() {
    if (this._loading) {
      return html`<uui-loader></uui-loader>`;
    }

    return html`
      <div class="workspace-content">${this._definition ? this.renderDefinition() : this.renderEmpty()}</div>
    `;
  }
}
```

### Context Integration

```typescript
export class CspDefinitionContext extends UmbControllerBase {
  #data = new UmbObjectState<CspDefinition | undefined>(undefined);
  public readonly data = this.#data.asObservable();

  async load(isBackOffice: boolean) {
    const repository = await this.getContext(CSP_DEFINITION_REPOSITORY_CONTEXT);
    const definition = await repository.getCspDefinition(isBackOffice);
    this.#data.setValue(definition);
  }
}

export const CSP_DEFINITION_CONTEXT = new UmbContextToken<CspDefinitionContext>("CspDefinitionContext");
```

## API Integration

### Repository Pattern

```typescript
export class CspDefinitionRepository {
  constructor(private readonly apiClient: ApiClient) {}

  async getCspDefinition(isBackOffice: boolean): Promise<CspDefinition> {
    try {
      const response = await this.apiClient.get<CspDefinition>(`/definitions?isBackOffice=${isBackOffice}`);
      return response.data;
    } catch (error) {
      if (error instanceof ValidationError) {
        throw new CspValidationError("Invalid CSP definition", error.validationErrors);
      }
      throw new CspError("Failed to load CSP definition", error);
    }
  }
}
```

### Type Safety

```typescript
// Define clear interfaces
interface CspDefinition {
  id: string;
  enabled: boolean;
  reportOnly: boolean;
  isBackOffice: boolean;
  sources: CspDefinitionSource[];
}

interface CspDefinitionSource {
  directive: CspDirective;
  source: string;
}

type CspDirective = "default-src" | "script-src" | "style-src" | "img-src" | "connect-src";
```

## Error Handling

### Custom Error Classes

```typescript
export class CspError extends Error {
  public readonly code: string;
  public readonly context: Record<string, unknown>;

  constructor(message: string, originalError?: Error, code = "CSP_ERROR") {
    super(message);
    this.name = "CspError";
    this.code = code;
    this.context = {
      originalError: originalError?.message,
      timestamp: new Date().toISOString(),
    };
  }
}
```

## Form Validation

### Real-time Validation

```typescript
export class CspSourceFormElement extends UmbLitElement {
  @state()
  private _validationErrors: Record<string, string[]> = {};

  private _validateSource(source: string): string[] {
    const errors: string[] = [];

    if (!source.trim()) {
      errors.push("Source is required");
    }

    if (source.includes("javascript:")) {
      errors.push("JavaScript protocol not allowed");
    }

    return errors;
  }

  private _handleSourceInput(event: InputEvent) {
    const input = event.target as HTMLInputElement;
    const errors = this._validateSource(input.value);

    this._validationErrors = {
      ...this._validationErrors,
      source: errors,
    };

    this._dispatchValidationEvent(!this._hasErrors());
  }
}
```
