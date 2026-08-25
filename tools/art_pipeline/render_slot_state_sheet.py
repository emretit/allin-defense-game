"""Render generated world-space slot state readability sheet.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_slot_state_sheet.py -- \
    --output docs/evidence/visual-development/slot-state-sheet-2026-08-16.png
"""

from __future__ import annotations

import argparse
import math
import sys
import warnings
from pathlib import Path

import bpy
from mathutils import Vector

warnings.filterwarnings("ignore", category=DeprecationWarning)


SLOT_STATES = (
    ("Empty", "Assets/Art/Models/Generated/OutpostSlot_Empty.fbx"),
    ("Selected", "Assets/Art/Models/Generated/OutpostSlot_Selected.fbx"),
    ("Available", "Assets/Art/Models/Generated/OutpostSlot_Available.fbx"),
    ("Insufficient", "Assets/Art/Models/Generated/OutpostSlot_Insufficient.fbx"),
    ("Locked", "Assets/Art/Models/Generated/OutpostSlot_Locked.fbx"),
)

PALETTE = {
    "DC_Navy": (0.027, 0.094, 0.165, 1.0),
    "DC_Defender": (0.114, 0.435, 0.820, 1.0),
    "DC_Stone": (0.541, 0.588, 0.639, 1.0),
    "DC_Forest": (0.098, 0.239, 0.173, 1.0),
    "DC_Enemy": (0.647, 0.169, 0.196, 1.0),
    "DC_Magic": (0.235, 0.749, 0.937, 1.0),
    "DC_Fire": (1.0, 0.541, 0.141, 1.0),
    "DC_Dark": (0.055, 0.066, 0.078, 1.0),
    "DC_Earth": (0.439, 0.329, 0.239, 1.0),
    "DC_Grass": (0.169, 0.357, 0.263, 1.0),
    "DC_Metal": (0.349, 0.412, 0.475, 1.0),
}


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1920
    bpy.context.scene.render.resolution_y = 1080
    bpy.context.scene.render.film_transparent = False
    bpy.context.scene.world = bpy.data.worlds.new("World") if bpy.context.scene.world is None else bpy.context.scene.world
    bpy.context.scene.world.color = (0.012, 0.019, 0.034)


def make_material(name: str, color: tuple[float, float, float, float]) -> bpy.types.Material:
    existing = bpy.data.materials.get(name)
    if existing is not None:
        existing.diffuse_color = color
        existing.use_nodes = True
        principled = existing.node_tree.nodes.get("Principled BSDF")
        if principled is not None:
            principled.inputs["Base Color"].default_value = color
            principled.inputs["Roughness"].default_value = 0.82
        return existing
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = color
        principled.inputs["Roughness"].default_value = 0.82
    return material


def import_slot_state(
    path: Path,
    label: str,
    location: tuple[float, float, float],
    scale: float,
    override_material: bpy.types.Material | None,
) -> None:
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported = [obj for obj in bpy.context.scene.objects if obj not in before]

    mesh_objects = [obj for obj in imported if obj.type == "MESH"]
    for obj in mesh_objects:
        obj.name = f"{label}_{obj.name}"
        world = obj.matrix_world.copy()
        for vertex in obj.data.vertices:
            vertex.co = world @ vertex.co
        obj.matrix_world.identity()
        if override_material is not None:
            obj.data.materials.clear()
            obj.data.materials.append(override_material)
        else:
            for slot_index, material_slot in enumerate(obj.data.materials):
                if material_slot is None:
                    continue
                palette_key = material_slot.name.split(".")[0]
                if palette_key in PALETTE:
                    obj.data.materials[slot_index] = make_material(palette_key, PALETTE[palette_key])

    for obj in imported:
        if obj.type != "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.context.view_layer.update()
    vertices = [obj.matrix_world @ vertex.co for obj in mesh_objects for vertex in obj.data.vertices]
    if not vertices:
        return

    min_x = min(vertex.x for vertex in vertices)
    max_x = max(vertex.x for vertex in vertices)
    min_y = min(vertex.y for vertex in vertices)
    max_y = max(vertex.y for vertex in vertices)
    min_z = min(vertex.z for vertex in vertices)
    center = Vector(((min_x + max_x) * 0.5, (min_y + max_y) * 0.5, min_z))
    target = Vector(location)

    for obj in mesh_objects:
        for vertex in obj.data.vertices:
            vertex.co = (vertex.co - center) * scale + target


def add_label(text: str, location: tuple[float, float, float], size: float = 0.13) -> None:
    bpy.ops.object.text_add(location=location, rotation=(math.radians(68), 0, 0))
    label = bpy.context.object
    label.name = f"Label_{text}"
    label.data.body = text
    label.data.align_x = "CENTER"
    label.data.align_y = "CENTER"
    label.data.size = size
    label.data.materials.append(make_material("LabelWhite", (0.9, 0.95, 1.0, 1.0)))


def add_lighting_and_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -4.5, 6.6))
    key = bpy.context.object
    key.name = "SlotStateKeyLight"
    key.data.energy = 720
    key.data.size = 5.8

    bpy.ops.object.light_add(type="POINT", location=(-3.8, -2.2, 2.0))
    rim = bpy.context.object
    rim.name = "SlotStateRimLight"
    rim.data.energy = 45
    rim.data.color = (0.35, 0.63, 1.0)

    bpy.ops.object.camera_add(location=(0, -7.0, 5.0), rotation=(math.radians(60), 0, 0))
    camera = bpy.context.object
    camera.name = "SlotStateSheetCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 8.8
    bpy.context.scene.camera = camera


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    reset_scene()
    grayscale_material = make_material("SlotNeutralGray", (0.58, 0.6, 0.62, 1.0))
    thumbnail_material = make_material("SlotThumbnailDark", (0.15, 0.19, 0.24, 1.0))

    start_x = -2.9
    spacing_x = 1.45
    rows = (
        ("Color / material language", 1.6, 1.25, None),
        ("Grayscale / shape language", 0.02, 1.25, grayscale_material),
        ("Tiny / tap target read", -1.62, 0.56, thumbnail_material),
    )

    for index, (label, path) in enumerate(SLOT_STATES):
        x = start_x + index * spacing_x
        add_label(label, (x, -0.1, 2.75), 0.12)
        for row_label, z, scale, override in rows:
            import_slot_state(Path(path), f"{row_label}_{label}", (x, 0, z), scale, override)

    add_label("Color / material language", (-3.28, -0.15, 1.74), 0.1)
    add_label("Grayscale / shape language", (-3.28, -0.15, 0.16), 0.1)
    add_label("Tiny / tap target read", (-3.28, -0.15, -1.45), 0.1)

    add_lighting_and_camera()
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_SLOT_STATE_SHEET] {output}")


if __name__ == "__main__":
    main()
