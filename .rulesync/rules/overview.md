---
targets: ["*"]
description: "Project overview and general development guidelines for Umbraco Community CSP Manager"
globs: ["**/*"]
root: true
---

# Umbraco Community CSP Manager - Project Overview

## Purpose & Mission

The Umbraco Community CSP Manager is a comprehensive Content Security Policy management solution for Umbraco CMS that enables developers and content editors to manage security policies through an intuitive backoffice interface. This project transforms complex CSP configuration from a developer-only task into something manageable like content within Umbraco.

## Project Structure

```
src/
├── Umbraco.Community.CSPManager/          # Main package
│   ├── Controllers/                       # API controllers for CSP management
│   ├── Services/                          # Core CSP business logic
│   ├── Middleware/                        # HTTP middleware for CSP header injection
│   ├── Models/                           # Data models and API DTOs
│   ├── Client/                           # TypeScript/Lit frontend components
│   ├── Notifications/                    # Event system for extensibility
│   └── TagHelpers/                      # Razor tag helpers for nonce support
├── Umbraco.Community.CSPManager.TestSite/ # Development/testing site
└── Umbraco.Community.CSPManager.Tests/   # Unit and integration tests
```

## Technology Stack

### Backend (.NET)

- **Framework**: .NET 9 with Umbraco CMS 16+
- **Database**: Uses Umbraco's database abstraction (NPoco ORM)
- **Testing**: XUnit with Umbraco's testing framework
- **Patterns**: Repository pattern, notification pattern, middleware pattern

### Frontend (TypeScript)

- **Framework**: Lit Web Components for Umbraco 16+ compatibility
- **Build Tools**: Vite for development and bundling
- **Testing**: Playwright for E2E testing
- **API**: OpenAPI code generation for type-safe client

## Core Concepts

- **Content Security Policy (CSP)**: Web security standard preventing XSS attacks
- **Dual Context Management**: Separate policies for frontend and Umbraco backoffice
- **Nonce Support**: Automatic generation and injection of cryptographic nonces
- **Policy Evaluation**: Built-in tools for testing CSP policies before deployment
- **Report-Only Mode**: Safe testing without breaking functionality

## Development Principles

- **Security by Design**: All code should enhance security, never compromise it
- **Content-Like Management**: CSP policies should be as easy to manage as Umbraco content
- **Developer Experience**: APIs should be intuitive and well-documented
- **Performance**: Minimal impact on request processing and page load times
- **Extensibility**: Support for custom integrations via notification system
