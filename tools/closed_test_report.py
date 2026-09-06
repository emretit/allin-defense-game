#!/usr/bin/env python3
"""Collect existing closed-test evidence. Missing gates remain pending; this does not run tests."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / "docs/closed-test/evidence"
BUILD = "ct-20260906-01"

def digest(path):
    with path.open("rb") as source:
        return hashlib.file_digest(source, "sha256").hexdigest()

def collect_tests():
    results = {}
    for name in ("editmode-final.xml", "playmode-final.xml", "playmode-ui-final.xml", "fullscreen-playmode.xml"):
        path = EVIDENCE / name
        if not path.exists():
            results[name] = {"status": "pending"}
            continue
        run = ET.parse(path).getroot()
        results[name] = {key: run.get(key) for key in ("total", "passed", "failed", "skipped", "start-time", "end-time", "duration", "result")}
        results[name]["sha256"] = digest(path)
    path = EVIDENCE / "cloud-final.trx"
    if path.exists():
        counters = ET.parse(path).find(".//{*}Counters")
        results[path.name] = dict(counters.attrib) | {"sha256": digest(path)}
    return results

def collect_device():
    path = EVIDENCE / "iphone-30min.log"
    if not path.exists():
        return {"status": "pending", "reason": "No physical 30-minute log"}
    text = path.read_text(errors="replace")
    complete = re.findall(r"\[CLOSED_TEST_DEVICE_COMPLETE\].*elapsed=([\d.]+)", text)
    stages = {}
    for line in text.splitlines():
        if "[DC_DEVICE_METRICS]" not in line:
            continue
        values = dict(re.findall(r"(\w+)=([^\s]+)", line))
        stage = values.get("stage")
        if stage not in ("preparation", "normal", "dense", "boss"):
            continue
        stages.setdefault(stage, []).append(values)
    summary = {}
    for stage, rows in stages.items():
        summary[stage] = {
            "reports": len(rows),
            "lowest_reported_median_fps": min(float(r["median_fps"]) for r in rows),
            "highest_reported_p95_ms": max(float(r["p95_frame_ms"]) for r in rows),
            "peak_memory_mb": max(float(r["peak_memory_mb"]) for r in rows),
            "serious_or_critical_reports": sum(r.get("thermal") in ("serious", "critical") for r in rows),
        }
    passed_frames = len(summary) == 4 and all(r["lowest_reported_median_fps"] >= 55 and r["highest_reported_p95_ms"] <= 18 for r in summary.values())
    return {
        "status": "measured_requires_review" if complete else "incomplete",
        "elapsed_seconds": float(complete[-1]) if complete else None,
        "four_stage_frame_gate": passed_frames,
        "low_memory_warning": "[DC_DEVICE_MEMORY_WARNING]" in text,
        "stages": summary,
        "memory_gate": "Compare warmed repetitions at matching level/wave; initial load is not a warmed baseline.",
        "sha256": digest(path),
    }

def main():
    files = subprocess.check_output(["git", "ls-files", "-c", "-o", "--exclude-standard", "-z", "Assets", "Packages", "ProjectSettings", "CampaignProgressionCCM"], cwd=ROOT).decode().split("\0")
    hashes = {name: digest(ROOT / name) for name in sorted(set(files)) if name and (ROOT / name).is_file() and not any(part in ("bin", "obj") for part in Path(name).parts)}
    fingerprint = hashlib.sha256(json.dumps(hashes, sort_keys=True).encode()).hexdigest()
    packages = {}
    for path in ROOT.glob("Builds/ClosedTest-Android/*.apk"):
        packages[str(path.relative_to(ROOT))] = {"bytes": path.stat().st_size, "sha256": digest(path), "kind": "development_apk"}
    for path in ROOT.glob("Builds/ClosedTest-Xcode/Build/Products/Debug-iphoneos/*.app"):
        bundle_files = sorted(p for p in path.rglob("*") if p.is_file())
        bundle_hashes = {str(p.relative_to(path)): digest(p) for p in bundle_files}
        result = subprocess.run(["codesign", "--verify", str(path)], capture_output=True)
        packages[str(path.relative_to(ROOT))] = {
            "bytes": sum(p.stat().st_size for p in bundle_files),
            "files": len(bundle_files),
            "tree_sha256": hashlib.sha256(json.dumps(bundle_hashes, sort_keys=True).encode()).hexdigest(),
            "hash_method": "SHA-256 of sorted JSON mapping every bundle-relative file path to its SHA-256; includes UnityFramework and data.",
            "signature_status": "signed_requires_device_install" if result.returncode == 0 else "unsigned_cannot_install_on_device",
        }
    manifest = {
        "build": BUILD,
        "branch": subprocess.check_output(["git", "branch", "--show-current"], cwd=ROOT, text=True).strip(),
        "commit": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
        "source_fingerprint_sha256": fingerprint,
        "source_files": hashes,
        "tests": collect_tests(),
        "cloud_environment": "closed-test",
        "cloud_probe": "passed" if (EVIDENCE / "cloud-integration-proof.txt").exists() else "pending",
        "packages": packages,
        "device": collect_device(),
        "human_playtest": {"status": "not_run", "planned_cohorts": ["5–8", "30–50"]},
    }
    path = ROOT / "docs/closed-test/BUILD_MANIFEST.json"
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({"manifest": str(path), "tests": manifest["tests"], "packages": packages, "device": manifest["device"]}, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
