"""Render generated building candidate QA sheet and blind-recognition key.

This is a repository evidence generator for VIS-344. It intentionally renders
generated building candidates only; production catalog binding remains a Unity
validation step.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/render_building_qa_sheet.py -- \
    --output docs/evidence/visual-development/building-qa-sheet-2026-08-16.png \
    --answer-key docs/evidence/visual-development/building-blind-recognition-key-2026-08-16.json
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import random
import re
import sys
import warnings
from pathlib import Path
from typing import Any

import bpy
from mathutils import Vector

warnings.filterwarnings("ignore", category=DeprecationWarning)


BUILDINGS: tuple[dict[str, Any], ...] = (
    {
        "id": "Archer_L1",
        "kind": "Archer",
        "level": 1,
        "catalog_scale": 0.72,
        "path": "Assets/Art/Models/Generated/Archer_L1.fbx",
        "expected_read": "thin watchtower, visible bow/target language, low mass",
    },
    {
        "id": "Archer_L2",
        "kind": "Archer",
        "level": 2,
        "catalog_scale": 0.72,
        "path": "Assets/Art/Models/Generated/Archer_L2.fbx",
        "expected_read": "taller roofed archer post, larger banner/weapon profile",
    },
    {
        "id": "Barracks_L1",
        "kind": "Barracks",
        "level": 1,
        "catalog_scale": 0.76,
        "path": "Assets/Art/Models/Generated/Barracks_L1.fbx",
        "expected_read": "low guard post, horizontal mass, shield/spear language",
    },
    {
        "id": "Barracks_L2",
        "kind": "Barracks",
        "level": 2,
        "catalog_scale": 0.72,
        "path": "Assets/Art/Models/Generated/Barracks_L2.fbx",
        "expected_read": "fortified barracks, heavier roof/tower/weapon-rack mass",
    },
    {
        "id": "Mage_L1",
        "kind": "Mage",
        "level": 1,
        "catalog_scale": 0.66,
        "path": "Assets/Art/Models/Generated/Mage_L1.fbx",
        "expected_read": "open crystal shrine, rune ring and single crystal focus",
    },
    {
        "id": "Mage_L2",
        "kind": "Mage",
        "level": 2,
        "catalog_scale": 0.76,
        "path": "Assets/Art/Models/Generated/Mage_L2.fbx",
        "expected_read": "taller arcane sanctuary, multi-crystal vertical silhouette",
    },
)

PALETTE = {
    "DC_Defender": (0.114, 0.435, 0.820, 1.0),
    "DC_Stone": (0.541, 0.588, 0.639, 1.0),
    "DC_Magic": (0.235, 0.749, 0.937, 1.0),
    "DC_Fire": (1.0, 0.541, 0.141, 1.0),
    "DC_Dark": (0.055, 0.066, 0.078, 1.0),
    "DC_Navy": (0.045, 0.102, 0.246, 1.0),
}

BLIND_SEED = 20260816


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
            eevee.gtao_factor = 1.2
    bpy.context.scene.render.film_transparent = False
    bpy.context.scene.world = bpy.data.worlds.new("World") if bpy.context.scene.world is None else bpy.context.scene.world
    bpy.context.scene.world.color = (0.01, 0.016, 0.03)


def make_material(name: str, color: tuple[float, float, float, float]) -> bpy.types.Material:
    existing = bpy.data.materials.get(name)
    if existing is not None:
        existing.diffuse_color = color
        existing.use_nodes = True
        principled = existing.node_tree.nodes.get("Principled BSDF")
        if principled is not None:
            principled.inputs["Base Color"].default_value = color
            principled.inputs["Roughness"].default_value = 0.84
        return existing

    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = color
        principled.inputs["Roughness"].default_value = 0.84
    return material


def parse_meta(path: Path) -> dict[str, Any]:
    meta_path = path.with_suffix(path.suffix + ".meta")
    if not meta_path.exists():
        return {"exists": False}

    text = meta_path.read_text(encoding="utf-8")

    def find_scalar(key: str) -> str | None:
        match = re.search(rf"^\s*{re.escape(key)}:\s*(.+)$", text, re.MULTILINE)
        return match.group(1).strip() if match else None

    return {
        "exists": True,
        "guid": find_scalar("guid"),
        "globalScale": find_scalar("globalScale"),
        "useFileScale": find_scalar("useFileScale"),
        "animationType": find_scalar("animationType"),
        "importAnimation": find_scalar("importAnimation"),
    }


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


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


def add_lighting_and_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -4.8, 7.2))
    key = bpy.context.object
    key.name = "BuildingQAKeyLight"
    key.data.energy = 780
    key.data.size = 6.4

    bpy.ops.object.light_add(type="POINT", location=(-5.3, -2.8, 2.0))
    rim = bpy.context.object
    rim.name = "BuildingQACoolRim"
    rim.data.energy = 48
    rim.data.color = (0.32, 0.56, 1.0)

    bpy.ops.object.camera_add(location=(0, -9.25, 5.95), rotation=(math.radians(60), 0, 0))
    camera = bpy.context.object
    camera.name = "BuildingQACamera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 12.6
    bpy.context.scene.camera = camera


def build_blind_order() -> list[tuple[str, dict[str, Any]]]:
    randomized = list(BUILDINGS)
    random.Random(BLIND_SEED).shuffle(randomized)
    return [(chr(ord("A") + index), entry) for index, entry in enumerate(randomized)]


def write_answer_key(path: Path, blind_order: list[tuple[str, dict[str, Any]]]) -> None:
    items = []
    for code, entry in blind_order:
        asset_path = Path(entry["path"])
        items.append(
            {
                "code": code,
                "id": entry["id"],
                "kind": entry["kind"],
                "level": entry["level"],
                "asset_path": entry["path"],
                "expected_read": entry["expected_read"],
                "sha256": sha256(asset_path) if asset_path.exists() else None,
                "meta": parse_meta(asset_path),
            }
        )

    report = {
        "schema": "allin-defense.visual-development.building-blind-recognition-key.v1",
        "captured_on": "2026-08-16",
        "todo": "VIS-344",
        "asset_scope": "Generated building candidates only; production catalog binding remains pending.",
        "blind_seed": BLIND_SEED,
        "human_blind_recognition_status": "Pending: sheet/key are prepared, but no human recognition pass has been recorded yet.",
        "recognition_prompt": "For each code A-F, identify role (Archer/Barracks/Mage) and level (L1/L2) from silhouette/thumbnail, without reading this key.",
        "items": items,
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(report, indent=2), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--answer-key", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    reset_scene()
    grayscale_material = make_material("NeutralGrayReadability", (0.58, 0.6, 0.62, 1.0))
    thumbnail_material = make_material("DarkThumbnailSilhouette", (0.15, 0.19, 0.24, 1.0))
    blind_material = make_material("BlindQAInk", (0.12, 0.15, 0.19, 1.0))
    blind_order = build_blind_order()

    spacing_x = 1.62
    start_x = -4.02
    rows = (
        ("Color named", 2.25, 0.78, None),
        ("Grayscale named", 0.10, 0.78, grayscale_material),
        ("Tiny 160-220px read", -1.75, 0.43, thumbnail_material),
    )

    add_label("VIS-344 generated building readability QA", (0, -0.18, 3.98), 0.16)

    for index, entry in enumerate(BUILDINGS):
        x = start_x + index * spacing_x
        add_label(entry["id"].replace("_", " "), (x, -0.12, 3.72), 0.095)
        for row_name, row_z, visual_scale, override in rows:
            normalize_imported_meshes(
                Path(entry["path"]),
                f"{row_name}_{entry['id']}",
                float(entry["catalog_scale"]),
                (x, 0, row_z),
                visual_scale,
                override,
            )

    add_label("color: material family", (-5.02, -0.2, 2.52), 0.12)
    add_label("gray: silhouette/value", (-5.02, -0.2, 0.36), 0.12)
    add_label("tiny: gameplay read", (-5.02, -0.2, -1.47), 0.12)
    add_label("blind quiz: A-F", (-5.02, -0.2, -3.17), 0.12)

    for index, (code, entry) in enumerate(blind_order):
        x = start_x + index * spacing_x
        add_label(code, (x, -0.12, -2.72), 0.17)
        normalize_imported_meshes(
            Path(entry["path"]),
            f"Blind_{code}",
            float(entry["catalog_scale"]),
            (x, 0, -3.25),
            0.43,
            blind_material,
        )

    add_lighting_and_camera()

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    write_answer_key(Path(args.answer_key), blind_order)
    print(f"[DC_BUILDING_QA_SHEET] {output}")
    print(f"[DC_BUILDING_BLIND_KEY] {args.answer_key}")


if __name__ == "__main__":
    main()
