#!/usr/bin/env python3
"""Generate Defender Chronicles' original low-poly Blender source and FBX exports."""

import argparse
import math
import sys
from pathlib import Path

import bpy


PALETTE = {
    "DC_Navy": (0.027, 0.094, 0.165, 1.0),
    "DC_Defender": (0.114, 0.435, 0.820, 1.0),
    "DC_Stone": (0.541, 0.588, 0.639, 1.0),
    "DC_Forest": (0.098, 0.239, 0.173, 1.0),
    "DC_Enemy": (0.647, 0.169, 0.196, 1.0),
    "DC_Magic": (0.235, 0.749, 0.937, 1.0),
    "DC_Fire": (1.0, 0.541, 0.141, 1.0),
    "DC_Dark": (0.055, 0.066, 0.078, 1.0),
}


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights):
        for block in list(datablocks):
            datablocks.remove(block)


def material(name):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = PALETTE[name]
        mat.use_nodes = False
        mat.roughness = 0.82
    return mat


def unity_location(value):
    """Convert Unity (X, Y-up, Z) coordinates to Blender (X, Y, Z-up)."""
    return value[0], value[2], value[1]


def unity_scale(value):
    return value[0], value[2], value[1]


def unity_rotation(value):
    return value[0], value[2], value[1]


def finish(obj, name, mat_name):
    obj.name = name
    obj.data.name = f"{name}_Mesh"
    obj.data.materials.append(material(mat_name))
    if hasattr(obj.data, "polygons"):
        for polygon in obj.data.polygons:
            polygon.use_smooth = False
    return obj


def box(name, location, scale, mat_name, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=unity_location(location))
    obj = bpy.context.object
    converted_scale = unity_scale(scale)
    obj.scale = tuple(axis * 0.5 for axis in converted_scale)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        modifier = obj.modifiers.new("Small bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return finish(obj, name, mat_name)


def cylinder(name, location, radius, depth, mat_name, vertices=8, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                       location=unity_location(location), rotation=unity_rotation(rotation))
    return finish(bpy.context.object, name, mat_name)


def cone(name, location, radius1, radius2, depth, mat_name, vertices=8, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth,
                                   location=unity_location(location), rotation=unity_rotation(rotation))
    return finish(bpy.context.object, name, mat_name)


def sphere(name, location, scale, mat_name):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1.0, location=unity_location(location))
    obj = bpy.context.object
    obj.scale = unity_scale(scale)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, mat_name)


def shadow(radius=0.58):
    cylinder("BlobShadow", (0, 0.025, 0), radius, 0.025, "DC_Dark", 16)


def banner(prefix, location, color="DC_Defender", scale=1.0):
    cylinder(f"{prefix}_Pole", location, 0.035 * scale, 1.15 * scale, "DC_Stone", 8)
    x, y, z = location
    cloth = box(f"{prefix}_Cloth", (x + 0.23 * scale, y + 0.32 * scale, z),
                (0.42 * scale, 0.36 * scale, 0.06 * scale), color, 0.025)
    cloth.rotation_euler[1] = math.radians(-5)


def build_environment():
    box("ValleyGround", (11, -0.42, 5.5), (28, 0.55, 14.2), "DC_Forest", 0.08)
    box("DeepBackdrop", (11, -0.72, 5.5), (33, 0.18, 19), "DC_Navy")
    box("NorthRidge", (11, -0.05, 12.68), (28, 0.72, 0.9), "DC_Stone", 0.12)
    box("SouthRidge", (11, -0.05, -1.68), (28, 0.72, 0.9), "DC_Stone", 0.12)
    path = [(-2, 2), (4, 2), (4, 8), (10, 8), (10, 3.5), (16, 3.5), (16, 9), (24, 9)]
    for i, (a, b) in enumerate(zip(path, path[1:])):
        ax, az = a
        bx, bz = b
        dx, dz = bx - ax, bz - az
        length = math.hypot(dx, dz)
        angle = math.atan2(dx, dz)
        edge = box(f"RoadEdge_{i}", ((ax + bx) / 2, -0.08, (az + bz) / 2),
                   (1.9, 0.18, length + 1.6), "DC_Dark", 0.08)
        edge.rotation_euler[2] = -angle
        road = box(f"Road_{i}", ((ax + bx) / 2, 0.025, (az + bz) / 2),
                   (1.42, 0.13, length + 1.25), "DC_Stone", 0.05)
        road.rotation_euler[2] = -angle

    # Defender keep
    box("KeepMain", (24.1, 1.0, 9), (2.35, 2.0, 3.0), "DC_Stone", 0.12)
    for z in (7.65, 10.35):
        cylinder("KeepTower", (23.45, 1.42, z), 0.72, 2.8, "DC_Stone", 10)
        cone("KeepRoof", (23.45, 3.05, z), 0.9, 0.12, 0.95, "DC_Defender", 10)
    banner("KeepBanner", (22.95, 3.35, 9), "DC_Defender", 1.25)
    box("KeepDoor", (22.88, 0.72, 9), (0.08, 1.15, 0.72), "DC_Navy", 0.08)

    # Enemy gate
    box("GateBase", (-2.35, 0.2, 2), (1.65, 0.4, 2.7), "DC_Dark", 0.06)
    for z in (1.12, 2.88):
        box("GatePillar", (-2.52, 1.05, z), (0.58, 2.1, 0.58), "DC_Enemy", 0.08)
        cone("GateSpike", (-2.52, 2.32, z), 0.42, 0.02, 0.55, "DC_Fire", 6)
    box("GateBeam", (-2.52, 2.05, 2), (0.66, 0.44, 2.35), "DC_Enemy", 0.06)

    # Boss arena runes near entrance
    for i in range(8):
        angle = i * math.pi / 4
        sphere("BossRune", (-0.1 + math.cos(angle) * 1.75, 0.08, 2 + math.sin(angle) * 1.75),
               (0.13, 0.06, 0.24), "DC_Fire")

    for i in range(20):
        x = -0.5 + (i * 1.31) % 27
        z = -1.1 if i % 2 == 0 else 12.1
        sphere("BoundaryRock", (x, 0.22, z), (0.34 + (i % 3) * 0.1, 0.3, 0.38), "DC_Stone")
    for i in range(14):
        x = 0.6 + (i * 1.78) % 23
        z = 0.55 if i % 3 == 0 else 11.25
        cylinder("PineTrunk", (x, 0.3, z), 0.09, 0.6, "DC_Dark", 7)
        cone("PineCrownA", (x, 0.8, z), 0.48, 0.03, 0.9, "DC_Forest", 8)
        cone("PineCrownB", (x, 1.18, z), 0.34, 0.01, 0.72, "DC_Forest", 8)


def build_slot():
    cylinder("SlotStone", (0, 0.1, 0), 0.62, 0.2, "DC_Stone", 12)
    cylinder("SlotInset", (0, 0.215, 0), 0.46, 0.035, "DC_Magic", 12)
    for i in range(4):
        angle = i * math.pi / 2
        box("SlotRune", (math.cos(angle) * 0.47, 0.25, math.sin(angle) * 0.47),
            (0.12, 0.04, 0.22), "DC_Defender", 0.02).rotation_euler[1] = -angle


def build_command():
    cylinder("CommandStone", (0, 0.08, 0), 0.42, 0.16, "DC_Stone", 10)
    cylinder("CommandGlow", (0, 0.18, 0), 0.31, 0.03, "DC_Magic", 10)
    banner("CommandBanner", (-0.18, 0.68, 0), "DC_Defender", 0.78)


def build_humanoid(kind):
    specs = {
        "knight": ("DC_Defender", "DC_Stone", 1.0),
        "guard": ("DC_Stone", "DC_Defender", 0.78),
        "raider": ("DC_Enemy", "DC_Dark", 0.72),
        "vanguard": ("DC_Enemy", "DC_Stone", 0.95),
        "regenerator": ("DC_Forest", "DC_Enemy", 1.08),
        "support": ("DC_Magic", "DC_Enemy", 0.84),
        "boss": ("DC_Enemy", "DC_Fire", 1.55),
    }
    body, accent, scale = specs[kind]
    shadow(0.48 * scale)
    cylinder("Body", (0, 0.82 * scale, 0), 0.28 * scale, 0.74 * scale, body, 8)
    sphere("Shoulders", (0, 1.12 * scale, 0), (0.42 * scale, 0.22 * scale, 0.26 * scale), accent)
    sphere("Head", (0, 1.46 * scale, 0), (0.25 * scale, 0.28 * scale, 0.23 * scale), "DC_Stone")
    cone("Helmet", (0, 1.7 * scale, 0), 0.3 * scale, 0.06 * scale, 0.38 * scale, body, 8)
    for side in (-1, 1):
        cylinder("Arm", (side * 0.36 * scale, 0.92 * scale, 0), 0.09 * scale, 0.58 * scale,
                 accent, 7, rotation=(0, 0, math.radians(side * 12)))
        cylinder("Leg", (side * 0.15 * scale, 0.38 * scale, 0), 0.1 * scale, 0.58 * scale, "DC_Dark", 7)

    if kind in ("knight", "guard", "vanguard", "boss"):
        box("Shield", (-0.42 * scale, 0.88 * scale, -0.08 * scale),
            (0.12 * scale, 0.66 * scale, 0.52 * scale), accent, 0.05 * scale)
        cylinder("Weapon", (0.45 * scale, 1.02 * scale, 0), 0.035 * scale, 0.95 * scale, "DC_Fire", 6,
                 rotation=(0, 0, math.radians(-22)))
    if kind == "support":
        cylinder("Staff", (0.42 * scale, 1.0 * scale, 0), 0.04 * scale, 1.3 * scale, "DC_Stone", 7)
        sphere("StaffGem", (0.42 * scale, 1.65 * scale, 0), (0.16, 0.16, 0.16), "DC_Magic")
    if kind == "regenerator":
        for side in (-1, 1):
            cone("VineHorn", (side * 0.2, 1.72 * scale, 0), 0.1, 0.02, 0.36, "DC_Forest", 6,
                 rotation=(0, 0, math.radians(side * 18)))
    if kind in ("raider", "boss"):
        for side in (-1, 1):
            cone("Horn", (side * 0.18 * scale, 1.72 * scale, 0), 0.11 * scale, 0.015,
                 0.36 * scale, "DC_Fire", 6, rotation=(0, 0, math.radians(side * 20)))
    if kind == "knight":
        box("Cape", (0, 0.92, 0.23), (0.56, 0.82, 0.08), "DC_Defender", 0.03)
        sphere("Crest", (0, 1.9, 0), (0.12, 0.2, 0.12), "DC_Fire")


def build_siege():
    shadow(0.85)
    box("SiegeBody", (0, 0.48, 0), (1.25, 0.66, 1.0), "DC_Dark", 0.08)
    for x in (-0.58, 0.58):
        for z in (-0.36, 0.36):
            cylinder("Wheel", (x, 0.35, z), 0.28, 0.18, "DC_Stone", 10, rotation=(0, 0, math.pi / 2))
    box("Ram", (0, 0.78, -0.72), (0.34, 0.34, 1.65), "DC_Enemy", 0.04)
    cone("RamHead", (0, 0.78, -1.62), 0.26, 0.02, 0.55, "DC_Fire", 6, rotation=(math.pi / 2, 0, 0))
    banner("SiegeBanner", (-0.38, 1.12, 0.2), "DC_Enemy", 0.8)


def build_outpost(kind, level):
    shadow(0.62)
    cylinder("TowerBase", (0, 0.22, 0), 0.58, 0.42, "DC_Stone", 10)
    if kind == "archer":
        cylinder("ArcherTower", (0, 0.83, 0), 0.43, 0.98, "DC_Stone", 10)
        cone("ArcherRoof", (0, 1.46, 0), 0.62, 0.08, 0.46, "DC_Enemy", 8)
        for side in (-1, 1):
            box("ArcherBow", (side * 0.42, 1.1, 0), (0.08, 0.58, 0.12), "DC_Fire", 0.02)
        if level == 2:
            banner("ArcherBanner", (0.26, 1.72, 0), "DC_Defender", 0.72)
            cylinder("ArcherRing", (0, 1.2, 0), 0.5, 0.14, "DC_Defender", 10)
    elif kind == "barracks":
        box("BarracksHall", (0, 0.72, 0), (1.04, 0.92, 0.88), "DC_Stone", 0.1)
        cone("BarracksRoof", (0, 1.34, 0), 0.74, 0.12, 0.58, "DC_Defender", 4,
             rotation=(0, math.radians(45), 0))
        box("BarracksDoor", (0, 0.58, -0.47), (0.34, 0.58, 0.08), "DC_Dark", 0.04)
        banner("BarracksBanner", (0.35, 1.52, 0), "DC_Defender", 0.72 + 0.12 * level)
        if level == 2:
            for side in (-1, 1):
                cylinder("BarracksTurret", (side * 0.48, 1.0, 0.3), 0.18, 0.8, "DC_Stone", 8)
    else:
        cylinder("MageSpire", (0, 0.9, 0), 0.42, 1.3, "DC_Navy", 8)
        for i in range(3 + level):
            angle = i * math.pi * 2 / (3 + level)
            cone("MageProng", (math.cos(angle) * 0.37, 1.38, math.sin(angle) * 0.37),
                 0.14, 0.02, 0.72, "DC_Stone", 6)
        sphere("MageCrystal", (0, 1.62, 0), (0.27 + level * 0.05,) * 3, "DC_Magic")
        cylinder("MageRing", (0, 0.5, 0), 0.6, 0.12, "DC_Magic", 12)


ASSETS = {
    "Environment": build_environment,
    "OutpostSlot": build_slot,
    "CommandPoint": build_command,
    "Knight": lambda: build_humanoid("knight"),
    "Guard": lambda: build_humanoid("guard"),
    "Raider": lambda: build_humanoid("raider"),
    "Vanguard": lambda: build_humanoid("vanguard"),
    "Regenerator": lambda: build_humanoid("regenerator"),
    "Support": lambda: build_humanoid("support"),
    "Siege": build_siege,
    "Boss": lambda: build_humanoid("boss"),
    "Archer_L1": lambda: build_outpost("archer", 1),
    "Archer_L2": lambda: build_outpost("archer", 2),
    "Barracks_L1": lambda: build_outpost("barracks", 1),
    "Barracks_L2": lambda: build_outpost("barracks", 2),
    "Mage_L1": lambda: build_outpost("mage", 1),
    "Mage_L2": lambda: build_outpost("mage", 2),
}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-assets", required=True)
    parser.add_argument("--source", required=True)
    script_args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    args = parser.parse_args(script_args)
    output = Path(args.output_assets)
    source = Path(args.source)
    output.mkdir(parents=True, exist_ok=True)
    source.mkdir(parents=True, exist_ok=True)

    for asset_name, builder in ASSETS.items():
        reset_scene()
        builder()
        for obj in bpy.context.scene.objects:
            obj.select_set(obj.type == "MESH")
        bpy.ops.wm.save_as_mainfile(filepath=str(source / f"{asset_name}.blend"))
        bpy.ops.export_scene.fbx(filepath=str(output / f"{asset_name}.fbx"), use_selection=True,
                                 object_types={"MESH"}, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                                 bake_anim=False, add_leaf_bones=False, path_mode="AUTO")
        print(f"Generated {asset_name}")


if __name__ == "__main__":
    main()
