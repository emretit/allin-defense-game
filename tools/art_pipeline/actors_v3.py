"""Actor v3 models (enemies + friendlies) in the tower v3 language: soft stylised shapes on the shared palette atlas.

Coordinates follow tower_v3_kit: Unity (x, y-up, z) with the gameplay camera looking along +Z, so actors face -Z.
Every part carries the rig bone that will own it (av3_bone) so the export can join one mesh per bone and keep the
shared DC_HumanoidRig animation contract.

Review renders (Blender):  blender -b --factory-startup --python actors_v3.py -- --render a1|a2 --out Builds/actors-v3/a1|a2
Review sheet (Pillow):     /usr/bin/python3 actors_v3.py compose a1|a2  (the "old" tiles are the archived A1 renders)
Export: generate_lowpoly_assets.py ASSETS -> build_for_export().
"""

import math
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Builds" / "actors-v3"

# Appended after the tower swatches so every existing tower UV keeps its atlas cell.
ACTOR_SWATCHES = {
    "crimson": ("#E5484D", "#8C1D2A", False),
    "crimson_dark": ("#A3283A", "#4A0F1C", False),
    "iron": ("#A7B0BF", "#4A5161", False),
    "iron_dark": ("#5D6474", "#262B36", False),
    "bone": ("#F4EAD0", "#B39D78", False),
    "glow_green": ("#C8FF9A", "#22D05A", True),
    "glow_red": ("#FFC09A", "#FF2A1A", True),
}

ACTOR_KINDS = ["Raider", "Vanguard", "Regenerator", "Support", "Siege", "Boss", "Guard", "Knight"]
LABELS = {
    "Raider": "Yağmacı", "Vanguard": "Öncü", "Regenerator": "Yenilenen", "Support": "Şifacı",
    "Siege": "Kuşatma", "Boss": "Boss (taban)", "Guard": "Kışla Muhafızı", "Knight": "Kahraman",
}
NOTES = {
    "Raider": "kapüşon · hançer · en küçük",
    "Vanguard": "kule kalkanı · miğfer · iri",
    "Regenerator": "yeşil çekirdek + sırt kristalleri",
    "Support": "cüppe · asa · altın hale",
    "Siege": "çatılı koçbaşı arabası",
    "Boss": "iri komutan · boynuz · balta",
    "Guard": "mızrak + kalkan · mavi/çelik",
    "Knight": "pelerin · kılıç · altın taç",
}
OLD_SCALE = {"Knight": 0.74, "Guard": 0.66, "Raider": 0.64, "Vanguard": 0.68, "Regenerator": 0.60,
             "Support": 0.62, "Siege": 0.92, "Boss": 1.02}
# Proposed catalog Actor(..., scale), approved 2026-10-14: small units +40 %, the rest +25 % over the old values.
NEW_SCALE = {"Knight": 0.92, "Guard": 0.82, "Raider": 0.90, "Vanguard": 0.85, "Regenerator": 0.84,
             "Support": 0.87, "Siege": 1.15, "Boss": 1.20}


# ============================================================================================ Blender side

def _blender():
    import bpy  # noqa: F401
    import tower_v3_kit as kit
    for name, value in ACTOR_SWATCHES.items():
        kit.SWATCHES.setdefault(name, value)
    kit.SWATCH_INDEX.clear()
    kit.SWATCH_INDEX.update({name: index for index, name in enumerate(kit.SWATCHES)})
    return kit


sys.path.insert(0, str(Path(__file__).resolve().parent))
try:
    import bpy
    from mathutils import Matrix, Vector
    kit = _blender()
except ImportError:
    bpy = None
    kit = None

BONE = ["Root"]
GROUP = [""]


def bone(name):
    BONE[0] = name


def group(name):
    GROUP[0] = name


def _own(obj):
    obj["av3_bone"] = BONE[0]
    obj["av3_group"] = GROUP[0]
    return obj


# Mobile budget: 60 concurrent enemies; smooth normals hide the facets at gameplay scale.
MAX_SEG = 10
MAX_BALL = (8, 4)
MAX_LIMB = 6


def ball(name, at, r, swatch, sy=1.0, sx=1.0, sz=1.0, seg=8, rings=4, **kw):
    seg, rings = min(seg, MAX_BALL[0]), min(rings, MAX_BALL[1])
    prof = [(0.0, -r * sy)]
    for i in range(1, rings):
        a = math.pi * i / rings
        prof.append((r * math.sin(a), -r * math.cos(a) * sy))
    prof.append((0.0, r * sy))
    return _own(kit.lathe(name, prof, swatch, at=at, segments=seg, round_r=0, scale_x=sx, scale_z=sz, **kw))


def lathe(name, profile, swatch, at=(0, 0, 0), **kw):
    kw["segments"] = min(kw.get("segments", MAX_SEG), MAX_SEG)
    return _own(kit.lathe(name, profile, swatch, at=at, **kw))


def rbox(name, at, size, swatch, **kw):
    kw["segments"] = 1
    return _own(kit.rbox(name, at, size, swatch, **kw))


def outline(name, points, thickness, swatch, **kw):
    return _own(kit.outline(name, points, thickness, swatch, **kw))


def torus(name, at, major, minor, swatch, **kw):
    kw.setdefault("major_segments", 12 if major > 0.15 else 8)
    kw.setdefault("minor_segments", 4)
    return _own(kit.torus(name, at, major, minor, swatch, **kw))


def gem(name, at, radius, height, swatch, **kw):
    return _own(kit.gem(name, at, radius, height, swatch, **kw))


def tube(name, points, radius, swatch, **kw):
    kw.setdefault("resolution", 1)
    kw["sides"] = min(kw.get("sides", 6), 6)
    return _own(kit.tube(name, points, radius, swatch, **kw))


def ring(name, y, r, h, swatch, at=(0, 0, 0), **kw):
    kw["round_r"] = 0
    kw["segments"] = min(kw.get("segments", MAX_SEG), MAX_SEG)
    obj = kit.ring(name, y, r, h, swatch, **kw)
    obj.location = kit.U(at)
    return _own(obj)


def orient(obj, a, b):
    """Point a part built along +up at the origin from a to b (Unity coordinates)."""
    direction = kit.U(b) - kit.U(a)
    rot = Vector((0, 0, 1)).rotation_difference(direction.normalized()).to_matrix().to_4x4()
    obj.matrix_world = Matrix.Translation(kit.U(a)) @ rot
    return obj


def limb(name, a, b, r, swatch, r2=None, seg=8, **kw):
    seg = min(seg, MAX_LIMB if r < 0.06 else 8)
    length = (Vector(b) - Vector(a)).length
    r2 = r if r2 is None else r2
    obj = lathe(name, [(0, -r * 0.6), (r, 0), (r2, length), (0, length + r2 * 0.6)], swatch, segments=seg,
                round_r=min(r, r2) * 0.5, **kw)
    return orient(obj, a, b)


def cone_to(name, a, b, r, swatch, seg=8, **kw):
    length = (Vector(b) - Vector(a)).length
    obj = lathe(name, [(0, 0), (r, 0), (r * 0.25, length * 0.8), (0, length)], swatch, segments=seg,
                round_r=r * 0.15, **kw)
    return orient(obj, a, b)


def shell(name, profile, a0, a1, swatch, thickness=0.04, at=(0, 0, 0), segs=8, **kw):
    """Partial lathe surface with thickness (capes): profile (r, h), angles in degrees, +90 = behind the actor."""
    import bmesh
    bm = bmesh.new()
    base = kit.U(at)
    rows = []
    for r, h in profile:
        outer, inner = [], []
        for i in range(segs + 1):
            a = math.radians(a0 + (a1 - a0) * i / segs)
            for target, radius in ((outer, r), (inner, r - thickness)):
                target.append(bm.verts.new((base.x + math.cos(a) * radius, base.y + math.sin(a) * radius,
                                            base.z + h)))
        rows.append((outer, inner))
    for (o0, i0), (o1, i1) in zip(rows, rows[1:]):
        for i in range(segs):
            bm.faces.new((o0[i], o0[i + 1], o1[i + 1], o1[i]))
            bm.faces.new((i0[i + 1], i0[i], i1[i], i1[i + 1]))
        for i in (0, segs):
            bm.faces.new((o0[i], i0[i], i1[i], o1[i]))
    for outer, inner in (rows[0], rows[-1]):
        for i in range(segs):
            bm.faces.new((outer[i], outer[i + 1], inner[i + 1], inner[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return _own(kit._tag(kit._link(name, bm), swatch, **kw))


def rounded_rect(w, h, r, steps=2):
    pts = []
    for cx, cy, a0 in ((w / 2 - r, h - r, 0), (-w / 2 + r, h - r, 90), (-w / 2 + r, r, 180), (w / 2 - r, r, 270)):
        for s in range(steps + 1):
            a = math.radians(a0 + 90 * s / steps)
            pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


def socket(name, at, size=0.1):
    obj = kit.socket(name, at, size)
    obj["av3_bone"] = BONE[0]
    return obj


def legs(x, top, r, swatch, boot, boot_size, foot_y=0.0):
    for side, bone_name in ((-1, "Leg.L"), (1, "Leg.R")):
        bone(bone_name)
        limb(f"Leg{bone_name[-1]}", (side * x, foot_y + boot_size[1] * 0.7, 0), (side * x, top, 0), r, swatch)
        rbox(f"Boot{bone_name[-1]}", (side * x, foot_y + boot_size[1] * 0.5, -boot_size[2] * 0.18), boot_size, boot,
             bevel=min(boot_size) * 0.35)


def humanoid_sockets(chest, head, hand_r=None, hand_l=None, tip=None, cast=None, core=None, back=None):
    bone("Root")
    socket("Socket_Root", (0, 0, 0), 0.14)
    socket("Socket_Ground", (0, 0.04, 0), 0.12)
    bone("Spine")
    socket("Socket_Chest", chest, 0.12)
    if core:
        socket("Socket_Core", core, 0.11)
    if back:
        socket("Socket_Back", back, 0.11)
    if cast:
        socket("Socket_Cast", cast, 0.11)
    bone("Head")
    socket("Socket_Head", head, 0.11)
    if hand_r:
        bone("Arm.R")
        socket("Socket_Hand_R", hand_r, 0.1)
        if tip:
            socket("Socket_Weapon_Tip", tip, 0.1)
    if hand_l:
        bone("Arm.L")
        socket("Socket_Hand_L", hand_l, 0.1)


# --------------------------------------------------------------------------------------------- enemies


def raider():
    """Smallest unit: deep hood, glowing eyes, one curved dagger, trailing scarf."""
    legs(0.10, 0.40, 0.07, "crimson_dark", "leather", (0.14, 0.13, 0.24))
    bone("Spine")
    lathe("Tunic", [(0.15, 0.30), (0.22, 0.34), (0.2, 0.52), (0, 0.54)], "crimson_dark", round_r=0.03, segments=12)
    lathe("Torso", [(0, 0.40), (0.19, 0.42), (0.22, 0.54), (0.2, 0.70), (0.13, 0.80), (0, 0.82)], "crimson",
          round_r=0.04, segments=12)
    ring("Belt", 0.43, 0.21, 0.06, "leather", segments=12)
    rbox("Buckle", (0, 0.46, -0.215), (0.07, 0.07, 0.03), "gold", bevel=0.012)
    torus("Collar", (0, 0.79, 0), 0.17, 0.06, "crimson_dark")
    rbox("Scarf", (0.07, 0.66, 0.26), (0.12, 0.05, 0.36), "crimson_dark", bevel=0.02, rot=(-28, 12, 0), taper=0.7)
    bone("Head")
    hood = lathe("Hood", [(0.25, 0.0), (0.26, 0.10), (0.235, 0.22), (0.16, 0.34), (0.07, 0.45), (0, 0.56)],
                 "crimson", round_r=0.05, segments=12)
    kit._place(hood, (0, 0.76, 0.02), (-22, 0, 0))
    ball("Face", (0, 0.93, -0.13), 0.14, "dark", sy=1.05, sz=0.55)
    for side in (-1, 1):
        ball("Eye", (side * 0.055, 0.94, -0.205), 0.03, "glow_gold", seg=8, rings=4)
    bone("Arm.R")
    limb("ArmR", (0.19, 0.74, 0), (0.30, 0.52, -0.12), 0.055, "crimson")
    ball("HandR", (0.31, 0.50, -0.13), 0.06, "leather", seg=8, rings=4)
    blade = [(-0.05, 0.0), (0.05, 0.0), (0.09, 0.18), (0.07, 0.38), (0.0, 0.56), (-0.03, 0.36), (-0.045, 0.14)]
    outline("Dagger", blade, 0.04, "metal", at=(0.31, 0.53, -0.16), rot=(35, 0, -30), bevel=0.012)
    rbox("DaggerGuard", (0.31, 0.53, -0.15), (0.2, 0.045, 0.06), "gold", bevel=0.015, rot=(35, 0, -30))
    bone("Arm.L")
    limb("ArmL", (-0.19, 0.74, 0), (-0.29, 0.52, 0.06), 0.055, "crimson")
    ball("HandL", (-0.30, 0.50, 0.06), 0.06, "leather", seg=8, rings=4)
    humanoid_sockets((0, 0.62, -0.03), (0, 1.18, 0), hand_r=(0.31, 0.50, -0.13), tip=(0.36, 0.74, -0.42))


def vanguard():
    """Heavy shield wall: big crimson tower shield with iron rim, closed helmet with crest, iron pauldrons."""
    legs(0.17, 0.56, 0.105, "iron_dark", "iron_dark", (0.22, 0.17, 0.32))
    bone("Spine")
    lathe("Tasset", [(0.27, 0.46), (0.34, 0.50), (0.31, 0.76), (0, 0.78)], "crimson_dark", round_r=0.04, segments=12)
    lathe("Torso", [(0, 0.52), (0.30, 0.54), (0.36, 0.76), (0.36, 1.04), (0.27, 1.24), (0, 1.27)], "crimson",
          round_r=0.06, segments=12, scale_x=1.12)
    rbox("ChestPlate", (0, 0.98, -0.24), (0.48, 0.42, 0.14), "iron", bevel=0.06)
    ring("Belt", 0.74, 0.37, 0.07, "iron_dark", segments=12)
    for side in (-1, 1):
        ball("Pauldron", (side * 0.42, 1.18, 0), 0.21, "iron", sy=0.78)
        torus("PauldronRim", (side * 0.42, 1.12, 0), 0.19, 0.03, "gold")
    bone("Head")
    lathe("Helmet", [(0.24, 1.18), (0.26, 1.30), (0.25, 1.48), (0.18, 1.62), (0, 1.68)], "iron", round_r=0.05,
          segments=12)
    rbox("Visor", (0, 1.40, -0.235), (0.32, 0.05, 0.06), "dark", bevel=0.015)
    rbox("NoseGuard", (0, 1.34, -0.25), (0.05, 0.16, 0.04), "iron", bevel=0.015)
    rbox("Crest", (0, 1.72, 0.03), (0.07, 0.16, 0.42), "crimson", bevel=0.03)
    bone("Arm.L")
    limb("ArmL", (-0.38, 1.06, 0), (-0.36, 0.78, -0.22), 0.08, "crimson")
    shape = rounded_rect(0.80, 1.02, 0.14)
    outline("ShieldRim", rounded_rect(0.90, 1.12, 0.18), 0.07, "iron_dark", at=(-0.24, 0.30, -0.40),
            rot=(0, 16, 0))
    outline("Shield", [(x, y + 0.05) for x, y in shape], 0.08, "crimson", at=(-0.24, 0.30, -0.44), rot=(0, 16, 0),
            bevel=0.03)
    outline("ShieldBand", rounded_rect(0.82, 0.12, 0.05), 0.04, "iron", at=(-0.25, 0.80, -0.49), rot=(0, 16, 0))
    ball("ShieldBoss", (-0.26, 0.86, -0.52), 0.1, "gold", sz=0.6)
    bone("Arm.R")
    limb("ArmR", (0.38, 1.06, 0), (0.48, 0.82, -0.08), 0.08, "crimson")
    ball("HandR", (0.49, 0.80, -0.09), 0.08, "iron_dark", seg=8, rings=4)
    limb("MaceHaft", (0.49, 0.62, -0.09), (0.55, 1.34, -0.12), 0.035, "wood_dark", seg=8)
    ball("MaceHead", (0.555, 1.38, -0.12), 0.12, "iron", seg=10, rings=5)
    torus("MaceFlange", (0.555, 1.38, -0.12), 0.12, 0.035, "iron_dark", rot=(90, 0, 0))
    humanoid_sockets((0, 1.02, -0.05), (0, 1.80, 0), hand_r=(0.49, 0.80, -0.09), hand_l=(-0.36, 0.78, -0.22),
                     tip=(0.555, 1.50, -0.12))


def regenerator():
    """Hunched mutant: dark crimson body, glowing green chest core, crystal spikes on the back, green eyes."""
    legs(0.14, 0.48, 0.09, "crimson_dark", "dark", (0.18, 0.13, 0.28))
    bone("Spine")
    lathe("Body", [(0, 0.42), (0.28, 0.44), (0.34, 0.68), (0.33, 0.94), (0.22, 1.10), (0, 1.13)], "crimson_dark",
          round_r=0.06, segments=12, scale_z=1.08)
    torus("VineRing", (0, 0.60, 0), 0.32, 0.03, "glow_green")
    gem("CoreGem", (0, 0.72, -0.30), 0.12, 0.30, "glow_green", sides=6)
    torus("CoreSocket", (0, 0.86, -0.30), 0.13, 0.03, "gold", rot=(90, 0, 0))
    for i, (x, y, z, h, tilt, roll) in enumerate(((0.0, 0.98, 0.16, 0.62, -26, 0), (-0.22, 0.86, 0.16, 0.44, -18, -30),
                                                  (0.22, 0.86, 0.16, 0.44, -18, 30), (0.10, 1.10, 0.04, 0.30, -8, 14))):
        gem(f"BackCrystal{i}", (x, y, z), 0.12 if h > 0.5 else 0.09, h, "glow_green", sides=5, rot=(tilt, 0, roll))
    bone("Head")
    ball("Head", (0, 1.10, -0.18), 0.18, "crimson_dark", sy=0.95)
    ball("Brow", (0, 1.17, -0.24), 0.15, "dark", sy=0.45, sz=0.8)
    for side in (-1, 1):
        ball("Eye", (side * 0.065, 1.10, -0.335), 0.035, "glow_green", seg=8, rings=4)
    for side in (-1, 1):
        bone("Arm.L" if side < 0 else "Arm.R")
        limb("Arm", (side * 0.30, 0.98, -0.04), (side * 0.40, 0.50, -0.16), 0.075, "crimson_dark")
        ball("Hand", (side * 0.41, 0.46, -0.17), 0.08, "green_dark", seg=8, rings=4)
        for k in (-1, 0, 1):
            cone_to("Claw", (side * 0.41 + k * 0.04, 0.42, -0.2), (side * 0.43 + k * 0.06, 0.30, -0.28), 0.025,
                    "glow_green", seg=5)
    humanoid_sockets((0, 0.80, -0.08), (0, 1.40, -0.1), core=(0, 0.80, -0.36), back=(0, 1.10, 0.26),
                     cast=(0, 1.40, -0.16))


def support():
    """Healer: long crimson robe with cream mantle and stole, golden halo, staff with a green heal orb."""
    bone("Spine")
    lathe("Robe", [(0.36, 0.0), (0.38, 0.05), (0.31, 0.40), (0.23, 0.80), (0.21, 1.02), (0, 1.06)], "crimson",
          round_r=0.04, segments=14)
    ring("Hem", 0.02, 0.375, 0.06, "gold", segments=14)
    rbox("Stole", (0, 0.50, -0.29), (0.15, 0.86, 0.04), "stone_cream", bevel=0.015, rot=(-10, 0, 0), taper=0.8)
    lathe("Mantle", [(0.31, 0.86), (0.33, 0.92), (0.27, 1.04), (0.14, 1.12), (0, 1.14)], "stone_cream",
          round_r=0.05, segments=14)
    ball("ChestGem", (0, 0.96, -0.30), 0.05, "glow_green", seg=8, rings=4)
    bone("Head")
    lathe("Hood", [(0.20, 1.06), (0.22, 1.20), (0.19, 1.34), (0.11, 1.44), (0, 1.47)], "stone_cream", round_r=0.05,
          segments=12)
    ball("Face", (0, 1.23, -0.17), 0.13, "dark", sz=0.55)
    torus("Halo", (0, 1.62, 0.02), 0.20, 0.028, "glow_gold")
    bone("Arm.R")
    limb("ArmR", (0.22, 0.98, 0), (0.36, 0.92, -0.10), 0.07, "crimson")
    ball("HandR", (0.37, 0.92, -0.10), 0.06, "stone_warm", seg=8, rings=4)
    limb("Staff", (0.38, 0.02, -0.10), (0.38, 1.86, -0.10), 0.03, "wood", seg=8)
    torus("StaffRing", (0.38, 2.0, -0.10), 0.15, 0.03, "gold", rot=(90, 0, 0))
    ball("StaffOrb", (0.38, 2.0, -0.10), 0.09, "glow_green", seg=10, rings=5)
    lathe("StaffTip", [(0, 2.14), (0.04, 2.15), (0, 2.26)], "gold", at=(0.38, 0, -0.10), segments=8, round_r=0)
    bone("Arm.L")
    limb("ArmL", (-0.22, 0.98, 0), (-0.27, 0.72, -0.10), 0.07, "crimson")
    ring("Cuff", 0.72, 0.075, 0.06, "stone_cream", at=(-0.27, 0, -0.10), segments=10)
    humanoid_sockets((0, 0.86, -0.05), (0, 1.56, 0), hand_r=(0.37, 0.92, -0.10), tip=(0.38, 2.12, -0.10),
                     cast=(0.38, 2.0, -0.10))


def siege():
    """Covered battering-ram cart: big wheels, crimson canvas roof, iron ram head with bone horns, pennant."""
    bone("Spine")
    rbox("Chassis", (0, 0.40, 0.10), (0.86, 0.18, 1.30), "wood", bevel=0.05)
    for x in (-0.36, 0.36):
        for z in (-0.40, 0.60):
            rbox("Post", (x, 0.74, z), (0.08, 0.56, 0.08), "wood_dark", bevel=0.03)
    for side in (-1, 1):
        rbox("Roof", (side * 0.24, 1.07, 0.10), (0.56, 0.06, 1.30), "crimson", bevel=0.03, rot=(0, 0, side * 40))
        rbox("RoofTrim", (side * 0.46, 0.89, 0.10), (0.06, 0.06, 1.32), "gold", bevel=0.02)
    rbox("Ridge", (0, 1.25, 0.10), (0.10, 0.09, 1.40), "wood_dark", bevel=0.03)
    bone("Arm.R")
    limb("RamLog", (0, 0.64, 0.80), (0, 0.64, -1.10), 0.13, "wood", seg=10)
    for z in (-0.55, 0.30):
        ring("RamBand", 0.0, 0.14, 0.07, "iron_dark", at=(0, 0.64, z), segments=10, out=0.015)
    head = lathe("RamHead", [(0, 0), (0.21, 0.0), (0.24, 0.12), (0.18, 0.30), (0.07, 0.40), (0, 0.42)], "iron",
                 round_r=0.04, segments=10)
    orient(head, (0, 0.64, -1.05), (0, 0.64, -1.6))
    for side in (-1, 1):
        tube("RamHorn", [(side * 0.15, 0.74, -1.22), (side * 0.32, 0.86, -1.12), (side * 0.38, 1.02, -1.26)], 0.05,
             "bone", taper_end=0.3, sides=8)
    for side in (-1, 1):
        for z in (-0.42, 0.62):
            bone("Leg.L" if side < 0 else "Leg.R")
            wheel = lathe("Wheel", [(0, -0.07), (0.27, -0.07), (0.31, -0.03), (0.31, 0.03), (0.27, 0.07), (0, 0.07)],
                          "wood_dark", round_r=0.02, segments=14)
            kit._place(wheel, (side * 0.50, 0.31, z), (0, 0, 90))
            ball("Hub", (side * 0.58, 0.31, z), 0.08, "iron", sx=0.6, seg=8, rings=4)
    bone("Arm.L")
    limb("BannerPole", (-0.32, 0.9, 0.62), (-0.32, 1.80, 0.62), 0.025, "wood_dark", seg=8)
    outline("Pennant", kit.pennant_points(0.40, 0.26), 0.03, "crimson", at=(-0.30, 1.46, 0.62), bevel=0.01)
    ball("PoleTip", (-0.32, 1.84, 0.62), 0.045, "gold", seg=8, rings=4)
    bone("Root")
    socket("Socket_Root", (0, 0, 0), 0.16)
    socket("Socket_Ground", (0, 0.05, 0), 0.13)
    bone("Spine")
    socket("Socket_Chest", (0, 0.80, -0.08), 0.13)
    socket("Socket_Core", (0, 0.66, 0.18), 0.13)
    bone("Arm.R")
    socket("Socket_Muzzle", (0, 0.64, -1.62), 0.13)


def boss(ornament=None, cape=True):
    """Big commander: broad iron pauldrons, horned helm, crimson cape, great axe, glowing core.

    ornament selects the region dressing; the export builds every ornament (with its cape) as a toggled group.
    """
    legs(0.24, 0.78, 0.15, "iron_dark", "iron_dark", (0.32, 0.22, 0.44))
    bone("Spine")
    lathe("Tasset", [(0.38, 0.60), (0.48, 0.66), (0.43, 1.00), (0, 1.02)], "crimson_dark", round_r=0.05, segments=14)
    lathe("Torso", [(0, 0.72), (0.36, 0.74), (0.38, 0.96), (0.46, 1.24), (0.40, 1.60), (0, 1.66)], "crimson_dark",
          round_r=0.08, segments=14, scale_x=1.15)
    lathe("Breastplate", [(0.40, 1.10), (0.52, 1.28), (0.54, 1.46), (0.42, 1.64), (0, 1.70)], "crimson", round_r=0.07,
          segments=14, scale_x=1.15, scale_z=0.95)
    ball("Core", (0, 1.36, -0.50), 0.11, "glow_red", sz=0.6, seg=10, rings=5)
    torus("CoreRim", (0, 1.36, -0.49), 0.13, 0.03, "gold", rot=(90, 0, 0))
    ring("Belt", 0.96, 0.40, 0.09, "iron_dark", segments=14)
    if cape:
        boss_cape(CAPES.get(ornament, "crimson_dark"))
    for side in (-1, 1):
        ball("Pauldron", (side * 0.60, 1.58, 0.02), 0.30, "iron", sy=0.72)
        torus("PauldronRim", (side * 0.60, 1.50, 0.02), 0.28, 0.04, "gold")
        cone_to("PauldronSpike", (side * 0.66, 1.72, 0.02), (side * 0.86, 2.02, 0.02), 0.08, "bone")
    bone("Head")
    lathe("Helmet", [(0.27, 1.60), (0.29, 1.74), (0.27, 1.94), (0.17, 2.08), (0, 2.12)], "iron", round_r=0.06,
          segments=12)
    rbox("EyeSlit", (0, 1.86, -0.265), (0.28, 0.05, 0.05), "glow_red", bevel=0.015)
    rbox("FaceGuard", (0, 1.74, -0.27), (0.07, 0.18, 0.05), "iron_dark", bevel=0.02)
    for side in (-1, 1):
        tube("Horn", [(side * 0.22, 1.98, 0), (side * 0.42, 2.06, -0.02), (side * 0.52, 2.28, -0.04),
                      (side * 0.48, 2.46, -0.02)], 0.07, "bone", taper_end=0.2, sides=8)
    if ornament:
        ORNAMENTS[ornament]()
    bone("Arm.R")
    limb("ArmR", (0.62, 1.42, 0), (0.70, 1.08, -0.16), 0.11, "crimson")
    ball("FistR", (0.71, 1.04, -0.17), 0.12, "iron_dark", seg=8, rings=4)
    limb("AxeHaft", (0.72, 0.30, -0.17), (0.72, 2.20, -0.17), 0.05, "wood_dark", seg=8)
    blade = [(0.0, -0.36), (0.18, -0.30), (0.34, -0.40), (0.42, -0.16), (0.44, 0.0), (0.42, 0.16), (0.34, 0.40),
             (0.18, 0.30), (0.0, 0.36)]
    outline("AxeBlade", blade, 0.07, "iron", at=(0.74, 1.94, -0.17), bevel=0.03)
    outline("AxeBack", [(-x * 0.55, y * 0.6) for x, y in blade], 0.07, "iron_dark", at=(0.70, 1.94, -0.17),
            bevel=0.03)
    ball("AxeCap", (0.72, 2.24, -0.17), 0.07, "gold", seg=8, rings=4)
    bone("Arm.L")
    limb("ArmL", (-0.62, 1.42, 0), (-0.72, 1.04, -0.12), 0.11, "crimson")
    ball("FistL", (-0.73, 1.00, -0.13), 0.13, "iron_dark", seg=8, rings=4)
    humanoid_sockets((0, 1.30, -0.05), (0, 2.18, 0), hand_r=(0.71, 1.04, -0.17), hand_l=(-0.73, 1.00, -0.13),
                     tip=(1.16, 1.94, -0.17), cast=(0, 1.90, -0.2), core=(0, 1.26, -0.5), back=(0, 1.40, 0.5))


def boss_cape(swatch):
    bone("Spine")
    shell("Cape", [(0.48, 1.62), (0.52, 1.20), (0.58, 0.62), (0.64, 0.12)], 34, 146, swatch, thickness=0.05, segs=8)


def boss_phase_parts():
    """Phase silhouettes kept as separate objects: BossPhaseVisual shows Phase2_* at phase 2 and Phase3_* at 3."""
    bone("Spine")
    for side, tag in ((-1, "L"), (1, "R")):
        group(f"Phase2_ShoulderShard_{tag}")
        gem("Shard", (side * 0.62, 1.66, 0.02), 0.11, 0.56, "glow_red", sides=5, rot=(-8, 0, side * 26))
        group(f"Phase3_BackBlade_{tag}")
        blade = [(0.0, 0.0), (0.16, 0.10), (0.30, 0.62), (0.22, 0.92), (0.06, 0.40)]
        outline("Blade", [(side * x, y) for x, y in blade], 0.06, "crimson_dark", at=(side * 0.22, 1.30, 0.46),
                rot=(-20, side * -18, 0), bevel=0.02)
    group("Phase2_Core")
    ball("Core", (0, 1.36, -0.53), 0.17, "glow_red", sz=0.55, seg=10, rings=5)
    group("Phase3_CoreRing")
    torus("Ring", (0, 1.36, -0.55), 0.24, 0.035, "glow_fire", rot=(90, 0, 0))
    bone("Head")
    group("Phase3_Crown")
    torus("Flame", (0, 2.62, 0), 0.30, 0.05, "glow_fire")
    for i in range(6):
        a = i * math.pi / 3
        cone_to("FlameTongue", (math.cos(a) * 0.30, 2.62, math.sin(a) * 0.30),
                (math.cos(a) * 0.34, 2.86, math.sin(a) * 0.34), 0.06, "glow_fire", seg=5)
    group("")


def _crown(points, y, r, swatch, height=0.16, tip=None):
    ring("CrownBand", y, r, 0.09, swatch, segments=12)
    for i in range(points):
        a = math.radians(-90 + (i - (points - 1) / 2) * (300 / points))
        base = (math.cos(a) * r, y + 0.06, math.sin(a) * r)
        cone_to("CrownPoint", base, (math.cos(a) * (r + 0.03), y + 0.06 + height, math.sin(a) * (r + 0.03)), 0.05,
                swatch, seg=5)
    if tip:
        ball("CrownGem", (0, y + 0.05, -r - 0.02), 0.05, tip, sz=0.6, seg=8, rings=4)


def ornament_region_lord():
    """Chapter 10, Greenwatch: gold crown with a red gem and a gold-trimmed war banner on the back."""
    bone("Head")
    _crown(5, 2.02, 0.25, "gold", height=0.28, tip="glow_red")
    bone("Spine")
    limb("BannerPole", (0.18, 1.2, 0.46), (0.18, 2.75, 0.46), 0.03, "wood_dark", seg=8)
    outline("BannerCloth", rounded_rect(0.42, 0.56, 0.06) , 0.04, "crimson", at=(0.18, 2.06, 0.47), bevel=0.012)
    outline("BannerTrim", rounded_rect(0.46, 0.08, 0.03), 0.05, "gold", at=(0.18, 2.58, 0.47))
    gem("BannerTip", (0.18, 2.75, 0.46), 0.05, 0.16, "gold", sides=4)


def ornament_gate_master():
    """Chapter 20: iron battlement crown with fire-glow slits, giant gate key on the back, chest chains."""
    bone("Head")
    lathe("BattlementBand", [(0.22, 1.98), (0.28, 2.00), (0.28, 2.16), (0, 2.16)], "iron_dark", round_r=0.02,
          segments=12)
    for i in range(6):
        a = i * math.pi / 3 + math.pi / 6
        rbox("Merlon", (math.cos(a) * 0.23, 2.24, math.sin(a) * 0.23), (0.15, 0.20, 0.10), "iron", bevel=0.03,
             rot=(0, math.degrees(a) + 90, 0))
    ring("FireBand", 2.05, 0.285, 0.04, "glow_fire", segments=12)
    bone("Spine")
    limb("KeyShaft", (-0.42, 0.95, 0.52), (0.30, 2.25, 0.52), 0.05, "gold", seg=8)
    torus("KeyBow", (0.38, 2.40, 0.52), 0.15, 0.045, "gold", rot=(90, 0, 0))
    rbox("KeyBit", (-0.36, 1.10, 0.52), (0.26, 0.10, 0.06), "gold", bevel=0.02, rot=(0, 0, -29))
    for i, x in enumerate((-0.28, 0.0, 0.28)):
        torus("ChainLink", (x, 1.18 - abs(x) * 0.3, -0.52), 0.07, 0.022, "iron_dark", rot=(90, 0, 90 * (i % 2)))


def ornament_frost_sovereign():
    """Chapter 30, Frost March: ice-crystal crown, ice shards on the pauldrons, white fur collar."""
    bone("Head")
    ring("IceBand", 2.0, 0.22, 0.08, "metal", segments=12)
    for i, (h, lean) in enumerate(((0.62, 0), (0.46, 18), (0.46, -18), (0.32, 34), (0.32, -34))):
        a = math.radians(-90 + lean * 2.4)
        gem("IceCrown", (math.cos(a) * 0.17, 2.04, math.sin(a) * 0.17), 0.09, h, "glow_ice", sides=5,
            rot=(0, 0, lean))
    bone("Spine")
    torus("FurCollar", (0, 1.66, 0), 0.40, 0.11, "cloth", scale_y=0.8)
    for side in (-1, 1):
        for k, (h, roll) in enumerate(((0.42, 18), (0.28, 40))):
            gem("ShoulderIce", (side * (0.56 + k * 0.12), 1.68, 0.04), 0.08, h, "glow_ice", sides=5,
                rot=(-6, 0, side * roll))


def ornament_deep_lord():
    """Chapter 40, Sunken Kingdom: teal coral crown with a glowing pearl, fin crests and shell pauldrons."""
    bone("Head")
    ring("CoralBand", 2.0, 0.22, 0.08, "teal", segments=12)
    for side, reach in ((-1, 0.20), (0, 0.0), (1, 0.20)):
        tube("Coral", [(side * 0.08, 2.04, -0.08), (side * (0.16 + reach * 1.4), 2.30, -0.08),
                       (side * (0.06 + reach * 1.4), 2.58, -0.04)], 0.055, "teal", taper_end=0.4, sides=8)
    ball("Pearl", (0, 2.04, -0.26), 0.09, "glow_cyan", seg=10, rings=5)
    fin = [(0.0, 0.0), (0.38, 0.14), (0.50, 0.44), (0.14, 0.32), (0.0, 0.34)]
    for side in (-1, 1):
        outline("HelmFin", [(side * x, y) for x, y in fin], 0.04, "teal", at=(side * 0.25, 1.78, 0.04),
                rot=(0, side * 80, 0), bevel=0.012)
    bone("Spine")
    fan = [(0.0, 0.0)] + [(math.cos(math.radians(a)) * 0.30, math.sin(math.radians(a)) * 0.30)
                          for a in range(20, 161, 28)]
    for side in (-1, 1):
        outline("ShellPauldron", fan, 0.05, "teal", at=(side * 0.62, 1.72, -0.06), rot=(-25, 0, side * 20),
                bevel=0.015)


def ornament_last_lord():
    """Chapter 50, finale: tall violet spike crown, dark floating halo, violet crystal pauldrons."""
    bone("Head")
    _crown(7, 2.0, 0.24, "purple_dark", height=0.44)
    ball("CrownGem", (0, 2.08, -0.24), 0.06, "glow_purple", sz=0.6, seg=8, rings=4)
    torus("DarkHalo", (0, 2.22, 0.30), 0.40, 0.035, "glow_purple", rot=(90, 0, 0))
    torus("DarkHaloOuter", (0, 2.22, 0.32), 0.47, 0.03, "purple_dark", rot=(90, 0, 0))
    bone("Spine")
    for side in (-1, 1):
        for k, (h, roll) in enumerate(((0.52, 14), (0.34, 38))):
            gem("VoidShard", (side * (0.56 + k * 0.12), 1.70, 0.04), 0.10, h, "glow_purple", sides=5,
                rot=(-6, 0, side * roll))


CAPES = {"region-lord": "crimson_dark", "gate-master": "iron_dark", "frost-sovereign": "cloth",
         "deep-lord": "teal", "last-lord": "purple_dark"}
ORNAMENTS = {
    "region-lord": ornament_region_lord,
    "gate-master": ornament_gate_master,
    "frost-sovereign": ornament_frost_sovereign,
    "deep-lord": ornament_deep_lord,
    "last-lord": ornament_last_lord,
}
BOSS_ORNAMENT_LABELS = [("region-lord", "Bölüm 10 · Bölge Lordu", "altın taç + sırt sancağı"),
                        ("gate-master", "Bölüm 20 · Kapı Ustası", "mazgallı demir taç + dev anahtar"),
                        ("frost-sovereign", "Bölüm 30 · Ayaz Hükümdarı", "buz kristali taç + kürk yaka"),
                        ("deep-lord", "Bölüm 40 · Derinlerin Lordu", "mercan taç + inci + yüzgeçler"),
                        ("last-lord", "Bölüm 50 · Son Lord", "mor diken taç + karanlık hale")]

# Bone pivots (Unity coordinates) per kind: the B export places the DC_HumanoidRig bones here; poses rotate about them.
PIVOTS = {
    "Raider": {"Spine": (0, 0.40, 0), "Head": (0, 0.78, 0), "Arm": (0.19, 0.74, 0), "Leg": (0.10, 0.40, 0)},
    "Vanguard": {"Spine": (0, 0.56, 0), "Head": (0, 1.20, 0), "Arm": (0.38, 1.06, 0), "Leg": (0.17, 0.56, 0)},
    "Regenerator": {"Spine": (0, 0.48, 0), "Head": (0, 1.00, -0.1), "Arm": (0.30, 0.98, -0.04),
                    "Leg": (0.14, 0.48, 0)},
    "Support": {"Spine": (0, 0.30, 0), "Head": (0, 1.06, 0), "Arm": (0.22, 0.98, 0), "Leg": (0.12, 0.30, 0)},
    "Siege": {"Spine": (0, 0.40, 0), "Head": (0, 1.0, 0), "Arm": (0, 0.64, 0), "Leg": (0.50, 0.31, 0)},
    "Boss": {"Spine": (0, 0.78, 0), "Head": (0, 1.62, 0), "Arm": (0.62, 1.42, 0), "Leg": (0.24, 0.78, 0)},
    "Guard": {"Spine": (0, 0.44, 0), "Head": (0, 0.95, 0), "Arm": (0.22, 0.84, 0), "Leg": (0.11, 0.44, 0)},
    "Knight": {"Spine": (0, 0.56, 0), "Head": (0, 1.12, 0), "Arm": (0.30, 1.00, 0), "Leg": (0.13, 0.56, 0)},
}
# Pose = {bone: forward swing degrees (+ = toward the walking direction) or (swing, side roll)}; "lift"/"ram" move.
POSES = {
    "Move": {
        "Raider": {"Spine": 16, "Leg.L": 35, "Leg.R": -35, "Arm.L": -35, "Arm.R": 30, "lift": 0.05},
        "Vanguard": {"Spine": 4, "Leg.L": 18, "Leg.R": -18, "Arm.R": 12, "lift": 0.02},
        "Regenerator": {"Spine": 10, "Head": 10, "Leg.L": 18, "Leg.R": -18, "Arm.L": -16, "Arm.R": 16},
        "Support": {"Spine": (3, 4), "Arm.L": -12, "Arm.R": 8, "lift": 0.04},
        "Siege": {"Spine": (3, 2), "ram": 0.12, "lift": 0.02},
        "Boss": {"Spine": 6, "Leg.L": 20, "Leg.R": -20, "Arm.L": -18, "Arm.R": 10, "lift": 0.02},
        "Guard": {"Spine": 5, "Leg.L": 25, "Leg.R": -25, "Arm.L": -10, "Arm.R": 12, "lift": 0.04},
        "Knight": {"Spine": 6, "Leg.L": 25, "Leg.R": -25, "Arm.L": -25, "Arm.R": 15, "lift": 0.04},
    },
    "Ability": {
        "Raider": {"Spine": 24, "Arm.R": (60, 40), "Arm.L": (-30, 30), "Leg.L": 40, "Leg.R": -30, "fx": "glow_gold"},
        "Vanguard": {"Spine": 12, "Arm.L": 35, "Arm.R": -30, "Leg.L": 14, "Leg.R": -10, "fx": "iron"},
        "Regenerator": {"Spine": -14, "Head": -20, "Arm.L": (10, 70), "Arm.R": (10, 70), "fx": "glow_green"},
        "Support": {"Spine": -8, "Head": -10, "Arm.R": (-25, 25), "Arm.L": (60, 50), "fx": "glow_green"},
        "Siege": {"Spine": 5, "ram": -0.45, "fx": None},
        "Boss": {"Spine": -12, "Arm.R": (70, -20), "Arm.L": (60, 20), "fx": "glow_red"},
        "Guard": {"Spine": 15, "Arm.R": (-60, 0), "Arm.L": 30, "Leg.L": 30, "Leg.R": -15, "fx": None},
        "Knight": {"Spine": -6, "Arm.R": (60, 30), "Arm.L": (-20, 25), "fx": "glow_gold"},
    },
}


def apply_pose(kind, pose):
    if not pose:
        return
    piv = PIVOTS[kind]

    def pivot(name):
        if name in ("Spine", "Head"):
            return piv[name]
        x, y, z = piv[name[:-2]]
        return (-x if name.endswith(".L") else x, y, z)

    def matrix(name):
        value = pose.get(name)
        if value is None:
            return Matrix.Identity(4)
        swing, roll = value if isinstance(value, tuple) else (value, 0)
        sign = 1 if name in ("Spine", "Head") else -1
        roll = -roll if name.endswith(".R") else roll
        p = kit.U(pivot(name))
        rot = Matrix.Rotation(math.radians(roll), 4, "Y") @ Matrix.Rotation(math.radians(sign * swing), 4, "X")
        return Matrix.Translation(p) @ rot @ Matrix.Translation(-p)

    spine = matrix("Spine")
    lift = Matrix.Translation((0, 0, pose.get("lift", 0.0)))
    for obj in [o for o in bpy.context.scene.objects if "av3_bone" in o]:
        name = obj["av3_bone"]
        if name == "Spine":
            m = spine
        elif name in ("Head", "Arm.L", "Arm.R"):
            m = spine @ matrix(name)
            if name == "Arm.R" and "ram" in pose:
                m = spine @ Matrix.Translation(kit.U((0, 0, pose["ram"])))
        elif name in ("Leg.L", "Leg.R"):
            m = matrix(name)
        else:
            m = Matrix.Identity(4)
        obj.matrix_world = lift @ m @ obj.matrix_world
    fx = pose.get("fx")
    if fx:
        bone("Root")
        torus("AbilityRing", (0, 0.05, 0), 0.75 if kind != "Boss" else 1.05, 0.035, fx)




# --------------------------------------------------------------------------------------------- friendlies


def guard():
    """Barracks soldier: steel-blue tabard, kettle helm, heater shield with white plaque, tall spear."""
    legs(0.11, 0.44, 0.075, "navy", "leather", (0.15, 0.13, 0.25))
    bone("Spine")
    lathe("Torso", [(0, 0.38), (0.21, 0.40), (0.24, 0.58), (0.23, 0.80), (0.16, 0.93), (0, 0.95)], "steel",
          round_r=0.05, segments=12)
    rbox("Mail", (0, 0.74, -0.19), (0.30, 0.30, 0.08), "metal", bevel=0.04)
    ring("Belt", 0.46, 0.235, 0.06, "leather", segments=12)
    rbox("Buckle", (0, 0.49, -0.24), (0.07, 0.07, 0.03), "gold", bevel=0.012)
    for side in (-1, 1):
        ball("Pauldron", (side * 0.24, 0.88, 0), 0.11, "gold", sy=0.7, seg=10, rings=4)
    bone("Head")
    ball("Face", (0, 1.06, -0.04), 0.15, "stone_warm", seg=10, rings=5)
    lathe("Helmet", [(0.19, 1.07), (0.20, 1.15), (0.17, 1.26), (0.08, 1.32), (0, 1.33)], "metal", round_r=0.04,
          segments=12)
    torus("Brim", (0, 1.08, 0), 0.22, 0.035, "metal")
    rbox("Visor", (0, 1.04, -0.175), (0.20, 0.04, 0.04), "dark", bevel=0.012)
    ball("HelmTip", (0, 1.35, 0), 0.04, "gold", seg=8, rings=4)
    bone("Arm.L")
    limb("ArmL", (-0.22, 0.84, 0), (-0.27, 0.62, -0.16), 0.06, "steel")
    shield = kit.shield_points(0.46, 0.58)
    outline("ShieldRim", kit.shield_points(0.53, 0.66), 0.05, "gold", at=(-0.22, 0.28, -0.27), rot=(0, 14, 0))
    outline("Shield", [(x, y + 0.045) for x, y in shield], 0.06, "steel", at=(-0.22, 0.28, -0.30), rot=(0, 14, 0),
            bevel=0.02)
    outline("ShieldPlaque", kit.shield_points(0.20, 0.24), 0.03, "cloth", at=(-0.225, 0.52, -0.34), rot=(0, 14, 0))
    bone("Arm.R")
    limb("ArmR", (0.22, 0.84, 0), (0.30, 0.66, -0.08), 0.06, "steel")
    ball("HandR", (0.31, 0.64, -0.09), 0.055, "stone_warm", seg=8, rings=4)
    limb("Spear", (0.31, 0.05, -0.09), (0.31, 1.70, -0.09), 0.025, "wood", seg=8)
    ring("SpearCollar", 1.66, 0.035, 0.06, "gold", at=(0.31, 0, -0.09), segments=8, out=0.012)
    gem("SpearTip", (0.31, 1.70, -0.09), 0.06, 0.28, "metal", sides=4, grad=(0.3, 1.0))
    humanoid_sockets((0, 0.70, -0.03), (0, 1.36, 0), hand_r=(0.31, 0.64, -0.09), hand_l=(-0.27, 0.62, -0.16),
                     tip=(0.31, 1.96, -0.09))


def knight():
    """Hero: polished armour, wide blue cape, gold crown crest with plume, raised sword with glowing gem."""
    legs(0.13, 0.56, 0.095, "metal", "steel_dark", (0.18, 0.15, 0.30))
    bone("Spine")
    lathe("Torso", [(0, 0.50), (0.26, 0.52), (0.30, 0.72), (0.30, 1.00), (0.21, 1.15), (0, 1.18)], "metal",
          round_r=0.06, segments=12)
    rbox("Tabard", (0, 0.72, -0.27), (0.30, 0.54, 0.05), "steel", bevel=0.02, taper=1.15)
    ring("Belt", 0.76, 0.305, 0.07, "gold", segments=12)
    shell("Cape", [(0.28, 1.13), (0.33, 0.95), (0.43, 0.52), (0.52, 0.10)], 32, 148, "steel", thickness=0.045,
          segs=10)
    for side in (-1, 1):
        ball("Pauldron", (side * 0.32, 1.08, 0), 0.15, "metal", sy=0.75, seg=10, rings=5)
        torus("PauldronRim", (side * 0.32, 1.03, 0), 0.14, 0.025, "gold")
    bone("Head")
    lathe("Helmet", [(0.21, 1.10), (0.23, 1.24), (0.21, 1.40), (0.12, 1.51), (0, 1.54)], "metal", round_r=0.05,
          segments=12)
    rbox("Visor", (0, 1.30, -0.215), (0.26, 0.045, 0.05), "dark", bevel=0.015)
    ring("Crown", 1.42, 0.205, 0.08, "gold", segments=12)
    for i in range(5):
        a = math.radians(-90 + (i - 2) * 32)
        cone_to("CrownPoint", (math.cos(a) * 0.2, 1.48, math.sin(a) * 0.2),
                (math.cos(a) * 0.22, 1.60, math.sin(a) * 0.22), 0.04, "gold", seg=5)
    tube("Plume", [(0, 1.52, -0.02), (0, 1.66, 0.08), (0, 1.62, 0.28), (0, 1.44, 0.40)], 0.07, "steel",
         taper_end=0.35, sides=8)
    bone("Arm.L")
    limb("ArmL", (-0.30, 1.0, 0), (-0.34, 0.74, -0.10), 0.075, "metal")
    ball("HandL", (-0.35, 0.72, -0.11), 0.07, "steel_dark", seg=8, rings=4)
    bone("Arm.R")
    limb("ArmR", (0.30, 1.0, 0), (0.42, 1.10, -0.16), 0.075, "metal")
    ball("HandR", (0.43, 1.11, -0.17), 0.07, "steel_dark", seg=8, rings=4)
    sword = [(-0.05, 0.0), (0.05, 0.0), (0.055, 0.62), (0.0, 0.76), (-0.055, 0.62)]
    outline("Blade", sword, 0.035, "metal", at=(0.43, 1.18, -0.17), rot=(0, 0, 22), bevel=0.012)
    rbox("CrossGuard", (0.43, 1.17, -0.17), (0.26, 0.05, 0.07), "gold", bevel=0.02, rot=(0, 0, 22))
    ball("SwordGem", (0.43, 1.17, -0.215), 0.035, "glow_cyan", seg=8, rings=4)
    ball("Pommel", (0.40, 1.04, -0.17), 0.04, "gold", seg=8, rings=4)
    humanoid_sockets((0, 0.92, -0.03), (0, 1.62, 0), hand_r=(0.43, 1.11, -0.17), hand_l=(-0.35, 0.72, -0.11),
                     tip=(0.72, 1.86, -0.17))


BUILDERS = {"Raider": raider, "Vanguard": vanguard, "Regenerator": regenerator, "Support": support,
            "Siege": siege, "Boss": boss, "Guard": guard, "Knight": knight}


# ============================================================================================ export (B)

RIG_NAME = "DC_HumanoidRig"


def _bone_layout(kind):
    piv = PIVOTS[kind]
    sx, sy, sz = piv["Spine"]
    hx, hy, hz = piv["Head"]
    ax, ay, az = piv["Arm"]
    lx, ly, lz = piv["Leg"]
    arm_len = 0.30 if kind != "Boss" else 0.40
    return {
        "Root": ((0, 0, 0), (0, sy, 0), None),
        "Spine": ((sx, sy, sz), (hx, hy, hz), "Root"),
        "Head": ((hx, hy, hz), (hx, hy + 0.35, hz), "Spine"),
        "Arm.L": ((-ax, ay, az), (-ax, ay - arm_len, az), "Spine"),
        "Arm.R": ((ax, ay, az), (ax, ay - arm_len, az), "Spine"),
        "Leg.L": ((-lx, ly, lz), (-lx, 0.04, lz), "Root"),
        "Leg.R": ((lx, ly, lz), (lx, 0.04, lz), "Root"),
    }


def add_rig(kind):
    """Same bone names/hierarchy as the shipped DC_HumanoidRig; heads sit on this actor's joints.

    Limb bones point straight down so their local X is the forward-swing axis used by the role clips.
    """
    data = bpy.data.armatures.new(RIG_NAME)
    rig = bpy.data.objects.new(RIG_NAME, data)
    bpy.context.collection.objects.link(rig)
    for other in bpy.context.selected_objects:
        other.select_set(False)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    made = {}
    for name, (head, tail, parent) in _bone_layout(kind).items():
        b = data.edit_bones.new(name)
        b.head = kit.U(head)
        b.tail = kit.U(tail)
        b.roll = 0.0
        if parent:
            b.parent = made[parent]
        made[name] = b
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def _finish_groups(kind, mat):
    """Paint every part, then join one mesh per (group, bone); returns {object: bone}."""
    bpy.context.view_layer.update()
    parts = [o for o in bpy.context.scene.objects if o.type == "MESH" and "tv3_swatch" in o]
    buckets = {}
    for obj in parts:
        obj.data.materials.clear()
        obj.data.materials.append(mat)
        kit._paint(obj)
        obj.data.shade_smooth()
        obj.data.set_sharp_from_angle(angle=math.radians(obj["tv3_sharp"]))
        buckets.setdefault((obj.get("av3_group", ""), obj["av3_bone"]), []).append(obj)
    owners = {}
    for (grp, bone_name), objs in buckets.items():
        for other in bpy.context.selected_objects:
            other.select_set(False)
        for obj in objs:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        if len(objs) > 1:
            bpy.ops.object.join()
        body = bpy.context.view_layer.objects.active
        if grp.startswith("Phase"):
            name = grp
        elif grp:
            name = f"{grp}_{bone_name}"
        else:
            name = f"{kind}_{bone_name}"
        body.name = name
        body.data.name = f"{name}_Mesh"
        for key in list(body.keys()):
            del body[key]
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        owners[body] = bone_name
    return owners


def build_for_export(kind, mat):
    """Build the export scene for one actor: per-bone meshes on tower_palette, rig, sockets (generate_lowpoly ASSETS)."""
    bone("Root")
    group("")
    if kind == "Boss":
        boss(None, cape=False)
        for ornament in ORNAMENTS:
            group(f"Ornament_{ornament}")
            boss_cape(CAPES[ornament])
            ORNAMENTS[ornament]()
        group("")
        boss_phase_parts()
    else:
        BUILDERS[kind]()
    sockets = {o: o["av3_bone"] for o in bpy.context.scene.objects if o.type == "EMPTY" and "av3_bone" in o}
    for obj in sockets:
        del obj["av3_bone"]
    owners = _finish_groups(kind, mat)
    owners.update(sockets)
    rig = add_rig(kind)
    bpy.context.view_layer.update()
    for obj, bone_name in owners.items():
        world = obj.matrix_world.copy()
        obj.parent = rig
        obj.parent_type = "BONE"
        obj.parent_bone = bone_name
        obj.matrix_world = world
    return rig


def export_test(kind, out_dir):
    """Standalone check (no Assets writes): build + export one actor FBX into out_dir."""
    import render_tower_v3_sheet as rts
    rts.reset()
    mat = kit.palette_material(OUT / "_palette")
    build_for_export(kind, mat)
    for obj in bpy.context.scene.objects:
        obj.select_set(obj.type in {"MESH", "ARMATURE", "EMPTY"})
    Path(out_dir).mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(Path(out_dir) / f"{kind}.fbx"), use_selection=True,
                             object_types={"MESH", "ARMATURE", "EMPTY"}, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", bake_anim=False, add_leaf_bones=False,
                             path_mode="AUTO")
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    print(f"EXPORT {kind}: meshes={len(meshes)} tris={tris} names={sorted(o.name for o in meshes)}")


# ============================================================================================ review renders


def build_actor(kind, palette_dir, ornament=None, pose=None):
    import render_tower_v3_sheet as rts
    rts.reset()
    bone("Root")
    mat = kit.palette_material(palette_dir, emission_strength=0.7)
    if kind == "Boss":
        boss(ornament)
    else:
        BUILDERS[kind]()
    bpy.context.view_layer.update()
    apply_pose(kind, POSES[pose][kind] if pose else None)
    for obj in [o for o in bpy.context.scene.objects if o.type == "EMPTY"]:
        bpy.data.objects.remove(obj)
    return kit.finish_tower(kind, mat)


def _frame(kind):
    return {"Boss": (1.3, 3.6), "Siege": (0.6, 3.4)}.get(kind, (0.78, 2.0))


def render_closeup(kind, path, palette_dir, ornament=None, size=520, yaw=-20, pose=None, samples=48):
    import render_tower_v3_sheet as rts
    body = build_actor(kind, palette_dir, ornament, pose)
    s = NEW_SCALE[kind]
    body.scale = (s, s, s)
    body.rotation_euler = (0, 0, math.radians(yaw))
    scene = bpy.context.scene
    rts.setup_render(scene, (size, size), samples=samples)
    rts.setup_world(scene)
    rts.add_lights()
    rts.add_ground()
    target, ortho = _frame(kind)
    rts.add_camera(target, ortho)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def render_game(kind, path, palette_dir, ppu, ornament=None, yaw=90, extent=4.0, pose=None):
    import render_tower_v3_sheet as rts
    body = build_actor(kind, palette_dir, ornament, pose)
    s = NEW_SCALE[kind]
    body.scale = (s, s, s)
    body.rotation_euler = (0, 0, math.radians(yaw))
    scene = bpy.context.scene
    px = int(round(extent * ppu))
    rts.setup_render(scene, (px, px), transparent=True, samples=64)
    rts.setup_world(scene)
    rts.add_lights()
    rts.add_ground(catcher=True)
    rts.add_camera(0.0, extent)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def render_main(argv):
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--render", default="a1")
    parser.add_argument("--out", required=True)
    parser.add_argument("--only", nargs="*")
    parser.add_argument("--skip-game", action="store_true")
    parser.add_argument("--game-only", action="store_true")
    args = parser.parse_args(argv)
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    palette_dir = OUT / "_palette"
    kinds = [k for k in ACTOR_KINDS if not args.only or k in args.only]
    if args.render == "export-test":
        for kind in kinds:
            export_test(kind, out)
        return
    if args.render == "a2":
        render_a2(out, palette_dir, kinds, args.game_only)
        return
    for kind in kinds:
        if not args.game_only:
            render_closeup(kind, out / f"{kind}.png", palette_dir, yaw=55 if kind == "Siege" else -20)
        if args.skip_game:
            continue
        for h in (720, 960):
            ppu = h / 17.8
            extent = 6.0 if kind == "Boss" else 4.0
            for yaw, tag in ((90, "r"), (0, "f"), (-90, "l")):
                if h == 960 and tag != "f":
                    continue
                render_game(kind, out / f"{kind}_game{h}_{tag}.png", palette_dir, ppu, yaw=yaw, extent=extent)
        print(f"Rendered {kind}")


def measure(kind, palette_dir, ornament=None):
    """Width across the road (front view, x) and length along it (z) in world units at the proposed scale."""
    body = build_actor(kind, palette_dir, ornament)
    s = NEW_SCALE[kind]
    xs = [v.co.x * s for v in body.data.vertices]
    ys = [v.co.y * s for v in body.data.vertices]
    zs = [v.co.z * s for v in body.data.vertices]
    return {"width": round(max(xs) - min(xs), 3), "length": round(max(ys) - min(ys), 3),
            "height": round(max(zs) - min(zs), 3)}


def render_a2(out, palette_dir, kinds, game_only=False):
    import json
    sizes = {}
    for kind in kinds:
        for pose in (None, "Move", "Ability"):
            tag = pose or "Idle"
            if not game_only:
                render_closeup(kind, out / f"{kind}_{tag}.png", palette_dir, size=380, samples=32, pose=pose,
                               ornament="region-lord" if kind == "Boss" else None,
                               yaw=55 if kind == "Siege" else (-20 if pose is None else -55))
        for h in (720,):
            extent = 6.0 if kind == "Boss" else 4.0
            for yaw, face in ((90, "r"), (0, "f"), (-90, "l")):
                render_game(kind, out / f"{kind}_game{h}_{face}.png", palette_dir, h / 17.8, yaw=yaw, extent=extent,
                            pose="Move", ornament="region-lord" if kind == "Boss" else None)
        sizes[kind] = measure(kind, palette_dir, "region-lord" if kind == "Boss" else None)
        print(f"Rendered {kind}")
    if "Boss" in kinds:
        for ornament, _, _ in BOSS_ORNAMENT_LABELS:
            if not game_only:
                render_closeup("Boss", out / f"Boss_{ornament}.png", palette_dir, ornament=ornament, size=460,
                               samples=48)
            render_game("Boss", out / f"Boss_{ornament}_game720.png", palette_dir, 720 / 17.8, ornament=ornament,
                        yaw=90, extent=6.0)
            print(f"Rendered Boss {ornament}")
    path = out / "sizes.json"
    merged = json.loads(path.read_text()) if path.exists() else {}
    merged.update(sizes)
    path.write_text(json.dumps(merged, indent=2))


# ============================================================================================ sheet (Pillow)

BG = (9, 19, 29)
INK = (233, 238, 245)
MUTED = (150, 166, 186)
GOLD = (242, 193, 78)


def _font(size, bold=False):
    from PIL import ImageFont
    name = "NotoSans-Bold.ttf" if bold else "NotoSans-Regular.ttf"
    return ImageFont.truetype(str(ROOT / "Assets" / "Art" / "Fonts" / name), size)


TOWER_SLOTS = {
    720: [(230, 304), (496, 243), (367, 453), (519, 469), (738, 469), (842, 313), (922, 373), (1058, 260)],
    960: [(230, 424), (496, 363), (367, 573), (519, 589), (738, 589), (842, 433), (922, 493), (1058, 380)],
}
TOWERS = ["archer_L1", "mage_L1", "barracks_L1", "archer_L2", "barracks_ShieldGuard", "mage_Frost",
          "archer_RapidFire", "mage_L2"]
# (kind, x, y at 720, facing tag) along the chapter-1 road; y+120 for the 1280x960 frame.
ROAD_ACTORS_720 = [
    ("Raider", 120, 478, "r"), ("Raider", 168, 470, "r"), ("Vanguard", 232, 478, "r"), ("Support", 297, 405, "r"),
    ("Regenerator", 360, 300, "r"), ("Siege", 440, 300, "r"), ("Vanguard", 515, 368, "f"), ("Raider", 590, 432, "r"),
    ("Support", 650, 428, "r"), ("Regenerator", 772, 352, "r"), ("Boss", 885, 272, "r"),
    ("Guard", 700, 434, "l"), ("Guard", 726, 422, "l"), ("Knight", 990, 330, "f"),
]


def _paste_actor(canvas, tile_path, x, y):
    from PIL import Image
    tile = Image.open(tile_path).convert("RGBA")
    canvas.alpha_composite(tile, (int(x - tile.width / 2), int(y - tile.height / 2)))


def battle_panel(h, src, actors, legacy=False):
    from PIL import Image
    battle = Image.open(ROOT / "Builds" / f"step6-after-chapter1-prep-1280x{h}.png").convert("RGBA")
    ppu = h / 17.8
    tower_src = ROOT / "Builds" / "towers-v3" / "a2"
    items = [(sy + 0.2 * 0.73 * ppu, "tower", tag, sx) for (sx, sy), tag in zip(TOWER_SLOTS[h], TOWERS)]
    dy = 0 if h == 720 else 120
    for kind, x, y, face in actors:
        items.append((y + dy, "actor", (kind, face), x))
    for y, what, tag, x in sorted(items, key=lambda item: item[0]):
        if what == "tower":
            _paste_actor(battle, tower_src / f"{tag}_game{h}.png", x, y)
        else:
            kind, face = tag
            name = f"{kind}_old_game{h}_r.png" if legacy else f"{kind}_game{h}_{face}.png"
            _paste_actor(battle, src / name, x, y)
    return battle.convert("RGB")


def grey_strip(paths, height=64, label_font=None):
    from PIL import Image
    tiles = []
    for p in paths:
        g = Image.open(p).convert("RGBA")
        g = g.crop(g.split()[3].point(lambda a: 255 if a > 90 else 0).getbbox())
        scale = height / max(g.height, 1)
        g = g.resize((max(1, int(g.width * scale)), height), Image.LANCZOS)
        back = Image.new("RGBA", (max(g.width + 12, 70), height + 12), (22, 34, 44, 255))
        back.alpha_composite(g, ((back.width - g.width) // 2, 6))
        tiles.append(back.convert("L").convert("RGB"))
    return tiles


def compose_a1():
    from PIL import Image, ImageDraw
    src = OUT / "a1"
    pad, tile, old_tile = 24, 300, 150
    cols = len(ACTOR_KINDS)
    width = pad * 2 + cols * tile + (cols - 1) * 10
    battle_h = 720
    height = 120 + tile + 40 + old_tile + 40 + 130 + 60 + battle_h + 40
    sheet = Image.new("RGB", (width, height), BG)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 20), "BİRLİKLER V3  ·  TASLAK A1 (kule yönü 1: yumuşak stilize)", font=_font(34, True), fill=GOLD)
    d.text((pad, 66), "Düşmanlar: ortak kızıl ordu rengi + tip vurgusu + ayrı siluet · dostlar: mavi/çelik + altın · "
           "oyun kamerası 43° · kulelerle aynı atlas/malzeme · ölçek eskinin ~%25 fazlası", font=_font(17), fill=MUTED)
    y0 = 112
    for c, kind in enumerate(ACTOR_KINDS):
        x = pad + c * (tile + 10)
        im = Image.open(src / f"{kind}.png").convert("RGB").resize((tile, tile), Image.LANCZOS)
        sheet.paste(im, (x, y0))
        d.text((x + 10, y0 + 8), LABELS[kind], font=_font(19, True), fill=INK)
        d.text((x + 10, y0 + tile - 26), NOTES[kind], font=_font(13), fill=INK)
        old = Image.open(src / f"{kind}_old.png").convert("RGB").resize((old_tile, old_tile), Image.LANCZOS)
        oy = y0 + tile + 30
        sheet.paste(old, (x, oy))
        d.text((x + old_tile + 8, oy + 4), "ESKİ", font=_font(14, True), fill=MUTED)
        d.text((x + old_tile + 8, oy + 24), "(oyunda 2 malzemeye", font=_font(12), fill=MUTED)
        d.text((x + old_tile + 8, oy + 40), "indirgenmiş hali)", font=_font(12), fill=MUTED)
        d.text((x + old_tile + 8, oy + 70), f"ölçek {OLD_SCALE[kind]:.2f}", font=_font(12), fill=MUTED)
        d.text((x + old_tile + 8, oy + 86), f"yeni {NEW_SCALE[kind]:.2f}", font=_font(12, True), fill=INK)
    d.text((pad, y0 + tile + 6), "Üst: yeni (yakın plan, aynı ışık) · alt: eski model aynı açıdan", font=_font(14),
           fill=MUTED)
    gy = y0 + tile + 30 + old_tile + 30
    d.text((pad, gy), "64 px gri tonlama · siluet/değer kontrolü (üst sıra yeni, alt sıra eski; aynı sıra: "
           "Yağmacı, Öncü, Yenilenen, Şifacı, Kuşatma, Boss, Muhafız, Kahraman)", font=_font(16, True), fill=INK)
    x = pad
    for g in grey_strip([src / f"{k}_game960_f.png" for k in ACTOR_KINDS]):
        sheet.paste(g, (x, gy + 28))
        x += g.width + 8
    x = pad
    for g in grey_strip([src / f"{k}_old_game720_r.png" for k in ACTOR_KINDS], height=48):
        sheet.paste(g, (x, gy + 28 + 84))
        x += g.width + 8
    by = gy + 200
    d.text((pad, by), "SAVAŞ EKRANI 1:1 (1280×720) · sol: yeni birlikler + v3 kuleler · sağ: aynı yerlerde ESKİ "
           "birlikler", font=_font(18, True), fill=INK)
    new_panel = battle_panel(720, src, ROAD_ACTORS_720)
    old_panel = battle_panel(720, src, ROAD_ACTORS_720, legacy=True)
    sheet.paste(new_panel, (pad, by + 32))
    rx = pad * 2 + 1280
    rw = width - rx - pad
    old_small = old_panel.resize((640, 360), Image.LANCZOS)
    sheet.paste(old_small, (rx, by + 32))
    old_zoom = old_panel.crop((200, 270, 200 + (rw - 652) // 2, 270 + 180)).resize(((rw - 652) // 2 * 2, 360),
                                                                               Image.LANCZOS)
    sheet.paste(old_zoom, (rx + 652, by + 32))
    d.text((rx + 660, by + 36), "ESKİ · 2× (aynı kesitin solu)", font=_font(14, True), fill=GOLD)
    d.text((rx + 8, by + 36), "ESKİ birlikler · aynı kare (küçültülmüş)", font=_font(14, True), fill=GOLD)
    zy = by + 32 + old_small.height + 12
    zh = by + 32 + 720 - zy
    zoom = new_panel.crop((200, 270, 200 + rw // 2, 270 + zh // 2)).resize((rw // 2 * 2, zh // 2 * 2), Image.LANCZOS)
    sheet.paste(zoom, (rx, zy))
    d.text((rx + 8, zy + 4), "YENİ · 2× büyütme (aynı kesit)", font=_font(14, True), fill=GOLD)
    sheet.save(OUT / "actors-a1-sheet.png")
    print(OUT / "actors-a1-sheet.png")


# --------------------------------------------------------------------------------------------- A2 sheet

PPU_720 = 720 / 17.8
VERTICAL = math.cos(math.radians(43))
ROAD_WIDTH = 1.73  # world units, measured from the 1280x720 chapter-1 frame (70 px / 40.4 px per unit)
TOKENS = {"Danger": (239, 106, 79), "Gold": (242, 184, 75), "GoldHigh": (255, 217, 138), "TextPrimary": (244, 250, 255),
          "TextSecondary": (165, 187, 204), "NightDeep": (2, 5, 10), "SurfaceRaised": (8, 15, 28), "Moon": (155, 203, 255)}
# Dense wave on the same road (720 frame): (kind, x, y, facing, health fraction or None when undamaged).
WAVE_720 = [
    ("Raider", 112, 480, "r", 0.35), ("Raider", 150, 470, "r", None), ("Raider", 186, 482, "r", 0.8),
    ("Vanguard", 238, 476, "r", 0.6), ("Support", 297, 430, "l", None), ("Regenerator", 297, 372, "l", 0.45),
    ("Siege", 380, 300, "r", 0.7), ("Raider", 452, 296, "r", 0.2), ("Vanguard", 515, 352, "f", 0.9),
    ("Support", 572, 432, "r", 0.5), ("Regenerator", 632, 428, "r", None), ("Boss", 772, 340, "l", 0.58),
    ("Guard", 684, 436, "l", 0.75), ("Guard", 708, 420, "l", None), ("Knight", 880, 268, "l", None),
]


def _pill(d, box, fill, outline=None, width=1):
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0, y0, x1, y1), radius=(y1 - y0) / 2, fill=fill, outline=outline, width=width)


def draw_bar(d, cx, cy, fraction, boss=False, proposed=True, shield=False):
    """Bars are sized from the world-space canvas: size units x 0.012 world units x px per unit."""
    t = TOKENS
    if not proposed:
        w, h = (146, 14) if boss else (100, 8)
        w, h = w * 0.012 * PPU_720, h * 0.012 * PPU_720
        box = (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)
        _pill(d, box, t["TextSecondary"])
        inner = (box[0] + 1, box[1] + 1, box[2] - 1, box[3] - 1)
        _pill(d, inner, t["SurfaceRaised"])
        _pill(d, (inner[0], inner[1], inner[0] + (inner[2] - inner[0]) * fraction, inner[3]),
              t["Gold"] if boss else t["Danger"])
        return
    w, h = (210, 26) if boss else (110, 17)
    w, h = w * 0.012 * PPU_720, h * 0.012 * PPU_720
    box = (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)
    _pill(d, (box[0] - 1, box[1] - 1, box[2] + 1, box[3] + 1), t["NightDeep"])
    _pill(d, box, t["Gold"] if boss else t["TextSecondary"])
    pad = 2 if boss else 1.5
    inner = (box[0] + pad, box[1] + pad, box[2] - pad, box[3] - pad)
    _pill(d, inner, t["SurfaceRaised"])
    trail = min(1.0, fraction + 0.12)
    width = inner[2] - inner[0]
    _pill(d, (inner[0], inner[1], inner[0] + width * trail, inner[3]), t["TextPrimary"])
    _pill(d, (inner[0], inner[1], inner[0] + width * fraction, inner[3]), t["Danger"])
    d.line((inner[0] + h * 0.3, inner[1] + 1, inner[0] + width * fraction - h * 0.3, inner[1] + 1),
           fill=(255, 168, 140), width=1)
    if boss:
        for mark in (0.66, 0.33):
            x = inner[0] + width * mark
            d.line((x, box[1] - 1, x, box[3] + 1), fill=t["GoldHigh"], width=2)
        r = h * 0.85
        d.ellipse((box[0] - r * 1.4, cy - r, box[0] - r * 1.4 + 2 * r, cy + r), fill=t["NightDeep"],
                  outline=t["Gold"], width=2)
        d.polygon([(box[0] - r * 0.95, cy + r * 0.35), (box[0] - r * 0.95, cy - r * 0.25),
                   (box[0] - r * 0.7, cy + r * 0.05), (box[0] - r * 0.4, cy - r * 0.45),
                   (box[0] - r * 0.1, cy + r * 0.05), (box[0] + r * 0.15, cy - r * 0.25),
                   (box[0] + r * 0.15, cy + r * 0.35)], fill=t["Gold"])
    if shield:
        sx = box[0] - h * 0.9
        d.polygon([(sx - h * 0.55, cy - h * 0.6), (sx + h * 0.55, cy - h * 0.6), (sx + h * 0.55, cy + h * 0.1),
                   (sx, cy + h * 0.75), (sx - h * 0.55, cy + h * 0.1)], fill=t["Moon"], outline=t["NightDeep"])


def wave_panel(src, proposed=True, legacy_src=None):
    import json
    from PIL import Image, ImageDraw
    battle = Image.open(ROOT / "Builds" / "step6-after-chapter1-prep-1280x720.png").convert("RGBA")
    sizes = json.loads((src / "sizes.json").read_text())
    tower_src = ROOT / "Builds" / "towers-v3" / "a2"
    items = [(sy + 0.2 * VERTICAL * PPU_720, "tower", tag, sx, None) for (sx, sy), tag in zip(TOWER_SLOTS[720], TOWERS)]
    for kind, x, y, face, frac in WAVE_720:
        items.append((y, "actor", (kind, face), x, frac))
    bars = []
    for y, what, tag, x, frac in sorted(items, key=lambda item: item[0]):
        if what == "tower":
            _paste_actor(battle, tower_src / f"{tag}_game720.png", x, y)
            continue
        kind, face = tag
        if legacy_src is not None:
            _paste_actor(battle, legacy_src / f"{kind}_old_game720_r.png", x, y)
        else:
            _paste_actor(battle, src / f"{kind}_game720_{face}.png", x, y)
        boss = kind == "Boss"
        if frac is None and not boss or kind in ("Guard", "Knight"):
            continue
        if proposed:
            top = sizes[kind]["height"] + (0.30 if boss else 0.22)
        else:
            top = (sizes[kind]["height"] / NEW_SCALE[kind] * OLD_SCALE[kind] + 0.8) if boss else 1.65
        bars.append((x, y - top * VERTICAL * PPU_720, frac if frac is not None else 1.0, boss, kind == "Vanguard"))
    d = ImageDraw.Draw(battle)
    for x, y, frac, boss, shield in bars:
        draw_bar(d, x, y, frac, boss, proposed, shield and proposed)
    return battle.convert("RGB")


def compose_a2():
    import json
    from PIL import Image, ImageDraw
    src = OUT / "a2"
    a1 = OUT / "a1"
    sizes = json.loads((src / "sizes.json").read_text())
    pad = 24
    width = 2560
    sheet = Image.new("RGB", (width, 2140), BG)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 20), "BİRLİKLER V3  ·  A2: BOSS BÖLGE SÜSLERİ · HAREKET POZLARI · CAN BARI", font=_font(34, True), fill=GOLD)
    d.text((pad, 66), "Onaylı A1 gövdeleri · ortak boss gövdesi + profil kimliğine göre süs (tek model, süs grupları) · "
           "ölçek: küçük birlikler +%40, diğerleri +%25", font=_font(17), fill=MUTED)

    y = 108
    d.text((pad, y), "1 · BOSS: ORTAK GÖVDE + BÖLGE SÜSÜ (taç · pelerin rengi · aksesuar)", font=_font(20, True), fill=INK)
    tile = 400
    for i, (ornament, label, note) in enumerate(BOSS_ORNAMENT_LABELS):
        x = pad + i * (tile + 10)
        sheet.paste(Image.open(src / f"Boss_{ornament}.png").convert("RGB").resize((tile, tile), Image.LANCZOS),
                    (x, y + 32))
        d.text((x + 10, y + 40), label, font=_font(16, True), fill=INK)
        d.text((x + 10, y + 32 + tile - 26), f"{ornament} · {note}", font=_font(13), fill=INK)
    gx = pad + 5 * (tile + 10) + 10
    d.text((gx, y + 32), "oyun ölçeği 720p (%62 küçültülmüş)", font=_font(14, True), fill=MUTED)
    cx = gx
    for ornament, _, _ in BOSS_ORNAMENT_LABELS:
        g = Image.open(src / f"Boss_{ornament}_game720.png").convert("RGBA")
        g = g.crop(g.split()[3].point(lambda a: 255 if a > 90 else 0).getbbox())
        g = g.resize((int(g.width * 0.62), int(g.height * 0.62)), Image.LANCZOS)
        back = Image.new("RGBA", (g.width + 8, 110), (22, 34, 44, 255))
        back.alpha_composite(g, (4, 110 - g.height - 6))
        sheet.paste(back.convert("RGB"), (cx, y + 56))
        cx += back.width + 6
    d.text((gx, y + 180), "64 px gri tonlama", font=_font(14, True), fill=MUTED)
    cx = gx
    for g in grey_strip([src / f"Boss_{o}_game720.png" for o, _, _ in BOSS_ORNAMENT_LABELS]):
        sheet.paste(g, (cx, y + 204))
        cx += g.width + 6
    d.text((gx, y + 340), "Uygulama (B): Boss.fbx tek gövde; süsler", font=_font(13), fill=MUTED)
    d.text((gx, y + 358), "Ornament_<profil-id>_* grupları, yalnız", font=_font(13), fill=MUTED)
    d.text((gx, y + 376), "BossEncounterProfile.id eşleşeni açık.", font=_font(13), fill=MUTED)

    y += 32 + tile + 24
    d.text((pad, y), "2 · HAREKET POZLARI (Idle · Move · Ability birer kare; aynı iskelet, kemik başına tek mesh)",
           font=_font(20, True), fill=INK)
    pt = 196
    for c, kind in enumerate(ACTOR_KINDS):
        x = pad + c * (pt + 6)
        d.text((x + 4, y + 30), LABELS[kind], font=_font(14, True), fill=INK)
        for r, pose in enumerate(("Idle", "Move", "Ability")):
            im = Image.open(src / f"{kind}_{pose}.png").convert("RGB").resize((pt, pt), Image.LANCZOS)
            sheet.paste(im, (x, y + 52 + r * (pt + 4)))
            if c == 0:
                d.text((x + 6, y + 52 + r * (pt + 4) + pt - 22), pose, font=_font(13, True), fill=GOLD)
    notes = ["Ability: Yağmacı hançer atılışı · Öncü kalkan siper · Yenilenen yeşil nabız halkası",
             "Şifacı asa + iyileştirme halkası · Kuşatma koç vuruşu · Boss balta kaldırma",
             "Muhafız mızrak hamlesi · Kahraman kılıç kaldırma + altın halka"]
    for i, line in enumerate(notes):
        d.text((pad, y + 52 + 3 * (pt + 4) + 4 + i * 18), line, font=_font(13), fill=MUTED)

    hx = pad + 8 * (pt + 6) + 14
    d.text((hx, y), "3 · CAN BARI ÖNERİSİ (2.5× büyütme, aynı kare)", font=_font(20, True), fill=INK)
    old_panel = wave_panel(src, proposed=False, legacy_src=a1)
    new_panel = wave_panel(src, proposed=True)
    region = (40, 400, 440, 500)
    hw = width - hx - pad
    zoom_h = int((region[3] - region[1]) * hw / (region[2] - region[0]))
    for i, (panel, label) in enumerate(((old_panel, "ŞİMDİ · 100×8, ince, kontur yok"),
                                        (new_panel, "ÖNERİ · 110×17, koyu kontur + hasar izi + tepe ışığı"))):
        zy = y + 32 + i * (zoom_h + 34)
        sheet.paste(new_panel.crop(region).resize((hw, zoom_h), Image.LANCZOS) if i else
                    old_panel.crop(region).resize((hw, zoom_h), Image.LANCZOS), (hx, zy + 24))
        d.text((hx, zy), label, font=_font(15, True), fill=GOLD if i else MUTED)
    by = y + 32 + 2 * (zoom_h + 34)
    boss_region = (690, 205, 870, 261)
    bh = int((boss_region[3] - boss_region[1]) * (hw // 2 - 6) / (boss_region[2] - boss_region[0]))
    d.text((hx, by), "Boss: şimdi (altın dolgu)  ·  öneri (altın çerçeve, faz çentikleri %66/%33, taç rozeti)",
           font=_font(14, True), fill=INK)
    sheet.paste(old_panel.crop(boss_region).resize((hw // 2 - 6, bh), Image.LANCZOS), (hx, by + 22))
    sheet.paste(new_panel.crop(boss_region).resize((hw // 2 - 6, bh), Image.LANCZOS), (hx + hw // 2 + 6, by + 22))
    specs = ["Kod (B): GreyboxEnemy.CreateHealthBar boyutları + bar yüksekliği gövde sınırından (renderer bounds)",
             "Renkler yalnız UiTokens: Danger dolgu · TextPrimary hasar izi · TextSecondary/Gold kontur · NightDeep dış hat",
             "Öncü: mevcut ShieldStatusIcon korunur · dostlarda bar yok (mevcut davranış)"]
    for i, line in enumerate(specs):
        d.text((hx, by + 30 + bh + i * 18), line, font=_font(13), fill=MUTED)

    y = max(y + 52 + 3 * (pt + 4) + 70, by + 30 + bh + 3 * 18 + 24)
    d.text((pad, y), "4 · SAVAŞ EKRANI 1:1 (1280×720) · yoğun dalga, Move pozları, önerilen can barları",
           font=_font(20, True), fill=INK)
    sheet.paste(new_panel, (pad, y + 32))
    tx = pad + 1280 + 30
    d.text((tx, y + 32), "YOL GENİŞLİĞİ KONTROLÜ (dünya birimi)", font=_font(18, True), fill=INK)
    d.text((tx, y + 58), f"yol genişliği ~{ROAD_WIDTH:.2f} (720p karede 70 px)", font=_font(15), fill=MUTED)
    rows = [("Birlik", "ölçek", "en", "boy", "yük.", "")]
    for kind in ACTOR_KINDS:
        sz = sizes[kind]
        ok = "sığar" if sz["width"] <= ROAD_WIDTH else "taşar"
        rows.append((LABELS[kind], f"{NEW_SCALE[kind]:.2f}", f"{sz['width']:.2f}", f"{sz['length']:.2f}",
                     f"{sz['height']:.2f}", ok))
    for i, row in enumerate(rows):
        ry = y + 92 + i * 28
        for j, (cell, cw) in enumerate(zip(row, (190, 80, 80, 80, 80, 90))):
            colour = INK if i == 0 else (GOLD if cell == "taşar" else INK if j == 0 else MUTED)
            d.text((tx + sum((190, 80, 80, 80, 80, 90)[:j]), ry), cell, font=_font(15, i == 0 or j == 0), fill=colour)
    ny = y + 92 + len(rows) * 28 + 14
    lines = ["Normal birliklerin hepsi yola sığar (en geniş: Kuşatma 1.44, Öncü 1.18).",
             "Boss en ölçüsü balta + sancak dahil; omuz genişliği ~2.16 (eski boss ~2.37).",
             "Boss yolu görsel olarak taşıyor ama eskisinden dar; istenirse ölçek 1.20 yerine 1.05.",
             "Dostlar yol kenarında durur; Kahraman pelerini 1.36 en (kılıç dahil)."]
    for i, line in enumerate(lines):
        d.text((tx, ny + i * 22), line, font=_font(14), fill=MUTED)
    sheet = sheet.crop((0, 0, width, y + 32 + 720 + 24))
    sheet.save(OUT / "actors-a2-sheet.png")
    print(OUT / "actors-a2-sheet.png")


if __name__ == "__main__":
    if bpy is not None:
        render_main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    else:
        {"a1": compose_a1, "a2": compose_a2}[sys.argv[2] if len(sys.argv) > 2 else "a1"]()
