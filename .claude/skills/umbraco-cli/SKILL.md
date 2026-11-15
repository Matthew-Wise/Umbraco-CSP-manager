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

1. **Env file.** The `.env.<site>` files are gitignored and hold the API user's secret.
   If the one you need is missing, copy `.claude/skills/umbraco-cli/templates/.env.<site>.example`
   to the repo root as `.env.<site>` and ask the user for the client secret. Never invent
   one, never commit a `.env*` file, and never print the secret back in chat.
2. **Site running.** The CLI talks to a live site. If a request fails to connect, start it:
   `dotnet run --project <project> --launch-profile Umbraco.Web.UI`.
3. **API user.** Each site has its own SQLite database, so the API user (client ID
   `umbraco-back-office-mcp` by default) must exist in each site's backoffice under
   Users > API users, with the same secret as the env file. An authentication error
   usually means it doesn't.

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

## Safety

- Default to reading. For anything that changes data, say what you're about to change and
  on which site before running it. Add `--umbraco-dry-run` to preview a change, or
  `--umbraco-readonly` to drop every mutation tool when you only need to look.
- `NODE_TLS_REJECT_UNAUTHORIZED=0` in the env files accepts the sites' self-signed dev
  certificate. It is only for these localhost URLs; never point these files at a remote site.
- These are dev-only sites and credentials. Don't reuse the files or the secret anywhere else.
