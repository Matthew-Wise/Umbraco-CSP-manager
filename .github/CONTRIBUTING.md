# Contributing Guidelines

Contributions to this package are most welcome! Whether it's a bug fix, new feature, or documentation improvement, we appreciate your help.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- An IDE such as Visual Studio, Rider, or VS Code

## Getting Started

1. Fork and clone the repository
2. Open the solution file at `src/Umbraco.Community.CSPManager.slnx`
3. Restore dependencies:
   ```bash
   dotnet restore src/Umbraco.Community.CSPManager.slnx
   ```
4. Build the solution:
   ```bash
   dotnet build src/Umbraco.Community.CSPManager.slnx
   ```

## Project Structure

```
src/
├── Umbraco.Community.CSPManager/            # Main package
│   └── Client/                              # TypeScript/Lit frontend
├── Umbraco.Community.CSPManager.TestSite/   # Dev/testing site (port 44370)
├── Umbraco.Community.CSPManager.Tests/      # Unit/integration tests
├── Umbraco.Community.CSPManager.Benchmarks/ # Performance benchmarks
└── uSync/                                   # uSync integration packages
    ├── Umbraco.Community.CSPManager.uSync/          # Base uSync serialization
    ├── Umbraco.Community.CSPManager.uSync.Complete/  # uSync Publisher push/pull
    ├── uSync.TestSite/                      # Test site A (port 44381)
    └── uSync.TestSiteB/                     # Test site B (port 44382)
```

## Running the Test Site

The main test site (`Umbraco.Community.CSPManager.TestSite`) is configured for unattended install, so it will set itself up automatically on first run. Login credentials are in `appsettings.json`:

- **Email:** admin@example.com
- **Password:** 1234567890

Run the test site:

```bash
dotnet run --project src/Umbraco.Community.CSPManager.TestSite
```

It will be available at `https://localhost:44370`.

## Frontend Development

The frontend client lives in `src/Umbraco.Community.CSPManager/Client/` and uses Lit 3.x web components with Vite.

```bash
cd src/Umbraco.Community.CSPManager/Client

# Install dependencies
npm install

# Dev server with hot reload
npm run dev

# Full build (TypeScript + Vite)
npm run build

# Regenerate OpenAPI client from backend swagger
npm run generate-client
```

> **Note:** Files in `src/Umbraco.Community.CSPManager/Client/src/api/` are auto-generated from the OpenAPI spec. Do not edit them manually — use `npm run generate-client` instead.

## Running Tests

### Backend Tests (NUnit)

```bash
dotnet test src/Umbraco.Community.CSPManager.Tests
```

### Frontend Tests (Playwright)

```bash
cd src/Umbraco.Community.CSPManager/Client

npm test              # Run E2E tests
npm run test:ui       # Playwright UI mode for debugging
npm run test:headed   # See the browser during tests
```

## Submitting a Pull Request

1. Create a feature branch from `main`
2. Make your changes, keeping commits focused and well-described
3. Ensure all tests pass before submitting
4. Open a pull request against `main` with a clear description of what you changed and why

## Code Style

- Backend code follows standard C# conventions and uses the `.editorconfig` in the repo root
- Frontend code uses TypeScript with Lit web components following Umbraco v17 patterns

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](../LICENSE).