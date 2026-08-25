#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h}"
app_path="$project_root/Builds/XcodeDerivedData/Build/Products/Debug-iphonesimulator/DefenderChronicles.app"
bundle_id="com.emreaydin.defenderchronicles"
evidence_root="$project_root/docs/evidence/runtime"
log_root="$project_root/Builds"

if [[ ! -d "$app_path" ]]; then
  print -u2 "Aynı kabul build'i bulunamadı: $app_path"
  exit 2
fi

mkdir -p "$evidence_root" "$log_root"
xcrun simctl list devices available -j > "$log_root/ratio-device-list.json"

run_ratio() {
  local label="$1"
  local device_name="$2"
  local prefer_booted="$3"
  local portrait_rotation="$4"
  local landscape_rotation="$5"
  local device_id
  if [[ "$prefer_booted" == "1" ]]; then
    device_id="$(jq -r --arg name "$device_name" '
      ([.devices[][] | select(.isAvailable == true and .name == $name and .state == "Booted")][0] //
       [.devices[][] | select(.isAvailable == true and .name == $name)][0]).udid // empty
    ' "$log_root/ratio-device-list.json")"
  else
    device_id="$(jq -r --arg name "$device_name" '
      [.devices[][] | select(.isAvailable == true and .name == $name)][0].udid // empty
    ' "$log_root/ratio-device-list.json")"
  fi
  if [[ -z "$device_id" ]]; then
    print -u2 "$label için simulator bulunamadı: $device_name"
    exit 3
  fi

  xcrun simctl boot "$device_id" >/dev/null 2>&1 || true
  xcrun simctl bootstatus "$device_id" -b
  xcrun simctl terminate "$device_id" "$bundle_id" >/dev/null 2>&1 || true
  xcrun simctl install "$device_id" "$app_path"

  local run_log="$log_root/ios-ratio-$label.log"
  SIMCTL_CHILD_DC_VIS812_CAPTURE=1 xcrun simctl launch --console "$device_id" "$bundle_id" > "$run_log" 2>&1 &
  local launch_pid=$!
  local deadline=$((SECONDS + 45))
  while ! rg -q '\[DC_VIS812_STATE\] state=preparation\b' "$run_log" 2>/dev/null; do
    if (( SECONDS >= deadline )); then
      xcrun simctl terminate "$device_id" "$bundle_id" >/dev/null 2>&1 || true
      wait "$launch_pid" >/dev/null 2>&1 || true
      print -u2 "$label hazırlık ekranı zaman aşımına uğradı."
      exit 4
    fi
    sleep 0.25
  done

  # The Unity state marker can arrive while the native launch view is still
  # fading. Stay inside the three-second preparation hold, but wait long
  # enough to capture the rendered battlefield instead of the splash screen.
  sleep 2
  local screenshot="$evidence_root/defender-simulator-final-ratio-$label-20260825.png"
  xcrun simctl io "$device_id" screenshot "$screenshot"
  local width="$(sips -g pixelWidth "$screenshot" 2>/dev/null | awk '/pixelWidth/ { print $2 }')"
  local height="$(sips -g pixelHeight "$screenshot" 2>/dev/null | awk '/pixelHeight/ { print $2 }')"
  if (( width <= height )); then
    # simctl portrait framebuffers are stored with the landscape content
    # rotated counter-clockwise. Rotate clockwise to keep the HUD upright.
    sips -r "$portrait_rotation" "$screenshot" >/dev/null
    local rotated="$width"
    width="$height"
    height="$rotated"
  elif (( landscape_rotation != 0 )); then
    # Simulator can preserve an inverted landscape device orientation between
    # launches. Normalize the stored evidence without changing the app layout.
    sips -r "$landscape_rotation" "$screenshot" >/dev/null
  fi
  print "[DC_RATIO_ACCEPTANCE] ratio=$label device=$device_name resolution=${width}x${height} state=preparation"

  xcrun simctl terminate "$device_id" "$bundle_id" >/dev/null 2>&1 || true
  wait "$launch_pid" >/dev/null 2>&1 || true
}

run_ratio "16x9" "DC 16x9 Acceptance" "0" "270" "0"
run_ratio "20x9" "iPhone 17 Pro" "1" "90" "180"
# The iPad acceptance simulator must be rotated to landscape from its toolbar
# before this run; Simulator stores that framebuffer in the opposite direction.
run_ratio "4x3" "DC 3x4 Acceptance" "0" "90" "0"
print "[DC_RATIO_ACCEPTANCE_PASS] ratios=16x9/20x9/4x3 build=reused"
