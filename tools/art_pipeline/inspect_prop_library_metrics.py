#!/usr/bin/env python3
"""Inspect VIS-408 generated Greenwatch prop library FBX files.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/inspect_prop_library_metrics.py -- \
    --output docs/evidence/visual-development/prop-library-metrics-2026-08-16.json
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
        "id": "Prop_PineTall_A",
        "family": "Tree",
        "variant": "A",
        "path": "Assets/Art/Models/Generated/Prop_PineTall_A.fbx",
        "source": "ArtSource/Blender/Prop_PineTall_A.blend",
        "triangle_soft_budget": 300,
        "material_soft_budget": 4,
    },
    {
        "id": "Prop_PineTall_B",
        "family": "Tree",
        "variant": "B",
        "path": "Assets/Art/Models/Generated/Prop_PineTall_B.fbx",
        "source": "ArtSource/Blender/Prop_PineTall_B.blend",
        "triangle_soft_budget": 300,
        "material_soft_budget": 4,
    },
    {
        "id": "Prop_RockCluster_A",
        "family": "Rock",
        "variant": "A",
        "path": "Assets/Art/Models/Generated/Prop_RockCluster_A.fbx",
        "source": "ArtSource/Blender/Prop_RockCluster_A.blend",
        "triangle_soft_budget": 160,
        "material_soft_budget": 2,
    },
    {
        "id": "Prop_RockCluster_B",
        "family": "Rock",
        "variant": "B",
        "path": "Assets/Art/Models/Generated/Prop_RockCluster_B.fbx",
        "source": "ArtSource/Blender/Prop_RockCluster_B.blend",
        "triangle_soft_budget": 200,
        "material_soft_budget": 2,
    },
    {
        "id": "Prop_BushRound_A",
        "family": "Bush",
        "variant": "A",
        "path": "Assets/Art/Models/Generated/Prop_BushRound_A.fbx",
        "source": "ArtSource/Blender/Prop_BushRound_A.blend",
        "triangle_soft_budget": 180,
        "material_soft_budget": 3,
    },
    {
        "id": "Prop_BushRound_B",
        "family": "Bush",
        "variant": "B",
        "path": "Assets/Art/Models/Generated/Prop_BushRound_B.fbx",
        "source": "ArtSource/Blender/Prop_BushRound_B.blend",
        "triangle_soft_budget": 180,
        "material_soft_budget": 3,
    },
    {
        "id": "Prop_GrassPatch_A",
        "family": "Grass",
        "variant": "A",
        "path": "Assets/Art/Models/Generated/Prop_GrassPatch_A.fbx",
        "source": "ArtSource/Blender/Prop_GrassPatch_A.blend",
        "triangle_soft_budget": 120,
        "material_soft_budget": 2,
    },
    {
        "id": "Prop_GrassPatch_B",
        "family": "Grass",
        "variant": "B",
        "path": "Assets/Art/Models/Generated/Prop_GrassPatch_B.fbx",
        "source": "ArtSource/Blender/Prop_GrassPatch_B.blend",
        "triangle_soft_budget": 120,
        "material_soft_budget": 3,
    },
    {
        "id": "Prop_CrystalCluster_A",
        "family": "Crystal",
        "variant": "A",
        "path": "Assets/Art/Models/Generated/Prop_CrystalCluster_A.fbx",
        "source": "ArtSource/Blender/Prop_CrystalCluster_A.blend",
        "triangle_soft_budget": 160,
        "material_soft_budget": 3,
    },
    {
        "id": "Prop_CrystalCluster_B",
        "family": "Crystal",
        "variant": "B",
        "path": "Assets/Art/Models/Generated/Prop_CrystalCluster_B.fbx",
        "source": "ArtSource/Blender/Prop_CrystalCluster_B.blend",
        "triangle_soft_budget": 160,
        "material_soft_budget": 4,
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
    family_counts: dict[str, int] = {}
    for asset in assets:
        family_counts[asset["family"]] = family_counts.get(asset["family"], 0) + (1 if asset.get("exists") else 0)

    report = {
        "schema": "allin-defense.visual-development.prop-library-metrics.v1",
        "captured_on": "2026-08-16",
        "todo": "VIS-408",
        "asset_scope": "Generated prop library FBX files only; not bound to runtime environment prefab.",
        "summary": {
            "expected_asset_count": len(ASSETS),
            "generated_asset_count": sum(1 for asset in assets if asset.get("exists")),
            "family_counts": family_counts,
            "families_with_two_or_more_variants": sorted([name for name, count in family_counts.items() if count >= 2]),
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
            "update_script_status": "No scripts are embedded in FBX candidates; runtime prefab checks remain pending.",
            "unity_validation_status": "Pending: Unity CLI/Editor unavailable in current shell.",
            "runtime_binding_status": "Pending: generated prop candidates are not attached to the production environment prefab.",
        },
        "assets": assets,
    }

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"[DC_PROP_LIBRARY_METRICS] {output}")


if __name__ == "__main__":
    main()
