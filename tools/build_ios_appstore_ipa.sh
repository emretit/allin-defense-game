#!/bin/zsh
# App Store / TestFlight IPA: Unity Release Xcode projesi -> xcodebuild archive -> imzalı IPA.
# Pafta ile aynı ExportOptions (app-store-connect, otomatik imza); yükleme Transporter ile yapılır.
# Kullanım: tools/build_ios_appstore_ipa.sh <sürüm> <build_no>   örn. tools/build_ios_appstore_ipa.sh 1.0.0 1
set -euo pipefail

project_root="${0:A:h:h}"
unity_app="/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity"
bundle_id="com.emreaydin.allindefense"
team_id="${DC_IOS_TEAM_ID:-T9QCW8Q2C3}"
export_options="$project_root/tools/ExportOptions-AppStore.plist"
unity_output="$project_root/Builds/iOS-AppStore"
link_xml=(Assets/AddressableAssetsData/link.xml Assets/AddressableAssetsData/link.xml.meta)

if (( $# != 2 )); then
  print -u2 "Kullanım: $0 <sürüm> <build_no>   (örn. 1.0.0 1)"
  exit 2
fi
version="$1"
build_number="$2"
if [[ ! "$version" =~ '^[0-9]+\.[0-9]+(\.[0-9]+)?$' || ! "$build_number" =~ '^[0-9]+$' ]]; then
  print -u2 "Sürüm 1.0.0 biçiminde, build no tam sayı olmalı."
  exit 2
fi

cd "$project_root"

# TestFlight build'i commitli kaynaktan alınır (Pafta'daki "drift 0" kuralı). link.xml'i Unity siliyor, sayılmaz.
dirty="$(git status --porcelain -- Assets Packages ProjectSettings ':!Assets/AddressableAssetsData/link.xml*')"
if [[ -n "$dirty" && "${ALLOW_DIRTY:-0}" != "1" ]]; then
  print -u2 "Assets/Packages/ProjectSettings'te commitlenmemiş değişiklik var:"
  print -u2 -- "$dirty"
  print -u2 "Önce commitleyin ya da bilerek devam için ALLOW_DIRTY=1 verin."
  exit 3
fi

if lsof -t "$project_root/Temp/UnityLockfile" >/dev/null 2>&1; then
  print -u2 "Bu projede Unity açık/çalışıyor; kapanmasını bekleyin."
  exit 4
fi

out_dir="$project_root/Builds/AppStore/$version+$build_number"
if [[ -e "$out_dir" ]]; then
  print -u2 "$out_dir zaten var; aynı build no ikinci kez yüklenemez, build no'yu artırın."
  exit 5
fi
mkdir -p "$out_dir"
archive_path="$out_dir/AllinDefense.xcarchive"
ipa_path="$out_dir/AllinDefense-$version-$build_number.ipa"
commit="$(git rev-parse HEAD)"

trap 'git checkout -- "${link_xml[@]}" 2>/dev/null || true' EXIT

print "1/4 Unity Release Xcode projesi ($version, build $build_number, $commit)..."
rm -rf "$unity_output"
if ! ALLIN_IOS_VERSION="$version" ALLIN_IOS_BUILD="$build_number" "$unity_app" -batchmode -quit \
  -projectPath "$project_root" \
  -executeMethod AllinDefense.Editor.IosFirstBuildSettings.BuildDeviceAppStoreProject \
  -logFile "$out_dir/unity.log"; then
  print -u2 "Unity build başarısız: $out_dir/unity.log"
  exit 1
fi

print "2/4 Xcode archive (Release)..."
if ! xcodebuild -project "$unity_output/Unity-iPhone.xcodeproj" \
  -scheme Unity-iPhone -configuration Release -destination "generic/platform=iOS" \
  -archivePath "$archive_path" DEVELOPMENT_TEAM="$team_id" CODE_SIGN_STYLE=Automatic \
  -allowProvisioningUpdates archive > "$out_dir/xcode-archive.log" 2>&1; then
  grep -E "error:|BUILD FAILED|ARCHIVE FAILED" "$out_dir/xcode-archive.log" | tail -20 >&2 || true
  print -u2 "Archive başarısız: $out_dir/xcode-archive.log"
  exit 1
fi

print "3/4 İmzalı IPA export (app-store-connect)..."
if ! xcodebuild -exportArchive -archivePath "$archive_path" -exportPath "$out_dir/export" \
  -exportOptionsPlist "$export_options" -allowProvisioningUpdates > "$out_dir/xcode-export.log" 2>&1; then
  grep -E "error:|EXPORT FAILED" "$out_dir/xcode-export.log" | tail -20 >&2 || true
  print -u2 "Export başarısız: $out_dir/xcode-export.log"
  exit 1
fi
exported=("$out_dir"/export/*.ipa(N))
if (( ${#exported} != 1 )); then
  print -u2 "Export klasöründe tek IPA bekleniyordu, bulunan: ${#exported}"
  exit 1
fi
mv "$exported[1]" "$ipa_path"

print "4/4 IPA doğrulama..."
verify_dir="$(mktemp -d)"
trap 'rm -rf "$verify_dir"; git checkout -- "${link_xml[@]}" 2>/dev/null || true' EXIT
unzip -q "$ipa_path" -d "$verify_dir"
apps=("$verify_dir"/Payload/*.app(N))
plist="$apps[1]/Info.plist"
plist_value() { /usr/libexec/PlistBuddy -c "Print :$1" "$plist" 2>/dev/null || print "<yok>"; }

failures=()
[[ "$(plist_value CFBundleIdentifier)" == "$bundle_id" ]] || failures+=("bundle id: $(plist_value CFBundleIdentifier)")
[[ "$(plist_value CFBundleShortVersionString)" == "$version" ]] || failures+=("sürüm: $(plist_value CFBundleShortVersionString)")
[[ "$(plist_value CFBundleVersion)" == "$build_number" ]] || failures+=("build no: $(plist_value CFBundleVersion)")
[[ "$(plist_value ITSAppUsesNonExemptEncryption)" == "false" ]] || failures+=("şifreleme beyanı: $(plist_value ITSAppUsesNonExemptEncryption)")
codesign --verify --deep --strict "$apps[1]" 2>/dev/null || failures+=("codesign --verify başarısız")
authority="$(codesign -dvv "$apps[1]" 2>&1 | awk -F= '/^Authority=/ && !found++ {print $2}')"  # exit yok: pipefail'de SIGPIPE 141
[[ "$authority" == "Apple Distribution"* ]] || failures+=("imza: $authority")

if (( ${#failures} )); then
  print -u2 "IPA doğrulaması başarısız:"
  printf '  - %s\n' "${failures[@]}" >&2
  exit 1
fi

sha="$(shasum -a 256 "$ipa_path" | awk '{print $1}')"
cat > "$out_dir/manifest.txt" <<EOF
version=$version
build=$build_number
bundle_id=$bundle_id
commit=$commit
signing=$authority
ipa=$ipa_path
sha256=$sha
bytes=$(stat -f %z "$ipa_path")
EOF

print "Hazır: $ipa_path"
print "İmza: $authority · sha256 $sha"
print "Yükleme: Transporter'ı açın (open -a Transporter) ve IPA'yı sürükleyip Deliver'a basın."
