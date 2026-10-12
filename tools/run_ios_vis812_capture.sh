#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h}"
log_path="$project_root/Builds/ios-vis812-final-evidence.log"
defeat_log_path="$project_root/Builds/ios-vis812-final-evidence-defeat.log"
bundle_id="com.emreaydin.allindefense"
capture_script="$project_root/tools/capture_ios_simulator_screenshot.sh"
stamp_prefix="vis812-final-$(date +%Y%m%d-%H%M%S)"

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
  simulator_id="$(xcrun simctl list devices available | awk '/iPhone/ && /Shutdown/ { gsub(/[()]/, "", $4); print $4; exit }')"
fi
if [[ -z "$simulator_id" ]]; then
  print -u2 "Uygun bir iOS Simulator bulunamadı."
  exit 2
fi

xcrun simctl boot "$simulator_id" >/dev/null 2>&1 || true
xcrun simctl bootstatus "$simulator_id" -b

if [[ "${DC_REUSE_INSTALLED_BUILD:-0}" != "1" ]]; then
  "$project_root/tools/build_and_run_ios_simulator.sh"
fi
xcrun simctl boot "$simulator_id" >/dev/null 2>&1 || true
xcrun simctl bootstatus "$simulator_id" -b
xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true

capture_state() {
  local state="$1"
  local source_log="$2"
  local deadline=$((SECONDS + 90))
  while ! rg -q "\\[DC_VIS812_STATE\\] state=$state\\b" "$source_log" 2>/dev/null; do
    if (( SECONDS >= deadline )); then
      print -u2 "VIS-812 state '$state' capture timed out. Log: $source_log"
      rg "DC_VIS812" "$source_log" || true
      exit 4
    fi
    sleep 0.25
  done

  # HUD cards use short slide/fade transitions. Capture after the transition
  # instead of preserving an in-between frame with clipped labels.
  local settle=1.8
  if [[ "$state" == "ability" ]]; then
    settle=2.5
  elif [[ "$state" == "victory" || "$state" == "defeat" ]]; then
    settle=2.0
  fi
  sleep "$settle"
  DC_SIMULATOR_ID="$simulator_id" "$capture_script" "$stamp_prefix-$state" --rotate-device >/dev/null
  local readable="$project_root/docs/evidence/runtime/defender-simulator-$stamp_prefix-$state-landscape-readable.png"
  local upright="$project_root/docs/evidence/runtime/defender-simulator-$stamp_prefix-$state-landscape-upright.png"
  cp "$readable" "$upright"
  print "Captured VIS-812 state '$state': $upright"
}

SIMCTL_CHILD_DC_VIS812_CAPTURE=1 xcrun simctl launch --console "$simulator_id" "$bundle_id" > "$log_path" 2>&1 &
launch_pid=$!

for state in preparation ability normal dense boss1 boss2 boss3 victory; do
  capture_state "$state" "$log_path"
done

deadline=$((SECONDS + 90))
while ! rg -q "\\[DC_VIS812_CAPTURE_PASS\\]" "$log_path" 2>/dev/null; do
  if (( SECONDS >= deadline )); then
    xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
    wait "$launch_pid" >/dev/null 2>&1 || true
    print -u2 "VIS-812 main capture run timed out. Log: $log_path"
    rg "DC_VIS812" "$log_path" || true
    exit 5
  fi
  sleep 0.5
done
xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
wait "$launch_pid" >/dev/null 2>&1 || true

xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
SIMCTL_CHILD_DC_VIS812_CAPTURE_DEFEAT=1 xcrun simctl launch --console "$simulator_id" "$bundle_id" > "$defeat_log_path" 2>&1 &
defeat_pid=$!

capture_state "defeat" "$defeat_log_path"

deadline=$((SECONDS + 60))
while ! rg -q "\\[DC_VIS812_CAPTURE_DEFEAT_PASS\\]" "$defeat_log_path" 2>/dev/null; do
  if (( SECONDS >= deadline )); then
    xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
    wait "$defeat_pid" >/dev/null 2>&1 || true
    print -u2 "VIS-812 defeat capture run timed out. Log: $defeat_log_path"
    rg "DC_VIS812" "$defeat_log_path" || true
    exit 6
  fi
  sleep 0.5
done
xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
wait "$defeat_pid" >/dev/null 2>&1 || true

print "VIS-812 logs:"
print "  $log_path"
print "  $defeat_log_path"
rg "DC_VIS812" "$log_path" "$defeat_log_path" || true
