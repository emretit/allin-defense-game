"""Render VIS-404 generated Greenwatch ground variation contact sheet.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_ground_variation_sheet.py -- \
    --output docs/evidence/visual-development/ground-variation-sheet-2026-08-16.png
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
    ("West Entry", "Assets/Art/Models/Generated/GreenwatchGround_West.fbx"),
    ("Mid Control", "Assets/Art/Models/Generated/GreenwatchGround_Mid.fbx"),
    ("Final Stand", "Assets/Art/Models/Generated/GreenwatchGround_Final.fbx"),
)

PATH = [(-2, 2), (4, 2), (4, 8), (10, 8), (10, 3.5), (16, 3.5), (16, 9), (24, 9)]
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
    "DC_Earth": (0.439, 0.329, 0.239, 1.0),
    "Road": (0.54, 0.36, 0.22, 1.0),
    "RoadEdge": (0.31, 0.34, 0.37, 1.0),
    "SlotFront": (1.0, 0.68, 0.25, 1.0),
    "SlotRange": (0.33, 0.68, 1.0, 1.0),
    "SlotControl": (0.35, 0.9, 0.72, 1.0),
    "Label": (0.88, 0.95, 1.0, 1.0),
    "Ground": (0.065, 0.21, 0.16, 1.0),
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
        bpy.context.scene.eevee.gtao_factor = 0.8


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
    bpy.ops.mesh.primitive_cube_add(size=1, location=(11, 5.5, -0.025))
    obj = bpy.context.object
    obj.name = "GreenwatchBaseGround"
    obj.dimensions = (28.6, 15.3, 0.02)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material("Ground", PALETTE["Ground"]))


def add_road_segment(name: str, a: tuple[float, float], b: tuple[float, float], width: float,
                     z: float, mat_name: str) -> None:
    ax, ay = a
    bx, by = b
    dx = bx - ax
    dy = by - ay
    length = math.hypot(dx, dy)
    angle = math.atan2(dy, dx)
    bpy.ops.mesh.primitive_cube_add(size=1, location=((ax + bx) * 0.5, (ay + by) * 0.5, z))
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = (length + 0.55, width, 0.028)
    obj.rotation_euler[2] = angle
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material(mat_name, PALETTE[mat_name]))


def add_path() -> None:
    for i, (a, b) in enumerate(zip(PATH, PATH[1:])):
        add_road_segment(f"RoadEdge_{i}", a, b, 1.95, 0.014, "RoadEdge")
        add_road_segment(f"Road_{i}", a, b, 1.42, 0.045, "Road")


def add_slot_ring(code: str, x: float, y: float, role: str) -> None:
    mat_name = {"front": "SlotFront", "range": "SlotRange", "control": "SlotControl"}[role]
    bpy.ops.mesh.primitive_torus_add(major_radius=0.46, minor_radius=0.035, major_segments=28,
                                    minor_segments=6, location=(x, y, 0.09))
    ring = bpy.context.object
    ring.name = f"{code}_{role}_SlotNoClutterRing"
    ring.data.materials.append(material(mat_name, PALETTE[mat_name]))
    add_label(code, (x, y, 0.14), 0.23)


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
    add_label("VIS-404 Greenwatch ground variation candidates", (11, 13.65, 0.08), 0.42)
    add_label("Road + 8 slot no-clutter rings remain dominant over broad ground tint/relief patches", (11, 13.05, 0.08), 0.24)
    add_label("West Entry", (1.0, -1.15, 0.08), 0.28)
    add_label("Mid Control Bend", (8.3, -1.15, 0.08), 0.28)
    add_label("Final Stand", (18.6, -1.15, 0.08), 0.28)


def add_lighting_and_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(11, 5.5, 14))
    light = bpy.context.object
    light.name = "GroundVariationSoftbox"
    light.data.energy = 420
    light.data.size = 18
    bpy.ops.object.camera_add(location=(11, 5.6, 24.5), rotation=(0, 0, 0))
    camera = bpy.context.object
    camera.name = "GroundVariationTopCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 20.5
    bpy.context.scene.camera = camera


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    reset_scene()
    add_base()
    for _label, path in ASSETS:
        bpy.ops.import_scene.fbx(filepath=str(Path(path)))
    remap_import_materials()
    add_path()
    for code, x, y, role in SLOTS:
        add_slot_ring(code, x, y, role)
    add_labels()
    add_lighting_and_camera()

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_GROUND_VARIATION_SHEET] {output}")


if __name__ == "__main__":
    main()
