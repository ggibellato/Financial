#!/usr/bin/env bash
# Classifies the files changed between BASE_SHA and HEAD_SHA into the CI jobs that must run.
#
# Usage: detect-changes.sh <base-sha> <head-sha>
# Writes backend/wpf/web/smoke (true|false) to $GITHUB_OUTPUT (stdout when unset) and a
# per-file explanation to $GITHUB_STEP_SUMMARY.
#
# Rules are evaluated top to bottom; the first pattern that matches a file decides its jobs.
# Fail-safe: a missing base, a failed diff, or a path no rule knows about runs everything.
set -u

BASE_SHA="${1:-}"
HEAD_SHA="${2:-HEAD}"

backend=false; wpf=false; web=false; smoke=false
reasons=()

run_everything() {
  backend=true; wpf=true; web=true; smoke=true
  reasons+=("everything: $1")
}

classify() {
  local path="$1"
  case "$path" in
    # Documentation and agent tooling: nothing to build.
    docs/*|specs/*|dev-util/*|.claude/*|.specify/*|*.md|LICENSE|.gitignore|.editorconfig|.dockerignore)
      reasons+=("docs: $path") ;;

    # API surface the web SPA is compiled against (controllers, OpenAPI snapshot).
    Financial.Api/*|Tests/Financial.Api.Tests/*)
      backend=true; web=true; smoke=true
      reasons+=("contract: $path") ;;

    # Application DTOs are the wire format AND are linked into the WPF app in-process.
    Financial.*.Application/DTOs/*)
      backend=true; wpf=true; web=true; smoke=true
      reasons+=("contract+backend: $path") ;;

    # Hand-written TypeScript mirror of the contract: end-to-end smoke proves it still matches.
    Financial.Web/src/api/*)
      web=true; smoke=true
      reasons+=("contract (web side): $path") ;;

    Financial.App/*|Tests/Financial.Presentation.Tests/*)
      wpf=true
      reasons+=("wpf: $path") ;;

    Financial.Web/*)
      web=true; smoke=true
      reasons+=("web: $path") ;;

    # Backend core. Financial.App references these projects directly, so WPF rebuilds too.
    Financial.*.Domain/*|Financial.*.Application/*|Financial.*.Infrastructure/*|Financial.Shared.*/*|Integrations/*|Tools/*|Tests/*|coverlet.runsettings)
      backend=true; wpf=true; smoke=true
      reasons+=("backend: $path") ;;

    # Build, deploy, data templates and CI plumbing affect every job.
    .github/*|Dockerfile*|docker-compose*|Financial.slnx|global.json|nuget.config|Directory.*|scripts/*|deploy/*|data/*)
      run_everything "infra: $path" ;;

    *)
      run_everything "unclassified path: $path" ;;
  esac
}

if [[ -z "$BASE_SHA" || "$BASE_SHA" =~ ^0+$ ]]; then
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
  echo "wpf=$wpf"
  echo "web=$web"
  echo "smoke=$smoke"
} >> "${GITHUB_OUTPUT:-/dev/stdout}"

{
  echo "### Affected jobs"
  echo
  echo "| backend | wpf | web | smoke |"
  echo "|---|---|---|---|"
  echo "| $backend | $wpf | $web | $smoke |"
  echo
  echo "<details><summary>Why</summary>"
  echo
  printf -- '- %s\n' "${reasons[@]}" | sort -u
  echo
  echo "</details>"
} >> "${GITHUB_STEP_SUMMARY:-/dev/stdout}"
