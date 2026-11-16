#!/usr/bin/env bash
# Runs the smoke check in CI and turns its output into annotations, which can be read without signing in to
# GitHub: a notice with the result, or errors with the end of the output and of the CEF log.
#
#   bash eng/ci-smoke.sh [xvfb-run -a] artifacts/bin/CefKit.Smoke/release/CefKit.Smoke --disable-gpu
set -u
log=smoke.log
"$@" 2>&1 | tee "$log"
status=${PIPESTATUS[0]}

# Workflow commands are one line: newlines become %0A
encode() {
  tail -n "$1" "$2" | sed -e 's/%/%25/g' -e 's/\r$//' | awk 'BEGIN { ORS = "%0A" } { print }'
}

if [ "$status" -eq 0 ]; then
  echo "::notice title=Smoke check ($RUNNER_OS)::$(encode 40 "$log")"
else
  echo "::error title=Smoke output ($RUNNER_OS, exit $status)::$(encode 80 "$log")"
  cef_log=$(grep -m1 'CEF log:' "$log" | sed -e 's/^.*CEF log: //' -e 's/\r$//')
  if [ -n "$cef_log" ] && command -v cygpath > /dev/null; then
    cef_log=$(cygpath -u "$cef_log")
  fi
  if [ -n "$cef_log" ] && [ -f "$cef_log" ]; then
    echo "::error title=CEF log ($RUNNER_OS)::$(encode 60 "$cef_log")"
  fi
fi
exit "$status"
