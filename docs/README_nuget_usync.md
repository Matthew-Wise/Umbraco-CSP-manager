# Umbraco.Community.CSPManager.uSync

[![Umbraco Version](https://img.shields.io/badge/Umbraco-17+-%233544B1?style=flat&logo=umbraco)](#)
[![NuGet Version](https://img.shields.io/nuget/vpre/Umbraco.Community.CSPManager.uSync)](#)
[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.CSPManager.uSync)](#)

uSync support for [Umbraco.Community.CSPManager](https://github.com/Matthew-Wise/Umbraco-CSP-manager) - automatically synchronize your Content Security Policy configurations across Umbraco environments.

## Table of Contents

- [Requirements](#requirements)
- [What This Package Adds](#what-this-package-adds)
- [Installation](#installation)
- [Contributing](#contributing)
- [License](#license)

## Requirements

- Umbraco 17+
- [Umbraco.Community.CSPManager](https://www.nuget.org/packages/Umbraco.Community.CSPManager/) 17+
- [uSync.BackOffice](https://www.nuget.org/packages/uSync.BackOffice/) 17+

## What This Package Adds

Once installed, uSync will automatically include your CSP definitions in its export/import cycle — syncing CSP configurations across environments alongside your other Umbraco settings.

![uSync synchronization of CSP definitions](https://raw.githubusercontent.com/Matthew-Wise/Umbraco-CSP-manager/main/images/usync-screen.png "uSync synchronization")

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Umbraco.Community.CSPManager.uSync
```

Or via the Package Manager Console:

```powershell
Install-Package Umbraco.Community.CSPManager.uSync
```

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/CONTRIBUTING.md) and feel free to submit issues and pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

## Related Projects

- [Umbraco.Community.CSPManager](https://github.com/Matthew-Wise/Umbraco-CSP-manager) - The main CSP management package
- [uSync](https://github.com/KevinJump/uSync9) - Umbraco synchronization framework
