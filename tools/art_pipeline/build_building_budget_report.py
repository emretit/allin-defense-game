"""Build VIS-346 generated building and building-action VFX budget report.

This script aggregates already captured Blender metrics into one machine-readable
budget gate. It does not claim Unity import, texture compression or runtime
Profiler acceptance; those remain explicit pending gates.

Usage:
  python3 tools/art_pipeline/build_building_budget_report.py \
    --output docs/evidence/visual-development/building-budget-report-2026-08-16.json
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any


CAPTURED_ON = "2026-08-16"
BUILDING_TRIANGLE_SOFT_BUDGET = 800
BUILDING_MATERIAL_BUDGET = 3
BUILDING_TOTAL_TRIANGLE_SOFT_BUDGET = 4500
VFX_TRIANGLE_SOFT_BUDGET = 500
VFX_MATERIAL_BUDGET = 3
VFX_TOTAL_TRIANGLE_SOFT_BUDGET = 2500

BUILDING_METRICS_PATH = Path("docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json")
VFX_METRIC_PATHS = (
    Path("docs/evidence/visual-development/build-vfx-metrics-2026-08-16.json"),
    Path("docs/evidence/visual-development/upgrade-vfx-metrics-2026-08-16.json"),
    Path("docs/evidence/visual-development/sell-vfx-metrics-2026-08-16.json"),
)


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def sha256(path: Path) -> str | None:
    if not path.exists():
        return None
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def summarize_buildings(metrics: dict[str, Any]) -> list[dict[str, Any]]:
    rows = []
    for entry in metrics.get("buildings", []):
        candidate = entry.get("generated_candidate", {})
        asset_path = Path(candidate.get("path", ""))
        triangle_count = int(candidate.get("triangle_count", 0))
        material_count = int(candidate.get("material_count", 0))
        rows.append(
            {
                "id": entry.get("id"),
                "kind": entry.get("kind"),
                "level": entry.get("level"),
                "asset_path": candidate.get("path"),
                "exists": bool(candidate.get("exists")),
                "sha256": sha256(asset_path),
                "meta_guid": candidate.get("meta", {}).get("guid"),
                "triangle_count": triangle_count,
                "material_count": material_count,
                "material_names": candidate.get("material_names", []),
                "texture_reference_count": 0,
                "texture_status": "Flat generated materials only; Unity texture import/compression profile pending.",
                "socket_gap_count": len(candidate.get("missing_required_runtime_sockets", [])),
                "scaled_bounds_size": candidate.get("scaled_bounds_size"),
                "over_triangle_soft_budget": triangle_count > BUILDING_TRIANGLE_SOFT_BUDGET,
                "over_material_budget": material_count > BUILDING_MATERIAL_BUDGET,
            }
        )
    return rows


def summarize_vfx(metrics_paths: tuple[Path, ...]) -> list[dict[str, Any]]:
    rows = []
    for metrics_path in metrics_paths:
        metrics = read_json(metrics_path)
        family = metrics_path.name.replace("-vfx-metrics-2026-08-16.json", "")
        for entry in metrics.get("assets", []):
            asset_path = Path(entry.get("path", ""))
            triangle_count = int(entry.get("triangle_count", 0))
            material_count = int(entry.get("material_count", 0))
            rows.append(
                {
                    "family": family,
                    "asset": entry.get("asset"),
                    "asset_path": entry.get("path"),
                    "exists": bool(entry.get("exists")),
                    "sha256": sha256(asset_path),
                    "meta_guid": entry.get("meta", {}).get("guid"),
                    "triangle_count": triangle_count,
                    "material_count": material_count,
                    "material_names": entry.get("material_names", []),
                    "over_triangle_soft_budget": triangle_count > VFX_TRIANGLE_SOFT_BUDGET,
                    "over_material_budget": material_count > VFX_MATERIAL_BUDGET,
                }
            )
    return rows


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    building_metrics = read_json(BUILDING_METRICS_PATH)
    buildings = summarize_buildings(building_metrics)
    vfx_assets = summarize_vfx(VFX_METRIC_PATHS)
    building_total_triangles = sum(item["triangle_count"] for item in buildings)
    vfx_total_triangles = sum(item["triangle_count"] for item in vfx_assets)
    building_overages = [item["id"] for item in buildings if item["over_triangle_soft_budget"]]
    building_material_overages = [item["id"] for item in buildings if item["over_material_budget"]]
    vfx_overages = [
        f"{item['family']}/{item['asset']}" for item in vfx_assets if item["over_triangle_soft_budget"]
    ]
    vfx_material_overages = [
        f"{item['family']}/{item['asset']}" for item in vfx_assets if item["over_material_budget"]
    ]

    report = {
        "schema": "defender-chronicles.visual-development.building-budget-report.v1",
        "captured_on": CAPTURED_ON,
        "todo": "VIS-346",
        "asset_scope": "Generated building candidates plus generated build/upgrade/sell action VFX.",
        "budgets": {
            "building_triangle_soft_budget_per_asset": BUILDING_TRIANGLE_SOFT_BUDGET,
            "building_material_budget_per_asset": BUILDING_MATERIAL_BUDGET,
            "building_total_triangle_soft_budget": BUILDING_TOTAL_TRIANGLE_SOFT_BUDGET,
            "vfx_triangle_soft_budget_per_asset": VFX_TRIANGLE_SOFT_BUDGET,
            "vfx_material_budget_per_asset": VFX_MATERIAL_BUDGET,
            "vfx_total_triangle_soft_budget": VFX_TOTAL_TRIANGLE_SOFT_BUDGET,
        },
        "summary": {
            "building_asset_count": len(buildings),
            "building_total_triangles": building_total_triangles,
            "building_assets_over_triangle_soft_budget": building_overages,
            "building_assets_over_material_budget": building_material_overages,
            "building_assets_with_socket_gaps": [
                item["id"] for item in buildings if item["socket_gap_count"] > 0
            ],
            "building_total_over_triangle_soft_budget": building_total_triangles > BUILDING_TOTAL_TRIANGLE_SOFT_BUDGET,
            "vfx_asset_count": len(vfx_assets),
            "vfx_total_triangles": vfx_total_triangles,
            "vfx_assets_over_triangle_soft_budget": vfx_overages,
            "vfx_assets_over_material_budget": vfx_material_overages,
            "vfx_total_over_triangle_soft_budget": vfx_total_triangles > VFX_TOTAL_TRIANGLE_SOFT_BUDGET,
            "texture_import_status": "Pending Unity import profile verification; generated FBX candidates do not reference external texture files.",
            "continuous_vfx_status": "Pending: no final always-on Mage/ambient building VFX runtime prefab has been profiled yet.",
            "unity_validation_status": "Pending: Unity CLI/Editor unavailable in current shell.",
        },
        "buildings": buildings,
        "building_action_vfx": vfx_assets,
        "decision": {
            "generated_candidate_budget_status": (
                "Needs review: one or more generated building candidates exceed soft per-asset budget."
                if building_overages or building_material_overages
                else "Pass for current generated building FBX geometry/material soft budgets."
            ),
            "generated_action_vfx_budget_status": (
                "Needs review: one or more generated building-action VFX assets exceed soft budget."
                if vfx_overages or vfx_material_overages
                else "Pass for current generated build/upgrade/sell VFX geometry/material soft budgets."
            ),
            "final_vis_346_status": "Not complete until Unity import settings, texture profile, continuous VFX cost and runtime evidence are recorded.",
        },
    }

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"[DC_BUILDING_BUDGET_REPORT] {output}")


if __name__ == "__main__":
    main()
