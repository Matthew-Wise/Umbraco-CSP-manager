---
title: Getting Started
nav_order: 2
---

# Getting Started

## Installation

Install the main package via the .NET CLI:

```bash
dotnet add package Umbraco.Community.CSPManager
```

Or via the NuGet Package Manager Console:

```powershell
Install-Package Umbraco.Community.CSPManager
```

The package registers itself automatically via Umbraco's composer pattern — no additional configuration in `Program.cs` or `Startup.cs` is required.

## Quick Start

1. **Install the package** using the command above
2. **Build and run** your Umbraco application
3. **Navigate to the CSP Management section** in the Umbraco backoffice left-hand navigation
4. **Configure your Content Security Policies** for frontend and/or backoffice
5. **Test your configuration** using the built-in evaluation tools

## Requirements

- Umbraco 17+
- .NET 10+

## Next Steps

- [Policy Management](guide/policy-management) — Learn how to add and manage CSP sources and directives
- [Policy Settings](guide/policy-settings) — Configure report-only mode, reporting endpoints, and more
- [Nonce Tag Helper](features/nonce-tag-helper) — Add nonces to your script and style tags
- [Configuration](features/configuration) — Application-level configuration options
