#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h}"
log_path="$project_root/Builds/ios-slot-regression-smoke.log"
bundle_id="com.emreaydin.defenderchronicles"

"$project_root/tools/build_and_run_ios_simulator.sh"
xcrun simctl terminate booted "$bundle_id" >/dev/null 2>&1 || true
SIMCTL_CHILD_DC_SLOT_REGRESSION=1 xcrun simctl launch --console booted "$bundle_id" > "$log_path" 2>&1

print "Smoke log: $log_path"
rg "DC_SLOT_REGRESSION_PASS|DC_SLOT_REGRESSION_FAIL|DC_SLOT_REGRESSION_COMPLETE" "$log_path" || true
