#!/usr/bin/env bash
# Self-test for detect-changes.sh: every case pins the jobs one path must trigger.
set -u

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$here/detect-changes.sh"

failures=0
cases=0

check() {
  local path="$1" expected="$2"
  backend=false; wpf=false; wpf_e2e=false; web=false; web_e2e=false; reasons=()
  classify "$path"
  local actual="$backend $wpf $wpf_e2e $web $web_e2e"
  cases=$((cases + 1))
  if [[ "$actual" != "$expected" ]]; then
    echo "FAIL $path: expected [$expected] got [$actual]"
    failures=$((failures + 1))
  fi
}

# columns: backend wpf wpf_e2e web web_e2e
check "docs/prd/x/prd.md"                                      "false false false false false"
check "README.md"                                              "false false false false false"
check "CLAUDE.md"                                              "false false false false false"
check ".claude/skills/testing-guide-Financial/SKILL.md"        "false false false false false"
check ".github/pull_request_template.md"                       "false false false false false"
check "deploy/README.md"                                       "false false false false false"
check "scripts/README.md"                                      "false false false false false"
check "Financial.Web/src/foo.md"                               "false false false true true"
check "Tests/Financial.Api.Tests/README.md"                    "true false false true true"
check "Financial.CashFlow.Domain/README.md"                    "true true true false true"
check "Financial.Api/Program.cs"                               "true false false true true"
check "Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json" "true false false true true"
check "Financial.CashFlow.Application/DTOs/ExpenseDTO.cs"      "true true true true true"
check "Financial.Web/src/api/types.ts"                         "false false false true true"
check "Financial.Web/src/App.tsx"                              "false false false true true"
check "Financial.App/Views/MainWindow.xaml"                    "true true true false false"
check "Tests/Financial.Presentation.Tests/X.cs"                "false true false false false"
check "Tests/Financial.App.E2ETests/Smoke/X.cs"                "false false true false false"
check "Tests/Financial.App.E2ETests/Financial.App.E2ETests.csproj" "false false true false false"
check "Financial.App/Views/CashFlow/ExpenseFormView.xaml"      "true true true false false"
check "Financial.CashFlow.Domain/Entities/X.cs"                "true true true false true"
check "Tests/Financial.Architecture.Tests/X.cs"                "true true true false true"
check ".github/workflows/build.yml"                            "true true true true true"
check ".github/scripts/detect-changes.sh"                      "true true true true true"
check "Dockerfile"                                             "true true true true true"
check "data/data-cashflow.example.json"                        "true true true true true"
check "some-unknown/file.txt"                                  "true true true true true"

echo "$((cases - failures))/$cases classifier cases passed"
[[ "$failures" -eq 0 ]]
