# Umbraco.Community.CSPManager.uSync

[![Umbraco Version](https://img.shields.io/badge/Umbraco-17+-%233544B1?style=flat&logo=umbraco)](#)
[![NuGet Version](https://img.shields.io/nuget/vpre/Umbraco.Community.CSPManager.uSync)](#)
[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.CSPManager.uSync)](#)

uSync support for [Umbraco.Community.CSPManager](https://github.com/Matthew-Wise/Umbraco-CSP-manager) — automatically synchronize your Content Security Policy configurations across Umbraco environments.

**Full documentation**: [matthew-wise.github.io/Umbraco-CSP-manager](https://matthew-wise.github.io/Umbraco-CSP-manager/integrations/usync)

## Requirements

- Umbraco 17+
- [Umbraco.Community.CSPManager](https://www.nuget.org/packages/Umbraco.Community.CSPManager/) 17+
- [uSync.BackOffice](https://www.nuget.org/packages/uSync.BackOffice/) 17+

## Installation

```bash
dotnet add package Umbraco.Community.CSPManager.uSync
```

Once installed, uSync automatically includes your CSP definitions in its export/import cycle — syncing CSP configurations across environments alongside your other Umbraco settings.

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/CONTRIBUTING.md) and feel free to submit issues and pull requests.

## License

This project is licensed under the MIT License — see the [LICENSE](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/LICENSE) file for details.

## Related Projects

- [Umbraco.Community.CSPManager](https://github.com/Matthew-Wise/Umbraco-CSP-manager) — The main CSP management package
- [Umbraco.Community.CSPManager.uSync.Complete](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync.Complete/) — Adds uSync Publisher push/pull support
- [uSync](https://github.com/KevinJump/uSync) — Umbraco synchronization framework
