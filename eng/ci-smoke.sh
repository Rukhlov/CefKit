#!/usr/bin/env bash
# Runs the smoke check in CI and turns its output into annotations, which can be read without signing in to
# GitHub: notices with the result, or errors with the output and the CEF log. An annotation keeps about 4 KB, so
# long text is split into several, and the D-Bus noise of CI machines is left out.
#
#   bash eng/ci-smoke.sh <smoke output directory> [launcher...]
#   bash eng/ci-smoke.sh artifacts/bin/CefKit.Smoke/release xvfb-run -a
#
#   SMOKE_ARGS  extra arguments of the smoke check
#   SMOKE_NAME  name in the annotation titles (default: Smoke check)
#   SMOKE_GREP  extended regex: only matching lines of a failed run go to the annotations
set -u
dir=$1
shift
name=${SMOKE_NAME:-Smoke check}
exe="$dir/CefKit.Smoke"
helper="$dir/cefkit-helper"
if [ -f "$exe.exe" ]; then
  exe="$exe.exe"
  helper="$helper.exe"
fi

# Annotations of a file: level (notice, error), title, file, number of last lines, line filter
annotate() {
  grep -v -e 'dbus/' -e 'UPower' "$3" | grep -E -e "$5" | tail -n "$4" | sed -e 's/%/%25/g' -e 's/\r$//' |
    awk -v level="$1" -v title="$2" '
      function flush() {
        if (chunk != "") { n++; printf "::%s title=%s (%d)::%s\n", level, title, n, chunk; chunk = "" }
      }
      { line = $0 "%0A"; if (length(chunk) + length(line) > 3500) flush(); chunk = chunk line }
      END { flush() }'
}

# The helper alone: is it executable, does it start and load libcef
{
  ls -l "$helper"
  timeout 5 "$helper" --type=gpu-process --no-sandbox 2>&1 | tail -n 20
  echo "helper exit code: ${PIPESTATUS[0]}"
} > helper.log 2>&1

log=smoke.log
# shellcheck disable=SC2086 # SMOKE_ARGS is a list of arguments
"$@" "$exe" --disable-gpu --keep-log ${SMOKE_ARGS:-} 2>&1 | tee "$log"
status=${PIPESTATUS[0]}

if [ "$status" -eq 0 ]; then
  annotate notice "$name ($RUNNER_OS)" "$log" 40 .
else
  filter=${SMOKE_GREP:-.}
  annotate error "Helper ($RUNNER_OS)" helper.log 30 .
  annotate error "$name output ($RUNNER_OS, exit $status)" "$log" 150 "$filter"
  cef_log=$(grep -m1 'CEF log:' "$log" | sed -e 's/^.*CEF log: //' -e 's/\r$//')
  if [ -n "$cef_log" ] && command -v cygpath > /dev/null; then
    cef_log=$(cygpath -u "$cef_log")
  fi
  if [ -n "$cef_log" ] && [ -f "$cef_log" ]; then
    annotate error "$name CEF log ($RUNNER_OS)" "$cef_log" 150 "$filter"
  fi
fi
exit "$status"
