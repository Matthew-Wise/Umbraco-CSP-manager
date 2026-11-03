#!/bin/bash
# Prepares a Claude Code cloud session to build and test the repo:
# .NET 10 SDK (global.json), Node 24 (client engines), NuGet + npm restore.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

cd "$CLAUDE_PROJECT_DIR"

# .NET SDK: the dotnet.microsoft.com download hosts are blocked by the default
# network policy, so install the Ubuntu-packaged 10.0.1xx SDK instead.
if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks | grep -q '^10\.0\.'; then
  export DEBIAN_FRONTEND=noninteractive
  apt-get update -qq
  apt-get install -y -qq dotnet-sdk-10.0 >/dev/null
fi

# Node: the clients declare engines node >=24; the image ships Node 22 and puts
# it first on PATH. Single place to change the Node version for cloud sessions.
NODE_MAJOR=24

export NVM_DIR="${NVM_DIR:-/opt/nvm}"
# shellcheck disable=SC1091
source "$NVM_DIR/nvm.sh"
nvm install "$NODE_MAJOR" >/dev/null
NODE_BIN="$(dirname "$(nvm which "$NODE_MAJOR")")"
export PATH="$NODE_BIN:$PATH"

if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  # Running as the SessionStart hook: persist into the session's environment.
  {
    echo "export PATH=\"$NODE_BIN:\$PATH\""
    echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
    echo 'export DOTNET_NOLOGO=1'
  } >> "$CLAUDE_ENV_FILE"
else
  # Running from the environment's Setup script: its exports end with the script,
  # but session shells source ~/.bashrc. Replace any earlier line so re-runs and
  # version bumps don't stack up PATH entries.
  marker='# csp-manager session-start'
  touch ~/.bashrc
  sed -i "/$marker\$/d" ~/.bashrc
  {
    echo "export PATH=\"$NODE_BIN:\$PATH\" $marker"
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 $marker"
  } >> ~/.bashrc
fi

dotnet restore src/Umbraco.Community.CSPManager.slnx

(cd src/Umbraco.Community.CSPManager/Client && npm install --no-audit --no-fund)

# Needs pkgs.dev.azure.com (@jumoo registry), which the default network policy
# blocks. Don't fail the session over it: only uSync.Complete needs this client.
(cd src/uSync/Umbraco.Community.CSPManager.uSync.Complete/Client && npm install --no-audit --no-fund) \
  || echo "WARNING: uSync.Complete client restore failed; allow pkgs.dev.azure.com in the environment's network settings to build it." >&2
