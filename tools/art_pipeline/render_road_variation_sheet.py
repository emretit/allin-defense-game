"""Render VIS-405 Greenwatch road variation contact sheet.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_road_variation_sheet.py -- \
    --output docs/evidence/visual-development/road-variation-sheet-2026-08-16.png
"""

from __future__ import annotations

import argparse
import math
import sys
import warnings
from pathlib import Path

import bpy

warnings.filterwarnings("ignore", category=DeprecationWarning)


ASSETS = (
    "Assets/Art/Models/Generated/GreenwatchGround_West.fbx",
    "Assets/Art/Models/Generated/GreenwatchGround_Mid.fbx",
    "Assets/Art/Models/Generated/GreenwatchGround_Final.fbx",
    "Assets/Art/Models/Generated/GreenwatchRoad_Surface.fbx",
    "Assets/Art/Models/Generated/GreenwatchRoad_Edges.fbx",
    "Assets/Art/Models/Generated/GreenwatchRoad_Wear.fbx",
)

SLOTS = [
    ("S1", 2.5, 2.1, "front"),
    ("S2", 5.5, 2.8, "range"),
    ("S3", 6.6, 5.9, "control"),
    ("S4", 10.0, 3.3, "front"),
    ("S5", 13.8, 4.4, "range"),
    ("S6", 15.6, 1.0, "control"),
    ("S7", 18.2, 4.6, "range"),
    ("S8", 19.2, 7.9, "front"),
]

PALETTE = {
    "DC_Navy": (0.045, 0.102, 0.246, 1.0),
    "DC_Forest": (0.098, 0.239, 0.173, 1.0),
    "DC_Grass": (0.169, 0.357, 0.263, 1.0),
    "DC_Earth": (0.50, 0.34, 0.20, 1.0),
    "DC_Stone": (0.46, 0.50, 0.54, 1.0),
    "DC_Metal": (0.26, 0.31, 0.36, 1.0),
    "DC_Dark": (0.12, 0.10, 0.09, 1.0),
    "SlotFront": (1.0, 0.68, 0.25, 1.0),
    "SlotRange": (0.33, 0.68, 1.0, 1.0),
    "SlotControl": (0.35, 0.9, 0.72, 1.0),
    "Label": (0.88, 0.95, 1.0, 1.0),
    "Ground": (0.055, 0.19, 0.145, 1.0),
}


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1920
    bpy.context.scene.render.resolution_y = 1080
    bpy.context.scene.render.film_transparent = False
    bpy.context.scene.world = bpy.data.worlds.new("World") if bpy.context.scene.world is None else bpy.context.scene.world
    bpy.context.scene.world.color = (0.01, 0.016, 0.026)
    if hasattr(bpy.context.scene, "eevee") and hasattr(bpy.context.scene.eevee, "use_gtao"):
        bpy.context.scene.eevee.use_gtao = True
        bpy.context.scene.eevee.gtao_distance = 2
        bpy.context.scene.eevee.gtao_factor = 0.9


def material(name: str, color: tuple[float, float, float, float]) -> bpy.types.Material:
    existing = bpy.data.materials.get(name)
    if existing is not None:
        existing.diffuse_color = color
        existing.use_nodes = True
        principled = existing.node_tree.nodes.get("Principled BSDF")
        if principled is not None:
            principled.inputs["Base Color"].default_value = color
            principled.inputs["Roughness"].default_value = 0.9
        return existing
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = color
        principled.inputs["Roughness"].default_value = 0.9
    return mat


def remap_import_materials() -> None:
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        for slot_index, mat in enumerate(obj.data.materials):
            if mat is None:
                continue
            key = mat.name.split(".")[0]
            if key in PALETTE:
                obj.data.materials[slot_index] = material(key, PALETTE[key])


def add_base() -> None:
    bpy.ops.mesh.primitive_cube_add(size=1, location=(11, 5.5, -0.045))
    obj = bpy.context.object
    obj.name = "GreenwatchRoadSheetBase"
    obj.dimensions = (29.6, 15.8, 0.02)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material("Ground", PALETTE["Ground"]))


def add_slot_ring(code: str, x: float, y: float, role: str) -> None:
    mat_name = {"front": "SlotFront", "range": "SlotRange", "control": "SlotControl"}[role]
    bpy.ops.mesh.primitive_torus_add(major_radius=0.46, minor_radius=0.032, major_segments=28,
                                    minor_segments=6, location=(x, y, 0.21))
    ring = bpy.context.object
    ring.name = f"{code}_{role}_SlotReadRing"
    ring.data.materials.append(material(mat_name, PALETTE[mat_name]))
    add_label(code, (x, y, 0.265), 0.24)


def add_label(text: str, location: tuple[float, float, float], size: float = 0.18) -> None:
    bpy.ops.object.text_add(location=location, rotation=(0, 0, 0))
    label = bpy.context.object
    label.name = f"Label_{text}"
    label.data.body = text
    label.data.align_x = "CENTER"
    label.data.align_y = "CENTER"
    label.data.size = size
    label.data.materials.append(material("Label", PALETTE["Label"]))


def add_labels() -> None:
    add_label("VIS-405 Greenwatch road surface / edge / wear candidates", (11, 13.7, 0.22), 0.42)
    add_label("Road remains continuous; edge stones add rhythm; wear patches stay below slot/route readability.", (11, 13.05, 0.22), 0.24)
    add_label("Enemy Gate", (-2.0, 0.72, 0.22), 0.24)
    add_label("Defender Keep", (23.3, 10.35, 0.22), 0.24)


def add_lighting_and_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(11, 5.5, 15))
    light = bpy.context.object
    light.name = "RoadVariationSoftbox"
    light.data.energy = 520
    light.data.size = 18
    bpy.ops.object.camera_add(location=(11, 5.6, 24.5), rotation=(0, 0, 0))
    camera = bpy.context.object
    camera.name = "RoadVariationTopCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 20.5
    bpy.context.scene.camera = camera


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    reset_scene()
    add_base()
    for path in ASSETS:
        bpy.ops.import_scene.fbx(filepath=str(Path(path)))
    remap_import_materials()
    for code, x, y, role in SLOTS:
        add_slot_ring(code, x, y, role)
    add_labels()
    add_lighting_and_camera()

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_ROAD_VARIATION_SHEET] {output}")


if __name__ == "__main__":
    main()
