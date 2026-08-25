"""Render a visual contact sheet for Defender Chronicles actor proxy FBX files.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_actor_proxy_contact_sheet.py -- \
    --output docs/evidence/visual-development/actor-proxy-contact-sheet-2026-08-16.png
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


ACTORS = (
    "Knight",
    "Guard",
    "Raider",
    "Vanguard",
    "Regenerator",
    "Support",
    "Siege",
    "Boss",
)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1920
    bpy.context.scene.render.resolution_y = 1080
    bpy.context.scene.render.film_transparent = False
    bpy.context.scene.world = bpy.data.worlds.new("World") if bpy.context.scene.world is None else bpy.context.scene.world
    bpy.context.scene.world.color = (0.015, 0.025, 0.045)


def import_actor(path: Path, name: str, location: tuple[float, float, float]) -> None:
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported = [obj for obj in bpy.context.scene.objects if obj not in before]

    mesh_objects = [obj for obj in imported if obj.type == "MESH"]
    for obj in mesh_objects:
        obj.name = f"{name}_{obj.name}"
        world = obj.matrix_world.copy()
        for vertex in obj.data.vertices:
            vertex.co = world @ vertex.co
        obj.parent = None
        obj.matrix_world.identity()

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
    scale = 0.70

    for obj in mesh_objects:
        for vertex in obj.data.vertices:
            vertex.co = (vertex.co - center) * scale + target


def add_label(text: str, location: tuple[float, float, float]) -> None:
    bpy.ops.object.text_add(location=location, rotation=(math.radians(70), 0, 0))
    label = bpy.context.object
    label.name = f"Label_{text}"
    label.data.body = text
    label.data.align_x = "CENTER"
    label.data.align_y = "CENTER"
    label.data.size = 0.16
    label.data.materials.append(make_material("LabelWhite", (0.92, 0.96, 1.0, 1.0)))


def make_material(name: str, color: tuple[float, float, float, float]) -> bpy.types.Material:
    existing = bpy.data.materials.get(name)
    if existing is not None:
        return existing
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    return material


def add_camera_and_light() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -4.5, 7.5))
    light = bpy.context.object
    light.name = "ContactSheetKeyLight"
    light.data.energy = 600
    light.data.size = 6

    bpy.ops.object.camera_add(location=(0, -8.5, 5.2), rotation=(math.radians(60), 0, 0))
    camera = bpy.context.object
    camera.name = "ContactSheetCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 10.0
    bpy.context.scene.camera = camera


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--model-root", default="Assets/Art/Models/Generated")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    reset_scene()
    model_root = Path(args.model_root)
    spacing_x = 1.55
    spacing_z = 2.05
    start_x = -2.325
    start_z = 1.0

    for index, actor in enumerate(ACTORS):
        col = index % 4
        row = index // 4
        x = start_x + col * spacing_x
        z = start_z - row * spacing_z
        import_actor(model_root / f"{actor}.fbx", actor, (x, 0, z))
        label_z = z + 1.35 if row == 0 else z - 0.62
        add_label(actor, (x, -0.22, label_z))

    add_camera_and_light()
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_ACTOR_PROXY_CONTACT_SHEET] {output}")


if __name__ == "__main__":
    main()
