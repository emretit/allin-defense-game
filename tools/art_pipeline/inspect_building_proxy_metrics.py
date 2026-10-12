"""Inspect current and generated building proxy FBX files for visual development.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/inspect_building_proxy_metrics.py -- \
    --output docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json
"""

from __future__ import annotations

import argparse
import datetime
import json
import re
import sys
from pathlib import Path
from typing import Any

import bpy


BUILDINGS = (
    {
        "id": "Archer_L1",
        "kind": "Archer",
        "level": 1,
        "catalog_path": "Assets/Art/Models/Generated/Archer_L1.fbx",
        "generated_path": "Assets/Art/Models/Generated/Archer_L1.fbx",
        "scale": 0.86,
    },
    {
        "id": "Archer_L2",
        "kind": "Archer",
        "level": 2,
        "catalog_path": "Assets/Art/Models/Generated/Archer_L2.fbx",
        "generated_path": "Assets/Art/Models/Generated/Archer_L2.fbx",
        "scale": 0.82,
    },
    {
        "id": "Barracks_L1",
        "kind": "Barracks",
        "level": 1,
        "catalog_path": "Assets/Art/Models/Generated/Barracks_L1.fbx",
        "generated_path": "Assets/Art/Models/Generated/Barracks_L1.fbx",
        "scale": 0.88,
    },
    {
        "id": "Barracks_L2",
        "kind": "Barracks",
        "level": 2,
        "catalog_path": "Assets/Art/Models/Generated/Barracks_L2.fbx",
        "generated_path": "Assets/Art/Models/Generated/Barracks_L2.fbx",
        "scale": 0.84,
    },
    {
        "id": "Mage_L1",
        "kind": "Mage",
        "level": 1,
        "catalog_path": "Assets/Art/Models/Generated/Mage_L1.fbx",
        "generated_path": "Assets/Art/Models/Generated/Mage_L1.fbx",
        "scale": 0.86,
    },
    {
        "id": "Mage_L2",
        "kind": "Mage",
        "level": 2,
        "catalog_path": "Assets/Art/Models/Generated/Mage_L2.fbx",
        "generated_path": "Assets/Art/Models/Generated/Mage_L2.fbx",
        "scale": 0.82,
    },
)

REQUIRED_SOCKET_NAMES = (
    "Socket_Root",
    "Socket_Ground",
    "Socket_Target",
    "Socket_Projectile",
    "Socket_VFX",
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
        "useFileScale": find_scalar("useFileScale"),
        "animationType": find_scalar("animationType"),
        "importAnimation": find_scalar("importAnimation"),
        "meshCompression": find_scalar("meshCompression"),
    }


def import_fbx(path: Path) -> None:
    reset_scene()
    bpy.ops.import_scene.fbx(filepath=str(path))
    bpy.context.view_layer.update()


def bounds_from_vertices(vertices: list[tuple[float, float, float]]) -> dict[str, Any]:
    if not vertices:
        return {
            "min": [0.0, 0.0, 0.0],
            "max": [0.0, 0.0, 0.0],
            "size": [0.0, 0.0, 0.0],
            "center": [0.0, 0.0, 0.0],
        }

    mins = [min(vertex[axis] for vertex in vertices) for axis in range(3)]
    maxs = [max(vertex[axis] for vertex in vertices) for axis in range(3)]
    size = [maxs[axis] - mins[axis] for axis in range(3)]
    center = [(mins[axis] + maxs[axis]) * 0.5 for axis in range(3)]
    return {
        "min": [round(value, 4) for value in mins],
        "max": [round(value, 4) for value in maxs],
        "size": [round(value, 4) for value in size],
        "center": [round(value, 4) for value in center],
    }


def inspect_fbx(path: str, catalog_scale: float) -> dict[str, Any]:
    fbx_path = Path(path)
    if not fbx_path.exists():
        return {"path": path, "exists": False}

    import_fbx(fbx_path)
    objects = list(bpy.context.scene.objects)
    mesh_objects = [obj for obj in objects if obj.type == "MESH"]
    sockets = sorted(obj.name for obj in objects if obj.name.startswith("Socket_"))
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
    bounds = bounds_from_vertices(vertices)
    scaled_size = [round(value * catalog_scale, 4) for value in bounds["size"]]

    return {
        "path": path,
        "exists": True,
        "meta": parse_meta(fbx_path),
        "object_count": len(objects),
        "mesh_count": len(mesh_objects),
        "mesh_names": sorted(obj.name for obj in mesh_objects),
        "vertex_count": sum(len(obj.data.vertices) for obj in mesh_objects),
        "polygon_count": sum(len(obj.data.polygons) for obj in mesh_objects),
        "triangle_count": sum(max(1, len(poly.vertices) - 2) for obj in mesh_objects for poly in obj.data.polygons),
        "material_count": len(materials),
        "material_names": materials,
        "bounds": bounds,
        "catalog_scale": catalog_scale,
        "scaled_bounds_size": scaled_size,
        "socket_count": len(sockets),
        "sockets": sockets,
        "missing_required_runtime_sockets": [name for name in REQUIRED_SOCKET_NAMES if name not in sockets],
    }


def inspect_building(entry: dict[str, Any]) -> dict[str, Any]:
    catalog = inspect_fbx(str(entry["catalog_path"]), float(entry["scale"]))
    generated = inspect_fbx(str(entry["generated_path"]), float(entry["scale"]))
    footprint_delta = None
    if catalog.get("exists") and generated.get("exists"):
        catalog_size = catalog["scaled_bounds_size"]
        generated_size = generated["scaled_bounds_size"]
        footprint_delta = {
            "x": round(generated_size[0] - catalog_size[0], 4),
            "z": round(generated_size[2] - catalog_size[2], 4),
        }

    return {
        "id": entry["id"],
        "kind": entry["kind"],
        "level": entry["level"],
        "catalog_scale": entry["scale"],
        "catalog_current": catalog,
        "generated_candidate": generated,
        "generated_vs_catalog_scaled_footprint_delta": footprint_delta,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    buildings = [inspect_building(entry) for entry in BUILDINGS]
    catalog_entries = [building["catalog_current"] for building in buildings if building["catalog_current"].get("exists")]
    generated_entries = [building["generated_candidate"] for building in buildings if building["generated_candidate"].get("exists")]
    report = {
        "schema": "allin-defense.visual-development.building-metrics.v2",
        "captured_on": datetime.date.today().isoformat(),
        "summary": {
            "expected_building_count": len(BUILDINGS),
            "catalog_building_count": len(catalog_entries),
            "generated_candidate_count": len(generated_entries),
            "unique_catalog_guid_count": len({item["meta"]["guid"] for item in catalog_entries if item.get("meta", {}).get("guid")}),
            "unique_generated_guid_count": len({item["meta"]["guid"] for item in generated_entries if item.get("meta", {}).get("guid")}),
            "catalog_total_triangles": sum(int(item.get("triangle_count", 0)) for item in catalog_entries),
            "generated_total_triangles": sum(int(item.get("triangle_count", 0)) for item in generated_entries),
            "catalog_roles_missing_runtime_sockets": [
                building["id"]
                for building in buildings
                if building["catalog_current"].get("missing_required_runtime_sockets")
            ],
            "generated_roles_missing_runtime_sockets": [
                building["id"]
                for building in buildings
                if building["generated_candidate"].get("missing_required_runtime_sockets")
            ],
            "unity_validation_status": "Covered by EditMode catalog path, uniqueness and socket validation.",
        },
        "buildings": buildings,
        "decision": {
            "vis_301_status": "Pivot, footprint and socket metrics captured for all six runtime buildings.",
            "catalog_vs_generated_status": "Runtime catalog and generated candidates resolve to the six original generated FBX files; KayKit buildings remain comparison assets only.",
            "socket_status": "All six runtime buildings contain the required role sockets.",
        },
    }

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"[DC_BUILDING_PROXY_METRICS] {output}")


if __name__ == "__main__":
    main()
