#!/usr/bin/env python3
"""Record sanitized physical iOS readiness evidence for VIS-006/VIS-007/VIS-807."""

from __future__ import annotations

import argparse
import json
import subprocess
import tempfile
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT = ROOT / "docs" / "evidence" / "visual-development" / "ios-physical-readiness-2026-08-25.json"
DEVICE_SUPPORT = Path("/Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/DeviceSupport")


def run(command: list[str], timeout: int = 30) -> tuple[int, str, str]:
    process = subprocess.run(
        command,
        cwd=ROOT,
        text=True,
        capture_output=True,
        timeout=timeout,
        check=False,
    )
    return process.returncode, process.stdout.strip(), process.stderr.strip()


def xcode_version() -> dict[str, str]:
    code, stdout, stderr = run(["xcodebuild", "-version"])
    lines = stdout.splitlines()
    return {
        "ok": str(code == 0).lower(),
        "version": lines[0] if lines else "",
        "build": lines[1].replace("Build version ", "") if len(lines) > 1 else "",
        "error": stderr,
    }


def device_support_versions() -> list[str]:
    if not DEVICE_SUPPORT.exists():
        return []
    return sorted(path.name for path in DEVICE_SUPPORT.iterdir() if path.is_dir())


def load_devicectl() -> dict[str, Any]:
    with tempfile.NamedTemporaryFile(suffix=".json") as tmp:
        code, _, stderr = run(["xcrun", "devicectl", "list", "devices", "--json-output", tmp.name])
        if code != 0:
            return {"ok": False, "error": stderr, "devices": []}
        with open(tmp.name, "r", encoding="utf-8") as stream:
            data = json.load(stream)
    return {"ok": True, "error": "", "devices": data.get("result", {}).get("devices", [])}


def sanitized_physical_devices(devices: list[dict[str, Any]]) -> list[dict[str, Any]]:
    sanitized: list[dict[str, Any]] = []
    for device in devices:
        hardware = device.get("hardwareProperties", {})
        properties = device.get("deviceProperties", {})
        connection = device.get("connectionProperties", {})
        if hardware.get("reality") != "physical" or hardware.get("platform") != "iOS":
            continue
        sanitized.append(
            {
                "name": properties.get("name", ""),
                "coreDeviceIdentifierRedacted": bool(device.get("identifier", "")),
                "marketingName": hardware.get("marketingName", ""),
                "productType": hardware.get("productType", ""),
                "platform": hardware.get("platform", ""),
                "osVersion": properties.get("osVersionNumber", ""),
                "osBuild": properties.get("osBuildUpdate", ""),
                "releaseType": properties.get("releaseType", ""),
                "developerModeStatus": properties.get("developerModeStatus", ""),
                "ddiServicesAvailable": properties.get("ddiServicesAvailable", False),
                "pairingState": connection.get("pairingState", ""),
                "transportType": connection.get("transportType", ""),
                "tunnelState": connection.get("tunnelState", ""),
            }
        )
    return sanitized


def xctrace_devices() -> dict[str, Any]:
    code, stdout, stderr = run(["xcrun", "xctrace", "list", "devices"], timeout=45)
    physical_lines: list[str] = []
    section = ""
    for raw_line in stdout.splitlines():
        line = raw_line.strip()
        if line.startswith("== ") and line.endswith(" =="):
            section = line.strip("= ")
            continue
        if section not in {"Devices", "Devices Offline"}:
            continue
        if "iPhone" not in line:
            continue
        physical_lines.append(line.rsplit(" (", 1)[0])
    return {
        "ok": code == 0,
        "sanitizedPhysicalDeviceLines": physical_lines,
        "error": stderr,
    }


def readiness_status(devices: list[dict[str, Any]], support_versions: list[str]) -> dict[str, Any]:
    blockers: list[str] = []
    if not devices:
        blockers.append("No physical iOS device is visible to devicectl.")
    for device in devices:
        if device.get("pairingState") != "paired":
            blockers.append(f"{device.get('name', 'device')} is not paired.")
        if device.get("developerModeStatus") != "enabled":
            blockers.append(f"{device.get('name', 'device')} Developer Mode is not enabled.")
        if device.get("ddiServicesAvailable") is not True:
            blockers.append(f"{device.get('name', 'device')} DDI services are not available.")
        os_version = str(device.get("osVersion", ""))
        if os_version and os_version not in support_versions:
            blockers.append(f"Xcode DeviceSupport does not include iOS {os_version}.")
    return {
        "readyForPhysicalAcceptance": not blockers,
        "blockers": blockers,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()

    devicectl = load_devicectl()
    devices = sanitized_physical_devices(devicectl["devices"])
    support_versions = device_support_versions()
    status = readiness_status(devices, support_versions)
    report = {
        "schema": "defender-chronicles.visual-development.ios-physical-readiness.v1",
        "capturedOn": "2026-08-25",
        "xcode": xcode_version(),
        "deviceSupportVersions": support_versions,
        "devicectlOk": devicectl["ok"],
        "physicalDevices": devices,
        "xctrace": xctrace_devices(),
        "status": status,
        "note": "Sanitized report: serial number, ECID, UDID and CoreDevice identifier are intentionally omitted.",
    }

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(args.output.relative_to(ROOT))
    if status["readyForPhysicalAcceptance"]:
        print("[DC_IOS_PHYSICAL_READY]")
    else:
        print("[DC_IOS_PHYSICAL_BLOCKED]")
        for blocker in status["blockers"]:
            print(f"- {blocker}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
