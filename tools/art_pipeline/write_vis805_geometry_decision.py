#!/usr/bin/env python3
"""Write the VIS-805 geometry optimization decision report from existing metrics."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
VISUAL = ROOT / "docs" / "evidence" / "visual-development"
OUTPUT = VISUAL / "vis805-geometry-optimization-decision-2026-08-25.json"


def load(relative_path: str) -> dict[str, Any]:
    with (ROOT / relative_path).open("r", encoding="utf-8") as stream:
        return json.load(stream)


def percent_reduction(before: float, after: float) -> float:
    if before <= 0:
        return 0.0
    return round((before - after) * 100.0 / before, 1)


def main() -> None:
    closeout = load("docs/evidence/visual-development/closeout-metrics-2026-08-25.json")
    actor_metrics = load("docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json")
    building_metrics = load("docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json")
    environment = load("docs/evidence/visual-development/environment-budget-proxy-report-2026-08-17.json")
    vfx = load("docs/evidence/visual-development/vfx-profiler-overdraw-report-2026-08-17.json")

    catalog_building_triangles = int(building_metrics["summary"]["catalog_total_triangles"])
    generated_building_triangles = int(closeout["buildings"]["total_triangles"])

    report: dict[str, Any] = {
        "schema": "defender-chronicles.visual-development.vis805-geometry-decision.v1",
        "checkpoint": "VIS-805",
        "capturedOn": "2026-08-25",
        "status": "in_review",
        "acceptedVisualStamp": closeout["accepted_visual_stamp"],
        "decision": "No geometry mutation applied before physical Profiler/Frame Debugger evidence.",
        "inputs": [
            "docs/evidence/visual-development/closeout-metrics-2026-08-25.json",
            "docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json",
            "docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json",
            "docs/evidence/visual-development/environment-budget-proxy-report-2026-08-17.json",
            "docs/evidence/visual-development/vfx-profiler-overdraw-report-2026-08-17.json",
        ],
        "currentStaticBudgets": {
            "actors": {
                "count": closeout["actors"]["found"],
                "totalTriangles": closeout["actors"]["total_triangles"],
                "maxActorTriangles": closeout["actors"]["maximum_actor_triangles"],
                "missingSockets": closeout["actors"]["roles_missing_required_sockets"],
                "sourceMetricsTotalTriangles": actor_metrics["summary"]["total_triangle_count"],
            },
            "buildings": {
                "count": closeout["buildings"]["found"],
                "generatedTriangles": generated_building_triangles,
                "legacyCatalogTriangles": catalog_building_triangles,
                "legacyToGeneratedTriangleReductionPercent": percent_reduction(
                    catalog_building_triangles, generated_building_triangles
                ),
                "missingSockets": closeout["buildings"]["roles_missing_required_sockets"],
            },
            "environment": {
                "rendererCount": environment["rendererCount"],
                "rendererSoftBudget": environment["rendererSoftBudget"],
                "triangleCount": environment["triangleCount"],
                "triangleSoftBudget": environment["triangleSoftBudget"],
                "materialSlots": environment["materialSlots"],
                "materialSlotSoftBudget": environment["materialSlotSoftBudget"],
                "shadowCasters": environment["shadowCasters"],
                "colliderCount": environment["colliderCount"],
            },
            "vfxStressProxy": {
                "signals": vfx["signals"],
                "poolGrowth": vfx["poolGrowth"],
                "estimatedTransparentCost": vfx["estimatedTransparentCost"],
                "estimatedTransparentBudget": vfx["estimatedTransparentBudget"],
            },
        },
        "candidateDecisions": [
            {
                "candidate": "Mesh combine",
                "status": "defer",
                "reason": "Environment renderer count is already at the soft budget and there is no physical draw-pass bottleneck yet.",
                "allowedScopeAfterDeviceEvidence": "Only non-interactive background strips or decorative clusters.",
                "blockedScope": "Actors, outposts, build slots, touch targets, pathing roots and VFX sockets.",
            },
            {
                "candidate": "GPU instancing",
                "status": "candidate",
                "reason": "Repeated props and road stones may benefit if Frame Debugger shows repeated material draw calls.",
                "allowedScopeAfterDeviceEvidence": "Repeated non-interactive prop families sharing one material.",
                "blockedScope": "Unique actor/building silhouettes where per-role material or socket identity matters.",
            },
            {
                "candidate": "Static batching",
                "status": "candidate",
                "reason": "Static terrain, road and boundary dressing are safe candidates only after draw-call evidence.",
                "allowedScopeAfterDeviceEvidence": "Terrain, road, boundary and keep dressing with no gameplay collider or input role.",
                "blockedScope": "Build slots, selected outpost panels, dynamic outpost state models, actors and VFX.",
            },
            {
                "candidate": "LOD",
                "status": "reject_for_current_actor_building_set",
                "reason": "Current actor/building triangle counts are well below art-bible soft budgets; LOD would add authoring and QA cost without measured benefit.",
                "allowedScopeAfterDeviceEvidence": "Only large boundary/keep silhouettes if physical triangle or overdraw evidence demands it.",
                "blockedScope": "Small actors and the six gameplay-critical building silhouettes.",
            },
        ],
        "regressionGuards": [
            "No generated FBX, prefab, catalog, slot, route or camera file changed for this checkpoint.",
            "Picking, pathing, eight build slots and touch targets remain untouched.",
            "Any future geometry optimization must include before/after draw call, triangle and interaction regression evidence.",
        ],
        "passesStaticDecisionGate": True,
        "completionGate": "VIS-805 may move from Bekliyor to In review. It cannot be Tamamlandi until physical Profiler/Frame Debugger evidence proves whether a candidate should be applied or rejected.",
    }

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(OUTPUT.relative_to(ROOT))


if __name__ == "__main__":
    main()
