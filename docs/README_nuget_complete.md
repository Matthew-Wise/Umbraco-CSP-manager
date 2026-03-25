# Umbraco.Community.CSPManager.uSync.Complete

[![Umbraco Version](https://img.shields.io/badge/Umbraco-17+-%233544B1?style=flat&logo=umbraco)](#)
[![NuGet Version](https://img.shields.io/nuget/vpre/Umbraco.Community.CSPManager.uSync.Complete)](#)
[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.CSPManager.uSync.Complete)](#)

uSync Publisher support for [Umbraco.Community.CSPManager](https://github.com/Matthew-Wise/Umbraco-CSP-manager) — adds Push/Pull actions to synchronize Content Security Policy configurations between Umbraco environments.

**Full documentation**: [matthew-wise.github.io/Umbraco-CSP-manager](https://matthew-wise.github.io/Umbraco-CSP-manager/integrations/usync-complete)

## Requirements

- Umbraco 17.1+
- [Umbraco.Community.CSPManager](https://www.nuget.org/packages/Umbraco.Community.CSPManager/)
- [Umbraco.Community.CSPManager.uSync](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync/)
- [uSync.Complete](https://www.nuget.org/packages/uSync.Complete/)

## Installation

```bash
dotnet add package Umbraco.Community.CSPManager.uSync.Complete
```

Push and pull CSP policies between environments directly from the backoffice.

![Push CSP policies to a remote environment](https://raw.githubusercontent.com/Matthew-Wise/Umbraco-CSP-manager/main/images/push-screen.png "Push CSP policies")

![Pull CSP policies from a remote environment](https://raw.githubusercontent.com/Matthew-Wise/Umbraco-CSP-manager/main/images/pull-screen.png "Pull CSP policies")

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/CONTRIBUTING.md) and feel free to submit issues and pull requests.

## License

This project is licensed under the MIT License — see the [LICENSE](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/LICENSE) file for details.

## Related Projects

- [Umbraco.Community.CSPManager](https://github.com/Matthew-Wise/Umbraco-CSP-manager) — The main CSP management package
- [Umbraco.Community.CSPManager.uSync](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync/) — Base uSync serialization support (required)
- [uSync](https://github.com/KevinJump/uSync) — Umbraco synchronization framework
