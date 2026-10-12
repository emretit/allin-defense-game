#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h}"
unity_app="/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity"
unity_output="$project_root/Builds/iOS-Simulator"
derived_data="$project_root/Builds/XcodeDerivedData"
unity_log="$project_root/Builds/unity-ios-simulator-build.log"
xcode_log="$project_root/Builds/xcodebuild-ios-simulator.log"
bundle_id="com.emreaydin.allindefense"

simulator_id="$(xcrun simctl list devices booted -j | plutil -extract devices xml1 -o - - | \
  xmllint --xpath 'string(//dict/key[contains(.,"iOS")]/following-sibling::array[1]/dict[1]/key[.="udid"]/following-sibling::string[1])' - 2>/dev/null || true)"

if [[ -z "$simulator_id" ]]; then
  print -u2 "Açık bir iOS Simulator bulunamadı. Önce Simulator'da bir iPhone açın."
  exit 2
fi

mkdir -p "$project_root/Builds"
"$unity_app" -batchmode -quit -projectPath "$project_root" \
  -executeMethod AllinDefense.Editor.IosFirstBuildSettings.BuildSimulatorProject \
  -logFile "$unity_log"

xcodebuild -project "$unity_output/Unity-iPhone.xcodeproj" \
  -scheme Unity-iPhone -configuration Debug -sdk iphonesimulator \
  -destination "id=$simulator_id" -derivedDataPath "$derived_data" build > "$xcode_log" 2>&1

app_path="$derived_data/Build/Products/Debug-iphonesimulator/AllinDefense.app"
xcrun simctl terminate "$simulator_id" "$bundle_id" >/dev/null 2>&1 || true
xcrun simctl install "$simulator_id" "$app_path"
xcrun simctl launch "$simulator_id" "$bundle_id"

print "ALLIN: Defense Simulator'a kuruldu ve açıldı."
