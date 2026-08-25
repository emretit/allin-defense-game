"""Render transparent actor proxy icon PNGs with a shared camera/light preset.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_actor_icon_set.py -- \
    --output-dir docs/evidence/visual-development/actor-icons-2026-08-16
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


ACTOR_SCALES = {
    "Knight": 0.74,
    "Guard": 0.66,
    "Raider": 0.64,
    "Vanguard": 0.68,
    "Regenerator": 0.60,
    "Support": 0.62,
    "Siege": 0.92,
    "Boss": 1.02,
}

REVIEW_COLORS = {
    "DC_Defender": (0.12, 0.31, 0.68, 1.0),
    "DC_Enemy": (0.58, 0.12, 0.09, 1.0),
    "DC_Fire": (0.95, 0.55, 0.15, 1.0),
    "DC_Metal": (0.58, 0.64, 0.68, 1.0),
    "DC_Stone": (0.38, 0.34, 0.29, 1.0),
    "DC_Dark": (0.09, 0.08, 0.08, 1.0),
    "DC_Forest": (0.15, 0.52, 0.25, 1.0),
    "DC_Magic": (0.24, 0.78, 0.86, 1.0),
    "DC_RegenCore": (0.20, 0.78, 0.28, 1.0),
    "DC_SupportCloth": (0.34, 0.44, 0.48, 1.0),
}


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 512
    bpy.context.scene.render.resolution_y = 512
    bpy.context.scene.render.film_transparent = True
    bpy.context.scene.view_settings.view_transform = "Standard"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.world = bpy.data.worlds.new("World") if bpy.context.scene.world is None else bpy.context.scene.world
    bpy.context.scene.world.color = (0.015, 0.025, 0.045)


def make_material(name: str, color: tuple[float, float, float, float]) -> bpy.types.Material:
    existing = bpy.data.materials.get(name)
    if existing is not None:
        return existing
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = color
        principled.inputs["Roughness"].default_value = 0.72
    return material


def apply_review_material_colors(mesh_objects: list[bpy.types.Object]) -> None:
    for obj in mesh_objects:
        for slot in obj.material_slots:
            material = slot.material
            if material is None:
                continue

            for prefix, color in REVIEW_COLORS.items():
                if material.name.startswith(prefix):
                    material.diffuse_color = color
                    material.use_nodes = True
                    principled = material.node_tree.nodes.get("Principled BSDF")
                    if principled is not None:
                        principled.inputs["Base Color"].default_value = color
                        principled.inputs["Roughness"].default_value = 0.72
                    break


def import_actor(path: Path, role: str) -> list[bpy.types.Object]:
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported = [obj for obj in bpy.context.scene.objects if obj not in before]
    mesh_objects = [obj for obj in imported if obj.type == "MESH"]

    for obj in mesh_objects:
        obj.name = f"{role}_{obj.name}"
        world = obj.matrix_world.copy()
        for vertex in obj.data.vertices:
            vertex.co = world @ vertex.co
        obj.matrix_world.identity()

    for obj in imported:
        if obj.type != "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.context.view_layer.update()
    vertices = [obj.matrix_world @ vertex.co for obj in mesh_objects for vertex in obj.data.vertices]
    if not vertices:
        return mesh_objects

    min_x = min(vertex.x for vertex in vertices)
    max_x = max(vertex.x for vertex in vertices)
    min_y = min(vertex.y for vertex in vertices)
    max_y = max(vertex.y for vertex in vertices)
    min_z = min(vertex.z for vertex in vertices)
    max_z = max(vertex.z for vertex in vertices)
    center = Vector(((min_x + max_x) * 0.5, (min_y + max_y) * 0.5, min_z))
    scale = ACTOR_SCALES[role]
    source_height = max(max_z - min_z, 0.001)
    icon_scale = min(2.4, 2.65 / (source_height * scale))

    for obj in mesh_objects:
        for vertex in obj.data.vertices:
            vertex.co = (vertex.co - center) * scale * icon_scale + Vector((0, 0, -0.58))

    apply_review_material_colors(mesh_objects)

    return mesh_objects


def add_camera_and_light() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -3.8, 4.8))
    light = bpy.context.object
    light.name = "IconKeyLight"
    light.data.energy = 450
    light.data.size = 4.2

    bpy.ops.object.light_add(type="POINT", location=(-1.7, 2.3, 2.0))
    rim = bpy.context.object
    rim.name = "IconCoolRim"
    rim.data.color = (0.45, 0.72, 1.0)
    rim.data.energy = 55

    bpy.ops.object.camera_add(location=(0, -4.8, 2.6), rotation=(math.radians(62), 0, 0))
    camera = bpy.context.object
    camera.name = "IconCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 4.35
    bpy.context.scene.camera = camera


def render_role(role: str, model_root: Path, output_dir: Path) -> None:
    reset_scene()
    import_actor(model_root / f"{role}.fbx", role)
    add_camera_and_light()
    output_path = output_dir / f"{role}.png"
    bpy.context.scene.render.filepath = str(output_path)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_ACTOR_ICON] {output_path}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--model-root", default="Assets/Art/Models/Generated")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    model_root = Path(args.model_root)

    for role in ACTOR_SCALES:
        render_role(role, model_root, output_dir)


if __name__ == "__main__":
    main()
