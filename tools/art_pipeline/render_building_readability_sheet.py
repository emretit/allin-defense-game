"""Render generated building candidate readability checks.

The sheet shows each generated building in three gameplay-art checks:

1. Preserved generated material colors.
2. Neutral grayscale override for silhouette/material-value readability.
3. Small thumbnail silhouette for HUD/icon-distance readability.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_building_readability_sheet.py -- \
    --output docs/evidence/visual-development/building-readability-sheet-2026-08-16.png
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
    ("Archer L1", 0.72, "Assets/Art/Models/Generated/Archer_L1.fbx"),
    ("Archer L2", 0.72, "Assets/Art/Models/Generated/Archer_L2.fbx"),
    ("Barracks L1", 0.76, "Assets/Art/Models/Generated/Barracks_L1.fbx"),
    ("Barracks L2", 0.72, "Assets/Art/Models/Generated/Barracks_L2.fbx"),
    ("Mage L1", 0.66, "Assets/Art/Models/Generated/Mage_L1.fbx"),
    ("Mage L2", 0.76, "Assets/Art/Models/Generated/Mage_L2.fbx"),
)

PALETTE = {
    "DC_Defender": (0.114, 0.435, 0.820, 1.0),
    "DC_Stone": (0.541, 0.588, 0.639, 1.0),
    "DC_Magic": (0.235, 0.749, 0.937, 1.0),
    "DC_Fire": (1.0, 0.541, 0.141, 1.0),
    "DC_Dark": (0.055, 0.066, 0.078, 1.0),
}


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1920
    bpy.context.scene.render.resolution_y = 1080
    if hasattr(bpy.context.scene, "eevee"):
        eevee = bpy.context.scene.eevee
        if hasattr(eevee, "use_gtao"):
            eevee.use_gtao = True
        if hasattr(eevee, "gtao_distance"):
            eevee.gtao_distance = 3
        if hasattr(eevee, "gtao_factor"):
            eevee.gtao_factor = 1.25
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


def normalize_imported_meshes(
    path: Path,
    label: str,
    catalog_scale: float,
    location: tuple[float, float, float],
    visual_scale: float,
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
            vertex.co = (vertex.co - center) * catalog_scale * visual_scale + target


def add_label(text: str, location: tuple[float, float, float], size: float = 0.12) -> None:
    bpy.ops.object.text_add(location=location, rotation=(math.radians(70), 0, 0))
    label = bpy.context.object
    label.name = f"Label_{text}"
    label.data.body = text
    label.data.align_x = "CENTER"
    label.data.align_y = "CENTER"
    label.data.size = size
    label.data.materials.append(make_material("LabelWhite", (0.9, 0.95, 1.0, 1.0)))


def add_row_label(text: str, location: tuple[float, float, float]) -> None:
    add_label(text, location, 0.15)


def add_lighting_and_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -4.8, 7.4))
    key = bpy.context.object
    key.name = "ReadabilityKeyLight"
    key.data.energy = 760
    key.data.size = 6.2

    bpy.ops.object.light_add(type="POINT", location=(-5.0, -2.4, 2.0))
    rim = bpy.context.object
    rim.name = "ReadabilityCoolRim"
    rim.data.energy = 55
    rim.data.color = (0.35, 0.63, 1.0)

    bpy.ops.object.camera_add(location=(0, -9.0, 5.7), rotation=(math.radians(60), 0, 0))
    camera = bpy.context.object
    camera.name = "BuildingReadabilityCamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 12.0
    bpy.context.scene.camera = camera


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    reset_scene()
    grayscale_material = make_material("NeutralGrayReadability", (0.58, 0.6, 0.62, 1.0))
    thumbnail_material = make_material("DarkThumbnailSilhouette", (0.16, 0.2, 0.25, 1.0))

    spacing_x = 1.66
    start_x = -4.15
    rows = (
        ("Color", 1.86, 0.92, None),
        ("Grayscale", -0.86, 0.92, grayscale_material),
        ("Thumbnail", -3.18, 0.42, thumbnail_material),
    )

    for index, (label, scale, path) in enumerate(BUILDINGS):
        x = start_x + index * spacing_x
        add_label(label, (x, -0.15, 3.3), 0.11)
        for row_name, row_z, visual_scale, override in rows:
            normalize_imported_meshes(
                Path(path),
                f"{row_name}_{label}",
                scale,
                (x, 0, row_z),
                visual_scale,
                override,
            )

    add_row_label("Color / candidate material", (-4.95, -0.2, 2.25))
    add_row_label("Grayscale / silhouette value", (-4.95, -0.2, -0.48))
    add_row_label("Tiny thumbnail / role read", (-4.95, -0.2, -2.92))

    add_lighting_and_camera()
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_BUILDING_READABILITY_SHEET] {output}")


if __name__ == "__main__":
    main()
