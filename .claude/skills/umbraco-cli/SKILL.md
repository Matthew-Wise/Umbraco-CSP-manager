---
name: umbraco-cli
description: Connect the Umbraco CLI (@umbraco-cms/mcp-dev) to one of the three local test sites (CSP Manager, uSync A, uSync B) to inspect or change backoffice data while manually testing. Use when asked to query, list, check or change content, settings or CSP data on a running test site.
---

# Umbraco CLI against the local test sites

The Umbraco Developer MCP server doubles as a CLI: `npx @umbraco-cms/mcp-dev@latest`
(the `@latest` tag is the 18.x line, matching this repo's Umbraco 18). It reads its
connection from a `.env` file, and `--env <path>` picks which one, so each test site
has its own file in the repo root.

| Site | Project | URL | Env file |
|---|---|---|---|
| CSP Manager | `src/Umbraco.Community.CSPManager.TestSite` | https://localhost:44370 | `.env.csp-manager` |
| uSync A | `src/uSync/uSync.TestSite` | https://localhost:44381 | `.env.usync-a` |
| uSync B | `src/uSync/uSync.TestSiteB` | https://localhost:44382 | `.env.usync-b` |

If the user doesn't say which site, use CSP Manager. For uSync Publisher push/pull
testing, A and B are paired (each one's `appsettings.Development.json` points at the other).

## Before calling the CLI

1. **Env file.** The three `.env.<site>` files are committed in the repo root. They hold
   dev-only credentials for these localhost sites, so it's fine to use them as they are.
   Don't point them at any other site, and don't commit any other `.env*` file.
2. **Site running.** The CLI talks to a live site. If a request fails to connect, start it:
   `dotnet run --project <project> --launch-profile Umbraco.Web.UI`.
3. **API user.** Each site has its own SQLite database. In Development, each site creates
   the `umbraco-back-office-mcp` API user on startup if it's missing (`DevApiUserSeeder.cs`).
   An authentication error usually means the env file and `TestSite:ApiUser` disagree.

## Cloud sessions

A cloud session can't reach the user's localhost, so it runs the site itself in the
background (`ASPNETCORE_ENVIRONMENT=Development dotnet run --project <project> --launch-profile Umbraco.Web.UI &`)
and waits for `/umbraco/management/api/v1/server/status` to return 200. The SQLite databases
aren't committed, so a fresh clone installs an empty site. Each site's `DevApiUserSeeder`
then creates the API user from `TestSite:ApiUser` in `appsettings.Development.json` (the same
values as the env file), so the CLI can log in straight away.

## Commands

Run from the repo root, so the relative `--env` path resolves:

```bash
# Check the resolved config and which file it came from (secrets masked)
npx @umbraco-cms/mcp-dev@latest --env .env.csp-manager --debug-config

# Discover tools, then the arguments one takes
npx @umbraco-cms/mcp-dev@latest --env .env.csp-manager --list-tools
npx @umbraco-cms/mcp-dev@latest --env .env.csp-manager --describe-tool <tool-name>

# Call a tool (prints JSON)
npx @umbraco-cms/mcp-dev@latest --env .env.usync-a --call <tool-name> --call-args '{"id": "..."}'
```

`--list-tools`, `--describe-tool` and `--debug-config` don't need the site running.
