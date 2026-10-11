#!/bin/zsh
# Run against a freshly built iOS development app. Uses an isolated campaign save.
set -euo pipefail
project_root="${0:A:h:h}"
app_path="${1:-$project_root/Builds/XcodeDerivedData/Build/Products/Debug-iphonesimulator/DefenderChronicles.app}"
output_path="${2:-$project_root/Builds/FrontendExperience/iOS}"
bundle_id="com.emreaydin.defenderchronicles"
simulator_id="$(xcrun simctl list devices booted -j | python3 -c 'import sys,json; d=json.load(sys.stdin); print(next(x["udid"] for devices in d["devices"].values() for x in devices if x["state"]=="Booted"))')"
mkdir -p "$output_path"
xcrun simctl install "$simulator_id" "$app_path"
data_path="$(xcrun simctl get_app_container "$simulator_id" "$bundle_id" data)"
# Only delete the previous diagnostic report, never the player's campaign save.
rm -f "$data_path/Documents/FrontendExperience/report.json"
SIMCTL_CHILD_DC_FRONTEND_ACCEPTANCE=1 xcrun simctl launch --terminate-running-process "$simulator_id" "$bundle_id"
for attempt in {1..180}; do
  if [[ -f "$data_path/Documents/FrontendExperience/report.json" ]]; then
    run_status="$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["status"])' "$data_path/Documents/FrontendExperience/report.json")"
    if [[ "$run_status" != "running" ]]; then
      cp -R "$data_path/Documents/FrontendExperience/." "$output_path/"
      print "Frontend acceptance: $run_status — $output_path"
      [[ "$run_status" == "passed" ]]
      exit $?
    fi
  fi
  sleep 2
done
print -u2 "Frontend acceptance did not finish within 360 seconds."
exit 2
