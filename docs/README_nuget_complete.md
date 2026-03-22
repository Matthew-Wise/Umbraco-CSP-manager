# Umbraco.Community.CSPManager.uSync.Complete

[![Umbraco Version](https://img.shields.io/badge/Umbraco-17+-%233544B1?style=flat&logo=umbraco)](#)
[![NuGet Version](https://img.shields.io/nuget/vpre/Umbraco.Community.CSPManager.uSync.Complete)](#)
[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.CSPManager.uSync.Complete)](#)

uSync Complete (Publisher) support for [Umbraco.Community.CSPManager](https://github.com/Matthew-Wise/Umbraco-CSP-manager) - adds Push/Pull entity actions to synchronize Content Security Policy configurations between Umbraco environments using uSync Publisher.

## Table of Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [What This Package Adds](#what-this-package-adds)
- [Contributing](#contributing)
- [License](#license)

## Requirements

- Umbraco 17.1+
- [Umbraco.Community.CSPManager](https://www.nuget.org/packages/Umbraco.Community.CSPManager/)
- [Umbraco.Community.CSPManager.uSync](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync/)
- [uSync.Complete](https://www.nuget.org/packages/uSync.Complete/)

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Umbraco.Community.CSPManager.uSync.Complete
```

Or via the Package Manager Console:

```powershell
Install-Package Umbraco.Community.CSPManager.uSync.Complete
```

## What This Package Adds

This package extends the base [Umbraco.Community.CSPManager.uSync](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync/) package with uSync Publisher support, adding:

- **Push** - Push CSP policies from your current environment to a remote Umbraco instance
- **Pull** - Pull CSP policies from a remote Umbraco instance into your current environment

These actions are available on both individual CSP policy items and the CSP Manager tree root in the backoffice.

![Push CSP policies to a remote environment](https://raw.githubusercontent.com/Matthew-Wise/Umbraco-CSP-manager/main/images/push-screen.png "Push CSP policies")

![Pull CSP policies from a remote environment](https://raw.githubusercontent.com/Matthew-Wise/Umbraco-CSP-manager/main/images/pull-screen.png "Pull CSP policies")

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/CONTRIBUTING.md) and feel free to submit issues and pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

## Related Projects

- [Umbraco.Community.CSPManager](https://github.com/Matthew-Wise/Umbraco-CSP-manager) - The main CSP management package
- [Umbraco.Community.CSPManager.uSync](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync/) - Base uSync serialization support
- [uSync](https://github.com/KevinJump/uSync9) - Umbraco synchronization framework
