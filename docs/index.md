---
title: Home
nav_order: 1
---

# CSP Manager for Umbraco

[![Platform](https://img.shields.io/badge/Umbraco-17+-%233544B1?style=flat&logo=umbraco){: height="20" }](https://umbraco.com/products/umbraco-cms/)
[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.CSPManager?color=cc9900){: height="20" }](https://www.nuget.org/packages/Umbraco.Community.CSPManager/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.CSPManager?color=0273B3){: height="20" }](https://www.nuget.org/packages/Umbraco.Community.CSPManager)
[![GitHub license](https://img.shields.io/github/license/Matthew-Wise/Umbraco-CSP-manager?color=8AB803){: height="20" }](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/LICENSE)
[![Build](https://github.com/Matthew-Wise/Umbraco-CSP-manager/actions/workflows/csp-manager.yml/badge.svg?event=push){: height="20" }](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/.github/workflows/csp-manager.yml)
[![OpenSSF Best Practices](https://www.bestpractices.dev/projects/11084/badge){: height="20" }](https://www.bestpractices.dev/projects/11084)

A Content Security Policy management package for Umbraco CMS. Manage CSP headers for your frontend and backoffice through the Umbraco backoffice.

[Get Started](getting-started){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[View on GitHub](https://github.com/Matthew-Wise/Umbraco-CSP-manager){: .btn .fs-5 .mb-4 .mb-md-0 }

---

## Features

- **Frontend & Backend CSP Management** — Configure separate Content Security Policies for your website frontend and Umbraco backoffice
- **Intuitive Backoffice Interface** — Easy-to-use management screens within the Umbraco backoffice
- **CSP Evaluation Tools** — Test and validate your Content Security Policies before deployment
- **Nonce Support** — Built-in tag helpers for script, style, and link nonces
- **Flexible Configuration** — Customize CSP directives to match your website's requirements
- **Notification Events** — Extend behaviour with `CspWritingNotification` and `CspSavedNotification`
- **uSync Integration** — Sync CSP policies across environments using uSync

## Packages

| Package | NuGet | Purpose |
|---|---|---|
| [Umbraco.Community.CSPManager](https://www.nuget.org/packages/Umbraco.Community.CSPManager/) | [![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.CSPManager?color=0273B3){: height="20" }](https://www.nuget.org/packages/Umbraco.Community.CSPManager) | Main CSP management package |
| [Umbraco.Community.CSPManager.uSync](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync/) | [![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.CSPManager.uSync?color=0273B3){: height="20" }](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync) | uSync export/import integration |
| [Umbraco.Community.CSPManager.uSync.Complete](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync.Complete/) | [![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.CSPManager.uSync.Complete?color=0273B3){: height="20" }](https://www.nuget.org/packages/Umbraco.Community.CSPManager.uSync.Complete) | uSync Publisher push/pull support |
