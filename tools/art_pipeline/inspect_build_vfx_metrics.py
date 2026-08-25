"""Inspect generated build-in VFX FBX metrics.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/inspect_build_vfx_metrics.py -- \
    --output docs/evidence/visual-development/build-vfx-metrics-2026-08-16.json
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
import warnings
from pathlib import Path

import bpy

warnings.filterwarnings("ignore", category=DeprecationWarning)


VFX_ASSETS = (
    ("Dust", "Assets/Art/Models/Generated/BuildVfx_Dust.fbx"),
    ("Ring", "Assets/Art/Models/Generated/BuildVfx_Ring.fbx"),
    ("Spark", "Assets/Art/Models/Generated/BuildVfx_Spark.fbx"),
)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def parse_meta(path: Path) -> dict[str, str | bool]:
    meta_path = Path(f"{path}.meta")
    data: dict[str, str | bool] = {"exists": meta_path.exists()}
    if not meta_path.exists():
        return data
    for line in meta_path.read_text(encoding="utf-8", errors="ignore").splitlines():
        stripped = line.strip()
        for key in ("guid", "globalScale", "useFileScale", "animationType", "importAnimation", "meshCompression"):
            prefix = f"{key}: "
            if stripped.startswith(prefix):
                data[key] = stripped[len(prefix):]
    return data


def sha256(path: Path) -> str | None:
    if not path.exists():
        return None
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


def inspect_fbx(path: Path) -> dict:
    if not path.exists():
        return {"path": str(path), "exists": False}

    reset_scene()
    bpy.ops.import_scene.fbx(filepath=str(path))
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    triangle_count = 0
    vertex_count = 0
    material_names: set[str] = set()

    for obj in mesh_objects:
        vertex_count += len(obj.data.vertices)
        for polygon in obj.data.polygons:
            triangle_count += max(1, len(polygon.vertices) - 2)
        for material in obj.data.materials:
            if material is not None:
                material_names.add(material.name.split(".")[0])

    vertices = [obj.matrix_world @ vertex.co for obj in mesh_objects for vertex in obj.data.vertices]
    bounds = None
    if vertices:
        min_x = min(vertex.x for vertex in vertices)
        max_x = max(vertex.x for vertex in vertices)
        min_y = min(vertex.y for vertex in vertices)
        max_y = max(vertex.y for vertex in vertices)
        min_z = min(vertex.z for vertex in vertices)
        max_z = max(vertex.z for vertex in vertices)
        bounds = {
            "size": [round(max_x - min_x, 4), round(max_y - min_y, 4), round(max_z - min_z, 4)],
            "center": [
                round((min_x + max_x) * 0.5, 4),
                round((min_y + max_y) * 0.5, 4),
                round((min_z + max_z) * 0.5, 4),
            ],
        }

    return {
        "path": str(path),
        "exists": True,
        "size_bytes": path.stat().st_size,
        "sha256": sha256(path),
        "meta": parse_meta(path),
        "mesh_count": len(mesh_objects),
        "object_count": len(bpy.context.scene.objects),
        "vertex_count": vertex_count,
        "triangle_count": triangle_count,
        "material_count": len(material_names),
        "material_names": sorted(material_names),
        "bounds": bounds,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    assets = []
    for label, path in VFX_ASSETS:
        metrics = inspect_fbx(Path(path))
        assets.append({"asset": label, **metrics})

    report = {
        "summary": {
            "expected_asset_count": len(VFX_ASSETS),
            "generated_asset_count": sum(1 for asset in assets if asset["exists"]),
            "total_triangles": sum(asset.get("triangle_count", 0) for asset in assets),
            "assets_over_material_budget": [
                asset["asset"] for asset in assets if asset.get("material_count", 0) > 3
            ],
            "assets_over_triangle_soft_budget": [
                asset["asset"] for asset in assets if asset.get("triangle_count", 0) > 500
            ],
            "unity_validation_status": "Pending: Unity CLI/Editor unavailable in current shell.",
        },
        "assets": assets,
    }

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"[DC_BUILD_VFX_METRICS] {output}")


if __name__ == "__main__":
    main()
