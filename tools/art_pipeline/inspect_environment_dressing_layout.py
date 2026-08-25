#!/usr/bin/env python3
"""Inspect VIS-409 Greenwatch environment dressing layout manifest.

Usage:
  python3 tools/art_pipeline/inspect_environment_dressing_layout.py -- \
    --input docs/evidence/visual-development/environment-dressing-layout-2026-08-16.json \
    --output docs/evidence/visual-development/environment-dressing-layout-metrics-2026-08-16.json
"""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Any


SCALE_BY_FAMILY = {
    "Tree": (0.90, 1.20),
    "Rock": (0.72, 1.10),
    "Bush": (0.60, 0.95),
    "Grass": (0.50, 0.85),
    "Crystal": (0.60, 0.90),
}
ROTATION_LIMIT = 90.0


def distance_point_to_segment(point: tuple[float, float], a: tuple[float, float], b: tuple[float, float]) -> float:
    px, pz = point
    ax, az = a
    bx, bz = b
    dx = bx - ax
    dz = bz - az
    nx = px - ax
    nz = pz - az
    seg_len2 = dx * dx + dz * dz
    if seg_len2 == 0:
        return math.hypot(nx, nz)

    t = (nx * dx + nz * dz) / seg_len2
    t = max(0.0, min(1.0, t))
    cx = ax + t * dx
    cz = az + t * dz
    return math.hypot(px - cx, pz - cz)


def min_distance_to_polyline(point: tuple[float, float], polyline: list[tuple[float, float]]) -> float:
    if len(polyline) < 2:
        return 10_000.0
    return min(
        distance_point_to_segment(point, polyline[i], polyline[i + 1])
        for i in range(len(polyline) - 1)
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    layout_path = Path(args.input)
    out_path = Path(args.output)
    layout = json.loads(layout_path.read_text(encoding="utf-8"))

    path = [(entry["x"], entry["z"]) for entry in layout["path"]]
    slots = [(entry["code"], entry["x"], entry["z"]) for entry in layout["slots"]]
    placements = layout["placements"]
    cluster_rules = {entry["id"]: entry for entry in layout["cluster_rules"]}
    radius_by_family = {k: float(v) for k, v in layout["radius_by_family"].items()}

    violations: list[str] = []
    cluster_counts: dict[str, int] = {}
    family_counts: dict[str, int] = {}
    variant_counts: dict[str, int] = {}

    for item in placements:
        cluster_id = item["cluster_id"]
        cluster_counts[cluster_id] = cluster_counts.get(cluster_id, 0) + 1
        family_counts[item["family"]] = family_counts.get(item["family"], 0) + 1
        variant_counts[item["asset_id"]] = variant_counts.get(item["asset_id"], 0) + 1

        point = (float(item["x"]), float(item["z"]))
        scale = float(item["scale"])
        rot = abs(float(item["rotation_y"]))
        family = item["family"]
        asset_id = item["asset_id"]

        if not (SCALE_BY_FAMILY[family][0] <= scale <= SCALE_BY_FAMILY[family][1]):
            violations.append(
                f"{asset_id}: scale {scale:.2f} outside family range "
                f"{SCALE_BY_FAMILY[family][0]:.2f}-{SCALE_BY_FAMILY[family][1]:.2f}"
            )

        if rot > ROTATION_LIMIT:
            violations.append(f"{asset_id}: rotation {rot:.1f} out of [-90, 90] safety range")

        px, pz = point
        world_ok = layout["world_bounds"]["min_x"] <= px <= layout["world_bounds"]["max_x"] and \
            layout["world_bounds"]["min_z"] <= pz <= layout["world_bounds"]["max_z"]
        if not world_ok:
            violations.append(f"{asset_id}: out of world bounds at ({px:.2f},{pz:.2f})")

        path_distance = min_distance_to_polyline(point, path)
        if path_distance < float(layout["path_clear_radius"]):
            violations.append(
                f"{asset_id}: too near path ({path_distance:.2f} < {layout['path_clear_radius']:.2f})"
            )

        family_radius = radius_by_family.get(family, 0.8)
        for slot_code, sx, sz in slots:
            slot_distance = math.hypot(px - sx, pz - sz)
            required = float(layout["slot_clear_radius"]) + float(layout["slot_hit_margin"]) + family_radius * 0.5
            if slot_distance < required:
                violations.append(
                    f"{asset_id}: too near slot {slot_code} "
                    f"({slot_distance:.2f} < {required:.2f})"
                )

    for cluster_id, count in cluster_counts.items():
        rule = cluster_rules.get(cluster_id)
        if rule and count > int(rule["max_items"]):
            violations.append(f"{cluster_id}: has {count} items, exceeds max {rule['max_items']}")

    for index, left in enumerate(placements):
        for right in placements[index + 1 :]:
            if left["asset_id"] == right["asset_id"]:
                d = math.hypot(left["x"] - right["x"], left["z"] - right["z"])
                if d < layout["no_repeat_radius"]:
                    violations.append(
                        f"repeat hazard: {left['asset_id']} and {right['asset_id']} distance {d:.2f}"
                    )

    report: dict[str, Any] = {
        "schema": "defender-chronicles.visual-development.greenwatch-dressing-layout-metrics.v1",
        "captured_on": layout["captured_on"],
        "todo": layout["todo"],
        "source": str(layout_path),
        "path_clear_radius": layout["path_clear_radius"],
        "slot_clear_radius": layout["slot_clear_radius"],
        "no_repeat_radius": layout["no_repeat_radius"],
        "total_placements": len(placements),
        "family_counts": family_counts,
        "variant_counts": variant_counts,
        "cluster_counts": cluster_counts,
        "cluster_rules": cluster_rules,
        "violations": violations,
        "violations_count": len(violations),
        "passes": {
            "within_world_bounds": all(
                layout["world_bounds"]["min_x"] <= item["x"] <= layout["world_bounds"]["max_x"]
                and layout["world_bounds"]["min_z"] <= item["z"] <= layout["world_bounds"]["max_z"]
                for item in placements
            ),
            "slot_clear": all(
                all(
                    math.hypot(item["x"] - sx, item["z"] - sz)
                    >= float(layout["slot_clear_radius"]) + float(layout["slot_hit_margin"]) +
                    radius_by_family.get(item["family"], 0.8) * 0.5
                    for _code, sx, sz in slots
                )
                for item in placements
            ),
            "path_clear": all(
                min_distance_to_polyline((item["x"], item["z"]), path) >= float(layout["path_clear_radius"])
                for item in placements
            ),
            "scale_and_rotation_valid": all(
                SCALE_BY_FAMILY[item["family"]][0] <= item["scale"] <= SCALE_BY_FAMILY[item["family"]][1]
                and abs(item["rotation_y"]) <= ROTATION_LIMIT
                for item in placements
            ),
            "repeat_control": all(
                math.hypot(left["x"] - right["x"], left["z"] - right["z"]) >= layout["no_repeat_radius"]
                for i, left in enumerate(placements)
                for right in placements[i + 1 :]
                if left["asset_id"] == right["asset_id"]
            ),
            "has_all_asset_variants": all(v in variant_counts for v in layout["asset_variants"]),
        },
        "ready_for_runtime_integration": len(violations) == 0,
    }

    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[DC_ENV_DRESSING_LAYOUT] {out_path}")


if __name__ == "__main__":
    main()
