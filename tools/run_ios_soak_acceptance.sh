#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h}"
log_path="$project_root/Builds/ios-soak-acceptance.log"
bundle_id="com.emreaydin.defenderchronicles"
simulator_id="${DC_SIMULATOR_ID:-}"
if [[ -z "$simulator_id" ]]; then
  simulator_id="$(xcrun simctl list devices booted -j | jq -r \
    '[.devices[][] | select(.name == "iPhone 17 Pro")][0].udid // empty')"
fi

if [[ -z "$simulator_id" ]]; then
  simulator_id="$(xcrun simctl list devices available | awk '/iPhone 17 Pro \(/ && /Shutdown/ { gsub(/[()]/, "", $4); print $4; exit }')"
fi
if [[ -z "$simulator_id" ]]; then
  print -u2 "Uygun bir iPhone 17 Pro Simulator bulunamadı."
  exit 2
fi

xcrun simctl boot "$simulator_id" >/dev/null 2>&1 || true
xcrun simctl bootstatus "$simulator_id" -b

xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
SIMCTL_CHILD_DC_SOAK_ACCEPTANCE=1 \
SIMCTL_CHILD_DC_CAMPAIGN_LEVEL=50 \
xcrun simctl launch --console "$simulator_id" "$bundle_id" > "$log_path" 2>&1 &
launch_pid=$!

deadline=$((SECONDS + 130))
while kill -0 "$launch_pid" >/dev/null 2>&1; do
  if (( SECONDS >= deadline )); then
    xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
    wait "$launch_pid" >/dev/null 2>&1 || true
    print -u2 "Uzun oturum testi zaman aşımına uğradı. Log: $log_path"
    exit 4
  fi
  sleep 2
done
wait "$launch_pid" >/dev/null 2>&1 || true

rg "DC_SOAK_ACCEPTANCE|DC_DEVICE_METRICS" "$log_path" || true
if ! rg -q "\[DC_SOAK_ACCEPTANCE_PASS\]" "$log_path"; then
  print -u2 "Uzun oturum testi geçiş işareti üretmedi."
  exit 5
fi
