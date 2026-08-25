#!/usr/bin/env python3
"""Validate accepted brand/logo handoff evidence for NEXT-203."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
REQUIRED_CHANGED_FILES = (
    "Assets/Resources/Presentation/Brand/DefenderChronicles-Logo-v2.png",
    "Assets/Resources/Presentation/Brand/DefenderChronicles-Mark-v2.png",
    "Assets/Art/AppIcon/BrandV2/DefenderChronicles-AppIcon-v2-1024.png",
    "Assets/Art/Brand/Launch/GreenwatchLaunch-v2.png",
)
REQUIRED_TESTS = (
    "BrandAssetsMeetTheMobileImportAndAlphaContract",
    "BrandScreensCaptureAtAcceptanceRatios",
    "FrontendFlowUsesBrandAssetsAndSafeArea",
)


def load(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8") as stream:
        data = json.load(stream)
    if not isinstance(data, dict):
        raise ValueError("top-level JSON must be an object")
    return data


def relative_exists(value: Any) -> bool:
    return isinstance(value, str) and bool(value.strip()) and (ROOT / value).exists()


def filled(data: dict[str, Any], key: str, missing: list[str]) -> str:
    value = data.get(key, "")
    if not isinstance(value, str) or not value.strip() or value.startswith("TODO"):
        missing.append(f"{key} must be filled")
        return ""
    return value


def validate(data: dict[str, Any]) -> list[str]:
    missing: list[str] = []
    if data.get("schema") != "defender-chronicles.brand-handoff.v1":
        missing.append("schema must be defender-chronicles.brand-handoff.v1")

    for key in ("acceptedDirection", "sourceTaskTitle", "sourceTaskStatus", "handoffDate", "reviewer"):
        filled(data, key, missing)
    if data.get("sourceTaskStatus") != "accepted":
        missing.append("sourceTaskStatus must be accepted")

    changed = data.get("changedFiles", [])
    if not isinstance(changed, list):
        missing.append("changedFiles must be a list")
        changed = []
    changed_set = {item for item in changed if isinstance(item, str)}
    for path in REQUIRED_CHANGED_FILES:
        if path not in changed_set:
            missing.append(f"changedFiles must include {path}")
        elif not (ROOT / path).exists():
            missing.append(f"changed file is missing in workspace: {path}")

    evidence = data.get("evidence", {})
    if not isinstance(evidence, dict):
        missing.append("evidence must be an object")
        evidence = {}
    for key in (
        "dynamicIslandSafeScreenshot",
        "bootScreenshot",
        "welcomeScreenshot",
        "mainMenuScreenshot",
        "brandCaptureDirectory",
    ):
        if not relative_exists(evidence.get(key)):
            missing.append(f"evidence.{key} must point to an existing workspace path")

    tests = data.get("tests", {})
    if not isinstance(tests, dict):
        missing.append("tests must be an object")
        tests = {}
    passed = tests.get("passed", [])
    if not isinstance(passed, list):
        missing.append("tests.passed must be a list")
        passed = []
    passed_set = {item for item in passed if isinstance(item, str)}
    for test in REQUIRED_TESTS:
        if test not in passed_set:
            missing.append(f"tests.passed must include {test}")
    if not isinstance(tests.get("editModeResult"), str) or not tests["editModeResult"].strip():
        missing.append("tests.editModeResult must be filled")
    if not isinstance(tests.get("playModeResult"), str) or not tests["playModeResult"].strip():
        missing.append("tests.playModeResult must be filled")

    signoffs = data.get("signoffs", {})
    if not isinstance(signoffs, dict):
        missing.append("signoffs must be an object")
        signoffs = {}
    for key in ("productOwnerAccepted", "dynamicIslandSafeAreaAccepted", "frontendFlowRegressionAccepted"):
        if signoffs.get(key) is not True:
            missing.append(f"signoffs.{key} must be true")

    notes = data.get("notes", [])
    if not isinstance(notes, list) or not notes:
        missing.append("notes must include at least one note")
    return missing


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("handoff", type=Path)
    parser.add_argument("--allow-pending", action="store_true")
    args = parser.parse_args()

    data = load(args.handoff)
    missing = validate(data)
    if missing:
        print("[DC_BRAND_HANDOFF_PENDING]")
        for item in missing:
            print(f"- {item}")
        return 0 if args.allow_pending else 1

    print("[DC_BRAND_HANDOFF_PASS]")
    print(f"acceptedDirection={data['acceptedDirection']}")
    print(f"sourceTaskTitle={data['sourceTaskTitle']}")
    print(f"handoffDate={data['handoffDate']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
