import { LitElement, css, html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";

@customElement('umb-csp-section-dashboard')
export class UmbCspSectionDashboardElement extends UmbElementMixin(LitElement) {

  override connectedCallback() {
    super.connectedCallback();
  }

  override render() {
    return html`
      <uui-box headline="CSP Manager">
        <div class="dashboard-content">
          <div class="welcome-section">
            <h2>Content Security Policy Management</h2>
            <p>Welcome to the CSP Manager. Use the tree navigation on the left to manage your Content Security Policy settings for both front-end and back-office environments.</p>
          </div>
          
          <div class="info-cards">
            <uui-card>
              <div class="card-content">
                <h3>Front-end CSP</h3>
                <p>Configure Content Security Policy for your website visitors. This controls what resources can be loaded on your public-facing pages.</p>
                <uui-button 
                  look="primary" 
                  href="/umbraco/section/csp-manager/workspace/frontend"
                  label="Configure Front-end CSP"
                >
                  Configure Front-end CSP
                </uui-button>
              </div>
            </uui-card>

            <uui-card>
              <div class="card-content">
                <h3>Back-office CSP</h3>
                <p>Configure Content Security Policy for the Umbraco back-office. This controls what resources can be loaded in the admin interface.</p>
                <uui-button 
                  look="primary" 
                  href="/umbraco/section/csp-manager/workspace/backoffice"
                  label="Configure Back-office CSP"
                >
                  Configure Back-office CSP
                </uui-button>
              </div>
            </uui-card>
          </div>

          <div class="help-section">
            <h3>About Content Security Policy</h3>
            <p>Content Security Policy (CSP) is a security standard which helps prevent cross-site scripting (XSS), clickjacking and other code injection attacks. It works by restricting the sources from which content can be loaded.</p>
            <p>
              <a href="https://content-security-policy.com/" target="_blank">
                Learn more about CSP at content-security-policy.com
              </a>
            </p>
          </div>
        </div>
      </uui-box>
    `;
  }

  static override styles = [
    css`
      :host {
        display: block;
        padding: var(--uui-size-layout-1);
      }

      .dashboard-content {
        max-width: 1200px;
        margin: 0 auto;
      }

      .welcome-section {
        margin-bottom: var(--uui-size-space-6);
      }

      .welcome-section h2 {
        margin: 0 0 var(--uui-size-space-3) 0;
        color: var(--uui-color-text);
        font-size: 1.5rem;
        font-weight: 400;
      }

      .welcome-section p {
        margin: 0;
        color: var(--uui-color-text-alt);
        font-size: 1rem;
        line-height: 1.5;
      }

      .info-cards {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
        gap: var(--uui-size-space-5);
        margin-bottom: var(--uui-size-space-6);
      }

      .card-content {
        padding: var(--uui-size-space-4);
      }

      .card-content h3 {
        margin: 0 0 var(--uui-size-space-3) 0;
        color: var(--uui-color-text);
        font-size: 1.2rem;
        font-weight: 500;
      }

      .card-content p {
        margin: 0 0 var(--uui-size-space-4) 0;
        color: var(--uui-color-text-alt);
        line-height: 1.4;
      }

      .help-section {
        background: var(--uui-color-surface-alt);
        padding: var(--uui-size-space-4);
        border-radius: var(--uui-border-radius);
      }

      .help-section h3 {
        margin: 0 0 var(--uui-size-space-3) 0;
        color: var(--uui-color-text);
        font-size: 1.1rem;
        font-weight: 500;
      }

      .help-section p {
        margin: 0 0 var(--uui-size-space-2) 0;
        color: var(--uui-color-text-alt);
        line-height: 1.4;
      }

      .help-section p:last-child {
        margin-bottom: 0;
      }
    `
  ];
}

export default UmbCspSectionDashboardElement;

declare global {
  interface HTMLElementTagNameMap {
    'umb-csp-section-dashboard': UmbCspSectionDashboardElement;
  }
}