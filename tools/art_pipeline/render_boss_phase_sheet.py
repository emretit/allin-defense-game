"""Render a three-phase boss silhouette and telegraph planning sheet.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_boss_phase_sheet.py -- \
    --output docs/evidence/visual-development/boss-phase-sheet-2026-08-17.png
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


REVIEW_COLORS = {
    "DC_Enemy": (0.58, 0.12, 0.09, 1.0),
    "DC_Dark": (0.08, 0.07, 0.07, 1.0),
    "DC_Fire": (0.95, 0.47, 0.10, 1.0),
    "DC_Metal": (0.42, 0.42, 0.44, 1.0),
    "DC_Stone": (0.34, 0.31, 0.27, 1.0),
}

PHASES = (
    ("Phase 1", "Core wake", 0.75, 0.46, (0.90, 0.25, 0.08, 1.0)),
    ("Phase 2", "Shoulder break", 0.96, 0.68, (1.0, 0.48, 0.10, 1.0)),
    ("Phase 3", "Critical telegraph", 1.18, 0.92, (1.0, 0.78, 0.20, 1.0)),
)


def make_material(name: str, color: tuple[float, float, float, float], emission: float = 0.0) -> bpy.types.Material:
    existing = bpy.data.materials.get(name)
    if existing is not None:
        return existing

    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = color
        principled.inputs["Roughness"].default_value = 0.68
        if "Emission Color" in principled.inputs:
            principled.inputs["Emission Color"].default_value = color
        if "Emission Strength" in principled.inputs:
            principled.inputs["Emission Strength"].default_value = emission
    return material


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.eevee.taa_render_samples = 64
    bpy.context.scene.render.resolution_x = 2400
    bpy.context.scene.render.resolution_y = 1350
    bpy.context.scene.view_settings.view_transform = "Standard"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.world = bpy.data.worlds.new("World") if bpy.context.scene.world is None else bpy.context.scene.world
    bpy.context.scene.world.color = (0.014, 0.018, 0.025)


def apply_review_material_colors(mesh_objects: list[bpy.types.Object]) -> None:
    for obj in mesh_objects:
        for slot in obj.material_slots:
            material = slot.material
            if material is None:
                continue

            for prefix, color in REVIEW_COLORS.items():
                if material.name.startswith(prefix):
                    slot.material = make_material(f"{material.name}_Review", color)
                    break


def object_bounds(mesh_objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    vertices = [obj.matrix_world @ vertex.co for obj in mesh_objects for vertex in obj.data.vertices]
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
        obj.parent = None
        obj.matrix_world.identity()

    for obj in imported:
        if obj.type != "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)

    return mesh_objects


def add_panel(x: float) -> None:
    mesh = bpy.data.meshes.new("BossPhasePanelMesh")
    width = 2.06
    height = 2.75
    y = 0.46
    z = -0.18
    vertices = [
        (x - width * 0.5, y, z),
        (x + width * 0.5, y, z),
        (x + width * 0.5, y, z + height),
        (x - width * 0.5, y, z + height),
    ]
    mesh.from_pydata(vertices, [], [(0, 1, 2, 3)])
    mesh.update()
    panel = bpy.data.objects.new("BossPhasePanel", mesh)
    panel.data.materials.append(make_material("BossPanelCharcoal", (0.045, 0.050, 0.060, 1.0)))
    bpy.context.collection.objects.link(panel)


def add_ring(x: float, radius: float, material: bpy.types.Material, name: str, z: float = 0.012) -> None:
    mesh = bpy.data.meshes.new(f"{name}Mesh")
    segments = 48
    inner = radius * 0.78
    vertices: list[tuple[float, float, float]] = []
    faces: list[tuple[int, int, int, int]] = []
    for index in range(segments):
        angle = math.tau * index / segments
        direction = Vector((math.cos(angle), math.sin(angle), 0.0))
        vertices.append(tuple(Vector((x, 0.0, z)) + direction * inner))
        vertices.append(tuple(Vector((x, 0.0, z)) + direction * radius))
    for index in range(segments):
        next_index = (index + 1) % segments
        faces.append((index * 2, next_index * 2, next_index * 2 + 1, index * 2 + 1))
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    ring = bpy.data.objects.new(name, mesh)
    ring.data.materials.append(material)
    bpy.context.collection.objects.link(ring)


def add_phase_marks(x: float, phase_index: int, core_scale: float, telegraph_radius: float,
                    color: tuple[float, float, float, float]) -> None:
    phase_material = make_material(f"BossPhase{phase_index + 1}HotCore", color, emission=0.75 + phase_index * 0.35)
    dark_material = make_material("BossPhaseDarkShard", (0.07, 0.055, 0.045, 1.0))

    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=10, radius=0.13 * core_scale, location=(x, -0.05, 1.02))
    core = bpy.context.object
    core.name = f"Phase{phase_index + 1}_ReadableCore"
    core.scale.y = 0.45
    core.data.materials.append(phase_material)

    for side in (-1, 1):
        bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.10 + phase_index * 0.035,
                                       radius2=0.02, depth=0.42 + phase_index * 0.12,
                                       location=(x + side * (0.48 + phase_index * 0.08), 0.0, 1.62 + phase_index * 0.08),
                                       rotation=(0.0, math.radians(18 * side), math.radians(45)))
        shard = bpy.context.object
        shard.name = f"Phase{phase_index + 1}_ShoulderShard"
        shard.data.materials.append(phase_material if phase_index > 0 else dark_material)

    add_ring(x, telegraph_radius, phase_material, f"Phase{phase_index + 1}_GroundTelegraph")


def import_phase(path: Path, phase_index: int, x: float) -> None:
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    mesh_objects = imported_mesh_objects(before)
    hidden_prefixes = ("Phase2_", "Phase3_") if phase_index == 0 else (("Phase3_",) if phase_index == 1 else ())
    for obj in list(mesh_objects):
        if obj.name.startswith(hidden_prefixes):
            mesh_objects.remove(obj)
            bpy.data.objects.remove(obj, do_unlink=True)
    apply_review_material_colors(mesh_objects)

    minimum, maximum = object_bounds(mesh_objects)
    size = maximum - minimum
    scale = 1.78 / size.z if size.z > 0 else 1.0
    center = Vector(((minimum.x + maximum.x) * 0.5, (minimum.y + maximum.y) * 0.5, minimum.z))
    rotation = Matrix.Rotation(math.radians(-28), 4, "Z")
    target = Vector((x, 0.0, 0.0))

    for obj in mesh_objects:
        obj.name = f"Phase{phase_index + 1}_{obj.name}"
        for vertex in obj.data.vertices:
            vertex.co = rotation @ ((vertex.co - center) * scale * (1.0 + phase_index * 0.035)) + target


def add_label(text: str, location: tuple[float, float, float], size: float = 0.15) -> None:
    bpy.ops.object.text_add(location=location, rotation=(math.radians(68), 0, 0))
    label = bpy.context.object
    label.data.body = text
    label.data.align_x = "CENTER"
    label.data.align_y = "CENTER"
    label.data.size = size
    label.data.materials.append(make_material("BossLabelWhite", (0.92, 0.96, 1.0, 1.0)))


def add_camera_and_light() -> None:
    bpy.ops.object.light_add(type="AREA", location=(-3.4, -4.6, 6.2))
    key = bpy.context.object
    key.data.energy = 820
    key.data.size = 5.0

    bpy.ops.object.light_add(type="POINT", location=(2.8, -2.8, 2.4))
    rim = bpy.context.object
    rim.data.color = (1.0, 0.42, 0.12)
    rim.data.energy = 95

    bpy.ops.object.camera_add(location=(0, -7.7, 3.62), rotation=(math.radians(64), 0, 0))
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 6.5
    bpy.context.scene.camera = camera


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--model-root", default="Assets/Art/Models/Generated")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    reset_scene()
    model_path = Path(args.model_root) / "Boss.fbx"
    x_positions = (-2.05, 0.0, 2.05)
    add_label("Boss Phase Silhouette + Telegraph Plan", (0.0, -0.42, 2.56), size=0.17)

    for index, ((label, subtitle, core_scale, telegraph_radius, color), x) in enumerate(zip(PHASES, x_positions, strict=True)):
        add_panel(x)
        import_phase(model_path, index, x)
        add_phase_marks(x, index, core_scale, telegraph_radius, color)
        add_label(label, (x, -0.35, -0.37), size=0.18)
        add_label(subtitle, (x, -0.35, -0.60), size=0.11)

    add_camera_and_light()
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_BOSS_PHASE_SHEET] {output}")


if __name__ == "__main__":
    main()
