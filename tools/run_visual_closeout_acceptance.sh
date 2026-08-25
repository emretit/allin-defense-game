#!/bin/zsh
set -euo pipefail

project_root="${0:A:h:h}"
readiness_log="$project_root/Builds/visual-closeout-readiness.log"
device_smoke_log="$project_root/Builds/visual-closeout-device-smoke.log"
device_performance_log="$project_root/Builds/visual-closeout-device-performance.log"
device_soak_log="$project_root/Builds/visual-closeout-device-soak.log"
signoff_path="${DC_VISUAL_SIGNOFF_PATH:-$project_root/docs/evidence/visual-development/human-review-signoff-2026-08-25.json}"
signoff_template="$project_root/docs/evidence/visual-development/human-review-signoff-template-2026-08-25.json"
device_evidence_path="${DC_VISUAL_DEVICE_ACCEPTANCE_PATH:-$project_root/docs/evidence/visual-development/physical-device-acceptance-2026-08-25.json}"
device_evidence_template="$project_root/docs/evidence/visual-development/physical-device-acceptance-template-2026-08-25.json"

mkdir -p "$project_root/Builds"

print "== Defender Chronicles visual closeout acceptance =="
print "1/6 iPhone physical readiness"
python3 "$project_root/tools/check_ios_physical_readiness.py" | tee "$readiness_log"

python3 - "$project_root/docs/evidence/visual-development/ios-physical-readiness-2026-08-25.json" <<'PY'
import json
import sys
from pathlib import Path

report = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
if not report.get("status", {}).get("readyForPhysicalAcceptance", False):
    print("[DC_VISUAL_CLOSEOUT_BLOCKED] physical iPhone is not ready.")
    for blocker in report.get("status", {}).get("blockers", []):
        print(f"- {blocker}")
    sys.exit(10)
PY

print "2/6 physical iPhone smoke install/launch"
DC_IOS_SMOKE=1 "$project_root/tools/build_and_run_ios_device.sh" | tee "$device_smoke_log"

print "3/6 physical iPhone performance scenarios"
DC_IOS_PERFORMANCE_ACCEPTANCE=1 "$project_root/tools/build_and_run_ios_device.sh" | tee "$device_performance_log"

print "4/6 physical iPhone soak/stress scenario"
DC_IOS_SOAK_ACCEPTANCE=1 "$project_root/tools/build_and_run_ios_device.sh" | tee "$device_soak_log"

print "5/6 physical Profiler/Frame Debugger evidence"
if [[ -f "$device_evidence_path" ]]; then
  python3 "$project_root/tools/validate_visual_device_acceptance.py" "$device_evidence_path"
else
  print "[DC_VISUAL_CLOSEOUT_BLOCKED] physical device acceptance evidence missing: $device_evidence_path"
  print "Use the template: $device_evidence_template"
  exit 12
fi

print "6/6 human audio/accessibility sign-off"
if [[ -f "$signoff_path" ]]; then
  python3 "$project_root/tools/validate_visual_human_signoff.py" "$signoff_path"
else
  print "[DC_VISUAL_CLOSEOUT_BLOCKED] human sign-off file missing: $signoff_path"
  print "Use the template: $signoff_template"
  exit 11
fi

print "[DC_VISUAL_CLOSEOUT_ACCEPTANCE_PASS]"
