#!/usr/bin/env python3
"""Validate the human review sign-off file for visual-slice closeout."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]

REQUIRED_FLAGS = {
    "audio": [
        "vis706AmbienceMaskingApproved",
        "vis707SpeakerReviewApproved",
        "vis707HeadphoneReviewApproved",
        "vis707AudioOnlyStateReviewApproved",
        "vis707VisualOnlyStateReviewApproved",
        "vis708EightMinuteHumanReviewApproved",
        "vis708FinalVideoApproved",
    ],
    "accessibility": [
        "vis813GrayscaleReviewApproved",
        "vis813TouchTargetReviewApproved",
        "vis813NonColorStateReviewApproved",
        "vis813VoiceOverReviewApproved",
        "vis813BlindRecognitionApproved",
        "vis814BeforeAfterReviewApproved",
    ],
}

REQUIRED_EVIDENCE = [
    "audioVisualAcceptanceLog",
    "visualAuditReport",
    "grayscaleContactSheet",
    "beforeAfterContactSheet",
]


def as_dict(value: Any, path: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise ValueError(f"{path} object olmali.")
    return value


def path_exists(relative_path: str) -> bool:
    return bool(relative_path) and (ROOT / relative_path).exists()


def validate(data: dict[str, Any]) -> list[str]:
    missing: list[str] = []
    if data.get("schema") != "defender-chronicles.visual-human-signoff.v1":
        missing.append("schema defender-chronicles.visual-human-signoff.v1 olmali")
    if not str(data.get("acceptedVisualStamp", "")).strip():
        missing.append("acceptedVisualStamp bos olamaz")
    if not str(data.get("reviewer", "")).strip():
        missing.append("reviewer doldurulmali")
    if not str(data.get("reviewDate", "")).strip():
        missing.append("reviewDate doldurulmali")

    evidence = as_dict(data.get("evidence", {}), "evidence")
    for key in REQUIRED_EVIDENCE:
        value = str(evidence.get(key, "")).strip()
        if not path_exists(value):
            missing.append(f"evidence.{key} mevcut dosyaya baglanmali")

    signoffs = as_dict(data.get("signoffs", {}), "signoffs")
    for group, flags in REQUIRED_FLAGS.items():
        section = as_dict(signoffs.get(group, {}), f"signoffs.{group}")
        for flag in flags:
            if section.get(flag) is not True:
                missing.append(f"signoffs.{group}.{flag} true olmali")

    notes = data.get("notes", [])
    if not isinstance(notes, list) or not notes:
        missing.append("notes en az bir review notu icermeli")
    return missing


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("signoff", type=Path)
    parser.add_argument(
        "--allow-pending",
        action="store_true",
        help="Eksikleri raporla ama cikis kodunu basarili dondur.",
    )
    args = parser.parse_args()

    with args.signoff.open("r", encoding="utf-8") as stream:
        data = json.load(stream)

    missing = validate(data)
    if missing:
        print("[DC_HUMAN_SIGNOFF_PENDING]")
        for item in missing:
            print(f"- {item}")
        return 0 if args.allow_pending else 1

    print("[DC_HUMAN_SIGNOFF_PASS]")
    print(f"acceptedVisualStamp={data['acceptedVisualStamp']}")
    print(f"reviewer={data['reviewer']}")
    print(f"reviewDate={data['reviewDate']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
