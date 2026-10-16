#!/usr/bin/env bash
# Classifies the files changed between BASE_SHA and HEAD_SHA into the CI jobs that must run.
#
# Usage: detect-changes.sh <base-sha> <head-sha>
# Writes backend/web/web_e2e (true|false) to $GITHUB_OUTPUT (stdout when unset) and a
# per-file explanation to $GITHUB_STEP_SUMMARY.
#
# Rules are evaluated top to bottom; the first pattern that matches a file decides its jobs.
# Fail-safe: a missing base, a failed diff, or a path no rule knows about runs everything.
# FULL_RUN=true bypasses classification entirely (used for every push to main).
set -u

BASE_SHA="${1:-}"
HEAD_SHA="${2:-HEAD}"

backend=false; web=false; web_e2e=false
reasons=()

run_everything() {
  backend=true; web=true; web_e2e=true
  reasons+=("everything: $1")
}

classify() {
  local path="$1"
  case "$path" in
    # Documentation and agent tooling: nothing to build.
    docs/*|specs/*|dev-util/*|.claude/*|.specify/*|LICENSE|.gitignore|.editorconfig|.dockerignore)
      reasons+=("docs: $path") ;;

    # API surface the web SPA is compiled against (controllers, OpenAPI snapshot).
    Financial.Api/*|Tests/Financial.Api.Tests/*)
      backend=true; web=true; web_e2e=true
      reasons+=("contract: $path") ;;

    # Application DTOs are the wire format the web SPA is compiled against.
    Financial.*.Application/DTOs/*)
      backend=true; web=true; web_e2e=true
      reasons+=("contract+backend: $path") ;;

    # Hand-written TypeScript mirror of the contract: web E2E proves it still matches.
    Financial.Web/src/api/*)
      web=true; web_e2e=true
      reasons+=("contract (web side): $path") ;;

    # WPF front end, being removed (ADR-002): CI no longer tests it, but the backend job's
    # solution build still compiles it and Financial.Architecture.Tests pin Financial.App's references.
    Financial.App/*|Tests/Financial.Presentation.Tests/*|Tests/Financial.App.E2ETests/*)
      backend=true
      reasons+=("backend (wpf build): $path") ;;

    Financial.Web/*)
      web=true; web_e2e=true
      reasons+=("web: $path") ;;

    Financial.*.Domain/*|Financial.*.Application/*|Financial.*.Infrastructure/*|Financial.Shared.*/*|Integrations/*|Tools/*|Tests/*|coverlet.runsettings)
      backend=true; web_e2e=true
      reasons+=("backend: $path") ;;

    # Markdown inside a source directory matched a rule above; only the rest is documentation.
    *.md)
      reasons+=("docs: $path") ;;

    # Build, deploy, data templates and CI plumbing affect every job.
    .github/*|Dockerfile*|docker-compose*|Financial.slnx|global.json|nuget.config|Directory.*|scripts/*|deploy/*|data/*)
      run_everything "infra: $path" ;;

    *)
      run_everything "unclassified path: $path" ;;
  esac
}

[[ "${BASH_SOURCE[0]}" != "$0" ]] && return 0

if [[ "${FULL_RUN:-false}" == true ]]; then
  run_everything "FULL_RUN requested"
elif [[ -z "$BASE_SHA" || "$BASE_SHA" =~ ^0+$ ]]; then
  run_everything "no base commit to diff against"
elif ! changed=$(git diff --name-only "$BASE_SHA" "$HEAD_SHA" 2>/dev/null); then
  run_everything "git diff $BASE_SHA..$HEAD_SHA failed"
elif [[ -z "$changed" ]]; then
  reasons+=("no files changed")
else
  while IFS= read -r path; do
    classify "$path"
  done <<< "$changed"
fi

{
  echo "backend=$backend"
  echo "web=$web"
  echo "web_e2e=$web_e2e"
} >> "${GITHUB_OUTPUT:-/dev/stdout}"

{
  echo "### Affected jobs"
  echo
  echo "| backend | web | web-e2e |"
  echo "|---|---|---|"
  echo "| $backend | $web | $web_e2e |"
  echo
  echo "<details><summary>Why</summary>"
  echo
  printf -- '- %s\n' "${reasons[@]}" | sort -u
  echo
  echo "</details>"
} >> "${GITHUB_STEP_SUMMARY:-/dev/stdout}"
