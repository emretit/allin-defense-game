#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h}"
timestamp="${1:-$(date +%Y%m%d-%H%M%S)}"
mode="${2:-}"
output_dir="$project_root/docs/evidence/runtime"
raw_path="$output_dir/defender-simulator-${timestamp}-raw.png"
landscape_path="$output_dir/defender-simulator-${timestamp}-landscape-readable.png"

mkdir -p "$output_dir"
simulator_id="${DC_SIMULATOR_ID:-}"
if [[ -z "$simulator_id" ]]; then
  simulator_id="$(xcrun simctl list devices booted -j | jq -r \
    '[.devices[][] | select(.name == "iPhone 17 Pro")][0].udid // [.devices[][]][0].udid // empty')"
fi
if [[ -z "$simulator_id" ]]; then
  print -u2 "Ekran görüntüsü için açık Simulator bulunamadı."
  exit 2
fi

xcrun simctl io "$simulator_id" screenshot "$raw_path"
cp "$raw_path" "$landscape_path"
width="$(sips -g pixelWidth "$landscape_path" 2>/dev/null | awk '/pixelWidth/ { print $2 }')"
height="$(sips -g pixelHeight "$landscape_path" 2>/dev/null | awk '/pixelHeight/ { print $2 }')"
if [[ "$mode" == "--rotate-device" && "$height" -gt "$width" ]]; then
  sips -r "${DC_SCREENSHOT_ROTATION:-90}" "$landscape_path" >/dev/null
  print "Screenshot framebuffer is portrait-shaped; readable copy rotated to landscape."
elif [[ "$height" -gt "$width" ]]; then
  print "Screenshot is portrait-shaped; leaving readable copy unrotated for orientation review."
else
  print "Screenshot is landscape-shaped; readable copy kept as captured."
fi

print "Raw screenshot: $raw_path"
print "Readable landscape review: $landscape_path"
