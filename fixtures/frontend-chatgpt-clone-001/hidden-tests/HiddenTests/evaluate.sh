#!/usr/bin/env bash
set -euo pipefail
shopt -s nullglob globstar

item="${1:-}"
tmp_prefix="${TMPDIR:-/tmp}/${DOTNET_AI_BENCHMARK_VALIDATION_ID:-frontend-hidden-$$}"

fail() {
  echo "[frontend-hidden:${item:-unknown}] $*" >&2
  exit 1
}

info() {
  echo "[frontend-hidden:${item:-unknown}] $*"
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
  [[ ${#projects[@]} -eq 1 ]] || fail "expected exactly one root-level web project"
  printf '%s\n' "${projects[0]}"
}

root_solution() {
  local solutions=(./*.sln)
  [[ ${#solutions[@]} -eq 1 ]] || fail "expected exactly one root-level .sln file, found ${#solutions[@]}"
  printf '%s\n' "${solutions[0]}"
}

check_chat_behavior() {
  mapfile -t test_files < <(find . -type f \( -name '*Tests.cs' -o -name '*Test.cs' \) -not -path './hidden-tests/*' -not -path './bin/*' -not -path './obj/*')
  [[ ${#test_files[@]} -ge 6 ]] || fail "expected component test files for each required component"
  grep -R "Bunit\|TestContext\|RenderComponent" "${test_files[@]}" >/dev/null || fail "component tests should use bUnit or equivalent"
  grep -R "Change\|Input\|Click\|Submit\|send-button\|prompt-input" "${test_files[@]}" >/dev/null || fail "tests should cover chat input/send interactions"
  for project in $(find . -type f -name '*.csproj' -path './tests/*'); do
    dotnet test "$project"
  done
}

check_mvvm() {
  mapfile -t view_models < <(find . -type f -name '*ViewModel.cs' -not -path './hidden-tests/*' -not -path './bin/*' -not -path './obj/*')
  [[ ${#view_models[@]} -ge 3 ]] || fail "expected at least three ViewModel classes"
  grep -R "INotifyPropertyChanged\|ObservableObject" "${view_models[@]}" >/dev/null || fail "ViewModels should expose bindable state"
  grep -R "ChatViewModel" --include='*.razor' . --exclude-dir=bin --exclude-dir=obj >/dev/null || fail "Razor should bind to ChatViewModel"
  grep -R "MessageComposerViewModel\|ComposerViewModel" --include='*.razor' --include='*.cs' . --exclude-dir=bin --exclude-dir=obj >/dev/null || fail "expected a dedicated composer ViewModel"
}

check_solid() {
  local interface_count
  interface_count=$(find . -type f -name 'I*.cs' -not -path './hidden-tests/*' -not -path './bin/*' -not -path './obj/*' | wc -l | tr -d ' ')
  [[ "$interface_count" -ge 2 ]] || fail "expected at least two service abstractions/interfaces"
  grep -R "AddScoped<.*I[A-Za-z0-9_]*\|AddSingleton<.*I[A-Za-z0-9_]*\|AddTransient<.*I[A-Za-z0-9_]*" --include='*.cs' . --exclude-dir=bin --exclude-dir=obj >/dev/null || fail "service abstractions should be registered through DI"
  if grep -RniE 'new[[:space:]]+[A-Za-z0-9_]*(Service|Repository|Provider)\(' --include='*.razor' . --exclude-dir=bin --exclude-dir=obj >"${tmp_prefix}-new-service.txt" 2>/dev/null; then
    cat "${tmp_prefix}-new-service.txt" >&2
    fail "Razor components should not directly construct services"
  fi
  for component in ChatPage ChatShell ConversationSidebar MessageList MessageBubble MessageComposer; do
    local matches=(**/${component}.razor)
    [[ ${#matches[@]} -ge 1 ]] || fail "required component ${component}.razor is missing"
    local line_count
    line_count=$(wc -l < "${matches[0]}" | tr -d ' ')
    [[ "$line_count" -le 180 ]] || fail "${component}.razor is too large"
  done
}

check_security() {
  if grep -RniE '(sk-[A-Za-z0-9_-]{20,}|ghp_[A-Za-z0-9_]{20,}|AKIA[0-9A-Z]{16}|-----BEGIN (RSA |EC |OPENSSH |)PRIVATE KEY-----)' . --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj --exclude-dir=hidden-tests >"${tmp_prefix}-secrets.txt" 2>/dev/null; then
    cat "${tmp_prefix}-secrets.txt" >&2
    fail "possible committed secret detected"
  fi
  local solution
  solution="$(root_solution)"
  run_codepass analyze --solution "$solution" --rules .codepass/rules --output codepass-quality.json --fail-on-rule-warnings false || true
  [[ -f codepass-quality.json ]] || fail "codepass-quality.json was not created"
  grep -qi '"status"[[:space:]]*:[[:space:]]*"succeeded"' codepass-quality.json || fail "CodePass rule analysis did not succeed"
  grep -qi '"errorCount"[[:space:]]*:[[:space:]]*0' codepass-quality.json || fail "CodePass rule errors were found"
}

check_docker() {
  [[ -f Dockerfile ]] || fail "Dockerfile is missing"
  grep -qi 'FROM .*dotnet.*/sdk' Dockerfile || fail "Dockerfile should use a .NET SDK build stage"
  grep -qi 'FROM .*dotnet.*/aspnet' Dockerfile || fail "Dockerfile should use an ASP.NET runtime stage"
  grep -qi 'dotnet[[:space:]]\+publish' Dockerfile || fail "Dockerfile should publish the app"
  grep -qi 'ENTRYPOINT' Dockerfile || fail "Dockerfile should define an ENTRYPOINT"
  local compose_file=""
  [[ -f docker-compose.yml ]] && compose_file="docker-compose.yml"
  [[ -z "$compose_file" && -f compose.yml ]] && compose_file="compose.yml"
  [[ -n "$compose_file" ]] || fail "docker-compose.yml or compose.yml is missing"
  grep -qi 'services:' "$compose_file" || fail "compose file should define services"
  grep -qi 'build:' "$compose_file" || fail "compose file should build the app image"
  grep -qi 'ports:' "$compose_file" || fail "compose file should expose ports"
  if command -v docker >/dev/null 2>&1 && [[ -S /var/run/docker.sock ]]; then
    local image_tag="${DOTNET_AI_BENCHMARK_DOCKER_IMAGE_TAG:-frontend-chatgpt-clone-validation:$(date +%s)-$$}"
    docker build -t "$image_tag" .
    docker image rm "$image_tag" >/dev/null 2>&1 || true
    if docker compose version >/dev/null 2>&1; then
      docker compose -f "$compose_file" config >/dev/null
    fi
  fi
}

check_readme() {
  [[ -f README.md ]] || fail "README.md is missing"
  for term in "dotnet run" "dotnet test" "docker" "docker compose" "Tailwind"; do
    grep -qi "$term" README.md || fail "README.md should document $term"
  done
}

check_publish() {
  local project publish_dir
  project="$(root_web_project)"
  publish_dir="$(mktemp -d)"
  dotnet publish "$project" -c Release -o "$publish_dir"
  find "$publish_dir" -maxdepth 1 -name '*.dll' | grep -q . || fail "dotnet publish did not produce an app dll"
}

case "$item" in
  chat-behavior) check_chat_behavior ;;
  mvvm) check_mvvm ;;
  solid) check_solid ;;
  security) check_security ;;
  docker) check_docker ;;
  readme) check_readme ;;
  publish) check_publish ;;
  all)
    check_chat_behavior
    check_mvvm
    check_solid
    check_security
    check_docker
    check_readme
    check_publish
    ;;
  *) fail "unknown evaluation item: $item" ;;
esac

info "passed"
