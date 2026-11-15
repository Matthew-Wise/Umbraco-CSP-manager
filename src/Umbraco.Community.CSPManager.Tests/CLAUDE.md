# CSP Manager - Tests

## Framework

- NUnit (not xUnit) with `Umbraco.Cms.Tests.Integration`
- Moq for dependency mocking
- `CspGlobalSetupTeardown` for test lifecycle

## Running Tests

- `dotnet test` from this directory or solution root

## Pitfalls

- API tests (`CspManagementApiTest`) wait for Umbraco's `UnattendedUpgradeBackgroundService` before
  touching the database. In a few tests in a hundred Umbraco finishes booting in that background
  service, and a test that writes while it runs deadlocks with it on SQLite's table locks, which
  looks like a hung run. Keep that wait first in the base `SetUp`.
- Umbraco's in-memory navigation structure isn't kept up to date in the test host: call
  `IDocumentNavigationManagementService.RebuildAsync()` after creating or moving content when a
  test depends on ancestors.
- A hang names its test when run with `--blame-hang-timeout 3m` (CI passes `5m`).
