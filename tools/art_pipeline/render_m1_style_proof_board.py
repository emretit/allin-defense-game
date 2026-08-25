"""Render the M1 style proof board for Knight, Raider and Archer L1.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_m1_style_proof_board.py -- \
    --output docs/evidence/visual-development/m1-style-proof-board-2026-08-17.png
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


SUBJECTS = (
    {
        "label": "Knight",
        "path": "Knight.fbx",
        "role": "Defender hero proxy",
        "target_height": 1.72,
    },
    {
        "label": "Raider",
        "path": "Raider.fbx",
        "role": "Fast enemy proxy",
        "target_height": 1.72,
    },
    {
        "label": "Archer L1",
        "path": "Archer_L1.fbx",
        "role": "Tower level 1 candidate",
        "target_height": 1.72,
    },
)

REVIEW_COLORS = {
    "DC_Defender": (0.12, 0.31, 0.68, 1.0),
    "DC_Enemy": (0.58, 0.12, 0.09, 1.0),
    "DC_Fire": (0.95, 0.55, 0.15, 1.0),
    "DC_Metal": (0.58, 0.64, 0.68, 1.0),
    "DC_Stone": (0.38, 0.34, 0.29, 1.0),
    "DC_Dark": (0.09, 0.08, 0.08, 1.0),
}


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
    bpy.context.scene.render.film_transparent = False
    bpy.context.scene.world = bpy.data.worlds.new("World") if bpy.context.scene.world is None else bpy.context.scene.world
    bpy.context.scene.world.color = (0.018, 0.024, 0.035)


def add_panel(location: tuple[float, float, float], width: float = 2.0, height: float = 2.62) -> None:
    x, y, z = location
    mesh = bpy.data.meshes.new("StyleProofPanelMesh")
    vertices = [
        (x - width * 0.5, y, z - 0.15),
        (x + width * 0.5, y, z - 0.15),
        (x + width * 0.5, y, z + height),
        (x - width * 0.5, y, z + height),
    ]
    mesh.from_pydata(vertices, [], [(0, 1, 2, 3)])
    mesh.update()

    panel = bpy.data.objects.new("StyleProofPanel", mesh)
    panel.data.materials.append(make_material("PanelBlueGrey", (0.055, 0.074, 0.095, 1.0)))
    bpy.context.collection.objects.link(panel)


def add_floor(location: tuple[float, float, float], width: float = 2.05, depth: float = 0.64) -> None:
    x, y, z = location
    mesh = bpy.data.meshes.new("StyleProofFloorMesh")
    vertices = [
        (x - width * 0.5, y - depth * 0.5, z),
        (x + width * 0.5, y - depth * 0.5, z),
        (x + width * 0.5, y + depth * 0.5, z),
        (x - width * 0.5, y + depth * 0.5, z),
    ]
    mesh.from_pydata(vertices, [], [(0, 1, 2, 3)])
    mesh.update()

    floor = bpy.data.objects.new("StyleProofFootprint", mesh)
    floor.data.materials.append(make_material("FootprintSlate", (0.16, 0.18, 0.18, 1.0)))
    bpy.context.collection.objects.link(floor)


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


def object_bounds(mesh_objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    vertices = [obj.matrix_world @ vertex.co for obj in mesh_objects for vertex in obj.data.vertices]
    if not vertices:
        return Vector((0, 0, 0)), Vector((0, 0, 0))

    minimum = Vector((min(vertex.x for vertex in vertices), min(vertex.y for vertex in vertices), min(vertex.z for vertex in vertices)))
    maximum = Vector((max(vertex.x for vertex in vertices), max(vertex.y for vertex in vertices), max(vertex.z for vertex in vertices)))
    return minimum, maximum


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


def import_subject(model_root: Path, subject: dict[str, object], location: tuple[float, float, float]) -> None:
    path = model_root / str(subject["path"])
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    mesh_objects = imported_mesh_objects(before)
    bpy.context.view_layer.update()
    apply_review_material_colors(mesh_objects)

    minimum, maximum = object_bounds(mesh_objects)
    size = maximum - minimum
    scale = float(subject["target_height"]) / size.z if size.z > 0 else 1.0
    center = Vector(((minimum.x + maximum.x) * 0.5, (minimum.y + maximum.y) * 0.5, minimum.z))
    target = Vector(location)

    for obj in mesh_objects:
        obj.name = f"{subject['label']}_{obj.name}"
        for vertex in obj.data.vertices:
            vertex.co = (vertex.co - center) * scale + target


def add_label(text: str, location: tuple[float, float, float], size: float = 0.18) -> None:
    bpy.ops.object.text_add(location=location, rotation=(math.radians(68), 0, 0))
    label = bpy.context.object
    label.name = f"Label_{text}"
    label.data.body = text
    label.data.align_x = "CENTER"
    label.data.align_y = "CENTER"
    label.data.size = size
    label.data.materials.append(make_material("LabelWhite", (0.92, 0.96, 1.0, 1.0)))


def add_camera_and_light() -> None:
    bpy.ops.object.light_add(type="AREA", location=(-3.2, -4.8, 6.0))
    key = bpy.context.object
    key.name = "M1StyleProofKeyLight"
    key.data.energy = 720
    key.data.size = 5.2

    bpy.ops.object.light_add(type="POINT", location=(3.1, -3.2, 2.2))
    fill = bpy.context.object
    fill.name = "M1StyleProofSoftFill"
    fill.data.energy = 55

    bpy.ops.object.camera_add(location=(0, -7.4, 3.45), rotation=(math.radians(64), 0, 0))
    camera = bpy.context.object
    camera.name = "M1StyleProofCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 6.35
    bpy.context.scene.camera = camera


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--model-root", default="Assets/Art/Models/Generated")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    reset_scene()
    model_root = Path(args.model_root)
    x_positions = (-2.05, 0.0, 2.05)

    for subject, x in zip(SUBJECTS, x_positions, strict=True):
        add_panel((x, 0.42, -0.18))
        add_floor((x, 0.02, -0.02))
        import_subject(model_root, subject, (x, 0.0, 0.0))
        add_label(str(subject["label"]), (x, -0.34, -0.4), size=0.2)
        add_label(str(subject["role"]), (x, -0.34, -0.66), size=0.1)

    add_label("M1 Style Proof - same camera and lighting", (0.0, -0.42, 2.55), size=0.16)
    add_camera_and_light()

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_M1_STYLE_PROOF_BOARD] {output}")


if __name__ == "__main__":
    main()
