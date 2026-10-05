#!/usr/bin/env bash
# Rejects newly added test-hygiene violations in the lines a change adds (legacy lines are never reported).
#
# Usage: test-hygiene.sh <base-sha> <head-sha>
# Prints "test-hygiene: <file>:<line> adds <pattern> - <hint>" per finding, writes a summary to
# $GITHUB_STEP_SUMMARY, exits 1 when any error was found. A trailing "// hygiene-allow: <reason>"
# exempts one line and is listed in the summary; the reason is mandatory.
set -u

BASE_SHA="${1:-}"
HEAD_SHA="${2:-HEAD}"
summary="${GITHUB_STEP_SUMMARY:-/dev/null}"

errors=()
warnings=()
allowed=()

WALL_CLOCK='DateTime\.(Now|Today|UtcNow)'
FIXED_DELAY='Task\.Delay\(|Thread\.Sleep\('
SKIPPED_TEST='\bSkip[[:space:]]*='
FOCUSED_OR_WAIT='\.only\(|\.skip\(|waitForTimeout\('
UNPINNED_DATE='new Date\(\)'
FAKE_CLOCK='useFakeTimers|setSystemTime|pinDate'
ALLOW='//[[:space:]]*hygiene-allow:'
ALLOW_WITH_REASON='//[[:space:]]*hygiene-allow:[[:space:]]*[^[:space:]]'

declare -A head_content

file_mentions() {
  [[ -v head_content[$1] ]] || head_content[$1]="$(git show "$HEAD_SHA:$1" 2>/dev/null)"
  grep -Eq "$2" <<< "${head_content[$1]}"
}

record() {
  local severity="$1" file="$2" line="$3" text="$4" token="$5" hint="$6"
  if [[ "$text" =~ $ALLOW_WITH_REASON ]]; then
    allowed+=("$file:$line $token")
    return
  fi
  if [[ "$severity" == error && "$text" =~ $ALLOW ]]; then
    hint="hygiene-allow needs a reason after the colon"
  fi
  local message="test-hygiene: $file:$line adds $token - $hint"
  if [[ "$severity" == error ]]; then
    errors+=("$message")
    echo "::error file=$file,line=$line::$message"
  else
    warnings+=("$message")
    echo "::warning file=$file,line=$line::$message"
  fi
}

scan_line() {
  local file="$1" line="$2" text="$3"
  case "$file" in
    Tests/*.cs)
      if [[ "$text" =~ $WALL_CLOCK ]]; then
        record error "$file" "$line" "$text" "${BASH_REMATCH[0]}" "inject TimeProvider (FakeTimeProvider in tests) or pass a fixed date"
      fi
      if [[ "$text" =~ $FIXED_DELAY ]]; then
        record error "$file" "$line" "$text" "${BASH_REMATCH[0]%(}" "await the operation's Task or a deterministic signal instead"
      fi
      if [[ "$text" =~ $SKIPPED_TEST ]]; then
        record error "$file" "$line" "$text" "Skip =" "fix or delete the test instead of skipping it"
      fi ;;
    Financial.*/*.cs)
      if [[ "$text" =~ $WALL_CLOCK ]]; then
        record error "$file" "$line" "$text" "${BASH_REMATCH[0]}" "take a TimeProvider and read time from it"
      fi ;;
    Financial.Web/src/*.ts|Financial.Web/src/*.tsx|Financial.Web/tests/e2e/*.ts|Financial.Web/tests/e2e/*.tsx)
      if [[ "$text" =~ $FOCUSED_OR_WAIT ]]; then
        record error "$file" "$line" "$text" "${BASH_REMATCH[0]%(}" "remove .only/.skip; wait on a condition, not a delay"
      fi
      if [[ "$file" =~ \.test\.tsx?$ && "$text" =~ $UNPINNED_DATE ]] && ! file_mentions "$file" "$FAKE_CLOCK"; then
        record warning "$file" "$line" "$text" "new Date()" "pin the clock with pinDate(...) and assert a literal"
      fi ;;
  esac
}

if [[ -z "$BASE_SHA" ]] || ! diff_output=$(git diff -U0 --no-color "$BASE_SHA" "$HEAD_SHA" -- '*.cs' '*.ts' '*.tsx' 2>/dev/null); then
  echo "test-hygiene: cannot diff ${BASE_SHA:-<missing base>}..$HEAD_SHA"
  exit 1
fi

added=0
while IFS=$'\t' read -r file line text; do
  [[ -z "$file" ]] && continue
  added=$((added + 1))
  scan_line "$file" "$line" "${text%$'\r'}"
done < <(awk '
  /^\+\+\+ / { file = ($0 == "+++ /dev/null") ? "" : substr($0, 7); next }
  /^@@/      { match($0, /\+[0-9]+/); n = substr($0, RSTART + 1, RLENGTH - 1) + 0; next }
  /^\+/      { if (file != "") printf "%s\t%d\t%s\n", file, n, substr($0, 2); n++ }
' <<< "$diff_output")

{
  echo "### Test hygiene"
  echo "Scanned $added added line(s)."
  for list in errors warnings allowed; do
    declare -n items="$list"
    if ((${#items[@]} > 0)); then
      echo
      echo "**${list^}** (${#items[@]})"
      printf -- '- %s\n' "${items[@]}"
    fi
  done
} >> "$summary"

if ((${#errors[@]} > 0)); then
  printf '%s\n' "${errors[@]}"
  exit 1
fi
echo "test-hygiene: ok ($added added line(s) scanned, ${#warnings[@]} warning(s), ${#allowed[@]} allowed)"
