#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h}"
log_path="$project_root/Builds/ios-audio-visual-acceptance.log"
bundle_id="com.emreaydin.defenderchronicles"
duration_seconds="${DC_AUDIO_ACCEPTANCE_SECONDS:-480}"

if ! [[ "$duration_seconds" =~ '^[0-9]+$' ]]; then
  print -u2 "DC_AUDIO_ACCEPTANCE_SECONDS tam sayı olmalı."
  exit 2
fi

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
  print -u2 "Açık bir iOS Simulator bulunamadı."
  exit 3
fi

xcrun simctl boot "$simulator_id" >/dev/null 2>&1 || true
xcrun simctl bootstatus "$simulator_id" -b

if [[ "${DC_REUSE_INSTALLED_BUILD:-0}" != "1" ]]; then
  "$project_root/tools/build_and_run_ios_simulator.sh"
else
  print "Mevcut kurulu kabul build'i yeniden kullanılıyor."
fi

xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
mkdir -p "$project_root/Builds"

SIMCTL_CHILD_DC_AUDIO_ACCEPTANCE=1 \
SIMCTL_CHILD_DC_AUDIO_ACCEPTANCE_SECONDS="$duration_seconds" \
xcrun simctl launch --console "$simulator_id" "$bundle_id" > "$log_path" 2>&1 &
launch_pid=$!

deadline=$((SECONDS + duration_seconds + 120))
while kill -0 "$launch_pid" >/dev/null 2>&1; do
  if (( SECONDS >= deadline )); then
    xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
    wait "$launch_pid" >/dev/null 2>&1 || true
    print -u2 "Ses-görsel kabul koşusu zaman aşımına uğradı. Log: $log_path"
    rg "DC_AUDIO_" "$log_path" || true
    exit 4
  fi
  sleep 2
done
wait "$launch_pid" >/dev/null 2>&1 || true

print "Ses-görsel kabul logu: $log_path"
rg "DC_AUDIO_(REVIEW_LEVEL|SNAPSHOT_PASS|VISUAL_ACCEPTANCE_(PASS|FAIL))" "$log_path" || true
if ! rg -q "\[DC_AUDIO_VISUAL_ACCEPTANCE_PASS\]" "$log_path"; then
  print -u2 "Ses-görsel kabul koşusu başarı işareti üretmedi."
  exit 5
fi
