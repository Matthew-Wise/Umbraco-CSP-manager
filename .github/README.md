# Umbraco.Community.CSPManager

[![Platform](https://img.shields.io/badge/Umbraco-16.1+-%233544B1?style=flat&logo=umbraco)](https://umbraco.com/products/umbraco-cms/)
[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.CSPManager?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.CSPManager/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.CSPManager?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.CSPManager)
[![GitHub license](https://img.shields.io/github/license/Matthew-Wise/Umbraco-CSP-manager?color=8AB803)](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/LICENSE)
[![Build](https://github.com/Matthew-Wise/Umbraco-CSP-manager/actions/workflows/main.yml/badge.svg?event=push)](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/.github/workflows/main.yml)

A comprehensive Content Security Policy (CSP) management package for Umbraco CMS that helps protect your website from XSS attacks and other code injection vulnerabilities. Manage CSP headers for both frontend and backend through an intuitive backoffice interface.

## Features

- 🛡️ **Frontend & Backend CSP Management** - Configure different Content Security Policies for your website frontend and Umbraco backoffice
- 🎛️ **Intuitive Backoffice Interface** - Easy-to-use management screens within the Umbraco backoffice
- 🔍 **CSP Evaluation Tools** - Test and validate your Content Security Policies before deployment
- 🏷️ **Nonce Support** - Built-in tag helpers for script and style nonces
- ⚙️ **Flexible Configuration** - Customize CSP directives to match your website's requirements
- 📊 **Real-time Testing** - Evaluate CSP effectiveness with built-in testing tools

## Table of Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Policy Management](#csp-management)
- [Policy Settings](#configuration)
- [Evaluation](#evaluation)
- [Configuration Options](#configuration-options)
- [Nonce Tag Helper](#nonce-tag-helper)
- [Advanced Usage](#advanced-usage)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)

## Getting Started

```bash
dotnet add package Umbraco.Community.CSPManager
```

## Quick Start

1. **Install the package** using the command above
2. **Build and run** your Umbraco application
3. **Navigate to the CSP Management section** in the Umbraco backoffice
4. **Configure your Content Security Policies** for frontend and/or backend
5. **Test your configuration** using the evaluation tools

## Policy Management

The CSP Manager provides an intuitive interface for managing Content Security Policy directives. The UI groups configuration by source first, then allows you to select which directives apply to each source.

This approach allows for flexible CSP configuration where the same source can be applied to multiple directives, or different sources can be used for the same directive.

![Policy section](https://raw.githubusercontent.com/Matthew-Wise/Umbraco-CSP-manager/main/images/management-screen.png "Csp Management section")

### Special Sources
To add `'strict-dynamic'` to your CSP:
1. Navigate to the Policy Management section
2. Add a new source entry with the value `'strict-dynamic'`
3. Select the directive(s) you want to apply it to (typically `script-src`)
4. Save your configuration

## Policy Settings

![Policy Settings section](https://raw.githubusercontent.com/Matthew-Wise/Umbraco-CSP-manager/main/images/settings-screen.png "Policy Settings section")

## Evaluation

![CSP Evaluation section](https://raw.githubusercontent.com/Matthew-Wise/Umbraco-CSP-manager/main/images/evaluate-screen.png "Csp Evaluation section")

### Configuration Options

You can configure CSP Manager behavior in your `appsettings.json`:

```json
{
  "CspManager": {
    "DisableBackOfficeHeader": false
  }
}
```

**DisableBackOfficeHeader** - Emergency kill switch to disable CSP headers for the backoffice if needed (default: `false`)

## Nonce Tag Helper

To use CSP nonce you can make use of the Tag Helper. To find out more about nonce see see [nonce Guide](https://content-security-policy.com/nonce/).

First you will need to include the namespace in the `ViewImports.cshtml`

```csharp
@addTagHelper *, Umbraco.Community.CSPManager
```

To use the nonce add `csp-manager-add-nonce="true"` to your `<script>` or `<style>` tags.

The nonce values shown are for demo purposes only.

```html
<script csp-manager-add-nonce="true"></script>
<style csp-manager-add-nonce="true"></style>

<!-- Output (nonce values are auto-generated): -->
<script nonce="scriptRAnd0m">
  doWhatever();
</script>
<style nonce="styleRAnd0m">
  .alert {
    color: red;
  }
</style>
```

When this is added it will include the nonce in the CSP header and output in the page.

If you need to access the nonce within a data attribute you can use `csp-manager-add-nonce-data-attribute="true"`

```html
<script csp-manager-add-nonce-data-attribute="true"></script>
<style csp-manager-add-nonce-data-attribute="true"></style>

<!-- Output (nonce values are auto-generated): -->
<script data-nonce="scriptRAnd0m">
  doWhatever();
</script>
<style data-nonce="styleRAnd0m">
  .alert {
    color: red;
  }
</style>
```

## Advanced Usage

### Notification Events

The CSP Manager provides notification events that allow you to extend functionality and integrate with your application logic.

#### CspWritingNotification

Triggered when building a CSP definition for an HTTP request. Use this to dynamically modify Content Security Policies based on request context.

```csharp
using Umbraco.Cms.Core.Events;
using Umbraco.Community.CSPManager.Notifications;

public class CustomCspWritingHandler : INotificationHandler<CspWritingNotification>
{
    public void Handle(CspWritingNotification notification)
    {
        // Modify CSP definition based on request context
        if (notification.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            // Apply different CSP for API endpoints
            notification.CspDefinition?.Directives.Add("connect-src", "'self' api.example.com");
        }
    }
}
```

#### CspSavedNotification

Triggered when a CSP definition is saved through the backoffice. Use this for cache invalidation, logging, or integration with external systems.

```csharp
public class CustomCspSavedHandler : INotificationHandler<CspSavedNotification>
{
    public void Handle(CspSavedNotification notification)
    {
        // Log CSP changes
        var csp = notification.CspDefinition;
        Logger.Information("CSP policy updated for {Area}",
            csp.IsBackOffice ? "BackOffice" : "Frontend");

        // Integrate with external monitoring
        // NotifySecurityTeam(csp);
    }
}
```

#### Registering Notification Handlers

Register your custom handlers in your `Startup.cs` or `Program.cs`:

```csharp
services.AddNotificationHandler<CspWritingNotification, CustomCspWritingHandler>();
services.AddNotificationHandler<CspSavedNotification, CustomCspSavedHandler>();
```

### Getting Help

If you encounter issues not covered here:

1. Check the [GitHub Issues](https://github.com/Matthew-Wise/Umbraco-CSP-manager/issues) page
2. Review the full documentation (link below)
3. Create a new issue with detailed information about your problem

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/CONTRIBUTING.md) and feel free to submit issues and pull requests.

## Contributing with AI Tools

This project is optimized for development with AI coding assistants. We provide instruction files for popular AI tools to help maintain consistency with our established patterns and testing standards.

### Using rulesync

The project includes rulesync configuration files that can automatically generate instruction files for 19+ AI development tools. Generate configuration files for your preferred AI tools:

```bash
# Generate only for Claude Code
npx rulesync generate --claudecode

# Generate only for Cursor
npx rulesync generate --cursor

# Generate only for Vs Code Copilot
npx rulesync generate --copilot
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/LICENSE) file for details.
