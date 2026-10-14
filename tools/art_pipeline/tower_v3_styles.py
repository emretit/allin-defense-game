"""A1 style exploration for tower v3: three modern directions, level-1 Archer / Mage / Barracks."""

import math

from tower_v3_kit import arch_points, gem, lathe, outline, pennant_points, rbox, ring, shield_points, torus, tube


# --------------------------------------------------------------------------------------------- style 1
# Soft stylised / toy quality: smooth lathed silhouettes, generous rounding, chunky trims.


def s1_plinth():
    lathe("Plinth", [(0, 0), (0.66, 0), (0.66, 0.12), (0.58, 0.18), (0.58, 0.26), (0, 0.26)],
          "stone_warm", round_r=0.05)


def s1_archer():
    s1_plinth()
    lathe("Shaft", [(0, 0.24), (0.44, 0.24), (0.36, 1.40), (0.41, 1.46), (0, 1.46)], "stone_cream",
          round_r=0.05)
    ring("Band", 0.62, 0.42, 0.09, "wood_dark")
    ring("Band2", 1.08, 0.385, 0.07, "wood_dark")
    rbox("Slit", (0, 0.88, -0.385), (0.1, 0.30, 0.08), "dark", bevel=0.045, segments=3, rot=(-4, 0, 0))
    lathe("Deck", [(0, 1.42), (0.56, 1.42), (0.60, 1.49), (0.56, 1.55), (0, 1.55)], "wood", round_r=0.03)
    lathe("Parapet", [(0.47, 1.53), (0.56, 1.53), (0.56, 1.74), (0.47, 1.74), (0.47, 1.53)], "wood",
          round_r=0.04, caps=False, grad=(0.25, 1.0))
    for i in range(4):
        a = math.radians(45 + i * 90)
        lathe(f"Post{i}", [(0, 1.72), (0.04, 1.72), (0.04, 1.96), (0, 1.96)],
              "wood_dark", at=(math.cos(a) * 0.44, 0, math.sin(a) * 0.44), segments=8, round_r=0.0)
    lathe("Roof", [(0, 2.95), (0.10, 2.78), (0.36, 2.30), (0.58, 2.0), (0.64, 1.93), (0.56, 1.90), (0, 1.90)],
          "green", round_r=0.06, steps=4)
    torus("RoofTrim", (0, 1.95, 0), 0.60, 0.04, "gold")
    lathe("Finial", [(0, 2.88), (0.06, 2.94), (0.085, 3.02), (0.05, 3.10), (0, 3.16)], "gold", round_r=0.02,
          segments=16)
    lathe("Lantern", [(0, 1.76), (0.10, 1.80), (0.10, 1.92), (0, 1.96)], "glow_gold", segments=12,
          round_r=0.04)
    tube("Bow", [(-0.30, 1.80, -0.58), (-0.18, 1.62, -0.61), (0, 1.57, -0.62), (0.18, 1.62, -0.61),
                 (0.30, 1.80, -0.58)], 0.032, "gold")
    tube("Arrow", [(0, 1.50, -0.63), (0, 1.86, -0.61)], 0.018, "cloth")
    lathe("ArrowTip", [(0, 0), (0.045, 0.0), (0, 0.11)], "metal", at=(0, 1.85, -0.61), segments=6, round_r=0)


def s1_mage():
    s1_plinth()
    lathe("Body", [(0, 0.24), (0.44, 0.24), (0.38, 0.70), (0.28, 1.40), (0.34, 1.50), (0.40, 1.60), (0, 1.60)],
          "stone_cool", round_r=0.07, steps=4)
    ring("Sash", 0.44, 0.405, 0.12, "purple")
    ring("Sash2", 1.18, 0.31, 0.06, "gold")
    rbox("Window", (0, 0.98, -0.33), (0.13, 0.26, 0.08), "glow_purple", bevel=0.06, segments=3, rot=(-8, 0, 0))
    lathe("Cap", [(0, 2.30), (0.10, 2.16), (0.32, 1.80), (0.50, 1.62), (0.46, 1.57), (0, 1.57)],
          "purple", round_r=0.05, steps=4)
    torus("CapTrim", (0, 1.63, 0), 0.47, 0.035, "gold")
    for i in range(3):
        a = math.radians(90 + i * 120)
        ca, sa = math.cos(a), math.sin(a)
        tube(f"Prong{i}", [(ca * 0.08, 2.18, sa * 0.08), (ca * 0.30, 2.36, sa * 0.30),
                           (ca * 0.26, 2.62, sa * 0.26), (ca * 0.12, 2.76, sa * 0.12)], 0.03, "gold",
             taper_end=0.5)
    gem("Crystal", (0, 2.34, 0), 0.18, 0.56, "glow_cyan", sides=6)
    torus("Orbit", (0, 2.62, 0), 0.36, 0.022, "gold", rot=(18, 0, 10))


def s1_barracks():
    s1_plinth()
    lathe("Drum", [(0, 0.24), (0.60, 0.24), (0.55, 1.16), (0.61, 1.22), (0.61, 1.30), (0, 1.30)],
          "stone_cream", round_r=0.05)
    ring("Band", 0.92, 0.575, 0.08, "steel_dark")
    lathe("Roof", [(0, 2.36), (0.14, 2.20), (0.48, 1.66), (0.70, 1.38), (0.74, 1.31), (0.64, 1.27), (0, 1.27)],
          "steel", round_r=0.06, steps=4)
    torus("RoofTrim", (0, 1.33, 0), 0.71, 0.04, "gold")
    outline("GateFrame", arch_points(0.44, 0.62), 0.06, "gold", at=(0, 0.25, -0.575), bevel=0.02)
    outline("Gate", arch_points(0.34, 0.54), 0.06, "dark", at=(0, 0.25, -0.60), bevel=0.02)
    outline("ShieldRim", shield_points(0.44, 0.50), 0.06, "gold", at=(0, 1.34, -0.64), rot=(-30, 0, 0),
            bevel=0.025)
    outline("Shield", shield_points(0.34, 0.40), 0.06, "cloth", at=(0, 1.385, -0.685), rot=(-30, 0, 0),
            bevel=0.025)
    lathe("Pole", [(0, 2.2), (0.03, 2.2), (0.03, 2.9), (0, 2.9)], "wood_dark", segments=8, round_r=0)
    lathe("PoleTip", [(0, 2.88), (0.06, 2.92), (0, 3.02)], "gold", segments=10, round_r=0.01)
    outline("Pennant", pennant_points(0.44, 0.26), 0.03, "steel", at=(0.02, 2.58, 0), bevel=0.01)


# --------------------------------------------------------------------------------------------- style 2
# Elegant minimal: crisp faceted (octagonal) forms, slim proportions, few parts, thin gold lines.


def s2_plinth():
    lathe("Plinth", [(0, 0), (0.64, 0), (0.64, 0.1), (0.56, 0.13), (0.56, 0.22), (0, 0.22)],
          "stone_dark", segments=8, round_r=0.012, steps=1, sharp=30)


def s2_archer():
    s2_plinth()
    lathe("Shaft", [(0, 0.2), (0.40, 0.2), (0.28, 1.62), (0, 1.62)], "stone_cream", segments=8,
          round_r=0.012, steps=1, sharp=30)
    ring("Band", 0.98, 0.335, 0.06, "gold", segments=8, out=0.012, round_r=0)
    lathe("Deck", [(0, 1.58), (0.50, 1.58), (0.50, 1.68), (0, 1.68)], "wood", segments=8, round_r=0.012,
          steps=1, sharp=30)
    for i in range(4):
        a = math.radians(45 + i * 90)
        rbox(f"Post{i}", (math.cos(a) * 0.40, 1.85, math.sin(a) * 0.40), (0.06, 0.36, 0.06), "wood_dark",
             bevel=0.01, segments=1)
    lathe("Roof", [(0, 3.0), (0.60, 2.06), (0.60, 2.01), (0, 2.01)], "roof_gold", segments=8, round_r=0.012,
          steps=1, sharp=30)
    lathe("Spire", [(0, 2.9), (0.035, 2.9), (0.012, 3.3), (0, 3.3)], "gold", segments=8, round_r=0)
    lathe("Lantern", [(0, 1.70), (0.10, 1.74), (0.10, 1.88), (0, 1.92)], "glow_gold", segments=8, round_r=0.01)
    rbox("Slit", (0, 1.05, -0.31), (0.07, 0.34, 0.06), "dark", bevel=0.02, segments=1, rot=(-5, 0, 0))


def s2_mage():
    s2_plinth()
    lathe("Obelisk", [(0, 0.2), (0.40, 0.2), (0.22, 1.78), (0, 2.06)], "stone_cool", segments=4,
          round_r=0.012, steps=1, sharp=30, phase=math.pi / 4)
    rbox("InlayFront", (0, 1.0, -0.215), (0.07, 1.1, 0.05), "glow_purple", bevel=0.01, segments=1,
         rot=(-6.5, 0, 0), taper=0.6)
    torus("Halo", (0, 2.40, 0), 0.30, 0.02, "gold", major_segments=8, minor_segments=4)
    gem("Crystal", (0, 2.20, 0), 0.16, 0.50, "glow_cyan", sides=4)


def s2_barracks():
    s2_plinth()
    lathe("Drum", [(0, 0.2), (0.58, 0.2), (0.55, 1.22), (0, 1.22)], "stone_cream", segments=8, round_r=0.012,
          steps=1, sharp=30)
    lathe("Roof", [(0, 2.16), (0.66, 1.28), (0.66, 1.20), (0, 1.20)], "steel", segments=8, round_r=0.012,
          steps=1, sharp=30)
    rbox("Gate", (0, 0.50, -0.52), (0.30, 0.56, 0.06), "dark", bevel=0.02, segments=1)
    rbox("Lintel", (0, 0.81, -0.525), (0.40, 0.06, 0.07), "gold", bevel=0.01, segments=1)
    lathe("Pole", [(0, 2.05), (0.025, 2.05), (0.025, 2.8), (0, 2.8)], "gold", segments=8, round_r=0)
    outline("Pennant", [(0, 0), (0.44, 0.13), (0, 0.26)], 0.025, "steel", at=(0.02, 2.5, 0), bevel=0.006,
            segments=1)


# --------------------------------------------------------------------------------------------- style 3
# Rich fantasy: masonry courses, timber hoarding with struts, roof ribs, banners, glowing windows.


def s3_plinth():
    lathe("Plinth", [(0, 0), (0.68, 0), (0.68, 0.10), (0.62, 0.14), (0.62, 0.26), (0, 0.26)],
          "stone_dark", segments=16, round_r=0.03)
    for i in range(8):
        a = math.radians(22.5 + i * 45)
        rbox(f"Step{i}", (math.cos(a) * 0.64, 0.13, math.sin(a) * 0.64), (0.14, 0.1, 0.1), "stone_warm",
             bevel=0.025, rot=(0, math.degrees(a) + 90, 0))


def s3_archer():
    s3_plinth()
    lathe("Shaft", [(0, 0.24), (0.45, 0.24), (0.38, 1.40), (0, 1.40)], "stone_warm", segments=16,
          round_r=0.03)
    for y in (0.56, 0.98):
        r = 0.45 - (y - 0.24) * 0.06
        ring(f"Course{y}", y, r, 0.06, "stone_cream", segments=16)
    rbox("Slit", (0, 0.80, -0.40), (0.08, 0.26, 0.08), "glow_gold", bevel=0.03, rot=(-3, 0, 0))
    lathe("Hoarding", [(0, 1.36), (0.54, 1.36), (0.60, 1.46), (0.60, 1.76), (0, 1.76)], "wood", segments=16,
          round_r=0.025, grad=(0.15, 0.9))
    for i in range(8):
        a = math.radians(i * 45)
        tube(f"Strut{i}", [(math.cos(a) * 0.38, 1.12, math.sin(a) * 0.38),
                           (math.cos(a) * 0.58, 1.38, math.sin(a) * 0.58)], 0.03, "wood_dark")
        rbox(f"Plank{i}", (math.cos(a) * 0.605, 1.61, math.sin(a) * 0.605), (0.05, 0.3, 0.05), "wood_dark",
             bevel=0.015)
    lathe("Gap", [(0, 1.74), (0.48, 1.74), (0.48, 1.88), (0, 1.88)], "dark", segments=16, round_r=0)
    lathe("Roof", [(0, 2.86), (0.10, 2.72), (0.38, 2.26), (0.66, 1.94), (0.70, 1.86), (0, 1.86)], "green",
          segments=16, round_r=0.03)
    for i in range(8):
        a = math.radians(i * 45)
        tube(f"Rib{i}", [(math.cos(a) * 0.08, 2.76, math.sin(a) * 0.08),
                         (math.cos(a) * 0.40, 2.26, math.sin(a) * 0.40),
                         (math.cos(a) * 0.70, 1.92, math.sin(a) * 0.70)], 0.022, "gold_dark")
    lathe("Finial", [(0, 2.80), (0.05, 2.84), (0.07, 2.92), (0, 3.06)], "gold", segments=12, round_r=0.015)
    lathe("Pole", [(0, 3.0), (0.02, 3.0), (0.02, 3.35), (0, 3.35)], "wood_dark", segments=6, round_r=0)
    outline("Flag", pennant_points(0.32, 0.2), 0.02, "green", at=(0.02, 3.12, 0), bevel=0.006, segments=1)
    tube("Bow", [(-0.26, 1.70, -0.64), (0, 1.52, -0.67), (0.26, 1.70, -0.64)], 0.03, "gold")


def s3_mage():
    s3_plinth()
    lathe("Body", [(0, 0.24), (0.44, 0.24), (0.32, 1.56), (0, 1.56)], "stone_cool", segments=16,
          round_r=0.03)
    for y in (0.46, 0.98):
        r = 0.44 - (y - 0.24) * 0.091
        ring(f"Course{y}", y, r, 0.06, "purple_dark", segments=16)
    rbox("Window", (0, 1.22, -0.31), (0.12, 0.2, 0.08), "glow_purple", bevel=0.05, rot=(-5, 0, 0))
    rbox("Window2", (0, 0.72, -0.37), (0.1, 0.16, 0.08), "glow_purple", bevel=0.04, rot=(-5, 0, 0))
    lathe("Balcony", [(0, 1.52), (0.48, 1.52), (0.50, 1.60), (0.44, 1.64), (0, 1.64)], "stone_cream",
          segments=16, round_r=0.02)
    for i in range(8):
        a = math.radians(i * 45 + 22.5)
        lathe(f"Spike{i}", [(0, 0), (0.045, 0), (0, 0.24)], "gold",
              at=(math.cos(a) * 0.44, 1.62, math.sin(a) * 0.44), segments=6, round_r=0)
    lathe("Cap", [(0, 2.30), (0.08, 2.18), (0.28, 1.84), (0.36, 1.66), (0, 1.66)], "purple", segments=16,
          round_r=0.03)
    for i in range(4):
        a = math.radians(45 + i * 90)
        ca, sa = math.cos(a), math.sin(a)
        tube(f"Prong{i}", [(ca * 0.16, 2.0, sa * 0.16), (ca * 0.34, 2.30, sa * 0.34),
                           (ca * 0.20, 2.62, sa * 0.20)], 0.025, "gold", taper_end=0.4)
    gem("Crystal", (0, 2.30, 0), 0.17, 0.50, "glow_cyan", sides=6)
    torus("Orbit", (0, 2.56, 0), 0.30, 0.018, "glow_cyan", rot=(20, 0, -12))
    torus("Orbit2", (0, 2.52, 0), 0.38, 0.014, "gold", rot=(-14, 0, 18))


def s3_barracks():
    s3_plinth()
    lathe("Drum", [(0, 0.24), (0.62, 0.24), (0.58, 1.16), (0, 1.16)], "stone_warm", segments=16, round_r=0.03)
    ring("Course", 0.70, 0.60, 0.06, "stone_cream", segments=16)
    for i in range(10):
        a = math.radians(i * 36 + 18)
        rbox(f"Merlon{i}", (math.cos(a) * 0.56, 1.25, math.sin(a) * 0.56), (0.16, 0.18, 0.12), "stone_cream",
             bevel=0.03, rot=(0, math.degrees(a) + 90, 0))
    lathe("Roof", [(0, 2.16), (0.12, 2.04), (0.42, 1.54), (0.50, 1.34), (0, 1.34)], "steel", segments=16,
          round_r=0.03)
    for i in range(8):
        a = math.radians(i * 45)
        tube(f"Rib{i}", [(math.cos(a) * 0.06, 2.10, math.sin(a) * 0.06),
                         (math.cos(a) * 0.50, 1.36, math.sin(a) * 0.50)], 0.018, "gold_dark")
    outline("GateFrame", arch_points(0.46, 0.64), 0.06, "stone_cream", at=(0, 0.25, -0.59), bevel=0.02)
    outline("Gate", arch_points(0.34, 0.54), 0.06, "wood_dark", at=(0, 0.25, -0.615), bevel=0.02)
    for side in (-1, 1):
        outline(f"ShieldRim{side}", shield_points(0.24, 0.28), 0.05, "gold", at=(side * 0.40, 0.62, -0.52),
                rot=(0, side * 32, 0), bevel=0.02)
        outline(f"Shield{side}", shield_points(0.18, 0.22), 0.05, "steel", at=(side * 0.41, 0.65, -0.545),
                rot=(0, side * 32, 0), bevel=0.02)
    lathe("Pole", [(0, 2.06), (0.025, 2.06), (0.025, 2.80), (0, 2.80)], "wood_dark", segments=6, round_r=0)
    outline("Banner", [(0, 0), (0.34, 0), (0.34, -0.38), (0.17, -0.30), (0, -0.38)], 0.025, "steel",
            at=(0.02, 2.74, 0), bevel=0.008, segments=1)
    lathe("PoleTip", [(0, 2.78), (0.05, 2.82), (0, 2.92)], "gold", segments=8, round_r=0.01)


STYLES = {
    1: {"archer": s1_archer, "mage": s1_mage, "barracks": s1_barracks},
    2: {"archer": s2_archer, "mage": s2_mage, "barracks": s2_barracks},
    3: {"archer": s3_archer, "mage": s3_mage, "barracks": s3_barracks},
}
