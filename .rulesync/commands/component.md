---
targets: ["*"]
description: "Guidelines for generating new UI components"
globs: ["src/**/Client/**/*.ts"]
---

# Component Generation Guidelines

## Lit Element Components

When creating new Umbraco UI components, follow these patterns:

### Basic Component Structure
```typescript
import { LitElement, html, css, customElement, property, state } from '@umbraco-ui/lit-element/lit-element.js';

@customElement('csp-[component-name]')
export class Csp[ComponentName]Element extends UmbLitElement {
  @property({ type: String })
  label = '';

  @property({ type: Boolean })
  disabled = false;

  @state()
  private _value = '';

  static styles = css`
    :host {
      display: block;
      padding: var(--uui-size-space-4);
    }
    
    .component-container {
      border: 1px solid var(--uui-color-divider);
      border-radius: var(--uui-border-radius);
    }
  `;

  render() {
    return html`
      <div class="component-container">
        <uui-label for="input">${this.label}</uui-label>
        <uui-input
          id="input"
          .value=${this._value}
          ?disabled=${this.disabled}
          @input=${this._handleInput}
        ></uui-input>
      </div>
    `;
  }

  private _handleInput(event: InputEvent) {
    const input = event.target as HTMLInputElement;
    this._value = input.value;
    this._dispatchChangeEvent();
  }

  private _dispatchChangeEvent() {
    this.dispatchEvent(new CustomEvent('change', {
      detail: { value: this._value },
      bubbles: true,
      composed: true
    }));
  }
}
```

### Form Components
For CSP-specific form components:

```typescript
@customElement('csp-directive-selector')
export class CspDirectiveSelectorElement extends UmbLitElement {
  @property({ type: Array })
  availableDirectives: string[] = [
    'default-src', 'script-src', 'style-src', 'img-src', 
    'font-src', 'connect-src', 'media-src', 'frame-src'
  ];

  @property({ type: String })
  selectedDirective = '';

  @state()
  private _isOpen = false;

  render() {
    return html`
      <uui-combobox
        .value=${this.selectedDirective}
        @change=${this._handleDirectiveChange}
        placeholder="Select CSP directive..."
      >
        ${this.availableDirectives.map(directive => html`
          <uui-combobox-list-option value=${directive}>
            <csp-directive-info .directive=${directive}></csp-directive-info>
          </uui-combobox-list-option>
        `)}
      </uui-combobox>
    `;
  }
}
```

## Workspace Components

For Umbraco workspace integration:

```typescript
@customElement('csp-[feature]-workspace')
export class Csp[Feature]WorkspaceElement extends UmbLitElement implements UmbWorkspaceViewInterface {
  @state()
  private _activeView: string = 'overview';

  render() {
    return html`
      <umb-workspace .headline=${this._getWorkspaceHeadline()}>
        <uui-tab-group slot="header">
          <uui-tab 
            label="Overview"
            ?active=${this._activeView === 'overview'}
            @click=${() => this._setActiveView('overview')}
          >
            Overview
          </uui-tab>
          <uui-tab
            label="Settings" 
            ?active=${this._activeView === 'settings'}
            @click=${() => this._setActiveView('settings')}
          >
            Settings
          </uui-tab>
        </uui-tab-group>

        ${this._renderActiveView()}

        <div slot="actions">
          <uui-button
            @click=${this._handleSave}
            look="primary"
            color="positive"
          >
            Save
          </uui-button>
        </div>
      </umb-workspace>
    `;
  }
}
```

## Component Testing

Always create tests for new components:

```typescript
// tests/components/csp-[component-name].spec.ts
import { test, expect } from '@playwright/test';

test.describe('CSP [ComponentName] Component', () => {
  test.beforeEach(async ({ page }) => {
    await page.setContent(`
      <csp-[component-name] label="Test Component"></csp-[component-name]>
      <script type="module" src="/App_Plugins/UmbracoCommunityCSPManager/umbraco-community-csp-manager.js"></script>
    `);
    await page.waitForSelector('csp-[component-name]');
  });

  test('should render with correct label', async ({ page }) => {
    const component = page.locator('csp-[component-name]');
    const label = component.locator('uui-label');
    
    await expect(label).toContainText('Test Component');
  });

  test('should emit change event when value changes', async ({ page }) => {
    const component = page.locator('csp-[component-name]');
    const input = component.locator('uui-input');

    await input.fill('new value');
    
    // Verify change event was dispatched
    const changeEvents = await page.evaluate(() => {
      return new Promise(resolve => {
        document.addEventListener('change', (e) => {
          resolve(e.detail);
        }, { once: true });
      });
    });

    expect(changeEvents).toEqual({ value: 'new value' });
  });
});
```

## Component Manifest Registration

Register new components in the manifest system:

```typescript
// src/bundle.manifests.ts
import { ManifestElement } from '@umbraco-cms/backoffice/extension-registry';

const elements: Array<ManifestElement> = [
  {
    type: 'element',
    alias: 'Csp.[ComponentName].Element',
    name: 'CSP [Component Name] Element',
    element: () => import('./components/csp-[component-name].element.js'),
  }
];

export const manifests = [...elements, ...workspaces, ...sections];
```