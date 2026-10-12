#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h}"
log_path="$project_root/Builds/ios-performance-acceptance.log"
bundle_id="com.emreaydin.allindefense"
campaign_level="${DC_CAMPAIGN_LEVEL:-10}"

simulator_id="${DC_SIMULATOR_ID:-}"
if [[ -z "$simulator_id" ]]; then
  simulator_id="$(xcrun simctl list devices booted -j | jq -r \
    '[.devices[][] | select(.name == "iPhone 17 Pro")][0].udid // empty')"
fi
if [[ -z "$simulator_id" ]]; then
  simulator_id="$(xcrun simctl list devices booted -j | plutil -extract devices xml1 -o - - | \
    xmllint --xpath 'string(//dict/key[contains(.,"iOS")]/following-sibling::array[1]/dict[1]/key[.="udid"]/following-sibling::string[1])' - 2>/dev/null || true)"
fi
if [[ -z "$simulator_id" ]]; then
  simulator_id="$(xcrun simctl list devices available | awk '/DC 16x9 Acceptance/ { gsub(/[()]/, "", $4); print $4; exit }')"
fi
if [[ -z "$simulator_id" ]]; then
  simulator_id="$(xcrun simctl list devices available | awk '/iPhone 17 Pro \(/ && /Shutdown/ { gsub(/[()]/, "", $4); print $4; exit }')"
fi
if [[ -z "$simulator_id" ]]; then
  print -u2 "Uygun bir iOS Simulator bulunamadı."
  exit 2
fi

xcrun simctl boot "$simulator_id" >/dev/null 2>&1 || true
xcrun simctl bootstatus "$simulator_id" -b

if [[ "${DC_REUSE_INSTALLED_BUILD:-0}" != "1" ]]; then
  "$project_root/tools/build_and_run_ios_simulator.sh"
else
  print "Mevcut kurulu kabul build'i yeniden kullanılıyor."
fi
xcrun simctl boot "$simulator_id" >/dev/null 2>&1 || true
xcrun simctl bootstatus "$simulator_id" -b
xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true

SIMCTL_CHILD_DC_PERFORMANCE_ACCEPTANCE=1 \
SIMCTL_CHILD_DC_CAMPAIGN_LEVEL="$campaign_level" \
xcrun simctl launch --console "$simulator_id" "$bundle_id" > "$log_path" 2>&1 &
launch_pid=$!

deadline=$((SECONDS + 260))
while kill -0 "$launch_pid" >/dev/null 2>&1; do
  if (( SECONDS >= deadline )); then
    xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
    wait "$launch_pid" >/dev/null 2>&1 || true
    print -u2 "VIS-807 performance acceptance run timed out. Log: $log_path"
    rg "DC_PERFORMANCE_ACCEPTANCE|DC_DEVICE_SCENARIO|DC_DEVICE_METRICS" "$log_path" || true
    exit 4
  fi
  sleep 2
done
wait "$launch_pid" >/dev/null 2>&1 || true

print "Performance acceptance log: $log_path"
rg "DC_PERFORMANCE_ACCEPTANCE|DC_DEVICE_SCENARIO|DC_DEVICE_METRICS" "$log_path" || true
if ! rg -q "\[DC_PERFORMANCE_ACCEPTANCE_PASS\]" "$log_path"; then
  print -u2 "Performance acceptance did not report a pass marker for campaign level $campaign_level."
  exit 5
fi
