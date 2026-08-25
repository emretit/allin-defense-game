"""Render a three-view style proof sheet for one generated actor FBX.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_actor_turnaround_sheet.py -- \
    --actor Knight --output docs/evidence/visual-development/knight-turnaround-2026-08-17.png
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


REVIEW_COLORS = {
    "DC_Defender": (0.12, 0.31, 0.68, 1.0),
    "DC_Enemy": (0.58, 0.12, 0.09, 1.0),
    "DC_Fire": (0.95, 0.55, 0.15, 1.0),
    "DC_Metal": (0.58, 0.64, 0.68, 1.0),
    "DC_Stone": (0.38, 0.34, 0.29, 1.0),
    "DC_Dark": (0.09, 0.08, 0.08, 1.0),
    "DC_RegenCore": (0.20, 0.78, 0.28, 1.0),
    "DC_SupportCloth": (0.34, 0.44, 0.48, 1.0),
}

VIEWS = (
    ("Front", 0.0),
    ("Side", 90.0),
    ("Iso", -35.0),
)


def make_material(name: str, color: tuple[float, float, float, float]) -> bpy.types.Material:
    existing = bpy.data.materials.get(name)
    if existing is not None:
        return existing

    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    return material


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 2400
    bpy.context.scene.render.resolution_y = 1350
    bpy.context.scene.world = bpy.data.worlds.new("World") if bpy.context.scene.world is None else bpy.context.scene.world
    bpy.context.scene.world.color = (0.018, 0.024, 0.035)


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


def object_bounds(mesh_objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    vertices = [obj.matrix_world @ vertex.co for obj in mesh_objects for vertex in obj.data.vertices]
    if not vertices:
        return Vector((0, 0, 0)), Vector((0, 0, 0))

    minimum = Vector((min(vertex.x for vertex in vertices), min(vertex.y for vertex in vertices), min(vertex.z for vertex in vertices)))
    maximum = Vector((max(vertex.x for vertex in vertices), max(vertex.y for vertex in vertices), max(vertex.z for vertex in vertices)))
    return minimum, maximum


def imported_mesh_objects(before: set[bpy.types.Object]) -> list[bpy.types.Object]:
    imported = [obj for obj in bpy.context.scene.objects if obj not in before]
    mesh_objects = [obj for obj in imported if obj.type == "MESH"]

    for obj in mesh_objects:
        world = obj.matrix_world.copy()
        for vertex in obj.data.vertices:
            vertex.co = world @ vertex.co
        obj.matrix_world.identity()

    for obj in imported:
        if obj.type != "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)

    return mesh_objects


def add_panel(x: float) -> None:
    mesh = bpy.data.meshes.new("TurnaroundPanelMesh")
    width = 2.0
    height = 2.65
    y = 0.42
    z = -0.16
    vertices = [
        (x - width * 0.5, y, z),
        (x + width * 0.5, y, z),
        (x + width * 0.5, y, z + height),
        (x - width * 0.5, y, z + height),
    ]
    mesh.from_pydata(vertices, [], [(0, 1, 2, 3)])
    mesh.update()
    panel = bpy.data.objects.new("TurnaroundPanel", mesh)
    panel.data.materials.append(make_material("PanelBlueGrey", (0.055, 0.074, 0.095, 1.0)))
    bpy.context.collection.objects.link(panel)


def import_view(path: Path, label: str, rotation_degrees: float, x: float) -> None:
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    mesh_objects = imported_mesh_objects(before)
    bpy.context.view_layer.update()
    apply_review_material_colors(mesh_objects)

    minimum, maximum = object_bounds(mesh_objects)
    size = maximum - minimum
    scale = 1.72 / size.z if size.z > 0 else 1.0
    center = Vector(((minimum.x + maximum.x) * 0.5, (minimum.y + maximum.y) * 0.5, minimum.z))
    rotation = Matrix.Rotation(math.radians(rotation_degrees), 4, "Z")
    target = Vector((x, 0.0, 0.0))

    for obj in mesh_objects:
        obj.name = f"{label}_{obj.name}"
        for vertex in obj.data.vertices:
            vertex.co = rotation @ ((vertex.co - center) * scale) + target


def add_label(text: str, location: tuple[float, float, float], size: float = 0.16) -> None:
    bpy.ops.object.text_add(location=location, rotation=(math.radians(68), 0, 0))
    label = bpy.context.object
    label.data.body = text
    label.data.align_x = "CENTER"
    label.data.align_y = "CENTER"
    label.data.size = size
    label.data.materials.append(make_material("LabelWhite", (0.92, 0.96, 1.0, 1.0)))


def add_camera_and_light() -> None:
    bpy.ops.object.light_add(type="AREA", location=(-3.2, -4.8, 6.0))
    key = bpy.context.object
    key.data.energy = 720
    key.data.size = 5.2

    bpy.ops.object.camera_add(location=(0, -7.4, 3.45), rotation=(math.radians(64), 0, 0))
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 6.5
    bpy.context.scene.camera = camera


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--actor", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--model-root", default="Assets/Art/Models/Generated")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    reset_scene()
    path = Path(args.model_root) / f"{args.actor}.fbx"
    x_positions = (-2.05, 0.0, 2.05)

    add_label(f"{args.actor} Turnaround", (0.0, -0.42, 2.5), size=0.18)
    for (view_label, rotation), x in zip(VIEWS, x_positions, strict=True):
        add_panel(x)
        import_view(path, view_label, rotation, x)
        add_label(view_label, (x, -0.34, -0.4), size=0.2)

    add_camera_and_light()
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_ACTOR_TURNAROUND_SHEET] {output}")


if __name__ == "__main__":
    main()
