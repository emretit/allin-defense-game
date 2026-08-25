#!/usr/bin/env python3
"""Validate physical-device visual closeout evidence for NEXT-003/NEXT-004/NEXT-005."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
REQUIRED_SCENARIOS = ("preparation", "normal", "dense", "bossPhase3")
REQUIRED_METRICS = (
    "fpsMedian",
    "fpsMinimum",
    "cpuFrameMsP95",
    "gpuFrameMsP95",
    "drawCallsP95",
    "trianglesP95",
    "gcAllocBytesPerSecond",
    "memoryMbPeak",
    "thermalState",
)


def load(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8") as stream:
        data = json.load(stream)
    if not isinstance(data, dict):
        raise ValueError("top-level JSON must be an object")
    return data


def relative_path_exists(value: Any) -> bool:
    if not isinstance(value, str) or not value.strip():
        return False
    return (ROOT / value).exists()


def number(value: Any) -> float | None:
    if isinstance(value, bool):
        return None
    if isinstance(value, (int, float)):
        return float(value)
    return None


def validate_profile_scenarios(data: dict[str, Any], missing: list[str]) -> None:
    scenarios = data.get("profilerScenarios", {})
    if not isinstance(scenarios, dict):
        missing.append("profilerScenarios object is required")
        return

    for scenario in REQUIRED_SCENARIOS:
        item = scenarios.get(scenario)
        if not isinstance(item, dict):
            missing.append(f"profilerScenarios.{scenario} object is required")
            continue
        duration = number(item.get("durationSeconds"))
        if duration is None or duration < 60:
            missing.append(f"profilerScenarios.{scenario}.durationSeconds must be >= 60")
        for metric in REQUIRED_METRICS:
            value = item.get(metric)
            if metric == "thermalState":
                if not isinstance(value, str) or not value.strip():
                    missing.append(f"profilerScenarios.{scenario}.{metric} must be filled")
            elif number(value) is None:
                missing.append(f"profilerScenarios.{scenario}.{metric} must be numeric")
        if scenario != "bossPhase3":
            fps = number(item.get("fpsMedian"))
            if fps is not None and fps < 55:
                missing.append(f"profilerScenarios.{scenario}.fpsMedian should be >= 55 for normal 60 FPS target")
        else:
            fps_min = number(item.get("fpsMinimum"))
            if fps_min is not None and fps_min < 30:
                missing.append("profilerScenarios.bossPhase3.fpsMinimum must stay >= 30")


def validate_frame_debugger(data: dict[str, Any], missing: list[str]) -> None:
    frame = data.get("frameDebugger", {})
    if not isinstance(frame, dict):
        missing.append("frameDebugger object is required")
        return
    if not frame.get("captured") is True:
        missing.append("frameDebugger.captured must be true")
    if not isinstance(frame.get("topRenderCosts"), list) or len(frame.get("topRenderCosts", [])) < 5:
        missing.append("frameDebugger.topRenderCosts must include at least 5 entries")
    for key in ("shadowCasterNotes", "additionalLightNotes", "overdrawNotes", "textureUsageNotes"):
        if not isinstance(frame.get(key), str) or not frame[key].strip():
            missing.append(f"frameDebugger.{key} must be filled")
    screenshots = frame.get("screenshots", [])
    if not isinstance(screenshots, list) or not screenshots:
        missing.append("frameDebugger.screenshots must include curated evidence paths")
    else:
        for index, path in enumerate(screenshots):
            if not relative_path_exists(path):
                missing.append(f"frameDebugger.screenshots[{index}] must point to an existing curated file")


def validate_stress(data: dict[str, Any], missing: list[str]) -> None:
    stress = data.get("stressAndSoak", {})
    if not isinstance(stress, dict):
        missing.append("stressAndSoak object is required")
        return
    actor_count = number(stress.get("actorCount"))
    if actor_count is None or actor_count < 60:
        missing.append("stressAndSoak.actorCount must be >= 60")
    soak = number(stress.get("soakSeconds"))
    if soak is None or soak < 90:
        missing.append("stressAndSoak.soakSeconds must be >= 90")
    for key in ("persistentGcSpike", "lowMemoryWarning", "controlLoss"):
        if stress.get(key) is not False:
            missing.append(f"stressAndSoak.{key} must be false")
    fps_floor = number(stress.get("bossPhase3FpsFloor"))
    if fps_floor is None or fps_floor < 30:
        missing.append("stressAndSoak.bossPhase3FpsFloor must be >= 30")


def validate(data: dict[str, Any]) -> list[str]:
    missing: list[str] = []
    if data.get("schema") != "defender-chronicles.visual-device-acceptance.v1":
        missing.append("schema must be defender-chronicles.visual-device-acceptance.v1")
    for key in ("acceptedBuildId", "deviceClass", "deviceModel", "osVersion", "capturedOn", "reviewer"):
        if not isinstance(data.get(key), str) or not data[key].strip() or data[key].startswith("TODO"):
            missing.append(f"{key} must be filled")

    validate_profile_scenarios(data, missing)
    validate_frame_debugger(data, missing)
    validate_stress(data, missing)

    notes = data.get("notes", [])
    if not isinstance(notes, list) or not notes:
        missing.append("notes must include at least one review note")
    return missing


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence", type=Path)
    parser.add_argument("--allow-pending", action="store_true")
    args = parser.parse_args()

    data = load(args.evidence)
    missing = validate(data)
    if missing:
        print("[DC_VISUAL_DEVICE_ACCEPTANCE_PENDING]")
        for item in missing:
            print(f"- {item}")
        return 0 if args.allow_pending else 1

    print("[DC_VISUAL_DEVICE_ACCEPTANCE_PASS]")
    print(f"acceptedBuildId={data['acceptedBuildId']}")
    print(f"device={data['deviceClass']} {data['deviceModel']} {data['osVersion']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
