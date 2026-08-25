"""Render catalog-current vs generated building proxy contact sheet.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_building_proxy_contact_sheet.py -- \
    --output docs/evidence/visual-development/building-proxy-contact-sheet-2026-08-16.png
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


BUILDINGS = (
    ("Archer L1", 0.72, "Assets/ThirdParty/KayKit/MedievalHexagon/Models/building_tower_A_blue.fbx", "Assets/Art/Models/Generated/Archer_L1.fbx"),
    ("Archer L2", 0.72, "Assets/ThirdParty/KayKit/MedievalHexagon/Models/building_archeryrange_blue.fbx", "Assets/Art/Models/Generated/Archer_L2.fbx"),
    ("Barracks L1", 0.76, "Assets/ThirdParty/KayKit/MedievalHexagon/Models/building_tower_base_blue.fbx", "Assets/Art/Models/Generated/Barracks_L1.fbx"),
    ("Barracks L2", 0.72, "Assets/ThirdParty/KayKit/MedievalHexagon/Models/building_barracks_blue.fbx", "Assets/Art/Models/Generated/Barracks_L2.fbx"),
    ("Mage L1", 0.66, "Assets/ThirdParty/KayKit/MedievalHexagon/Models/building_tower_B_blue.fbx", "Assets/Art/Models/Generated/Mage_L1.fbx"),
    ("Mage L2", 0.76, "Assets/ThirdParty/KayKit/MedievalHexagon/Models/building_church_blue.fbx", "Assets/Art/Models/Generated/Mage_L2.fbx"),
)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1920
    bpy.context.scene.render.resolution_y = 1080
    bpy.context.scene.render.film_transparent = False
    bpy.context.scene.world = bpy.data.worlds.new("World") if bpy.context.scene.world is None else bpy.context.scene.world
    bpy.context.scene.world.color = (0.014, 0.022, 0.038)


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
        principled.inputs["Roughness"].default_value = 0.78
    return material


def import_building(path: Path, label: str, catalog_scale: float, location: tuple[float, float, float],
                    material: bpy.types.Material) -> None:
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
        obj.data.materials.clear()
        obj.data.materials.append(material)

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
    visual_scale = 1.18

    for obj in mesh_objects:
        for vertex in obj.data.vertices:
            vertex.co = (vertex.co - center) * catalog_scale * visual_scale + target


def add_label(text: str, location: tuple[float, float, float], size: float = 0.13) -> None:
    bpy.ops.object.text_add(location=location, rotation=(math.radians(70), 0, 0))
    label = bpy.context.object
    label.name = f"Label_{text}"
    label.data.body = text
    label.data.align_x = "CENTER"
    label.data.align_y = "CENTER"
    label.data.size = size
    label.data.materials.append(make_material("LabelWhite", (0.92, 0.96, 1.0, 1.0)))


def add_camera_and_light() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -4.5, 7.2))
    light = bpy.context.object
    light.name = "BuildingContactSheetKeyLight"
    light.data.energy = 680
    light.data.size = 6.4

    bpy.ops.object.camera_add(location=(0, -8.2, 5.0), rotation=(math.radians(60), 0, 0))
    camera = bpy.context.object
    camera.name = "BuildingContactSheetCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 10.5
    bpy.context.scene.camera = camera


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    reset_scene()
    catalog_material = make_material("CatalogCurrentBlue", (0.28, 0.56, 0.95, 1.0))
    generated_material = make_material("GeneratedCandidateGold", (0.95, 0.58, 0.20, 1.0))
    spacing_x = 1.75
    start_x = -4.38
    catalog_z = 1.18
    generated_z = -1.42

    for index, (label, scale, catalog_path, generated_path) in enumerate(BUILDINGS):
        x = start_x + index * spacing_x
        import_building(Path(catalog_path), f"{label} Catalog", scale, (x, 0, catalog_z), catalog_material)
        import_building(Path(generated_path), f"{label} Generated", scale, (x, 0, generated_z), generated_material)
        add_label(label, (x, -0.12, 3.05), 0.13)

    add_camera_and_light()
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_BUILDING_PROXY_CONTACT_SHEET] {output}")


if __name__ == "__main__":
    main()
