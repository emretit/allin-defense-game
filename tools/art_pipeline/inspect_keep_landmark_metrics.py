#!/usr/bin/env python3
"""Inspect VIS-407 generated Greenwatch keep landmark FBX files.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/inspect_keep_landmark_metrics.py -- \
    --output docs/evidence/visual-development/keep-landmark-metrics-2026-08-16.json
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any

import bpy


ASSETS = (
    {
        "id": "GreenwatchKeep_Core",
        "category": "Defender keep core mass",
        "path": "Assets/Art/Models/Generated/GreenwatchKeep_Core.fbx",
        "source": "ArtSource/Blender/GreenwatchKeep_Core.blend",
        "triangle_soft_budget": 1800,
        "material_soft_budget": 7,
        "acceptance_note": "Strongest friendly mass with keep-center, banner and command sockets.",
    },
    {
        "id": "GreenwatchKeep_CommandLight",
        "category": "Keep command beacon",
        "path": "Assets/Art/Models/Generated/GreenwatchKeep_CommandLight.fbx",
        "source": "ArtSource/Blender/GreenwatchKeep_CommandLight.blend",
        "triangle_soft_budget": 520,
        "material_soft_budget": 3,
        "acceptance_note": "Cool/friendly command cue readable by ring, shard and beacon form.",
    },
    {
        "id": "GreenwatchKeep_Dressing",
        "category": "Keep apron and final-stand dressing",
        "path": "Assets/Art/Models/Generated/GreenwatchKeep_Dressing.fbx",
        "source": "ArtSource/Blender/GreenwatchKeep_Dressing.blend",
        "triangle_soft_budget": 600,
        "material_soft_budget": 8,
        "acceptance_note": "Final-stand framing stays out of S7/S8 and keep silhouette priority.",
    },
)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def parse_meta(path: Path) -> dict[str, Any]:
    meta_path = path.with_suffix(path.suffix + ".meta")
    if not meta_path.exists():
        return {"exists": False}

    text = meta_path.read_text(encoding="utf-8")

    def find_scalar(key: str) -> str | None:
        match = re.search(rf"^\s*{re.escape(key)}:\s*(.+)$", text, re.MULTILINE)
        return match.group(1).strip() if match else None

    return {
        "exists": True,
        "guid": find_scalar("guid"),
        "globalScale": find_scalar("globalScale"),
        "meshCompression": find_scalar("meshCompression"),
        "useFileScale": find_scalar("useFileScale"),
        "importAnimation": find_scalar("importAnimation"),
        "animationType": find_scalar("animationType"),
        "addColliders": find_scalar("addColliders"),
        "materialImportMode": find_scalar("materialImportMode"),
    }


def sha256(path: Path) -> str | None:
    if not path.exists():
        return None
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def bounds_from_vertices(vertices: list[tuple[float, float, float]]) -> dict[str, Any]:
    if not vertices:
        return {"min": [0, 0, 0], "max": [0, 0, 0], "size": [0, 0, 0], "center": [0, 0, 0]}
    mins = [min(vertex[axis] for vertex in vertices) for axis in range(3)]
    maxs = [max(vertex[axis] for vertex in vertices) for axis in range(3)]
    return {
        "min": [round(value, 4) for value in mins],
        "max": [round(value, 4) for value in maxs],
        "size": [round(maxs[axis] - mins[axis], 4) for axis in range(3)],
        "center": [round((mins[axis] + maxs[axis]) * 0.5, 4) for axis in range(3)],
    }


def inspect_asset(entry: dict[str, Any]) -> dict[str, Any]:
    path = Path(entry["path"])
    if not path.exists():
        return {**entry, "exists": False}

    reset_scene()
    bpy.ops.import_scene.fbx(filepath=str(path))
    bpy.context.view_layer.update()
    objects = list(bpy.context.scene.objects)
    mesh_objects = [obj for obj in objects if obj.type == "MESH"]
    empty_objects = [obj for obj in objects if obj.type == "EMPTY"]
    materials = sorted(
        {
            slot.material.name
            for obj in mesh_objects
            for slot in obj.material_slots
            if slot.material is not None
        }
    )
    vertices = [
        tuple(obj.matrix_world @ vertex.co)
        for obj in mesh_objects
        for vertex in obj.data.vertices
    ]
    triangle_count = sum(max(1, len(poly.vertices) - 2) for obj in mesh_objects for poly in obj.data.polygons)
    material_count = len(materials)
    return {
        **entry,
        "exists": True,
        "sha256": sha256(path),
        "source_exists": Path(entry["source"]).exists(),
        "source_sha256": sha256(Path(entry["source"])),
        "meta": parse_meta(path),
        "object_count": len(objects),
        "mesh_count": len(mesh_objects),
        "empty_count": len(empty_objects),
        "socket_names": sorted(obj.name for obj in empty_objects),
        "mesh_names_sample": sorted(obj.name for obj in mesh_objects)[:20],
        "vertex_count": sum(len(obj.data.vertices) for obj in mesh_objects),
        "polygon_count": sum(len(obj.data.polygons) for obj in mesh_objects),
        "triangle_count": triangle_count,
        "material_count": material_count,
        "material_names": materials,
        "bounds": bounds_from_vertices(vertices),
        "over_triangle_soft_budget": triangle_count > int(entry["triangle_soft_budget"]),
        "over_material_soft_budget": material_count > int(entry["material_soft_budget"]),
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    assets = [inspect_asset(entry) for entry in ASSETS]
    report = {
        "schema": "defender-chronicles.visual-development.keep-landmark-metrics.v1",
        "captured_on": "2026-08-16",
        "todo": "VIS-407",
        "asset_scope": "Generated defender keep/command landmark FBX files only; not bound to runtime environment prefab.",
        "summary": {
            "expected_asset_count": len(ASSETS),
            "generated_asset_count": sum(1 for asset in assets if asset.get("exists")),
            "total_triangles": sum(int(asset.get("triangle_count", 0)) for asset in assets),
            "assets_over_triangle_soft_budget": [
                asset["id"] for asset in assets if asset.get("over_triangle_soft_budget")
            ],
            "assets_over_material_soft_budget": [
                asset["id"] for asset in assets if asset.get("over_material_soft_budget")
            ],
            "assets_missing_meta": [
                asset["id"] for asset in assets if not asset.get("meta", {}).get("exists")
            ],
            "assets_with_imported_colliders_enabled": [
                asset["id"] for asset in assets if asset.get("meta", {}).get("addColliders") != "0"
            ],
            "assets_missing_source_blend": [
                asset["id"] for asset in assets if not asset.get("source_exists")
            ],
            "unity_validation_status": "Pending: Unity CLI/Editor unavailable in current shell.",
            "runtime_binding_status": "Pending: generated keep candidates are not attached to the production environment prefab.",
        },
        "assets": assets,
    }

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"[DC_KEEP_LANDMARK_METRICS] {output}")


if __name__ == "__main__":
    main()
