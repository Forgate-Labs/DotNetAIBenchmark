#!/usr/bin/env bash
set -euo pipefail

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/dotnet-cli-home}"
export PATH="$DOTNET_CLI_HOME/.dotnet/tools:$PATH"

package_source="${CODEPASS_PACKAGE_SOURCE:-/codepass/artifacts/packages}"
tool_path="$DOTNET_CLI_HOME/.dotnet/tools/codepass"

if [ -x "$tool_path" ]; then
  exec "$tool_path" "$@"
fi

if [ -d "$package_source" ] && find "$package_source" -maxdepth 1 -name 'CodePass.Tool.*.nupkg' | grep -q .; then
  mkdir -p "$DOTNET_CLI_HOME/.dotnet/tools"
  dotnet tool install --tool-path "$DOTNET_CLI_HOME/.dotnet/tools" CodePass.Tool --add-source "$package_source" >/tmp/codepass-install.log
  exec "$tool_path" "$@"
fi

echo "CodePass package source not found. Mount CodePass at /codepass and run dotnet pack src/CodePass.Cli first." >&2
exit 127
