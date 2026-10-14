#!/usr/bin/env python3
"""Generate ALLIN: Defense' original low-poly Blender source and FBX exports."""

import argparse
import math
import sys
import warnings
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import tower_v3_kit  # noqa: E402
import tower_v3_models  # noqa: E402

warnings.filterwarnings("ignore", category=DeprecationWarning)


PALETTE = {
    "DC_Navy": (0.027, 0.094, 0.165, 1.0),
    "DC_Defender": (0.114, 0.435, 0.820, 1.0),
    "DC_Stone": (0.541, 0.588, 0.639, 1.0),
    "DC_Forest": (0.098, 0.239, 0.173, 1.0),
    "DC_Enemy": (0.647, 0.169, 0.196, 1.0),
    "DC_Magic": (0.235, 0.749, 0.937, 1.0),
    "DC_Fire": (1.0, 0.541, 0.141, 1.0),
    "DC_Dark": (0.055, 0.066, 0.078, 1.0),
    "DC_RegenCore": (0.245, 0.820, 0.337, 1.0),
    "DC_SupportCloth": (0.365, 0.455, 0.500, 1.0),
    "DC_Earth": (0.439, 0.329, 0.239, 1.0),
    "DC_Grass": (0.169, 0.357, 0.263, 1.0),
    "DC_Metal": (0.349, 0.412, 0.475, 1.0),
}

TOWER_TEXTURE_DIR = Path(__file__).resolve().parents[2] / "Assets" / "Art" / "Models" / "Generated"

GREENWATCH_PATH = [(-2, 2), (4, 2), (4, 8), (10, 8), (10, 3.5), (16, 3.5), (16, 9), (24, 9)]


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


def irregular_patch(name, location, scale_x, scale_z, mat_name, seed=0, vertices=9):
    """Create a flat deterministic ground patch without a primitive rectangle silhouette."""
    points = []
    for i in range(vertices):
        angle = i * math.pi * 2 / vertices
        variation = 1.0 + 0.13 * math.sin((i + 1) * (seed + 2) * 1.71)
        points.append((math.cos(angle) * scale_x * variation,
                       math.sin(angle) * scale_z * variation, 0.0))
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(points, [], [tuple(range(vertices))])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = unity_location(location)
    return finish(obj, name, mat_name)


def relief_patch(name, location, scale_x, scale_z, mat_name, seed=0, vertices=11, height=0.018):
    """Create a shallow non-collider terrain color/height patch for VIS-404."""
    points = []
    for i in range(vertices):
        angle = i * math.pi * 2 / vertices
        variation = 1.0 + 0.16 * math.sin((i + 1) * (seed + 3) * 1.37)
        local_height = height * (0.25 + 0.75 * abs(math.sin((i + seed + 1) * 1.91)))
        points.append((math.cos(angle) * scale_x * variation,
                       math.sin(angle) * scale_z * variation,
                       local_height))
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(points, [], [tuple(range(vertices))])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = unity_location(location)
    return finish(obj, name, mat_name)


def build_ground_variation(region):
    """Build broad, low relief color/value patches for one VIS-401 region."""
    if region == "west":
        patches = [
            ("WestCoolBreakA", (0.9, 0.026, 4.35), 2.8, 1.25, "DC_Grass", 4),
            ("WestWarmGateScuff", (-0.7, 0.029, 2.45), 1.55, 0.62, "DC_Earth", 8),
            ("WestSouthForegroundValue", (2.9, 0.024, 0.15), 3.4, 0.72, "DC_Navy", 11),
            ("WestRidgeMoss", (4.6, 0.025, 4.05), 1.8, 0.9, "DC_Forest", 13),
        ]
    elif region == "mid":
        patches = [
            ("MidNorthBendCool", (6.6, 0.027, 6.25), 2.4, 1.65, "DC_Grass", 21),
            ("MidInnerQuietPool", (8.5, 0.025, 5.25), 1.7, 1.0, "DC_Forest", 24),
            ("MidSouthCutValue", (9.1, 0.024, 2.05), 2.55, 0.82, "DC_Navy", 28),
            ("MidRoadShoulderWear", (10.9, 0.03, 4.35), 1.7, 0.45, "DC_Earth", 31),
            ("MidFarMossBreak", (7.1, 0.024, 10.3), 2.6, 0.7, "DC_Forest", 35),
        ]
    elif region == "final":
        patches = [
            ("FinalBossApproachCool", (15.6, 0.026, 6.25), 2.3, 1.35, "DC_Grass", 42),
            ("FinalKeepShadowField", (20.6, 0.024, 7.05), 3.15, 1.55, "DC_Navy", 45),
            ("FinalSouthLowValue", (18.0, 0.024, 1.4), 3.6, 0.72, "DC_Forest", 48),
            ("FinalNorthRidgeGlowBreak", (18.5, 0.026, 11.0), 3.2, 0.82, "DC_Grass", 51),
            ("FinalRoadShoulderWear", (16.3, 0.03, 4.2), 1.55, 0.42, "DC_Earth", 55),
        ]
    else:
        raise ValueError(f"Unknown ground variation region: {region}")

    for name, location, scale_x, scale_z, mat_name, seed in patches:
        relief_patch(name, location, scale_x, scale_z, mat_name, seed=seed)


def create_unity_flat_mesh(name, vertices, faces, mat_name):
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata([unity_location(vertex) for vertex in vertices], [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, mat_name)


def road_side_vectors(a, b):
    ax, az = a
    bx, bz = b
    dx, dz = bx - ax, bz - az
    length = math.hypot(dx, dz)
    if length <= 0:
        return (1, 0), (0, 1), 0
    direction = (dx / length, dz / length)
    side = (-direction[1], direction[0])
    return direction, side, length


def build_road_surface():
    """VIS-405 low-poly road surface with slight width/value variation per segment."""
    for index, (a, b) in enumerate(zip(GREENWATCH_PATH, GREENWATCH_PATH[1:])):
        direction, side, length = road_side_vectors(a, b)
        ax, az = a
        bx, bz = b
        start_pad = 0.20 if index == 0 else 0.10
        end_pad = 0.20 if index == len(GREENWATCH_PATH) - 2 else 0.10
        left_width = 0.68 + 0.05 * math.sin(index * 1.7)
        right_width = 0.70 + 0.04 * math.cos(index * 2.1)
        y = 0.092 + 0.002 * (index % 2)
        start = (ax - direction[0] * start_pad, az - direction[1] * start_pad)
        end = (bx + direction[0] * end_pad, bz + direction[1] * end_pad)
        vertices = [
            (start[0] + side[0] * left_width, y, start[1] + side[1] * left_width),
            (end[0] + side[0] * (left_width + 0.04 * math.sin(index + 1)), y, end[1] + side[1] * (left_width + 0.04 * math.sin(index + 1))),
            (end[0] - side[0] * (right_width + 0.04 * math.cos(index + 1)), y, end[1] - side[1] * (right_width + 0.04 * math.cos(index + 1))),
            (start[0] - side[0] * right_width, y, start[1] - side[1] * right_width),
        ]
        create_unity_flat_mesh(f"RoadSurface_{index}", vertices, [(0, 1, 2, 3)], "DC_Earth")

    for index, (x, z) in enumerate(GREENWATCH_PATH[1:-1], start=1):
        relief_patch(f"RoadCornerBlend_{index}", (x, 0.108, z), 0.88, 0.82, "DC_Earth", seed=70 + index,
                     vertices=8, height=0.006)


def build_road_edges():
    """VIS-405 hand-placed low-poly road stones, separated from gameplay pathing."""
    stone_index = 0
    for segment, (a, b) in enumerate(zip(GREENWATCH_PATH, GREENWATCH_PATH[1:])):
        direction, side, length = road_side_vectors(a, b)
        count = max(2, math.ceil(length / 1.15))
        for i in range(count + 1):
            t = i / count
            along_x = a[0] + (b[0] - a[0]) * t
            along_z = a[1] + (b[1] - a[1]) * t
            for edge in (-1, 1):
                wave = math.sin((stone_index + 1) * 2.37)
                x = along_x + side[0] * edge * (0.91 + wave * 0.035)
                z = along_z + side[1] * edge * (0.91 + wave * 0.035)
                length_scale = 0.34 + 0.06 * abs(math.sin(stone_index * 1.91))
                width_scale = 0.18 + 0.04 * abs(wave)
                stone = box("RoadEdgeStone", (x, 0.135 + 0.012 * abs(wave), z),
                            (length_scale, 0.12, width_scale),
                            "DC_Stone" if stone_index % 3 else "DC_Metal", 0.0)
                stone.rotation_euler[1] = -math.atan2(direction[0], direction[1]) + wave * 0.08
                stone_index += 1


def build_road_wear():
    """VIS-405 readable travel marks and shoulder wear, very flat and non-blocking."""
    patch_index = 0
    for segment, (a, b) in enumerate(zip(GREENWATCH_PATH, GREENWATCH_PATH[1:])):
        direction, side, length = road_side_vectors(a, b)
        count = max(1, math.floor(length / 1.45))
        for i in range(count):
            t = (i + 0.5) / count
            wave = math.sin((patch_index + 2) * 3.17)
            x = a[0] + (b[0] - a[0]) * t + side[0] * wave * 0.22
            z = a[1] + (b[1] - a[1]) * t + side[1] * wave * 0.22
            relief_patch(f"RoadWearPatch_{patch_index}", (x, 0.132, z),
                         0.34 + 0.10 * abs(wave), 0.08 + 0.03 * abs(math.cos(patch_index)),
                         "DC_Dark" if patch_index % 4 == 0 else "DC_Earth",
                         seed=100 + patch_index, vertices=7, height=0.004)
            patch_index += 1

    for index, (x, z) in enumerate(((0.1, 2.0), (4.0, 7.6), (10.0, 3.8), (16.0, 8.55), (22.1, 9.0))):
        relief_patch(f"RoadShoulderScuff_{index}", (x, 0.126, z), 0.72, 0.22, "DC_Earth",
                     seed=150 + index, vertices=8, height=0.005)


def build_entrance_gate():
    """VIS-406 enemy entry gate: a warm, hostile landmark at the west spawn."""
    # Foundation and threshold are deliberately offset from the path so they read as spawn dressing,
    # not gameplay blockers.
    box("EntranceGate_Threshold", (-1.86, 0.11, 2.0), (1.34, 0.22, 2.42), "DC_Dark", 0.045)
    box("EntranceGate_BackSlab", (-2.50, 0.22, 2.0), (0.34, 0.44, 2.92), "DC_Dark", 0.055)

    for side, z in enumerate((0.95, 3.05)):
        height_variation = 0.10 if side else 0.0
        box("EntranceGate_PillarCore", (-2.30, 1.08 + height_variation * 0.5, z),
            (0.62, 2.16 + height_variation, 0.62), "DC_Enemy", 0.075)
        box("EntranceGate_PillarInnerShadow", (-1.94, 0.97, z),
            (0.13, 1.62, 0.40), "DC_Dark", 0.025)
        cone("EntranceGate_CrownSpike", (-2.30, 2.46 + height_variation, z),
             0.43, 0.025, 0.62, "DC_Fire", 6)
        for y in (0.48, 1.05, 1.62):
            band = box("EntranceGate_IronBand", (-1.92, y, z),
                       (0.16, 0.11, 0.72), "DC_Metal", 0.018)
            band.rotation_euler[1] = math.radians(4 if side else -4)

    box("EntranceGate_ArchBeam", (-2.21, 2.08, 2.0), (0.58, 0.42, 2.72), "DC_Enemy", 0.065)
    box("EntranceGate_InnerVoid", (-1.90, 1.20, 2.0), (0.08, 1.42, 1.30), "DC_Dark", 0.02)
    for index, z in enumerate((1.35, 1.68, 2.00, 2.32, 2.65)):
        tooth = cone("EntranceGate_PortcullisTooth", (-1.82, 1.00, z), 0.07, 0.018,
                     0.82 if index % 2 == 0 else 0.62, "DC_Metal", 5)
        tooth.rotation_euler[1] = math.radians(180)
    for z in (1.24, 2.76):
        banner("EntranceGate_TatteredBanner", (-1.92, 1.92, z), "DC_Enemy", 0.58)
    for z in (1.22, 2.0, 2.78):
        sphere("EntranceGate_EmberSocket", (-1.82, 1.55, z), (0.095, 0.14, 0.095), "DC_Fire")
    socket("Socket_Spawn", (-1.42, 0.12, 2.0), 0.20)
    socket("Socket_ThreatLight", (-1.88, 1.55, 2.0), 0.16)
    socket("Socket_GateCenter", (-1.85, 0.98, 2.0), 0.16)


def build_entrance_spawn_light():
    """VIS-406 static warm spawn cue: readable by shape and timing hooks, not only color."""
    cylinder("EntranceSpawn_OuterWarningRing", (-1.08, 0.055, 2.0), 0.72, 0.045, "DC_Fire", 20)
    cylinder("EntranceSpawn_InnerDarkWell", (-1.08, 0.085, 2.0), 0.46, 0.04, "DC_Dark", 18)
    for index in range(8):
        angle = index * math.pi * 2 / 8
        marker = box("EntranceSpawn_DirectionalShard",
                     (-1.08 + math.cos(angle) * 0.62, 0.16, 2.0 + math.sin(angle) * 0.62),
                     (0.09, 0.10 if index % 2 == 0 else 0.07, 0.28),
                     "DC_Fire" if index % 2 == 0 else "DC_Enemy", 0.0)
        marker.rotation_euler[1] = -angle
    for index, offset in enumerate((0.00, 0.26, -0.26)):
        flame = cone("EntranceSpawn_FlameWisp", (-1.25, 0.43 + index * 0.11, 2.0 + offset),
                     0.13 - index * 0.018, 0.018, 0.54 + index * 0.09,
                     "DC_Fire", 6)
        flame.rotation_euler[1] = math.radians(-12 + index * 9)
    for index, z in enumerate((1.56, 2.0, 2.44)):
        chevron = box("EntranceSpawn_PathChevron", (-0.52 + index * 0.16, 0.145, z),
                      (0.12, 0.055, 0.42), "DC_Enemy", 0.012)
        chevron.rotation_euler[1] = math.radians(90)
    socket("Socket_VFX", (-1.08, 0.22, 2.0), 0.16)
    socket("Socket_Audio", (-1.08, 0.18, 2.0), 0.12)


def build_entrance_dressing():
    """VIS-406 west entry dressing: hostile but kept outside path and first-slot readability."""
    for index, (x, z, sx, sz) in enumerate((
        (-1.20, 1.12, 0.64, 0.22),
        (-0.70, 2.88, 0.74, 0.20),
        (0.72, 1.28, 0.56, 0.18),
        (1.10, 2.64, 0.68, 0.20),
    )):
        patch = relief_patch(f"EntranceDressing_BurnScuff_{index}", (x, 0.138, z), sx, sz,
                             "DC_Dark" if index % 2 == 0 else "DC_Earth",
                             seed=220 + index, vertices=8, height=0.006)
        patch.rotation_euler[2] = math.radians(-18 + index * 11)

    for index, (x, z, scale) in enumerate((
        (-2.84, 0.68, 0.62),
        (-2.78, 3.36, 0.54),
        (-0.18, 0.78, 0.36),
        (0.62, 3.22, 0.34),
        (1.36, 0.94, 0.28),
    )):
        sphere("EntranceDressing_JaggedRock", (x, 0.18 + scale * 0.12, z),
               (scale * 0.35, scale * 0.30, scale * 0.42),
               "DC_Metal" if index % 2 else "DC_Stone")

    for index, (x, z, lean) in enumerate(((-0.42, 0.86, -9), (0.24, 3.12, 12), (1.62, 1.22, -14))):
        cylinder("EntranceDressing_CharredPost", (x, 0.36, z), 0.065, 0.72, "DC_Dark", 6)
        spike = cone("EntranceDressing_PostSpike", (x, 0.86, z), 0.14, 0.02, 0.32, "DC_Fire", 5)
        spike.rotation_euler[1] = math.radians(lean)

    for index, (x, z) in enumerate(((-0.05, 1.54), (0.24, 2.42), (1.12, 1.74), (1.44, 2.54))):
        shard = cone("EntranceDressing_ThreatCrystal", (x, 0.25, z), 0.10, 0.025, 0.46,
                     "DC_Fire" if index % 2 == 0 else "DC_Enemy", 5)
        shard.rotation_euler[1] = math.radians(-20 + index * 13)


def build_keep_core():
    """VIS-407 defender keep: strongest friendly mass at the final stand."""
    box("KeepCore_MainHall", (24.10, 1.10, 9.0), (2.85, 2.20, 3.34), "DC_Stone", 0.12)
    box("KeepCore_UpperHall", (24.46, 2.18, 9.0), (1.62, 1.06, 2.30), "DC_Stone", 0.09)
    box("KeepCore_FrontGateFrame", (22.72, 0.92, 9.0), (0.18, 1.48, 1.02), "DC_Navy", 0.05)
    box("KeepCore_FrontDoor", (22.58, 0.84, 9.0), (0.10, 1.28, 0.76), "DC_Dark", 0.04)
    box("KeepCore_Rampart", (23.72, 2.74, 9.0), (1.58, 0.26, 2.52), "DC_Metal", 0.04)

    for side, z in enumerate((7.28, 10.72)):
        tower = cylinder("KeepCore_Tower", (23.96, 1.62, z), 0.84, 3.26, "DC_Stone", 10)
        roof = cone("KeepCore_TowerRoof", (23.96, 3.50, z), 0.96, 0.10, 1.08, "DC_Defender", 10)
        roof.rotation_euler[2] = math.radians(6 if side else -6)
        for y in (0.86, 1.56, 2.26):
            box("KeepCore_TowerWindow", (23.20, y, z), (0.08, 0.22, 0.18), "DC_Magic", 0.015)
        for angle_index in range(4):
            angle = math.radians(45 + angle_index * 90)
            crenel = box("KeepCore_TowerCrenel",
                         (23.96 + math.cos(angle) * 0.52, 2.78, z + math.sin(angle) * 0.52),
                         (0.18, 0.24, 0.18), "DC_Stone", 0.02)
            crenel.rotation_euler[1] = -angle

    for x in (22.98, 23.54, 24.12, 24.68, 25.24):
        box("KeepCore_RampartCrenel", (x, 2.98, 7.92), (0.18, 0.26, 0.18), "DC_Stone", 0.02)
        box("KeepCore_RampartCrenel", (x, 2.98, 10.08), (0.18, 0.26, 0.18), "DC_Stone", 0.02)

    for x, z in ((22.84, 8.10), (22.84, 9.90), (24.98, 8.10), (24.98, 9.90)):
        box("KeepCore_Buttress", (x, 0.78, z), (0.22, 1.06, 0.34), "DC_Metal", 0.03)

    banner("KeepCore_MainBanner", (23.18, 3.88, 9.0), "DC_Defender", 1.28)
    for z in (8.06, 9.0, 9.94):
        sphere("KeepCore_GoldLantern", (22.62, 1.04, z), (0.10, 0.15, 0.10), "DC_Fire")

    socket("Socket_KeepCenter", (24.08, 1.64, 9.0), 0.16)
    socket("Socket_Banner", (23.18, 3.88, 9.0), 0.15)
    socket("Socket_CommandLight", (24.24, 3.24, 9.0), 0.16)
    socket("Socket_Rally", (22.88, 0.24, 9.0), 0.18)
    socket("Socket_Focus", (23.58, 2.18, 9.0), 0.14)


def build_keep_command_light():
    """VIS-407 cool friendly beacon and command focus around the keep."""
    cylinder("KeepLight_OuterRing", (21.58, 0.07, 8.98), 1.02, 0.05, "DC_Defender", 20)
    cylinder("KeepLight_InnerRune", (21.58, 0.10, 8.98), 0.68, 0.04, "DC_Magic", 16)
    cylinder("KeepLight_GoldAnchor", (21.58, 0.14, 8.98), 0.34, 0.03, "DC_Fire", 12)

    for index in range(6):
        angle = index * math.pi * 2 / 6
        shard = cone("KeepLight_CommandShard",
                     (21.58 + math.cos(angle) * 0.72, 0.28 + 0.05 * (index % 2), 8.98 + math.sin(angle) * 0.72),
                     0.08, 0.018, 0.34 if index % 2 == 0 else 0.24,
                     "DC_Magic" if index % 2 == 0 else "DC_Defender", 5)
        shard.rotation_euler[1] = -angle

    for index, offset in enumerate((0.0, -0.22, 0.22)):
        beam = cylinder("KeepLight_Beacon", (24.24, 3.28 + index * 0.28, 9.0 + offset),
                        0.09 - index * 0.015, 0.58 + index * 0.16, "DC_Magic", 8)
        beam.rotation_euler[0] = math.radians(90)

    for index in range(4):
        angle = math.radians(45 + index * 90)
        ray = cone("KeepLight_CrownRay",
                   (24.24 + math.cos(angle) * 0.26, 4.16, 9.0 + math.sin(angle) * 0.26),
                   0.05, 0.01, 0.44 if index % 2 == 0 else 0.32,
                   "DC_Fire" if index % 2 == 0 else "DC_Magic", 5)
        ray.rotation_euler[1] = -angle

    socket("Socket_VFX", (24.24, 3.34, 9.0), 0.14)
    socket("Socket_Audio", (21.58, 0.16, 8.98), 0.12)


def build_keep_dressing():
    """VIS-407 keep-side dressing: defensive apron, cold crystals and final-stand framing."""
    for index, (x, z, sx, sz, mat_name) in enumerate((
        (20.62, 8.24, 1.48, 0.54, "DC_Navy"),
        (20.74, 10.12, 1.12, 0.42, "DC_Grass"),
        (18.86, 8.92, 1.76, 0.62, "DC_Navy"),
        (23.92, 6.86, 0.82, 0.28, "DC_Earth"),
    )):
        patch = relief_patch(f"KeepDressing_ApronPatch_{index}", (x, 0.136, z), sx, sz,
                             mat_name, seed=260 + index, vertices=8, height=0.005)
        patch.rotation_euler[2] = math.radians(-16 + index * 9)

    for index, (x, z, scale, mat_name) in enumerate((
        (19.18, 7.16, 0.58, "DC_Stone"),
        (19.72, 10.74, 0.62, "DC_Metal"),
        (22.18, 6.98, 0.46, "DC_Stone"),
        (24.86, 11.08, 0.52, "DC_Metal"),
    )):
        sphere("KeepDressing_BoundaryRock", (x, 0.20 + scale * 0.10, z),
               (scale * 0.36, scale * 0.28, scale * 0.40), mat_name)

    for index, (x, z, lean) in enumerate(((20.24, 7.34, 0), (20.38, 10.62, 8), (22.74, 11.04, -10))):
        trunk = cylinder("KeepDressing_SignalPost", (x, 0.42, z), 0.07, 0.84, "DC_Dark", 6)
        trunk.rotation_euler[1] = math.radians(lean)
        banner("KeepDressing_PostBanner", (x, 0.88, z), "DC_Defender", 0.52)

    for index, (x, z, height) in enumerate(((20.98, 8.08, 0.54), (21.28, 9.88, 0.46), (22.16, 10.38, 0.42))):
        crystal = cone("KeepDressing_CoolCrystal", (x, 0.18 + height * 0.5, z),
                       0.11, 0.02, height, "DC_Magic", 5)
        crystal.rotation_euler[1] = math.radians(-14 + index * 11)

    for index, z in enumerate((8.22, 8.98, 9.74)):
        plate = box("KeepDressing_FinalStandPlate", (20.84 + index * 0.42, 0.10, z),
                    (0.34, 0.06, 0.24), "DC_Stone", 0.02)
        plate.rotation_euler[1] = math.radians(12 - index * 6)


def build_boundary_ridge_wall_a():
    """VIS-410 north and east foreground boundary: mixed ridge rocks and tall conifers."""
    ridge = box("BoundaryRidgeWallA_Base", (11.20, 0.34, 12.44), (27.6, 0.42, 0.68), "DC_Metal", 0.09)
    for index, x in enumerate((i * 2.8 for i in range(11))):
        offset = -0.65 + index * 3.0
        height = 0.40 + (index % 3) * 0.09
        sphere(f"BoundaryRidgeWallA_Puck_{index}", (offset + 0.5, 0.18 + height * 0.12, 12.25), (0.28 + 0.05 * (index % 2), height, 0.34), "DC_Stone")
        trunk = cylinder(f"BoundaryRidgeWallA_Pine_{index}", (offset + 0.5, 0.42 + height * 0.3, 12.52 + (index % 3) * 0.06),
                        0.065, 1.25 + 0.12 * (index % 2), "DC_Dark", 7)
        trunk.rotation_euler[1] = math.radians((index - 5) * 7)
        cone(f"BoundaryRidgeWallA_Crown_{index}", (offset + 0.5, 1.0 + height * 0.45, 12.52 + (index % 3) * 0.06),
             0.20 + (index % 2) * 0.03, 0.03, 0.85 + 0.05 * (index % 3), "DC_Grass", 8)
        if index % 2 == 0:
            sphere(f"BoundaryRidgeWallA_Stone_{index}", (offset - 0.36, 0.22 + 0.07 * (index % 2), 12.64),
                   (0.22, 0.18, 0.22), "DC_Metal")


def build_boundary_ridge_wall_b():
    """VIS-410 alternative north ridge silhouette with broken stones and colder tone."""
    ridge = box("BoundaryRidgeWallB_Base", (11.20, 0.31, 12.62), (27.6, 0.34, 0.44), "DC_Stone", 0.07)
    for index, x in enumerate((i * 2.6 for i in range(12))):
        offset = -2.4 + index * 2.5
        base = sphere(f"BoundaryRidgeWallB_Stone_{index}", (offset + 0.5, 0.21, 12.34 + (index % 4) * 0.03),
                      (0.30 + (index % 3) * 0.04, 0.18 + (index % 2) * 0.03, 0.26 + (index % 2) * 0.05), "DC_Metal")
        base.rotation_euler[1] = math.radians(-12 + index * 3)
        if index % 2 == 0:
            trunk = cylinder(f"BoundaryRidgeWallB_Pine_{index}", (offset + 0.52, 0.46, 12.54), 0.06, 0.92, "DC_Dark", 7)
            trunk.rotation_euler[1] = math.radians(18 if index % 4 == 0 else -14)
            cone(f"BoundaryRidgeWallB_Crown_{index}", (offset + 0.5, 0.94, 12.54), 0.18, 0.03, 0.72, "DC_Grass", 8)
            sphere(f"BoundaryRidgeWallB_Bloom_{index}", (offset + 0.16, 0.62, 12.48), (0.05, 0.05, 0.05), "DC_Fire")
        else:
            pillar = cone(f"BoundaryRidgeWallB_Pillar_{index}", (offset + 0.5, 0.47, 12.70), 0.11, 0.025, 0.72, "DC_Metal", 5)
            pillar.rotation_euler[1] = math.radians(-7 * index)


def build_boundary_foreground_strip_a():
    """VIS-410 south foreground frame strip for parallax and route separation."""
    strip = box("BoundaryForegroundA_Base", (11.00, 0.16, 0.76), (28.2, 0.31, 2.08), "DC_Metal", 0.09)
    for index in range(12):
        x = -1.8 + index * 2.4
        z = 0.95 if index % 2 == 0 else 0.44
        stone = box(f"BoundaryForegroundA_Stone_{index}", (x, 0.30, z), (0.45 + (index % 3) * 0.10, 0.2, 0.24), "DC_Stone", 0.0)
        if index % 3 == 0:
            cone(f"BoundaryForegroundA_Spike_{index}", (x + 0.08, 0.49, z - 0.34), 0.12, 0.02, 0.36, "DC_Dark", 6)
        bush = sphere(f"BoundaryForegroundA_Bush_{index}", (x - 0.16, 0.22, z + 0.36), (0.34, 0.24, 0.32), "DC_Forest")
        if index % 2 == 0:
            shard = cone(f"BoundaryForegroundA_Shard_{index}", (x + 0.24, 0.19, z + 0.08), 0.08, 0.02, 0.24, "DC_Grass", 6)


def build_boundary_foreground_strip_b():
    """VIS-410 alternate south foreground strip: flatter but denser silhouette language."""
    strip = box("BoundaryForegroundB_Base", (11.0, 0.17, 0.73), (28.2, 0.28, 1.58), "DC_Stone", 0.08)
    for index in range(11):
        x = -2.0 + index * 2.5
        z = 0.82 + (index % 2) * 0.17
        cluster_x = x + 0.42 * math.cos(index * 0.8)
        stone = sphere(f"BoundaryForegroundB_StoneA_{index}", (cluster_x, 0.24, z), (0.32, 0.17, 0.28), "DC_Metal")
        if index % 2:
            stone2 = sphere(f"BoundaryForegroundB_StoneB_{index}", (cluster_x + 0.22, 0.20, z + 0.19),
                            (0.21, 0.13, 0.21), "DC_Stone")
        bush = sphere(f"BoundaryForegroundB_Bush_{index}", (cluster_x - 0.18, 0.21, z - 0.32), (0.34, 0.24, 0.26), "DC_Grass")
        if index % 3 == 1:
            shard = cone(f"BoundaryForegroundB_BushTop_{index}", (cluster_x + 0.05, 0.34, z - 0.12),
                        0.10, 0.01, 0.28, "DC_Forest", 6)


def build_boundary_silhouette_wall():
    """VIS-410 distant mountain/forest silhouette wall with minimal geometry."""
    for row, (x, z, scale, mat) in enumerate((
        (-2.6, 10.72, 1.2, "DC_Metal"),
        (4.4, 10.95, 1.05, "DC_Stone"),
        (10.2, 10.98, 1.18, "DC_Dark"),
        (17.0, 11.08, 1.10, "DC_Metal"),
        (23.5, 10.92, 0.98, "DC_Stone"),
    )):
        mountain = box(f"BoundarySilhouetteWall_Mass_{row}", (x, 1.40 * scale, z), (2.7 * scale, 1.65 * scale, 1.85), "DC_Dark", 0.05)
        cone(f"BoundarySilhouetteWall_Cap_{row}", (x, 2.52 * scale, z + 0.08), 0.56 * scale, 0.04, 1.42 * scale, mat, 5)
        for index in range(2):
            pine = cylinder(f"BoundarySilhouetteWall_Forest_{row}_{index}", (x + (index - 0.5) * 0.52, 1.95 * scale,
                                                                          z + 0.22 + index * 0.12), 0.07, 1.05 * scale, "DC_Dark", 7)
            if index == row % 2:
                pine.rotation_euler[1] = math.radians(12 if row % 3 == 0 else -14)
            crown = cone(f"BoundarySilhouetteWall_Crown_{row}_{index}",
                        (x + (index - 0.5) * 0.52, 2.62 * scale, z + 0.22 + index * 0.12),
                        0.28 * scale, 0.02, 0.78 * scale, "DC_Grass" if index == 0 else "DC_Forest", 8)


def build_boundary_forest_silhouette():
    """VIS-410 dense distant forest edge with high readability from mid camera angles."""
    strip = box("BoundaryForest_Backing", (12.8, 0.20, 11.22), (27.6, 0.28, 0.65), "DC_Navy", 0.05)
    for index, (x, y, z, tilt) in enumerate((
        (-2.1, 0.0, 11.10, 8),
        (1.2, 0.0, 11.18, -6),
        (4.9, 0.0, 11.22, 5),
        (8.1, 0.0, 11.24, 2),
        (11.4, 0.0, 11.20, -10),
        (15.0, 0.0, 11.27, 4),
        (18.4, 0.0, 11.09, -8),
        (21.8, 0.0, 11.14, 7),
        (24.7, 0.0, 11.20, -4),
    )):
        forest = cone(f"BoundaryForest_Spike_{index}", (x, 0.35, z), 0.15, 0.03, 1.02, "DC_Stone" if index % 2 else "DC_Dark", 6)
        forest.rotation_euler[1] = math.radians(tilt)
        trunk = cylinder(f"BoundaryForest_Trunk_{index}", (x + 0.08, 0.72, z - 0.09), 0.05, 0.95, "DC_Dark", 7)
        trunk.rotation_euler[1] = math.radians(-13)
        leaves = cone(f"BoundaryForest_Leaves_{index}", (x, 1.20, z - 0.04), 0.42, 0.03, 1.18, "DC_Grass", 8)
        leaves.rotation_euler[1] = math.radians(8 - index)
        if index in (1, 4, 7):
            shard = sphere(f"BoundaryForest_Shard_{index}", (x - 0.12, 1.02, z + 0.17), (0.07, 0.07, 0.07), "DC_Fire")


def build_atmosphere_moon_disc():
    """VIS-411 moon element with subtle crater accents for night readability."""
    # Keep this moon very low-poly and small to avoid fill-rate pressure.
    disc = cone("AtmosphereMoonDisc", (-2.7, 0.93, 13.22), 0.74, 0.04, 0.26, "DC_Magic", 16,
                rotation=(math.radians(90), 0, 0))
    disc.location[1] = 0.94
    for index, (x, y, z, radius, depth) in enumerate((
        (-0.28, 0.17, 0.11, 0.16, 0.02),
        (0.24, 0.06, 0.13, 0.11, 0.02),
        (-0.03, -0.25, 0.11, 0.19, 0.016),
        (0.35, -0.29, 0.12, 0.08, 0.016),
    )):
        crater = cone(f"AtmosphereMoonCrater_{index}", (-2.7 + x, 0.96 + y, 13.22 + z),
                      radius, 0.01, depth, "DC_Navy", 10, rotation=(math.radians(90), 0, 0))
    ring = box("AtmosphereMoonRing", (-2.7, 0.90, 13.22), (0.12, 0.04, 0.16), "DC_Dark", 0.0)
    ring.rotation_euler[1] = math.radians(-9)


def build_atmosphere_fog_ribbon():
    """VIS-411 distant fog/rim ribbon for distant depth control."""
    ribbon = box("AtmosphereFogRibbon", (11.0, 0.66, 8.4), (28.2, 0.05, 2.3), "DC_Dark", 0.0)
    ribbon.rotation_euler[2] = 0.0
    for index in range(18):
        phase = index / 18.0
        x = -2.6 + index * 1.62
        z = 8.45 + math.sin(phase * math.pi * 2) * 0.22
        depth = 0.13 + 0.06 * abs(math.cos(phase * 3.7))
        puff = box("AtmosphereFogPuff", (x, 0.70 + depth * 0.12, z),
                  (0.34, 0.01, 0.38 + 0.07 * abs(math.sin(phase * 3.2))), "DC_Navy", 0.0)
        if index % 4 == 0:
            puff.scale = [s * 1.18 for s in puff.scale]


def build_atmosphere_dust_veil():
    """VIS-411 low-cost dust veil candidate for low-height lane readability."""
    for index in range(9):
        x = -0.6 + index * 3.05
        side = 0.35 if index % 2 else -0.32
        y = 0.12 + 0.017 * (index % 3)
        shard = cylinder(f"AtmosphereDustVeil_{index}", (x, y, 0.52 + side), 0.12, 0.04, "DC_Grass", 5)
        shard.rotation_euler[2] = math.radians(-12 + index * 7)
        if index % 2 == 0:
            shard2 = sphere(f"AtmosphereDustFluff_{index}", (x + 0.17, y + 0.028, 0.38 + side),
                            (0.08, 0.06, 0.12), "DC_Earth")


def build_prop_pine_tall_a():
    """VIS-408 north-frame conifer, tall/clean silhouette."""
    cylinder("PropPineTallA_Trunk", (0, 0.52, 0), 0.10, 1.04, "DC_Dark", 7)
    cone("PropPineTallA_CrownLow", (0, 0.96, 0), 0.56, 0.04, 0.94, "DC_Grass", 8)
    cone("PropPineTallA_CrownMid", (0, 1.34, 0), 0.42, 0.03, 0.82, "DC_Forest", 8)
    cone("PropPineTallA_CrownTop", (0, 1.68, 0), 0.26, 0.01, 0.62, "DC_Grass", 8)
    box("PropPineTallA_RootStone", (-0.14, 0.10, 0.06), (0.18, 0.10, 0.14), "DC_Stone", 0.0)


def build_prop_pine_tall_b():
    """VIS-408 north-frame conifer, wider silhouette with slight lean."""
    trunk = cylinder("PropPineTallB_Trunk", (0, 0.56, 0), 0.11, 1.12, "DC_Dark", 7)
    trunk.rotation_euler[1] = math.radians(6)
    cone("PropPineTallB_CrownLow", (0.04, 1.00, 0), 0.62, 0.06, 0.88, "DC_Forest", 8)
    cone("PropPineTallB_CrownMid", (0.07, 1.34, 0), 0.46, 0.03, 0.78, "DC_Grass", 8)
    cone("PropPineTallB_CrownTop", (0.10, 1.66, 0), 0.28, 0.01, 0.54, "DC_Forest", 8)
    box("PropPineTallB_BrokenLimb", (-0.18, 0.92, -0.10), (0.22, 0.05, 0.08), "DC_Dark", 0.0)


def build_prop_rock_cluster_a():
    """VIS-408 cool boundary rock cluster, low and broad."""
    sphere("PropRockClusterA_Main", (0, 0.24, 0), (0.48, 0.34, 0.42), "DC_Stone")
    sphere("PropRockClusterA_Side", (-0.36, 0.16, 0.18), (0.24, 0.18, 0.22), "DC_Metal")
    sphere("PropRockClusterA_Rear", (0.28, 0.14, -0.22), (0.20, 0.14, 0.16), "DC_Stone")


def build_prop_rock_cluster_b():
    """VIS-408 taller jagged rock cluster."""
    sphere("PropRockClusterB_Main", (0, 0.32, 0), (0.40, 0.48, 0.34), "DC_Metal")
    sphere("PropRockClusterB_Front", (0.26, 0.14, 0.20), (0.18, 0.14, 0.18), "DC_Stone")
    sphere("PropRockClusterB_Back", (-0.24, 0.12, -0.18), (0.16, 0.12, 0.14), "DC_Metal")
    box("PropRockClusterB_Shard", (-0.12, 0.42, 0.22), (0.12, 0.26, 0.10), "DC_Stone", 0.0)


def build_prop_bush_round_a():
    """VIS-408 bush family, calm rounded filler silhouette."""
    sphere("PropBushRoundA_Core", (0, 0.32, 0), (0.54, 0.34, 0.42), "DC_Grass")
    sphere("PropBushRoundA_Left", (-0.28, 0.24, 0.12), (0.22, 0.18, 0.18), "DC_Forest")
    sphere("PropBushRoundA_Right", (0.26, 0.22, -0.10), (0.20, 0.16, 0.16), "DC_Grass")
    sphere("PropBushRoundA_Bloom", (0.10, 0.42, 0.04), (0.08, 0.08, 0.08), "DC_Magic")


def build_prop_bush_round_b():
    """VIS-408 bush family, asymmetrical and slightly spikier."""
    sphere("PropBushRoundB_Core", (0, 0.28, 0), (0.48, 0.30, 0.36), "DC_Forest")
    cone("PropBushRoundB_Top", (0.08, 0.54, -0.04), 0.24, 0.04, 0.34, "DC_Grass", 6)
    sphere("PropBushRoundB_Left", (-0.24, 0.20, 0.16), (0.18, 0.14, 0.16), "DC_Forest")
    sphere("PropBushRoundB_Right", (0.30, 0.22, -0.12), (0.16, 0.12, 0.16), "DC_Grass")


def build_prop_grass_patch_a():
    """VIS-408 readable grass clump with low screen noise."""
    for i, (x, z, h) in enumerate(((-0.18, -0.04, 0.38), (-0.04, 0.08, 0.46), (0.10, -0.02, 0.34), (0.22, 0.10, 0.30))):
        blade = cone("PropGrassPatchA_Blade", (x, 0.12 + h * 0.5, z), 0.06, 0.01, h, "DC_Grass", 5)
        blade.rotation_euler[1] = math.radians(-14 + i * 10)
    sphere("PropGrassPatchA_Flower", (0.06, 0.36, 0.02), (0.05, 0.05, 0.05), "DC_Fire")


def build_prop_grass_patch_b():
    """VIS-408 slightly taller grass with cooler accent."""
    for i, (x, z, h) in enumerate(((-0.20, 0.02, 0.42), (-0.06, -0.08, 0.36), (0.10, 0.04, 0.52), (0.22, -0.04, 0.40), (0.02, 0.14, 0.32))):
        blade = cone("PropGrassPatchB_Blade", (x, 0.12 + h * 0.5, z), 0.05, 0.008, h,
                     "DC_Forest" if i % 2 == 0 else "DC_Grass", 5)
        blade.rotation_euler[1] = math.radians(10 - i * 8)
    sphere("PropGrassPatchB_Bloom", (-0.02, 0.40, 0.06), (0.05, 0.05, 0.05), "DC_Magic")


def build_prop_crystal_cluster_a():
    """VIS-408 small cool crystal cluster for keep-side and ridge accents."""
    cone("PropCrystalClusterA_Main", (0, 0.34, 0), 0.16, 0.03, 0.68, "DC_Magic", 5)
    cone("PropCrystalClusterA_SideA", (-0.18, 0.24, 0.10), 0.10, 0.02, 0.42, "DC_Defender", 5)
    cone("PropCrystalClusterA_SideB", (0.18, 0.22, -0.08), 0.09, 0.02, 0.36, "DC_Magic", 5)
    sphere("PropCrystalClusterA_Base", (0, 0.10, 0), (0.22, 0.12, 0.18), "DC_Stone")


def build_prop_crystal_cluster_b():
    """VIS-408 warmer mixed crystal/rock cluster for gate transition zones."""
    cone("PropCrystalClusterB_Main", (0, 0.28, 0), 0.14, 0.03, 0.56, "DC_Fire", 5)
    cone("PropCrystalClusterB_Side", (0.16, 0.22, 0.12), 0.10, 0.02, 0.38, "DC_Enemy", 5)
    sphere("PropCrystalClusterB_BaseA", (-0.10, 0.11, -0.08), (0.20, 0.12, 0.16), "DC_Stone")
    sphere("PropCrystalClusterB_BaseB", (0.18, 0.09, 0.00), (0.14, 0.10, 0.12), "DC_Metal")


def shadow(radius=0.58, mat_name="DC_Dark"):
    cylinder("BlobShadow", (0, 0.025, 0), radius, 0.025, mat_name, 16)


def add_humanoid_rig():
    """Add the shared lightweight mobile humanoid skeleton to every actor source."""
    rig_data = bpy.data.armatures.new("DC_HumanoidRig")
    rig = bpy.data.objects.new("DC_HumanoidRig", rig_data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bones = {
        "Root": ((0, 0, 0), (0, 0, 0.55)),
        "Spine": ((0, 0, 0.55), (0, 0, 1.2)),
        "Head": ((0, 0, 1.2), (0, 0, 1.7)),
        "Arm.L": ((0, 0, 1.08), (-0.55, 0, 0.9)),
        "Arm.R": ((0, 0, 1.08), (0.55, 0, 0.9)),
        "Leg.L": ((-0.13, 0, 0.58), (-0.13, 0, 0.05)),
        "Leg.R": ((0.13, 0, 0.58), (0.13, 0, 0.05)),
    }
    created = {}
    for name, (head, tail) in bones.items():
        bone = rig_data.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        created[name] = bone
    created["Spine"].parent = created["Root"]
    for name in ("Head", "Arm.L", "Arm.R"):
        created[name].parent = created["Spine"]
    for name in ("Leg.L", "Leg.R"):
        created[name].parent = created["Root"]
    bpy.ops.object.mode_set(mode="OBJECT")

    # Rigid-parent the modular low-poly pieces to meaningful bones.  This keeps
    # the source inexpensive (no skin weights are required) while allowing the
    # Unity role clips to articulate limbs instead of moving only the actor root.
    for obj in list(bpy.context.scene.objects):
        if obj == rig or obj.type not in {"MESH", "EMPTY"}:
            continue

        name = obj.name
        x = obj.matrix_world.translation.x
        z = obj.matrix_world.translation.z
        bone_name = "Root"
        if name.startswith("Socket_"):
            if name in {"Socket_Head"}:
                bone_name = "Head"
            elif name in {"Socket_Hand_L"}:
                bone_name = "Arm.L"
            elif name in {"Socket_Hand_R", "Socket_Weapon_Tip"}:
                bone_name = "Arm.R" if x >= 0 else "Arm.L"
            elif name == "Socket_Muzzle":
                bone_name = "Arm.R"
            elif name not in {"Socket_Root", "Socket_Ground"}:
                bone_name = "Spine"
        elif name != "BlobShadow":
            if name.startswith("Ram"):
                bone_name = "Arm.R"
            elif name.startswith("SiegeRearCounterweight"):
                bone_name = "Arm.L"
            elif z >= 1.25 and abs(x) < 0.30:
                bone_name = "Head"
            elif z >= 0.64 and abs(x) >= 0.25:
                bone_name = "Arm.L" if x < 0 else "Arm.R"
            elif z < 0.64 and abs(x) >= 0.08:
                bone_name = "Leg.L" if x < 0 else "Leg.R"
            else:
                bone_name = "Spine"

        world_matrix = obj.matrix_world.copy()
        obj.parent = rig
        obj.parent_type = "BONE"
        obj.parent_bone = bone_name
        obj.matrix_world = world_matrix
    return rig


def banner(prefix, location, color="DC_Defender", scale=1.0):
    cylinder(f"{prefix}_Pole", location, 0.035 * scale, 1.15 * scale, "DC_Stone", 8)
    x, y, z = location
    cloth = box(f"{prefix}_Cloth", (x + 0.23 * scale, y + 0.32 * scale, z),
                (0.42 * scale, 0.36 * scale, 0.06 * scale), color, 0.025)
    cloth.rotation_euler[1] = math.radians(-5)


def socket(name, location, size=0.12):
    empty = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(empty)
    empty.empty_display_type = "SPHERE"
    empty.empty_display_size = size
    empty.location = unity_location(location)
    return empty


def add_actor_sockets(kind, scale):
    """Add runtime presentation sockets without giving gameplay authority to the mesh."""
    socket("Socket_Root", (0, 0, 0), 0.14 * scale)
    socket("Socket_Ground", (0, 0.04 * scale, 0), 0.12 * scale)
    socket("Socket_Chest", (0, 1.08 * scale, -0.03 * scale), 0.12 * scale)
    socket("Socket_Head", (0, 1.76 * scale, 0), 0.11 * scale)

    if kind == "vanguard":
        socket("Socket_Hand_L", (-0.34 * scale, 1.05 * scale, -0.06 * scale), 0.1 * scale)
        socket("Socket_Hand_R", (0.34 * scale, 1.00 * scale, -0.02 * scale), 0.1 * scale)
        socket("Socket_Weapon_Tip", (0.72 * scale, 1.70 * scale, -0.04 * scale), 0.1 * scale)
    elif kind in ("knight", "guard", "boss"):
        socket("Socket_Hand_L", (-0.42 * scale, 0.94 * scale, -0.08 * scale), 0.1 * scale)
        socket("Socket_Hand_R", (0.45 * scale, 1.02 * scale, 0), 0.1 * scale)
        socket("Socket_Weapon_Tip", (0.66 * scale, 1.57 * scale, 0), 0.1 * scale)
    elif kind == "raider":
        socket("Socket_Hand_R", (0.36 * scale, 0.92 * scale, -0.03 * scale), 0.1 * scale)
        socket("Socket_Weapon_Tip", (0.46 * scale, 1.18 * scale, -0.06 * scale), 0.1 * scale)
    elif kind == "support":
        socket("Socket_Hand_R", (0.42 * scale, 1.0 * scale, 0), 0.1 * scale)
        socket("Socket_Cast", (0.42 * scale, 1.65 * scale, 0), 0.11 * scale)
        socket("Socket_Weapon_Tip", (0.42 * scale, 1.72 * scale, 0), 0.1 * scale)

    if kind in ("regenerator", "boss"):
        socket("Socket_Back", (0, 1.15 * scale, 0.24 * scale), 0.11 * scale)
        socket("Socket_Cast", (0, 1.50 * scale, -0.16 * scale), 0.11 * scale)
        socket("Socket_Core", (0, 1.02 * scale, -0.12 * scale), 0.11 * scale)


def add_siege_sockets():
    socket("Socket_Root", (0, 0, 0), 0.16)
    socket("Socket_Ground", (0, 0.05, 0), 0.13)
    socket("Socket_Chest", (0, 0.72, -0.08), 0.13)
    socket("Socket_Muzzle", (0, 0.78, -1.88), 0.13)
    socket("Socket_Core", (0, 0.60, 0.18), 0.13)


def build_environment():
    box("ValleyGround", (11, -0.42, 5.5), (28, 0.55, 14.2), "DC_Forest", 0.08)
    box("DeepBackdrop", (11, -0.72, 5.5), (33, 0.18, 19), "DC_Navy")
    box("NorthRidge", (11, 0.03, 12.68), (28, 0.88, 1.15), "DC_Metal", 0.16)
    box("SouthRidge", (11, 0.03, -1.68), (28, 0.88, 1.15), "DC_Metal", 0.16)
    path = [(-2, 2), (4, 2), (4, 8), (10, 8), (10, 3.5), (16, 3.5), (16, 9), (24, 9)]
    for i, (a, b) in enumerate(zip(path, path[1:])):
        ax, az = a
        bx, bz = b
        dx, dz = bx - ax, bz - az
        length = math.hypot(dx, dz)
        angle = math.atan2(dx, dz)
        edge = box(f"RoadEdge_{i}", ((ax + bx) / 2, -0.035, (az + bz) / 2),
                   (1.98, 0.23, length + 1.7), "DC_Stone", 0.11)
        edge.rotation_euler[2] = -angle
        road = box(f"Road_{i}", ((ax + bx) / 2, 0.075, (az + bz) / 2),
                   (1.52, 0.14, length + 1.34), "DC_Earth", 0.07)
        road.rotation_euler[2] = -angle

    # Deterministic broad ground variation: large enough to read from the fixed mobile camera.
    grass_patches = [
        (0.4, 0.018, 4.8, 2.3, 1.25), (2.5, 0.018, 9.7, 2.6, 1.4),
        (6.7, 0.018, 2.8, 2.2, 1.05), (7.4, 0.018, 10.8, 2.7, 1.0),
        (11.8, 0.018, 6.2, 2.4, 1.2), (13.4, 0.018, 1.25, 2.8, 0.8),
        (17.4, 0.018, 6.5, 2.2, 1.1), (20.5, 0.018, 2.3, 2.7, 1.0),
        (21.1, 0.018, 10.8, 2.2, 0.85), (5.0, 0.018, 5.1, 1.5, 0.75),
    ]
    for i, (x, y, z, sx, sz) in enumerate(grass_patches):
        patch = irregular_patch(f"GrassPatch_{i}", (x, y, z), sx * 0.5, sz * 0.5,
                                "DC_Grass", seed=i)
        patch.rotation_euler[2] = math.radians((i * 19) % 35 - 17)

    # Defender keep
    box("KeepMain", (24.1, 1.0, 9), (2.35, 2.0, 3.0), "DC_Stone", 0.12)
    for level in range(3):
        for z in (8.2, 9.0, 9.8):
            box("KeepMasonry", (22.89, 0.38 + level * 0.48, z + (level % 2) * 0.17),
                (0.10, 0.16, 0.48), "DC_Metal", 0.025)
    for z in (7.65, 10.35):
        cylinder("KeepTower", (23.45, 1.42, z), 0.72, 2.8, "DC_Stone", 10)
        cone("KeepRoof", (23.45, 3.05, z), 0.9, 0.12, 0.95, "DC_Defender", 10)
        for y in (0.85, 1.52):
            box("KeepWindow", (22.72, y, z), (0.07, 0.24, 0.16), "DC_Fire", 0.02)
    banner("KeepBanner", (22.95, 3.35, 9), "DC_Defender", 1.25)
    box("KeepDoor", (22.88, 0.72, 9), (0.08, 1.15, 0.72), "DC_Navy", 0.08)

    # Enemy gate
    box("GateBase", (-2.35, 0.2, 2), (1.65, 0.4, 2.7), "DC_Dark", 0.06)
    for z in (1.12, 2.88):
        box("GatePillar", (-2.52, 1.05, z), (0.58, 2.1, 0.58), "DC_Enemy", 0.08)
        cone("GateSpike", (-2.52, 2.32, z), 0.42, 0.02, 0.55, "DC_Fire", 6)
    box("GateBeam", (-2.52, 2.05, 2), (0.66, 0.44, 2.35), "DC_Enemy", 0.06)
    for y in (0.58, 1.25, 1.92):
        box("GateBrace", (-2.84, y, 2), (0.11, 0.12, 1.65), "DC_Metal", 0.025)
    for z in (1.45, 2.0, 2.55):
        sphere("GateEmber", (-2.9, 0.72, z), (0.09, 0.18, 0.09), "DC_Fire")

    # Boss arena runes near entrance
    for i in range(8):
        angle = i * math.pi / 4
        sphere("BossRune", (-0.1 + math.cos(angle) * 1.75, 0.08, 2 + math.sin(angle) * 1.75),
               (0.13, 0.06, 0.24), "DC_Fire")

    for i in range(20):
        x = -0.5 + (i * 1.31) % 27
        z = -1.1 if i % 2 == 0 else 12.1
        sphere("BoundaryRock", (x, 0.22, z), (0.34 + (i % 3) * 0.1, 0.3, 0.38),
               "DC_Metal" if i % 3 == 0 else "DC_Stone")
    for i in range(14):
        x = 0.6 + (i * 1.78) % 23
        z = 0.55 if i % 3 == 0 else 11.25
        cylinder("PineTrunk", (x, 0.3, z), 0.09, 0.6, "DC_Dark", 7)
        cone("PineCrownA", (x, 0.8, z), 0.48, 0.03, 0.9, "DC_Grass", 8)
        cone("PineCrownB", (x, 1.18, z), 0.34, 0.01, 0.72, "DC_Forest", 8)

    # Small readable clusters, kept away from the navigable road.
    for i, (x, z) in enumerate(((1.0, 4.2), (5.8, 10.7), (8.7, 1.1), (12.5, 6.4),
                                (15.0, 11.0), (18.6, 1.2), (21.8, 6.0))):
        for j in range(3):
            cone(f"GrassBlade_{i}_{j}", (x + j * 0.13, 0.18, z + (j % 2) * 0.1),
                 0.07, 0.01, 0.34 + j * 0.05, "DC_Grass", 5)
        sphere(f"Wildflower_{i}", (x + 0.16, 0.33, z + 0.05), (0.06, 0.06, 0.06),
               "DC_Magic" if i % 2 == 0 else "DC_Fire")


def build_slot():
    cylinder("SlotStone", (0, 0.1, 0), 0.62, 0.2, "DC_Stone", 12)
    cylinder("SlotInset", (0, 0.215, 0), 0.46, 0.035, "DC_Magic", 12)
    for i in range(4):
        angle = i * math.pi / 2
        box("SlotRune", (math.cos(angle) * 0.47, 0.25, math.sin(angle) * 0.47),
            (0.12, 0.04, 0.22), "DC_Defender", 0.02).rotation_euler[1] = -angle


def build_slot_state(state):
    """Build one world-space outpost slot state candidate.

    States must remain readable without color. Each state changes geometry shape,
    not only material:

    - Empty: low calm rim and four small stone ticks.
    - Selected: raised corner brackets and a brighter inner focus.
    - Available: upward arrows and friendly outer ticks.
    - Insufficient: broken warning shards and amber cost marks.
    - Locked: crossed dark bars and four blocking posts.
    """
    cylinder("SlotStateBaseStone", (0, 0.08, 0), 0.66, 0.16, "DC_Stone", 12)
    cylinder("SlotStateInnerDisc", (0, 0.19, 0), 0.45, 0.035,
             "DC_Magic" if state in {"selected", "available"} else "DC_Dark", 12)

    for i in range(4):
        angle = i * math.pi / 2
        tick = box("SlotStateTick", (math.cos(angle) * 0.50, 0.23, math.sin(angle) * 0.50),
                   (0.10, 0.04, 0.21), "DC_Stone", 0.018)
        tick.rotation_euler[1] = -angle

    if state == "empty":
        for i in range(4):
            angle = math.radians(45 + i * 90)
            box("SlotEmptyLowTick", (math.cos(angle) * 0.39, 0.245, math.sin(angle) * 0.39),
                (0.08, 0.025, 0.14), "DC_Defender", 0.012).rotation_euler[1] = -angle
    elif state == "selected":
        cylinder("SlotSelectedFocus", (0, 0.245, 0), 0.34, 0.035, "DC_Defender", 12)
        for x in (-0.46, 0.46):
            for z in (-0.46, 0.46):
                box("SlotSelectedCornerA", (x, 0.34, z), (0.20, 0.08, 0.06), "DC_Magic", 0.0)
                box("SlotSelectedCornerB", (x, 0.34, z), (0.06, 0.08, 0.20), "DC_Magic", 0.0)
    elif state == "available":
        cylinder("SlotAvailablePulse", (0, 0.255, 0), 0.53, 0.035, "DC_Forest", 12)
        for i in range(4):
            angle = math.radians(45 + i * 90)
            arrow = cone("SlotAvailableArrow", (math.cos(angle) * 0.39, 0.36, math.sin(angle) * 0.39),
                         0.09, 0.015, 0.20, "DC_Forest", 5)
            arrow.rotation_euler[1] = -angle
    elif state == "insufficient":
        for i in range(6):
            angle = i * math.pi / 3
            shard = box("SlotInsufficientShard", (math.cos(angle) * 0.41, 0.31, math.sin(angle) * 0.41),
                        (0.08, 0.16 if i % 2 == 0 else 0.09, 0.06), "DC_Fire", 0.0)
            shard.rotation_euler[1] = -angle
        box("SlotInsufficientSlashA", (-0.14, 0.31, 0), (0.09, 0.07, 0.78), "DC_Fire", 0.0).rotation_euler[1] = math.radians(34)
        box("SlotInsufficientSlashB", (0.16, 0.30, 0), (0.06, 0.06, 0.64), "DC_Fire", 0.0).rotation_euler[1] = math.radians(34)
    elif state == "locked":
        for x in (-0.36, 0.36):
            for z in (-0.36, 0.36):
                cylinder("SlotLockedPost", (x, 0.33, z), 0.055, 0.34, "DC_Dark", 6)
        bar_a = box("SlotLockedBarA", (0, 0.40, 0), (0.14, 0.09, 1.05), "DC_Dark", 0.02)
        bar_b = box("SlotLockedBarB", (0, 0.405, 0), (0.14, 0.09, 1.05), "DC_Dark", 0.02)
        bar_a.rotation_euler[1] = math.radians(45)
        bar_b.rotation_euler[1] = math.radians(-45)
    else:
        raise ValueError(f"Unknown slot state: {state}")


def build_vfx_build_dust():
    """Low-poly dust puffs used to hide the first frame of a newly built outpost."""
    cylinder("BuildDustGroundSoftener", (0, 0.018, 0), 0.82, 0.035, "DC_Earth", 16)
    for i in range(10):
        angle = i * math.pi * 2 / 10
        radius = 0.32 + 0.045 * (i % 3)
        height = 0.09 + 0.025 * (i % 2)
        puff = sphere("BuildDustPuff", (math.cos(angle) * radius, 0.08 + height * 0.5, math.sin(angle) * radius),
                      (0.11 + 0.025 * (i % 2), height, 0.09 + 0.02 * (i % 3)), "DC_Earth")
        puff.rotation_euler[1] = -angle
    for i in range(6):
        angle = math.radians(30 + i * 60)
        shard = box("BuildDustStoneChip", (math.cos(angle) * 0.47, 0.09, math.sin(angle) * 0.47),
                    (0.10, 0.045, 0.055), "DC_Stone", 0.0)
        shard.rotation_euler[1] = -angle


def build_vfx_build_ring():
    """Blueprint-style build ring: readable as construction, not an attack impact."""
    cylinder("BuildRingOuter", (0, 0.04, 0), 0.74, 0.035, "DC_Magic", 18)
    cylinder("BuildRingInnerCutLanguage", (0, 0.065, 0), 0.54, 0.025, "DC_Dark", 18)
    for i in range(8):
        angle = i * math.pi / 4
        tick = box("BuildRingTick", (math.cos(angle) * 0.62, 0.12, math.sin(angle) * 0.62),
                   (0.08, 0.07, 0.22), "DC_Defender", 0.0)
        tick.rotation_euler[1] = -angle
    for i in range(4):
        angle = math.radians(45 + i * 90)
        corner = box("BuildRingCorner", (math.cos(angle) * 0.44, 0.17, math.sin(angle) * 0.44),
                     (0.16, 0.07, 0.08), "DC_Magic", 0.0)
        corner.rotation_euler[1] = -angle


def build_vfx_build_spark():
    """Small upward shards used for the last construction beat."""
    for i in range(9):
        angle = i * math.pi * 2 / 9
        radius = 0.18 + 0.045 * (i % 3)
        height = 0.24 + 0.07 * (i % 3)
        shard = cone("BuildSparkShard", (math.cos(angle) * radius, 0.22 + height * 0.5,
                                         math.sin(angle) * radius),
                     0.055, 0.012, height, "DC_Fire" if i % 3 == 0 else "DC_Magic", 5)
        shard.rotation_euler[1] = -angle
    cylinder("BuildSparkCore", (0, 0.22, 0), 0.13, 0.32, "DC_Magic", 8)


def build_vfx_upgrade_anchor():
    """Blue-gold anchor ring that keeps the player's eye on the unchanged slot center."""
    cylinder("UpgradeAnchorOuterRing", (0, 0.045, 0), 0.78, 0.04, "DC_Defender", 18)
    cylinder("UpgradeAnchorInnerRing", (0, 0.078, 0), 0.50, 0.03, "DC_Magic", 16)
    for i in range(6):
        angle = i * math.pi / 3
        tick = box("UpgradeAnchorTick", (math.cos(angle) * 0.62, 0.16, math.sin(angle) * 0.62),
                   (0.09, 0.08, 0.24), "DC_Fire" if i % 2 == 0 else "DC_Magic", 0.0)
        tick.rotation_euler[1] = -angle
    for i in range(3):
        angle = math.radians(30 + i * 120)
        wedge = cone("UpgradeAnchorWedge", (math.cos(angle) * 0.36, 0.20, math.sin(angle) * 0.36),
                     0.11, 0.03, 0.28, "DC_Fire", 5)
        wedge.rotation_euler[1] = -angle


def build_vfx_upgrade_column():
    """Vertical upgrade lift cue: reads as level-up rather than build dust."""
    cylinder("UpgradeColumnCore", (0, 0.62, 0), 0.16, 1.05, "DC_Magic", 10)
    cylinder("UpgradeColumnInnerGold", (0, 0.62, 0), 0.075, 1.18, "DC_Fire", 8)
    for i in range(6):
        angle = i * math.pi * 2 / 6
        shard = cone("UpgradeColumnShard", (math.cos(angle) * 0.26, 0.43 + 0.09 * (i % 3),
                                            math.sin(angle) * 0.26),
                     0.06, 0.012, 0.44, "DC_Magic" if i % 2 else "DC_Fire", 5)
        shard.rotation_euler[1] = -angle
    for i in range(4):
        angle = math.radians(45 + i * 90)
        bar = box("UpgradeColumnBrace", (math.cos(angle) * 0.22, 0.18, math.sin(angle) * 0.22),
                  (0.07, 0.07, 0.34), "DC_Defender", 0.0)
        bar.rotation_euler[1] = -angle


def build_vfx_upgrade_crest():
    """Final crown flash for L2 arrival; small, bright, and non-blocking."""
    for i in range(8):
        angle = i * math.pi / 4
        shard = cone("UpgradeCrestRay", (math.cos(angle) * 0.24, 0.72, math.sin(angle) * 0.24),
                     0.045, 0.008, 0.52 if i % 2 == 0 else 0.38,
                     "DC_Fire" if i % 2 == 0 else "DC_Magic", 5)
        shard.rotation_euler[1] = -angle
    cylinder("UpgradeCrestCrown", (0, 0.66, 0), 0.24, 0.12, "DC_Defender", 8)
    sphere("UpgradeCrestCore", (0, 0.82, 0), (0.12, 0.12, 0.12), "DC_Fire")


def build_vfx_sell_ring():
    """Neutral/gold refund ring for sell/demolition; deliberately flatter than upgrade."""
    cylinder("SellRingOuter", (0, 0.045, 0), 0.70, 0.035, "DC_Fire", 18)
    cylinder("SellRingInner", (0, 0.075, 0), 0.46, 0.025, "DC_Earth", 16)
    for i in range(6):
        angle = i * math.pi / 3
        tick = box("SellRingRefundTick", (math.cos(angle) * 0.57, 0.13, math.sin(angle) * 0.57),
                   (0.08, 0.06, 0.20), "DC_Fire", 0.0)
        tick.rotation_euler[1] = -angle
    for i in range(3):
        angle = math.radians(60 + i * 120)
        coin = cylinder("SellRingCoinMark", (math.cos(angle) * 0.34, 0.17, math.sin(angle) * 0.34),
                        0.055, 0.025, "DC_Fire", 8, rotation=(math.pi / 2, 0, 0))
        coin.rotation_euler[1] = -angle


def build_vfx_sell_chips():
    """Downward dismantle chips; reads as removal, not enemy death or damage burst."""
    cylinder("SellChipGroundFade", (0, 0.018, 0), 0.64, 0.032, "DC_Dark", 14)
    for i in range(10):
        angle = i * math.pi * 2 / 10
        radius = 0.20 + 0.04 * (i % 4)
        chip = box("SellDismantleChip", (math.cos(angle) * radius, 0.18 + 0.035 * (i % 3),
                                         math.sin(angle) * radius),
                   (0.08 + 0.02 * (i % 2), 0.055, 0.12), "DC_Stone", 0.0)
        chip.rotation_euler[1] = -angle
    for i in range(4):
        angle = math.radians(45 + i * 90)
        plank = box("SellDismantlePlank", (math.cos(angle) * 0.30, 0.10, math.sin(angle) * 0.30),
                    (0.07, 0.045, 0.36), "DC_Earth", 0.0)
        plank.rotation_euler[1] = -angle


def build_vfx_sell_glint():
    """Small refund glint: gold/economy feedback, short and non-combat."""
    for i in range(5):
        angle = i * math.pi * 2 / 5
        coin = cylinder("SellGlintCoin", (math.cos(angle) * 0.16, 0.30 + 0.035 * (i % 2),
                                          math.sin(angle) * 0.16),
                        0.075, 0.025, "DC_Fire", 8, rotation=(math.pi / 2, 0, 0))
        coin.rotation_euler[1] = -angle
    for i in range(6):
        angle = i * math.pi / 3
        ray = cone("SellGlintRay", (math.cos(angle) * 0.18, 0.42, math.sin(angle) * 0.18),
                   0.035, 0.006, 0.28 if i % 2 == 0 else 0.20, "DC_Fire", 5)
        ray.rotation_euler[1] = -angle
    sphere("SellGlintCore", (0, 0.38, 0), (0.08, 0.08, 0.08), "DC_Fire")


def build_command():
    cylinder("CommandStone", (0, 0.08, 0), 0.42, 0.16, "DC_Stone", 10)
    cylinder("CommandGlow", (0, 0.18, 0), 0.31, 0.03, "DC_Magic", 10)
    banner("CommandBanner", (-0.18, 0.68, 0), "DC_Defender", 0.78)


def build_humanoid(kind):
    specs = {
        "knight": ("DC_Defender", "DC_Stone", 1.0),
        "guard": ("DC_Stone", "DC_Defender", 0.78),
        "raider": ("DC_Enemy", "DC_Dark", 0.72),
        "vanguard": ("DC_Dark", "DC_Enemy", 1.05),
        "regenerator": ("DC_Dark", "DC_RegenCore", 1.02),
        "support": ("DC_SupportCloth", "DC_Magic", 0.88),
        "boss": ("DC_Enemy", "DC_Fire", 1.55),
    }
    body, accent, scale = specs[kind]
    stone = "DC_Stone"
    metal = "DC_Metal"
    fire = "DC_Fire"
    dark = "DC_Dark"

    if kind == "knight":
        accent = "DC_Fire"
        stone = body
        metal = accent
        fire = accent
        dark = body
    elif kind == "guard":
        stone = body
        metal = accent
        fire = accent
        dark = body
    elif kind == "raider":
        stone = accent
        metal = accent
        fire = body
        dark = accent
    elif kind == "vanguard":
        stone = body
        metal = body
        fire = accent
        dark = body
    elif kind == "regenerator":
        stone = body
        metal = accent
        fire = accent
        dark = body
    elif kind == "support":
        stone = body
        metal = accent
        fire = accent
        dark = body
    elif kind == "boss":
        stone = body
        metal = accent
        fire = accent
        dark = body

    shadow(0.48 * scale, dark)
    body_radius = 0.34 * scale if kind == "vanguard" else 0.28 * scale
    body_depth = 0.82 * scale if kind == "vanguard" else 0.74 * scale
    shoulder_width = 0.52 * scale if kind == "vanguard" else 0.42 * scale
    if kind == "regenerator":
        body_radius = 0.30 * scale
        body_depth = 0.78 * scale
        shoulder_width = 0.36 * scale
    if kind == "support":
        body_radius = 0.24 * scale
        body_depth = 0.82 * scale
        shoulder_width = 0.34 * scale
    belt_mat = accent
    shoulder_mat = accent
    arm_mat = accent
    boot_mat = accent
    visor_mat = metal
    if kind == "regenerator":
        belt_mat = body
        shoulder_mat = body
        arm_mat = body
        boot_mat = body
        visor_mat = accent
    if kind == "support":
        belt_mat = accent
        shoulder_mat = body
        arm_mat = body
        boot_mat = body
        visor_mat = accent
    cylinder("Body", (0, 0.82 * scale, 0), body_radius, body_depth, body, 8)
    cylinder("Belt", (0, 0.69 * scale, 0), body_radius + 0.025 * scale, 0.13 * scale, belt_mat, 8)
    sphere("Shoulders", (0, 1.12 * scale, 0), (shoulder_width, 0.24 * scale, 0.28 * scale), shoulder_mat)
    sphere("Head", (0, 1.46 * scale, 0), (0.25 * scale, 0.28 * scale, 0.23 * scale), stone)
    cone("Helmet", (0, 1.7 * scale, 0), 0.3 * scale, 0.06 * scale, 0.38 * scale, body, 8)
    box("Visor", (0, 1.49 * scale, -0.225 * scale),
        (0.34 * scale, 0.08 * scale, 0.055 * scale), visor_mat, 0.015 * scale)
    for side in (-1, 1):
        cylinder("Arm", (side * 0.36 * scale, 0.92 * scale, 0), 0.09 * scale, 0.58 * scale,
                 arm_mat, 7, rotation=(0, 0, math.radians(side * 12)))
        cylinder("Leg", (side * 0.15 * scale, 0.38 * scale, 0), 0.1 * scale, 0.58 * scale, dark, 7)
        box("Boot", (side * 0.15 * scale, 0.10 * scale, -0.055 * scale),
            (0.21 * scale, 0.17 * scale, 0.32 * scale), boot_mat, 0.035 * scale)

    if kind == "knight":
        box("Shield", (-0.42 * scale, 0.88 * scale, -0.08 * scale),
            (0.12 * scale, 0.66 * scale, 0.52 * scale), accent, 0.05 * scale)
        cylinder("Weapon", (0.45 * scale, 1.02 * scale, 0), 0.035 * scale, 0.95 * scale, fire, 6,
                 rotation=(0, 0, math.radians(-22)))
        cone("WeaponBlade", (0.62 * scale, 1.42 * scale, 0), 0.10 * scale, 0.015,
             0.34 * scale, metal, 5, rotation=(0, 0, math.radians(-22)))
    if kind == "guard":
        # A tall spear line reads before colour at gameplay distance and separates
        # the guard from the Knight's broad shield-and-sword silhouette.
        cylinder("GuardSpearShaft", (0.43 * scale, 1.18 * scale, 0),
                 0.030 * scale, 1.82 * scale, body, 7)
        cone("GuardSpearTip", (0.43 * scale, 2.18 * scale, 0),
             0.12 * scale, 0.018 * scale, 0.42 * scale, accent, 5)
        box("GuardBuckler", (-0.34 * scale, 0.94 * scale, -0.10 * scale),
            (0.10 * scale, 0.42 * scale, 0.30 * scale), accent, 0.04 * scale)
    if kind == "raider":
        # Twin outward blades and a trailing scarf form an agile, forward-pointing
        # wedge instead of a colour-swapped defender body.
        for side in (-1, 1):
            cylinder("RaiderGrip", (side * 0.38 * scale, 0.86 * scale, -0.03 * scale),
                     0.035 * scale, 0.34 * scale, dark, 6,
                     rotation=(math.radians(12), 0, math.radians(side * 28)))
            cone("RaiderBlade", (side * 0.51 * scale, 0.67 * scale, -0.08 * scale),
                 0.13 * scale, 0.018 * scale, 0.56 * scale, body, 5,
                 rotation=(math.radians(16), 0, math.radians(side * 28)))
        scarf = box("RaiderScarf", (0, 1.27 * scale, 0.24 * scale),
                    (0.20 * scale, 0.12 * scale, 0.72 * scale), body, 0.025 * scale)
        scarf.rotation_euler[0] = math.radians(-18)
    if kind == "boss":
        # The boss base profile is deliberately asymmetric and weapon-heavy; phase
        # shards then expand it further without scaling the whole actor.
        box("BossCorePlate", (0, 1.04 * scale, -0.20 * scale),
            (0.52 * scale, 0.56 * scale, 0.11 * scale), accent, 0.045 * scale)
        box("BossLeftPauldron", (-0.54 * scale, 1.28 * scale, 0),
            (0.42 * scale, 0.34 * scale, 0.42 * scale), body, 0.07 * scale)
        cylinder("BossCleaverHaft", (0.48 * scale, 1.02 * scale, 0),
                 0.055 * scale, 1.28 * scale, body, 7,
                 rotation=(0, 0, math.radians(-26)))
        box("BossCleaverHead", (0.77 * scale, 1.56 * scale, 0),
            (0.40 * scale, 0.62 * scale, 0.18 * scale), accent, 0.055 * scale)
        cone("BossCleaverSpike", (0.93 * scale, 1.91 * scale, 0),
             0.14 * scale, 0.02 * scale, 0.44 * scale, accent, 5,
             rotation=(0, 0, math.radians(-26)))
    if kind == "vanguard":
        box("ChestPlate", (0, 1.03 * scale, -0.03 * scale),
            (0.56 * scale, 0.42 * scale, 0.10 * scale), accent, 0.035 * scale)
        cylinder("GreatWeaponHandle", (0.34 * scale, 1.08 * scale, -0.02 * scale),
                 0.045 * scale, 1.25 * scale, body, 6, rotation=(0, 0, math.radians(-32)))
        box("GreatWeaponHead", (0.67 * scale, 1.55 * scale, -0.04 * scale),
            (0.32 * scale, 0.38 * scale, 0.18 * scale), accent, 0.035 * scale)
        cone("GreatWeaponSpike", (0.82 * scale, 1.78 * scale, -0.04 * scale),
             0.12 * scale, 0.018 * scale, 0.34 * scale, accent, 5, rotation=(0, 0, math.radians(-32)))
    if kind == "support":
        box("OpenMantle", (0, 1.02 * scale, -0.04 * scale),
            (0.42 * scale, 0.54 * scale, 0.08 * scale), body, 0.025 * scale)
        cylinder("Staff", (0.42 * scale, 1.0 * scale, 0), 0.04 * scale, 1.34 * scale, body, 7,
                 rotation=(0, 0, math.radians(-3)))
        sphere("StaffLantern", (0.42 * scale, 1.66 * scale, 0), (0.16 * scale, 0.20 * scale, 0.16 * scale), accent)
        cylinder("LanternRing", (0.42 * scale, 1.66 * scale, 0), 0.18 * scale, 0.04 * scale, accent, 10,
                 rotation=(math.radians(90), 0, 0))
        cone("StaffBeacon", (0.42 * scale, 1.86 * scale, 0), 0.10 * scale, 0.018 * scale,
             0.26 * scale, accent, 6)
        sphere("ChestFocus", (0, 1.08 * scale, -0.25 * scale),
               (0.10 * scale, 0.12 * scale, 0.05 * scale), accent)
        for side in (-1, 1):
            ribbon = box("ClothRibbon", (side * 0.18 * scale, 0.56 * scale, -0.12 * scale),
                         (0.09 * scale, 0.62 * scale, 0.05 * scale), body, 0.018 * scale)
            ribbon.rotation_euler[1] = math.radians(side * 5)
    if kind == "regenerator":
        sphere("CoreOrb", (-0.09 * scale, 1.03 * scale, -0.255 * scale),
               (0.18 * scale, 0.22 * scale, 0.07 * scale), accent)
        cylinder("CoreRibA", (-0.25 * scale, 1.05 * scale, -0.235 * scale),
                 0.025 * scale, 0.38 * scale, accent, 5, rotation=(0, 0, math.radians(72)))
        cylinder("CoreRibB", (0.10 * scale, 0.95 * scale, -0.235 * scale),
                 0.022 * scale, 0.32 * scale, accent, 5, rotation=(0, 0, math.radians(-58)))
        sphere("BackTotemPod", (-0.24 * scale, 1.22 * scale, 0.28 * scale),
               (0.20 * scale, 0.42 * scale, 0.18 * scale), accent)
        cylinder("BackTotemStem", (-0.18 * scale, 1.18 * scale, 0.25 * scale),
                 0.055 * scale, 0.86 * scale, body, 7, rotation=(math.radians(-10), 0, math.radians(-8)))
        cone("BackTotemTip", (-0.32 * scale, 1.76 * scale, 0.30 * scale),
             0.12 * scale, 0.02 * scale, 0.34 * scale, accent, 6, rotation=(math.radians(-12), 0, math.radians(-16)))
        for index, (side, height, reach) in enumerate(((-1, 1.40, 0.34), (1, 1.22, 0.26), (-1, 0.96, 0.24))):
            tendril = cylinder(f"BackTendril_{index}", (side * reach * scale, height * scale, 0.20 * scale),
                               0.035 * scale, 0.54 * scale, body, 6,
                               rotation=(math.radians(18), 0, math.radians(side * 34)))
            tendril.scale.x *= 0.72
        for side in (-1, 1):
            cone("VineHorn", (side * 0.2 * scale, 1.68 * scale, 0.02 * scale),
                 0.085 * scale, 0.018 * scale, 0.34 * scale, accent, 6,
                 rotation=(0, 0, math.radians(side * 18)))
    if kind in ("raider", "boss"):
        for side in (-1, 1):
            cone("Horn", (side * 0.18 * scale, 1.72 * scale, 0), 0.11 * scale, 0.015,
                 0.36 * scale, fire, 6, rotation=(0, 0, math.radians(side * 20)))
    if kind == "boss":
        # Phase pieces are authored as separate silhouette groups. Runtime keeps Phase 2/3
        # hidden until their health thresholds so the boss changes shape, not only colour/scale.
        for side in (-1, 1):
            cone(f"Phase2_ShoulderShard_{'L' if side < 0 else 'R'}",
                 (side * 0.58 * scale, 1.48 * scale, 0.02 * scale),
                 0.16 * scale, 0.025 * scale, 0.62 * scale, fire, 6,
                 rotation=(math.radians(8), 0, math.radians(side * 34)))
            cone(f"Phase3_BackBlade_{'L' if side < 0 else 'R'}",
                 (side * 0.42 * scale, 1.28 * scale, 0.30 * scale),
                 0.20 * scale, 0.025 * scale, 0.90 * scale, "DC_Enemy", 6,
                 rotation=(math.radians(-18), 0, math.radians(side * 22)))
        cone("Phase3_Crown", (0, 2.02 * scale, 0.02 * scale),
             0.24 * scale, 0.03 * scale, 0.72 * scale, fire, 7)
        sphere("Phase2_Core", (0, 1.12 * scale, -0.31 * scale),
               (0.26 * scale, 0.31 * scale, 0.09 * scale), fire)
        cylinder("Phase3_CoreRing", (0, 1.12 * scale, -0.34 * scale),
                 0.34 * scale, 0.06 * scale, "DC_Magic", 14,
                 rotation=(math.pi / 2, 0, 0))
    if kind == "knight":
        box("Cape", (0, 0.92, 0.23), (0.56, 0.82, 0.08), "DC_Defender", 0.03)
        sphere("Crest", (0, 1.9, 0), (0.12, 0.2, 0.12), fire)
    add_actor_sockets(kind, scale)
    add_humanoid_rig()


def build_siege():
    shadow(0.85)
    box("SiegeBody", (0, 0.48, 0), (1.25, 0.66, 1.0), "DC_Dark", 0.08)
    box("SiegeUpperCradle", (0, 0.82, -0.18), (0.92, 0.24, 1.28), "DC_Dark", 0.04)
    box("SiegeRearCounterweight", (0, 0.86, 0.62), (0.52, 0.30, 0.42), "DC_Dark", 0.03)
    for x in (-0.58, 0.58):
        for z in (-0.36, 0.36):
            cylinder("Wheel", (x, 0.35, z), 0.28, 0.18, "DC_Dark", 10, rotation=(0, 0, math.pi / 2))
            box("WheelBrace", (x * 0.86, 0.42, z), (0.10, 0.18, 0.18), "DC_Dark", 0.01)
    box("Ram", (0, 0.78, -0.72), (0.34, 0.34, 1.65), "DC_Enemy", 0.04)
    box("RamCollar", (0, 0.78, -1.06), (0.42, 0.12, 0.18), "DC_Enemy", 0.02)
    cone("RamHead", (0, 0.78, -1.62), 0.26, 0.02, 0.55, "DC_Enemy", 6, rotation=(math.pi / 2, 0, 0))
    for side in (-1, 1):
        plate = box("SiegeSidePlate", (side * 0.72, 0.64, -0.12), (0.12, 0.42, 0.88), "DC_Enemy", 0.02)
        plate.rotation_euler[1] = math.radians(side * 6)
    cylinder("SiegeBanner_Pole", (-0.38, 1.12, 0.2), 0.028, 0.92, "DC_Dark", 8)
    box("SiegeBanner_Cloth", (-0.22, 1.36, 0.2), (0.30, 0.22, 0.05), "DC_Enemy", 0.02)
    add_siege_sockets()
    add_humanoid_rig()


def build_outpost(kind, level, branch=None):
    """Tower v3: one mesh on the shared tower_palette atlas material (see tower_v3_models.py)."""
    mat = tower_v3_kit.palette_material(TOWER_TEXTURE_DIR)
    tower_v3_models.BUILDERS[kind](level, branch)
    tower_v3_kit.finish_tower(f"{kind.capitalize()}Body", mat)


ASSETS = {
    "Environment": build_environment,
    "GreenwatchGround_West": lambda: build_ground_variation("west"),
    "GreenwatchGround_Mid": lambda: build_ground_variation("mid"),
    "GreenwatchGround_Final": lambda: build_ground_variation("final"),
    "GreenwatchRoad_Surface": build_road_surface,
    "GreenwatchRoad_Edges": build_road_edges,
    "GreenwatchRoad_Wear": build_road_wear,
    "GreenwatchEntrance_Gate": build_entrance_gate,
    "GreenwatchEntrance_SpawnLight": build_entrance_spawn_light,
    "GreenwatchEntrance_Dressing": build_entrance_dressing,
    "GreenwatchKeep_Core": build_keep_core,
    "GreenwatchKeep_CommandLight": build_keep_command_light,
    "GreenwatchKeep_Dressing": build_keep_dressing,
    "GreenwatchBoundary_RidgeWall_A": build_boundary_ridge_wall_a,
    "GreenwatchBoundary_RidgeWall_B": build_boundary_ridge_wall_b,
    "GreenwatchBoundary_ForegroundStrip_A": build_boundary_foreground_strip_a,
    "GreenwatchBoundary_ForegroundStrip_B": build_boundary_foreground_strip_b,
    "GreenwatchBoundary_SilhouetteWall": build_boundary_silhouette_wall,
    "GreenwatchBoundary_ForestSilhouette": build_boundary_forest_silhouette,
    "Atmosphere_MoonDisc": build_atmosphere_moon_disc,
    "Atmosphere_FogRibbon": build_atmosphere_fog_ribbon,
    "Atmosphere_DustVeil": build_atmosphere_dust_veil,
    "Prop_PineTall_A": build_prop_pine_tall_a,
    "Prop_PineTall_B": build_prop_pine_tall_b,
    "Prop_RockCluster_A": build_prop_rock_cluster_a,
    "Prop_RockCluster_B": build_prop_rock_cluster_b,
    "Prop_BushRound_A": build_prop_bush_round_a,
    "Prop_BushRound_B": build_prop_bush_round_b,
    "Prop_GrassPatch_A": build_prop_grass_patch_a,
    "Prop_GrassPatch_B": build_prop_grass_patch_b,
    "Prop_CrystalCluster_A": build_prop_crystal_cluster_a,
    "Prop_CrystalCluster_B": build_prop_crystal_cluster_b,
    "OutpostSlot": build_slot,
    "OutpostSlot_Empty": lambda: build_slot_state("empty"),
    "OutpostSlot_Selected": lambda: build_slot_state("selected"),
    "OutpostSlot_Available": lambda: build_slot_state("available"),
    "OutpostSlot_Insufficient": lambda: build_slot_state("insufficient"),
    "OutpostSlot_Locked": lambda: build_slot_state("locked"),
    "BuildVfx_Dust": build_vfx_build_dust,
    "BuildVfx_Ring": build_vfx_build_ring,
    "BuildVfx_Spark": build_vfx_build_spark,
    "UpgradeVfx_Anchor": build_vfx_upgrade_anchor,
    "UpgradeVfx_Column": build_vfx_upgrade_column,
    "UpgradeVfx_Crest": build_vfx_upgrade_crest,
    "SellVfx_Ring": build_vfx_sell_ring,
    "SellVfx_Chips": build_vfx_sell_chips,
    "SellVfx_Glint": build_vfx_sell_glint,
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
    "Archer_RapidFire": lambda: build_outpost("archer", 2, "RapidFire"),
    "Archer_Sharpshooter": lambda: build_outpost("archer", 2, "Sharpshooter"),
    "Barracks_ShieldGuard": lambda: build_outpost("barracks", 2, "ShieldGuard"),
    "Barracks_AssaultGuard": lambda: build_outpost("barracks", 2, "AssaultGuard"),
    "Mage_Blast": lambda: build_outpost("mage", 2, "Blast"),
    "Mage_Frost": lambda: build_outpost("mage", 2, "Frost"),
}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-assets", required=True)
    parser.add_argument("--source", required=True)
    parser.add_argument("--only", nargs="*", choices=sorted(ASSETS), help="Generate only selected asset names.")
    script_args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    args = parser.parse_args(script_args)
    output = Path(args.output_assets)
    source = Path(args.source)
    output.mkdir(parents=True, exist_ok=True)
    source.mkdir(parents=True, exist_ok=True)

    selected_assets = set(args.only) if args.only else set(ASSETS)
    for asset_name, builder in ASSETS.items():
        if asset_name not in selected_assets:
            continue
        reset_scene()
        builder()
        for obj in bpy.context.scene.objects:
            obj.select_set(obj.type in {"MESH", "ARMATURE", "EMPTY"})
        bpy.ops.wm.save_as_mainfile(filepath=str(source / f"{asset_name}.blend"))
        bpy.ops.export_scene.fbx(filepath=str(output / f"{asset_name}.fbx"), use_selection=True,
                                 object_types={"MESH", "ARMATURE", "EMPTY"}, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                                 bake_anim=False, add_leaf_bones=False, path_mode="AUTO")
        print(f"Generated {asset_name}")


if __name__ == "__main__":
    main()
