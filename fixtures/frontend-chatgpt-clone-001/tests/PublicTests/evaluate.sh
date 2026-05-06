#!/usr/bin/env bash
set -euo pipefail
shopt -s nullglob globstar

item="${1:-}"

fail() {
  echo "[frontend-public:${item:-unknown}] $*" >&2
  exit 1
}

info() {
  echo "[frontend-public:${item:-unknown}] $*"
}

run_codepass() {
  if command -v codepass >/dev/null 2>&1; then
    codepass "$@"
  elif [[ -x "$HOME/.dotnet/tools/codepass" ]]; then
    "$HOME/.dotnet/tools/codepass" "$@"
  elif [[ -f "$HOME/CodePass/src/CodePass.Cli/CodePass.Cli.csproj" ]]; then
    dotnet run --project "$HOME/CodePass/src/CodePass.Cli/CodePass.Cli.csproj" -- "$@"
  else
    fail "CodePass CLI is not available"
  fi
}

root_web_project() {
  local projects=()
  for project in ./*.csproj; do
    if grep -q 'Microsoft.NET.Sdk.Web' "$project"; then
      projects+=("$project")
    fi
  done
  [[ ${#projects[@]} -eq 1 ]] || fail "expected exactly one root-level web .csproj, found ${#projects[@]}"
  printf '%s\n' "${projects[0]}"
}

root_solution() {
  local solutions=(./*.sln)
  [[ ${#solutions[@]} -eq 1 ]] || fail "expected exactly one root-level .sln file, found ${#solutions[@]}"
  printf '%s\n' "${solutions[0]}"
}

check_root_layout() {
  [[ -f task.md ]] || fail "task.md is missing"
  [[ -f expected-behavior.md ]] || fail "expected-behavior.md is missing"
  root_web_project >/dev/null
  root_solution >/dev/null

  local nested_web_projects=()
  while IFS= read -r project; do
    if grep -q 'Microsoft.NET.Sdk.Web' "$project"; then
      nested_web_projects+=("$project")
    fi
  done < <(find . -mindepth 2 -name '*.csproj' -not -path './tests/*' -not -path './hidden-tests/*' -not -path './bin/*' -not -path './obj/*')

  [[ ${#nested_web_projects[@]} -eq 0 ]] || fail "nested web projects exist: ${nested_web_projects[*]}"
  [[ -f README.md ]] || fail "README.md is missing"
  [[ -f Dockerfile ]] || fail "Dockerfile is missing"
  [[ -f docker-compose.yml || -f compose.yml ]] || fail "docker-compose.yml or compose.yml is missing"
  [[ -d .codepass/rules ]] || fail ".codepass/rules is missing"
}

check_build() {
  local solution
  solution="$(root_solution)"
  dotnet build "$solution"
}

check_chat_ui_contract() {
  for component in ChatPage ChatShell ConversationSidebar MessageList MessageBubble MessageComposer; do
    local matches=(**/${component}.razor)
    [[ ${#matches[@]} -ge 1 ]] || fail "required component ${component}.razor is missing"
  done

  for testid in chat-layout conversation-sidebar message-list message-composer prompt-input send-button new-chat-button user-message assistant-message; do
    grep -R "data-testid=[\"']${testid}[\"']" --include='*.razor' . >/dev/null || fail "missing data-testid=${testid}"
  done
}

check_tailwind() {
  if ! grep -R "tailwindcss\|@tailwind\|@import[[:space:]]\+[\"']tailwindcss" package.json ./*.csproj ./*.css wwwroot 2>/dev/null | grep -q .; then
    fail "Tailwind CSS tooling or directives were not found"
  fi

  local tmp_prefix="${TMPDIR:-/tmp}/${DOTNET_AI_BENCHMARK_VALIDATION_ID:-frontend-public-$$}"
  if grep -Rni '<style' --include='*.razor' --include='*.cshtml' . --exclude-dir=bin --exclude-dir=obj --exclude-dir=hidden-tests >"${tmp_prefix}-inline-style.txt" 2>/dev/null; then
    cat "${tmp_prefix}-inline-style.txt" >&2
    fail "custom inline <style> blocks are not allowed"
  fi

  if grep -Rni '<script' --include='*.razor' --include='*.cshtml' . --exclude-dir=bin --exclude-dir=obj --exclude-dir=hidden-tests | grep -v '_framework/' >"${tmp_prefix}-inline-script.txt" 2>/dev/null; then
    cat "${tmp_prefix}-inline-script.txt" >&2
    fail "custom inline <script> blocks are not allowed"
  fi
}

check_component_tests() {
  local test_projects=()
  for project in ./*Tests*.csproj ./tests/**/*.csproj; do
    [[ -f "$project" ]] && test_projects+=("$project")
  done
  [[ ${#test_projects[@]} -ge 1 ]] || fail "expected at least one test project"

  for project in "${test_projects[@]}"; do
    info "running tests in $project"
    dotnet test "$project"
  done
}

check_vulnerable_packages() {
  local project output
  project="$(root_web_project)"
  output="$(dotnet list "$project" package --vulnerable --include-transitive 2>&1)"
  echo "$output"
  if echo "$output" | grep -qi "has the following vulnerable packages"; then
    fail "vulnerable package versions were reported"
  fi
}

check_codepass_rules() {
  local solution
  solution="$(root_solution)"
  run_codepass analyze --solution "$solution" --rules .codepass/rules --output codepass-quality.json --fail-on-rule-warnings false || true
  [[ -f codepass-quality.json ]] || fail "codepass-quality.json was not created"
  grep -qi '"status"[[:space:]]*:[[:space:]]*"succeeded"' codepass-quality.json || fail "CodePass rule analysis did not succeed"
  grep -qi '"errorCount"[[:space:]]*:[[:space:]]*0' codepass-quality.json || fail "CodePass rule errors were found"
}

case "$item" in
  root-layout) check_root_layout ;;
  build) check_build ;;
  chat-ui-contract) check_chat_ui_contract ;;
  tailwind) check_tailwind ;;
  component-tests) check_component_tests ;;
  vulnerable-packages) check_vulnerable_packages ;;
  codepass-rules) check_codepass_rules ;;
  all)
    check_root_layout
    check_build
    check_chat_ui_contract
    check_tailwind
    check_component_tests
    check_vulnerable_packages
    check_codepass_rules
    ;;
  *) fail "unknown evaluation item: $item" ;;
esac

info "passed"
