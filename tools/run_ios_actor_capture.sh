#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h}"
log_path="$project_root/Builds/ios-actor-capture.log"
bundle_id="com.emreaydin.defenderchronicles"
stamp="actor-runtime-vfx-animation-$(date +%Y%m%d-%H%M%S)"

simulator_id="$(xcrun simctl list devices booted -j | plutil -extract devices xml1 -o - - | \
  xmllint --xpath 'string(//dict/key[contains(.,"iOS")]/following-sibling::array[1]/dict[1]/key[.="udid"]/following-sibling::string[1])' - 2>/dev/null || true)"
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

SIMCTL_CHILD_DC_ACTOR_CAPTURE=1 xcrun simctl launch --console "$simulator_id" "$bundle_id" > "$log_path" 2>&1 &
launch_pid=$!
sleep 5

"$project_root/tools/capture_ios_simulator_screenshot.sh" "$stamp" --rotate-device
readable_path="$project_root/docs/evidence/runtime/defender-simulator-${stamp}-landscape-readable.png"
upright_path="$project_root/docs/evidence/runtime/defender-simulator-${stamp}-landscape-upright.png"
cp "$readable_path" "$upright_path"
print "Upright landscape review: $upright_path"

sleep 2
xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
wait "$launch_pid" >/dev/null 2>&1 || true

print "Actor capture log: $log_path"
rg "DC_ACTOR_CAPTURE_PASS|DC_ACTOR_CAPTURE_FAIL|DC_ACTOR_CAPTURE_SIGNAL" "$log_path" || true
