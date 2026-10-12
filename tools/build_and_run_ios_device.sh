#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h}"
unity_app="/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity"
unity_output="$project_root/Builds/iOS-Device"
derived_data="$project_root/Builds/XcodeDeviceDerivedData"
unity_log="$project_root/Builds/unity-ios-device-build.log"
xcode_log="$project_root/Builds/xcodebuild-ios-device.log"
device_run_log="$project_root/Builds/ios-device-acceptance.log"
bundle_id="com.emreaydin.allindefense"

mkdir -p "$project_root/Builds"

device_id="${DC_IOS_DEVICE_ID:-}"
if [[ -z "$device_id" ]]; then
  device_json="$(mktemp "$project_root/Builds/device-list.XXXXXX")"
  trap 'rm -f "$device_json"' EXIT
  xcrun devicectl list devices --json-output "$device_json" >/dev/null
  connected_count="$(jq '[.result.devices[] | select(
    .hardwareProperties.reality == "physical" and
    .hardwareProperties.deviceType == "iPhone" and
    .connectionProperties.pairingState == "paired" and
    .connectionProperties.transportType == "wired" and
    .deviceProperties.developerModeStatus == "enabled"
  )] | length' "$device_json")"
  if [[ "$connected_count" != "1" ]]; then
    print -u2 "Tam olarak bir bağlı ve kilidi açık fiziksel iPhone gerekli; bulunan: $connected_count."
    print -u2 "Birden fazla cihaz varsa DC_IOS_DEVICE_ID ile fiziksel cihaz UDID'sini belirtin."
    exit 2
  fi
  device_id="$(jq -r '.result.devices[] | select(
    .hardwareProperties.reality == "physical" and
    .hardwareProperties.deviceType == "iPhone" and
    .connectionProperties.pairingState == "paired" and
    .connectionProperties.transportType == "wired" and
    .deviceProperties.developerModeStatus == "enabled"
  ) | .hardwareProperties.udid' "$device_json")"
fi

team_id="${DC_IOS_TEAM_ID:-}"
if [[ -z "$team_id" ]]; then
  team_id="$(python3 - "$bundle_id" <<'PY'
import datetime
import plistlib
import subprocess
import sys
from pathlib import Path

bundle_id = sys.argv[1]
roots = [
    Path.home() / "Library/Developer/Xcode/UserData/Provisioning Profiles",
    Path.home() / "Library/MobileDevice/Provisioning Profiles",
]
best = None
now = datetime.datetime.now(datetime.timezone.utc).replace(tzinfo=None)
for root in roots:
    if not root.exists():
        continue
    for path in root.glob("*.mobileprovision"):
        result = subprocess.run(
            ["security", "cms", "-D", "-i", str(path)],
            capture_output=True,
            check=False,
        )
        if result.returncode != 0:
            continue
        try:
            data = plistlib.loads(result.stdout)
        except Exception:
            continue
        expiration = data.get("ExpirationDate")
        if expiration is not None and expiration < now:
            continue
        team = (data.get("TeamIdentifier") or [""])[0]
        app_id = data.get("Entitlements", {}).get("application-identifier", "")
        suffix = app_id.split(".", 1)[1] if "." in app_id else app_id
        devices = data.get("ProvisionedDevices") or []
        if not team or not devices:
            continue
        if suffix == bundle_id:
            score = 100
        elif suffix == "*" or (suffix.endswith("*") and bundle_id.startswith(suffix[:-1])):
            score = 80
        else:
            continue
        if best is None or score > best[0]:
            best = (score, team)
if best:
    print(best[1])
PY
)"
fi
if [[ -z "$team_id" ]]; then
  identity="$(security find-identity -v -p codesigning | awk '/Apple Development:/ { print; exit }')"
  team_id="$(print -r -- "$identity" | sed -E 's/.*\(([A-Z0-9]+)\)".*/\1/')"
fi
if [[ -z "$team_id" ]] || (( ${#team_id} < 8 )); then
  print -u2 "Apple Development signing takımı bulunamadı. DC_IOS_TEAM_ID değerini belirtin."
  exit 3
fi

export DC_IOS_CONNECT_PROFILER="${DC_IOS_CONNECT_PROFILER:-0}"

"$unity_app" -batchmode -quit -projectPath "$project_root" \
  -executeMethod AllinDefense.Editor.IosFirstBuildSettings.BuildDeviceDevelopmentProject \
  -logFile "$unity_log"

xcodebuild -project "$unity_output/Unity-iPhone.xcodeproj" \
  -scheme Unity-iPhone -configuration Debug -sdk iphoneos \
  -destination "platform=iOS,id=$device_id" -destination-timeout 60 \
  -derivedDataPath "$derived_data" DEVELOPMENT_TEAM="$team_id" CODE_SIGN_STYLE=Automatic \
  -allowProvisioningUpdates -allowProvisioningDeviceRegistration build > "$xcode_log" 2>&1

app_path="$derived_data/Build/Products/Debug-iphoneos/AllinDefense.app"
xcrun devicectl device install app --device "$device_id" "$app_path"

if [[ "${DC_IOS_SMOKE:-0}" == "1" ]]; then
  device_run_log="$project_root/Builds/ios-device-smoke.log"
  DEVICECTL_CHILD_DC_SMOKE=1 xcrun devicectl device process launch --console --timeout 180 \
    --terminate-existing --device "$device_id" "$bundle_id" | tee "$device_run_log"
  rg -q "\[DC_SMOKE_PASS\]" "$device_run_log"
elif [[ "${DC_IOS_DEFEAT_SMOKE:-0}" == "1" ]]; then
  device_run_log="$project_root/Builds/ios-device-defeat-smoke.log"
  DEVICECTL_CHILD_DC_SMOKE_DEFEAT=1 xcrun devicectl device process launch --console --timeout 60 \
    --terminate-existing --device "$device_id" "$bundle_id" | tee "$device_run_log"
elif [[ "${DC_IOS_PERFORMANCE_ACCEPTANCE:-0}" == "1" ]]; then
  device_run_log="$project_root/Builds/ios-device-performance.log"
  DEVICECTL_CHILD_DC_PERFORMANCE_ACCEPTANCE=1 xcrun devicectl device process launch --console --timeout 120 \
    --terminate-existing --device "$device_id" "$bundle_id" | tee "$device_run_log"
  rg -q "\[DC_PERFORMANCE_ACCEPTANCE_PASS\]" "$device_run_log"
elif [[ "${DC_IOS_SOAK_ACCEPTANCE:-0}" == "1" ]]; then
  device_run_log="$project_root/Builds/ios-device-soak.log"
  DEVICECTL_CHILD_DC_SOAK_ACCEPTANCE=1 xcrun devicectl device process launch --console --timeout 150 \
    --terminate-existing --device "$device_id" "$bundle_id" | tee "$device_run_log"
  rg -q "\[DC_SOAK_ACCEPTANCE_PASS\]" "$device_run_log"
else
  xcrun devicectl device process launch --terminate-existing --device "$device_id" "$bundle_id"
fi

print "ALLIN: Defense fiziksel iPhone'a kuruldu ve açıldı."
