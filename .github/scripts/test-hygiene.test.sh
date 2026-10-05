#!/usr/bin/env bash
# Self-test for test-hygiene.sh: builds a throwaway repository whose base commit already contains
# a legacy violation of every rule, then pins what each kind of added line must do.
set -u

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
scanner="$here/test-hygiene.sh"
workdir="$(mktemp -d)"
repo="$workdir/repo"
mkdir "$repo"
trap 'rm -rf "$workdir"' EXIT

git -C "$repo" init -q
git -C "$repo" config user.email test@example.com
git -C "$repo" config user.name test
git -C "$repo" config commit.gpgsign false
git -C "$repo" config core.autocrlf false

put() {
  mkdir -p "$repo/$(dirname "$1")"
  printf '%s\n' "${@:2}" > "$repo/$1"
}

put Tests/Legacy/LegacyTests.cs 'var a = DateTime.Now;' 'await Task.Delay(50);' '[Fact(Skip = "old")]' 'var keep = 1;'
put Financial.Web/src/legacy.test.ts "it.only('old', () => {})" 'const keep = 1'
put Financial.Orders.Application/Clocked.cs 'class Clocked { TimeProvider _t; }'
put Financial.Orders.Application/Unclocked.cs 'class Unclocked { }'
put Financial.Web/src/pinned.test.ts "import { pinDate } from '../test-utils/pinDate'"
put Financial.Web/src/loose.test.ts 'const keep = 1'
put Financial.Web/tests/e2e/page.spec.ts 'const keep = 1'
git -C "$repo" add -A && git -C "$repo" commit -q -m base
base="$(git -C "$repo" rev-parse HEAD)"

failures=0
cases=0
summary_file="$workdir/summary.md"

# check <name> <expected exit> <expected output fragment or ""> — the change is already in the working tree.
check() {
  local name="$1" expected_exit="$2" fragment="$3" output exit_code
  git -C "$repo" add -A && git -C "$repo" commit -q --allow-empty -m "case: $name"
  : > "$summary_file"
  output="$(cd "$repo" && GITHUB_STEP_SUMMARY="$summary_file" bash "$scanner" "$base" HEAD 2>&1)"
  exit_code=$?
  cases=$((cases + 1))
  if [[ "$exit_code" != "$expected_exit" ]] || [[ -n "$fragment" && "$output" != *"$fragment"* ]]; then
    echo "FAIL $name: expected exit $expected_exit with [$fragment], got exit $exit_code:"
    echo "$output" | sed 's/^/    /'
    failures=$((failures + 1))
  fi
  git -C "$repo" reset -q --hard "$base"
}

put Tests/Foo/FooTests.cs 'await Task.Delay(100);'
check "Task.Delay in a test" 1 "test-hygiene: Tests/Foo/FooTests.cs:1 adds Task.Delay"

put Tests/Foo/FooTests.cs 'Thread.Sleep(10);'
check "Thread.Sleep in a test" 1 "adds Thread.Sleep"

put Tests/Foo/FooTests.cs 'var now = DateTime.UtcNow;'
check "DateTime.UtcNow in a test" 1 "adds DateTime.UtcNow"

put Financial.Orders.Application/Clocked.cs 'class Clocked { TimeProvider _t; DateTime x = DateTime.Now; }'
check "DateTime.Now in a class that injects TimeProvider" 1 "Clocked.cs:1 adds DateTime.Now"

put Financial.Orders.Application/Unclocked.cs 'class Unclocked { DateTime x = DateTime.Now; }'
check "DateTime.Now in a Financial.* production file, injected clock or not" 1 "Unclocked.cs:1 adds DateTime.Now"

put Tools/Importer/Program.cs 'var now = DateTime.Now;'
check "DateTime.Now in a Tools project (outside the Financial.* gate)" 0 ""

put Tests/Foo/FooTests.cs '[Fact(Skip = "later")]'
check "Skip = in a test" 1 "adds Skip ="

put Financial.Web/src/legacy.test.ts "it.only('old', () => {})" 'const keep = 1' "it.only('new', () => {})"
check "it.only in a web test" 1 "legacy.test.ts:3 adds .only"

put Financial.Web/tests/e2e/page.spec.ts 'await page.waitForTimeout(500)'
check "waitForTimeout in an e2e spec" 1 "adds waitForTimeout"

put Financial.Web/src/loose.test.ts 'const d = new Date()'
check "new Date() in a web test with no fake clock warns only" 0 "warning"

put Financial.Web/src/pinned.test.ts "import { pinDate } from '../test-utils/pinDate'" 'const d = new Date()'
check "new Date() in a web test that pins the clock is silent" 0 "0 warning"

put Tests/Legacy/LegacyTests.cs 'var a = DateTime.Now;' 'await Task.Delay(50);' '[Fact(Skip = "old")]' 'var keep = 1;' 'var more = 2;'
check "legacy violations on unchanged lines are not reported" 0 "ok"

put Tests/Foo/FooTests.cs 'await Task.Delay(100); // hygiene-allow: waiting on the real driver'
check "hygiene-allow with a reason passes" 0 "1 allowed"

put Tests/Foo/FooTests.cs 'await Task.Delay(100); // hygiene-allow:'
check "hygiene-allow without a reason fails" 1 "needs a reason"

put Tests/Legacy/LegacyTests.cs 'var keep = 1;'
check "deleting violating lines passes" 0 "ok"

put docs/notes.md 'await Task.Delay(100); and DateTime.Now'
check "violation text in a markdown file is ignored" 0 "ok"

put Tests/Foo/FooTests.cs 'await Task.Delay(100);'
git -C "$repo" add -A && git -C "$repo" commit -q -m violation
: > "$summary_file"
(cd "$repo" && GITHUB_STEP_SUMMARY="$summary_file" bash "$scanner" "$base" HEAD >/dev/null 2>&1)
cases=$((cases + 1))
if ! grep -q "Task.Delay" "$summary_file"; then
  echo "FAIL summary: expected the finding in the step summary"
  failures=$((failures + 1))
fi
git -C "$repo" reset -q --hard "$base"

(cd "$repo" && bash "$scanner" "" HEAD >/dev/null 2>&1)
missing_base_exit=$?
cases=$((cases + 1))
if [[ "$missing_base_exit" -ne 1 ]]; then
  echo "FAIL missing base: expected exit 1"
  failures=$((failures + 1))
fi

echo "$((cases - failures))/$cases hygiene cases passed"
[[ "$failures" -eq 0 ]]
