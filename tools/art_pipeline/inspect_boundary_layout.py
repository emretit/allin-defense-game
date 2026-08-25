#!/usr/bin/env python3
"""Inspect VIS-410 boundary layout manifest for path, slot and repeat safety."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Any


SCALE_BY_FAMILY = {
    "RidgeWall": (0.70, 1.35),
    "ForegroundStrip": (0.75, 1.30),
    "Silhouette": (0.80, 1.30),
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
    parser.add_argument("--layout", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    layout_path = Path(args.layout)
    out_path = Path(args.output)
    layout = json.loads(layout_path.read_text(encoding="utf-8"))

    path = [(entry["x"], entry["z"]) for entry in layout["path"]]
    slots = [(entry["code"], entry["x"], entry["z"]) for entry in layout["slots"]]
    placements = layout["placements"]
    cluster_rules = {entry["id"]: entry for entry in layout["cluster_rules"]}
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

        if family in SCALE_BY_FAMILY:
            family_scale_min, family_scale_max = SCALE_BY_FAMILY[family]
            if not (family_scale_min <= scale <= family_scale_max):
                violations.append(
                    f"{asset_id}: scale {scale:.2f} outside family range "
                    f"{family_scale_min:.2f}-{family_scale_max:.2f}"
                )
        else:
            violations.append(f"{asset_id}: unknown family {family}")

        if rot > ROTATION_LIMIT:
            violations.append(f"{asset_id}: rotation {rot:.1f} out of [-90, 90] safety range")

        px, pz = point
        wb = layout["world_bounds"]
        if not (wb["min_x"] <= px <= wb["max_x"] and wb["min_z"] <= pz <= wb["max_z"]):
            violations.append(f"{asset_id}: out of world bounds at ({px:.2f},{pz:.2f})")

        path_distance = min_distance_to_polyline(point, path)
        if path_distance < float(layout["path_clear_radius"]):
            violations.append(
                f"{asset_id}: too near path ({path_distance:.2f} < {layout['path_clear_radius']:.2f})"
            )

        if family in layout.get("family_min_radius", {}):
            family_radius = float(layout["family_min_radius"][family])
        else:
            family_radius = 1.0

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
        if rule is None:
            continue
        if count > int(rule["max_items"]):
            violations.append(f"{cluster_id}: has {count} items, exceeds max {rule['max_items']}")

    for family, min_count in {k: 0 for k in layout.get("family_min_radius", {})}.items():
        # keep compatibility: family_min is enforced per cluster below
        pass

    for index, left in enumerate(placements):
        for right in placements[index + 1 :]:
            if left["asset_id"] == right["asset_id"]:
                d = math.hypot(left["x"] - right["x"], left["z"] - right["z"])
                if d < layout["no_repeat_radius"]:
                    violations.append(
                        f"repeat hazard: {left['asset_id']} and {right['asset_id']} distance {d:.2f}"
                    )

    # Family-min per cluster check
    for cluster_id, rule in cluster_rules.items():
        family_set = {item["family"] for item in placements if item["cluster_id"] == cluster_id}
        if len(family_set) < int(rule.get("family_min", 1)):
            violations.append(
                f"{cluster_id}: has {len(family_set)} distinct family, family_min is {rule['family_min']}"
            )

    passes = {
        "within_world_bounds": all(
            layout["world_bounds"]["min_x"] <= item["x"] <= layout["world_bounds"]["max_x"]
            and layout["world_bounds"]["min_z"] <= item["z"] <= layout["world_bounds"]["max_z"]
            for item in placements
        ),
        "slot_clear": all(
            all(
                math.hypot(item["x"] - sx, item["z"] - sz)
                >= float(layout["slot_clear_radius"]) + float(layout["slot_hit_margin"]) +
                layout.get("family_min_radius", {}).get(item["family"], 0.9) * 0.5
                for _code, sx, sz in slots
            )
            for item in placements
        ),
        "path_clear": all(
            min_distance_to_polyline((item["x"], item["z"]), path) >= float(layout["path_clear_radius"])
            for item in placements
        ),
        "repeat_control": all(
            math.hypot(left["x"] - right["x"], left["z"] - right["z"]) >= layout["no_repeat_radius"]
            for i, left in enumerate(placements)
            for right in placements[i + 1 :]
            if left["asset_id"] == right["asset_id"]
        ),
        "scale_and_rotation_valid": all(
            SCALE_BY_FAMILY[item["family"]][0] <= item["scale"] <= SCALE_BY_FAMILY[item["family"]][1]
            and abs(item["rotation_y"]) <= ROTATION_LIMIT
            for item in placements
        ),
        "asset_variants_present": all(v in variant_counts for v in layout["asset_variants"]),
        "has_cluster_rules": all(rule["id"] in cluster_counts or rule["max_items"] >= 0 for rule in layout["cluster_rules"]),
    }

    report: dict[str, Any] = {
        "schema": "defender-chronicles.visual-development.greenwatch-boundary-layout-metrics.v1",
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
        "passes": passes,
        "ready_for_runtime_integration": len(violations) == 0,
    }

    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[DC_BOUNDARY_LAYOUT] {out_path}")


if __name__ == "__main__":
    main()
