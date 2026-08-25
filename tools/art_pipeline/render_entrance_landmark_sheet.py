#!/usr/bin/env python3
"""Render VIS-406 Greenwatch entrance landmark contact sheet.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_entrance_landmark_sheet.py -- \
    --output docs/evidence/visual-development/entrance-landmark-sheet-2026-08-16.png
"""

from __future__ import annotations

import argparse
import sys
import warnings
from pathlib import Path

import bpy

warnings.filterwarnings("ignore", category=DeprecationWarning)


ASSETS = (
    "Assets/Art/Models/Generated/GreenwatchGround_West.fbx",
    "Assets/Art/Models/Generated/GreenwatchRoad_Surface.fbx",
    "Assets/Art/Models/Generated/GreenwatchRoad_Edges.fbx",
    "Assets/Art/Models/Generated/GreenwatchRoad_Wear.fbx",
    "Assets/Art/Models/Generated/GreenwatchEntrance_Dressing.fbx",
    "Assets/Art/Models/Generated/GreenwatchEntrance_SpawnLight.fbx",
    "Assets/Art/Models/Generated/GreenwatchEntrance_Gate.fbx",
)

SLOTS = [
    ("S1", 2.5, 2.1, "front"),
    ("S2", 5.5, 2.8, "range"),
    ("S3", 6.6, 5.9, "control"),
]

PATH_POINTS = [(-2, 2), (4, 2), (4, 8), (10, 8)]

PALETTE = {
    "DC_Navy": (0.045, 0.102, 0.246, 1.0),
    "DC_Forest": (0.098, 0.239, 0.173, 1.0),
    "DC_Grass": (0.169, 0.357, 0.263, 1.0),
    "DC_Earth": (0.50, 0.34, 0.20, 1.0),
    "DC_Stone": (0.46, 0.50, 0.54, 1.0),
    "DC_Metal": (0.26, 0.31, 0.36, 1.0),
    "DC_Dark": (0.12, 0.10, 0.09, 1.0),
    "DC_Enemy": (0.74, 0.16, 0.18, 1.0),
    "DC_Fire": (1.0, 0.55, 0.14, 1.0),
    "SlotFront": (1.0, 0.68, 0.25, 1.0),
    "SlotRange": (0.33, 0.68, 1.0, 1.0),
    "SlotControl": (0.35, 0.9, 0.72, 1.0),
    "Label": (0.88, 0.95, 1.0, 1.0),
    "Ground": (0.055, 0.19, 0.145, 1.0),
    "NoClutter": (0.12, 0.92, 0.84, 0.26),
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
            principled.inputs["Alpha"].default_value = color[3]
            principled.inputs["Roughness"].default_value = 0.9
        if color[3] < 1.0:
            existing.blend_method = "BLEND"
            existing.use_screen_refraction = False
        return existing
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = color
        principled.inputs["Alpha"].default_value = color[3]
        principled.inputs["Roughness"].default_value = 0.9
    if color[3] < 1.0:
        mat.blend_method = "BLEND"
        mat.use_screen_refraction = False
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
    bpy.ops.mesh.primitive_cube_add(size=1, location=(2.8, 3.7, -0.055))
    obj = bpy.context.object
    obj.name = "GreenwatchEntranceSheetBase"
    obj.dimensions = (12.4, 9.2, 0.02)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material("Ground", PALETTE["Ground"]))


def add_no_clutter_band() -> None:
    bpy.ops.mesh.primitive_cube_add(size=1, location=(2.2, 2.0, 0.115))
    obj = bpy.context.object
    obj.name = "EntranceNoClutterPathBand"
    obj.dimensions = (7.0, 1.65, 0.012)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material("NoClutter", PALETTE["NoClutter"]))


def add_slot_ring(code: str, x: float, y: float, role: str) -> None:
    mat_name = {"front": "SlotFront", "range": "SlotRange", "control": "SlotControl"}[role]
    bpy.ops.mesh.primitive_torus_add(major_radius=0.46, minor_radius=0.032, major_segments=28,
                                    minor_segments=6, location=(x, y, 0.24))
    ring = bpy.context.object
    ring.name = f"{code}_{role}_SlotReadRing"
    ring.data.materials.append(material(mat_name, PALETTE[mat_name]))
    add_label(code, (x, y, 0.31), 0.22)


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
    add_label("VIS-406 Enemy entrance / spawn landmark candidates", (2.8, 7.85, 0.28), 0.34)
    add_label("Warm threat gate is visible at first glance; cyan band marks protected route readability.", (2.8, 7.35, 0.28), 0.20)
    add_label("Gate", (-1.42, 3.78, 0.30), 0.22)
    add_label("S1 must stay clear", (2.55, 1.18, 0.30), 0.20)


def add_lighting_and_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(1.8, 2.5, 7.0))
    key = bpy.context.object
    key.name = "EntranceWarmKey"
    key.data.energy = 620
    key.data.size = 5.5
    key.data.color = (1.0, 0.55, 0.28)
    bpy.ops.object.light_add(type="AREA", location=(5.8, 5.5, 6.5))
    fill = bpy.context.object
    fill.name = "EntranceCoolFill"
    fill.data.energy = 220
    fill.data.size = 7.5
    fill.data.color = (0.35, 0.65, 1.0)
    bpy.ops.object.camera_add(location=(1.75, 3.75, 10.5), rotation=(0, 0, 0))
    camera = bpy.context.object
    camera.name = "EntranceLandmarkTopCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 9.6
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
    add_no_clutter_band()
    for code, x, y, role in SLOTS:
        add_slot_ring(code, x, y, role)
    add_labels()
    add_lighting_and_camera()

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_ENTRANCE_LANDMARK_SHEET] {output}")


if __name__ == "__main__":
    main()
