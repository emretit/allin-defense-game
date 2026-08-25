#!/usr/bin/env python3
"""Render VIS-408 Greenwatch prop library contact sheet.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_prop_library_sheet.py -- \
    --output docs/evidence/visual-development/prop-library-sheet-2026-08-16.png
"""

from __future__ import annotations

import argparse
import sys
import warnings
from pathlib import Path

import bpy

warnings.filterwarnings("ignore", category=DeprecationWarning)


ASSETS = (
    ("Tree A", "Assets/Art/Models/Generated/Prop_PineTall_A.fbx", (-4.8, 2.6, 0.0)),
    ("Tree B", "Assets/Art/Models/Generated/Prop_PineTall_B.fbx", (-2.4, 2.6, 0.0)),
    ("Rock A", "Assets/Art/Models/Generated/Prop_RockCluster_A.fbx", (0.2, 2.5, 0.0)),
    ("Rock B", "Assets/Art/Models/Generated/Prop_RockCluster_B.fbx", (2.4, 2.5, 0.0)),
    ("Bush A", "Assets/Art/Models/Generated/Prop_BushRound_A.fbx", (4.8, 2.5, 0.0)),
    ("Bush B", "Assets/Art/Models/Generated/Prop_BushRound_B.fbx", (-4.8, 0.2, 0.0)),
    ("Grass A", "Assets/Art/Models/Generated/Prop_GrassPatch_A.fbx", (-2.4, 0.2, 0.0)),
    ("Grass B", "Assets/Art/Models/Generated/Prop_GrassPatch_B.fbx", (0.0, 0.2, 0.0)),
    ("Crystal A", "Assets/Art/Models/Generated/Prop_CrystalCluster_A.fbx", (2.4, 0.2, 0.0)),
    ("Crystal B", "Assets/Art/Models/Generated/Prop_CrystalCluster_B.fbx", (4.8, 0.2, 0.0)),
)

PALETTE = {
    "DC_Navy": (0.045, 0.102, 0.246, 1.0),
    "DC_Forest": (0.098, 0.239, 0.173, 1.0),
    "DC_Grass": (0.169, 0.357, 0.263, 1.0),
    "DC_Earth": (0.50, 0.34, 0.20, 1.0),
    "DC_Stone": (0.46, 0.50, 0.54, 1.0),
    "DC_Metal": (0.26, 0.31, 0.36, 1.0),
    "DC_Dark": (0.12, 0.10, 0.09, 1.0),
    "DC_Defender": (0.24, 0.56, 0.98, 1.0),
    "DC_Magic": (0.32, 0.86, 1.0, 1.0),
    "DC_Fire": (1.0, 0.55, 0.14, 1.0),
    "Ground": (0.06, 0.14, 0.12, 1.0),
    "Label": (0.91, 0.95, 1.0, 1.0),
}


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1920
    bpy.context.scene.render.resolution_y = 1080
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
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0.2, 1.4, -0.08))
    obj = bpy.context.object
    obj.name = "PropLibrarySheetBase"
    obj.dimensions = (14.8, 6.6, 0.02)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material("Ground", PALETTE["Ground"]))


def add_label(text: str, location: tuple[float, float, float], size: float = 0.18) -> None:
    bpy.ops.object.text_add(location=location, rotation=(0, 0, 0))
    label = bpy.context.object
    label.name = f"Label_{text.replace(' ', '_')}"
    label.data.body = text
    label.data.align_x = "CENTER"
    label.data.align_y = "CENTER"
    label.data.size = size
    label.data.materials.append(material("Label", PALETTE["Label"]))


def place_assets() -> None:
    for label, path, location in ASSETS:
        bpy.ops.import_scene.fbx(filepath=str(Path(path)))
        imported = list(bpy.context.selected_objects)
        for obj in imported:
            obj.location.x += location[0]
            obj.location.y += location[1]
            obj.location.z += location[2]
        add_label(label, (location[0], location[1] - 0.95, 0.04), 0.16)


def add_titles() -> None:
    add_label("VIS-408 Prop library candidates", (0.2, 5.2, 0.04), 0.34)
    add_label("Two variants per family: tree, rock, bush, grass, crystal", (0.2, 4.75, 0.04), 0.22)


def add_lighting_and_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0.0, 1.6, 8.2))
    key = bpy.context.object
    key.data.energy = 720
    key.data.size = 12.0
    bpy.ops.object.light_add(type="AREA", location=(3.8, -1.0, 4.8))
    fill = bpy.context.object
    fill.data.energy = 160
    fill.data.size = 8.0
    fill.data.color = (0.35, 0.55, 0.88)
    bpy.ops.object.camera_add(location=(0.2, 1.35, 10.5), rotation=(0, 0, 0))
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 10.8
    bpy.context.scene.camera = camera


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    reset_scene()
    add_base()
    place_assets()
    remap_import_materials()
    add_titles()
    add_lighting_and_camera()

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    print(f"[DC_PROP_LIBRARY_SHEET] {output}")


if __name__ == "__main__":
    main()
