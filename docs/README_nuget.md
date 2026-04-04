# Umbraco.Community.CSPManager

[![Platform](https://img.shields.io/badge/Umbraco-17+-%233544B1?style=flat&logo=umbraco)](https://umbraco.com/products/umbraco-cms/)
[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.CSPManager?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.CSPManager/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.CSPManager?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.CSPManager)
[![GitHub license](https://img.shields.io/github/license/Matthew-Wise/Umbraco-CSP-manager?color=8AB803)](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/LICENSE)
[![Build](https://github.com/Matthew-Wise/Umbraco-CSP-manager/actions/workflows/csp-manager.yml/badge.svg?event=push)](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/.github/workflows/csp-manager.yml)
[![OpenSSF Best Practices](https://www.bestpractices.dev/projects/11084/badge)](https://www.bestpractices.dev/projects/11084)

A Content Security Policy management package for Umbraco CMS. Manage CSP headers for both frontend and backoffice through an intuitive backoffice interface.

## Features

- **Frontend & Backoffice CSP Management** — Configure separate policies for your site frontend and Umbraco backoffice
- **Intuitive Backoffice Interface** — Easy-to-use management screens within the Umbraco backoffice
- **CSP Evaluation Tools** — Test and validate your policies before deployment
- **Nonce Support** — Built-in tag helpers for script, style, and link nonces
- **Flexible Configuration** — Customise CSP directives to match your requirements
- **Notification Events** — Extend behaviour with `CspWritingNotification` and `CspSavedNotification`
- **uSync Integration** — Sync CSP policies across environments using uSync

## Installation

```bash
dotnet add package Umbraco.Community.CSPManager
```

The package registers itself automatically via Umbraco's composer system. No additional setup is required.

![Policy management screen](https://raw.githubusercontent.com/Matthew-Wise/Umbraco-CSP-manager/main/images/management-screen.png "CSP Management section")

**Full documentation**: [matthew-wise.github.io/Umbraco-CSP-manager](https://matthew-wise.github.io/Umbraco-CSP-manager/)

## uSync Integration

| Package | Purpose |
|---|---|
| [Umbraco.Community.CSPManager.uSync](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync/) | Automatically includes CSP definitions in uSync export/import cycles |
| [Umbraco.Community.CSPManager.uSync.Complete](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync.Complete/) | Adds Push/Pull actions via uSync Publisher for on-demand environment sync |

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/CONTRIBUTING.md) and feel free to submit issues and pull requests.

## License

This project is licensed under the MIT License — see the [LICENSE](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/LICENSE) file for details.
