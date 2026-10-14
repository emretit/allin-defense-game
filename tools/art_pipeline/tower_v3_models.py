"""Tower v3 final models (direction 1: soft stylised / toy quality).

Every tower is one mesh on the shared tower_palette material plus the gameplay sockets.
level 1 = base, level 2 = clear growth (extra tier + trims), branch = level-3 specialisation built on level 2.
"""

import math

from mathutils import Vector

from tower_v3_kit import arch_points, gem, lathe, outline, pennant_points, rbox, ring, shield_points, socket, torus, tube


def plinth():
    lathe("Plinth", [(0, 0), (0.66, 0), (0.66, 0.12), (0.58, 0.18), (0.58, 0.26), (0, 0.26)],
          "stone_warm", round_r=0.05)


def common_sockets(top):
    socket("Socket_Root", (0, 0, 0), 0.16)
    socket("Socket_Ground", (0, 0.04, 0), 0.13)
    socket("Socket_Target", (0, top * 0.55, 0), 0.13)
    socket("Socket_VFX", (0, top * 0.30, 0), 0.13)
    socket("Socket_Banner", (0.42, top * 0.80, 0), 0.13)
    socket("Socket_Upgrade", (0, top + 0.15, 0), 0.13)


def flag(at, swatch, length=0.44, height=0.26, pole=0.7):
    x, y, z = at
    lathe("FlagPole", [(0, y), (0.028, y), (0.028, y + pole), (0, y + pole)], "wood_dark", at=(x, 0, z),
          segments=8, round_r=0)
    lathe("FlagTip", [(0, y + pole - 0.02), (0.055, y + pole + 0.02), (0, y + pole + 0.12)], "gold",
          at=(x, 0, z), segments=10, round_r=0.01)
    outline("Flag", pennant_points(length, height), 0.03, swatch, at=(x + 0.02, y + pole - height - 0.06, z),
            bevel=0.01)


def ribs(name, profile, count, swatch, radius=0.022, lift=0.025, phase=0.0):
    """Gold roof ribs following a lathe profile [(r, h), ...] from the apex down to the eave."""
    for i in range(count):
        a = phase + i * 2 * math.pi / count
        tube(f"{name}{i}", [(math.cos(a) * (r + lift), h, math.sin(a) * (r + lift)) for r, h in profile], radius,
             swatch)


def merlons(name, y, r, count, size, swatch, phase=0.0):
    for i in range(count):
        a = phase + i * 2 * math.pi / count
        rbox(f"{name}{i}", (math.cos(a) * r, y + size[1] * 0.5, math.sin(a) * r), size, swatch, bevel=0.03,
             segments=1, rot=(0, math.degrees(a) + 90, 0))


# --------------------------------------------------------------------------------------------- archer


def archer(level, branch=None):
    plinth()
    grow = 0.0 if level == 1 else 0.36
    top_shaft = 1.40 + grow
    lathe("Shaft", [(0, 0.24), (0.44, 0.24), (0.36, top_shaft), (0.41, top_shaft + 0.06), (0, top_shaft + 0.06)],
          "stone_cream", round_r=0.05)
    ring("Band", 0.62, 0.42, 0.09, "wood_dark")
    ring("Band2", 1.08, 0.385, 0.07, "wood_dark")
    rbox("Slit", (0, 0.88, -0.385), (0.1, 0.30, 0.08), "dark", bevel=0.045, segments=3, rot=(-4, 0, 0))
    if level >= 2:
        ring("Collar", top_shaft - 0.20, 0.37, 0.07, "gold")
        for k, y in enumerate((0.40, 0.84, 1.30)):
            ring(f"Course{k}", y, 0.435 - (y - 0.24) * 0.072, 0.05, "stone_warm")
        rbox("Slit2", (0, 1.34, -0.36), (0.09, 0.24, 0.08), "glow_gold", bevel=0.04, segments=3, rot=(-4, 0, 0))
    deck = top_shaft + 0.02
    deck_r = 0.60 if level == 1 else 0.66
    lathe("Deck", [(0, deck), (deck_r - 0.04, deck), (deck_r, deck + 0.07), (deck_r - 0.04, deck + 0.13),
                   (0, deck + 0.13)], "wood", round_r=0.03)
    par_r = deck_r - 0.04
    lathe("Parapet", [(par_r - 0.09, deck + 0.11), (par_r, deck + 0.11), (par_r, deck + 0.32),
                      (par_r - 0.09, deck + 0.32), (par_r - 0.09, deck + 0.11)], "wood", round_r=0.04, caps=False,
          grad=(0.25, 1.0))
    if level >= 2:
        torus("ParapetTrim", (0, deck + 0.32, 0), par_r - 0.045, 0.035, "gold")
    post_top = deck + 0.54
    for i in range(4):
        a = math.radians(45 + i * 90)
        lathe(f"Post{i}", [(0, deck + 0.30), (0.04, deck + 0.30), (0.04, post_top), (0, post_top)],
              "wood_dark", at=(math.cos(a) * (par_r - 0.12), 0, math.sin(a) * (par_r - 0.12)), segments=8,
              round_r=0.0)
    lathe("Lantern", [(0, deck + 0.34), (0.10, deck + 0.38), (0.10, deck + 0.50), (0, deck + 0.54)], "glow_gold",
          segments=12, round_r=0.04)
    roof_base = post_top - 0.06
    if branch == "Sharpshooter":
        # Long lookout: slim steep spire, crow's nest and a glowing lens at the very top.
        lathe("Canopy", [(0, roof_base + 0.30), (deck_r - 0.02, roof_base + 0.06), (deck_r + 0.02, roof_base + 0.01),
                         (deck_r - 0.06, roof_base - 0.02), (0, roof_base - 0.02)], "green", round_r=0.04, steps=3)
        torus("RoofTrim", (0, roof_base + 0.03, 0), deck_r - 0.02, 0.035, "gold")
        lathe("Roof", [(0, roof_base + 1.75), (0.05, roof_base + 1.55), (0.16, roof_base + 0.90),
                       (0.26, roof_base + 0.30), (0.30, roof_base + 0.20), (0, roof_base + 0.20)], "green",
              round_r=0.04, steps=3)
        ring("SpireBand", roof_base + 0.62, 0.21, 0.06, "gold")
        nest = roof_base + 1.22
        lathe("Nest", [(0, nest - 0.1), (0.08, nest - 0.06), (0.30, nest), (0.33, nest + 0.07), (0.30, nest + 0.16),
                       (0, nest + 0.16)], "wood", round_r=0.02, segments=16)
        torus("NestTrim", (0, nest + 0.16, 0), 0.30, 0.03, "gold", major_segments=24)
        gem("Lens", (0, nest + 0.18, 0), 0.15, 0.40, "glow_gold", sides=8, waist=0.5, top=0.6)
        lathe("Spike", [(0, nest + 0.56), (0.04, nest + 0.56), (0, nest + 1.0)], "gold", segments=8, round_r=0)
        outline("LongPennant", pennant_points(0.70, 0.20, 0.3), 0.025, "green",
                at=(0.03, nest + 0.62, 0), bevel=0.008)
        top = nest + 1.0
    else:
        apex = roof_base + (1.02 if level == 1 else 1.22)
        roof_r = deck_r + 0.04
        lathe("Roof", [(0, apex), (0.10, apex - 0.17), (0.36 * roof_r / 0.64, apex - 0.62),
                       (roof_r - 0.06, roof_base + 0.10), (roof_r, roof_base + 0.03), (roof_r - 0.08, roof_base),
                       (0, roof_base)], "green", round_r=0.06, steps=4)
        torus("RoofTrim", (0, roof_base + 0.05, 0), roof_r - 0.04, 0.04, "gold")
        lathe("Finial", [(0, apex - 0.07), (0.06, apex - 0.01), (0.085, apex + 0.07), (0.05, apex + 0.15),
                         (0, apex + 0.21)], "gold", round_r=0.02, segments=16)
        top = apex + 0.2
        if level >= 2:
            ribs("RoofRib", [(0.08, apex - 0.12), (0.36 * roof_r / 0.64, apex - 0.62), (roof_r - 0.06, roof_base + 0.10)],
                 8, "gold_dark", phase=math.pi / 8)
            flag((0, apex + 0.12, 0), "green", pole=0.55)
            top = apex + 0.7
    bow_y = deck + 0.25
    bow_z = -(par_r + 0.03)
    tube("Bow", [(-0.30, bow_y + 0.23, bow_z + 0.03), (-0.18, bow_y + 0.05, bow_z), (0, bow_y, bow_z - 0.01),
                 (0.18, bow_y + 0.05, bow_z), (0.30, bow_y + 0.23, bow_z + 0.03)], 0.032, "gold")
    tube("Arrow", [(0, bow_y - 0.07, bow_z - 0.02), (0, bow_y + 0.29, bow_z)], 0.018, "cloth")
    lathe("ArrowTip", [(0, 0), (0.045, 0.0), (0, 0.11)], "metal", at=(0, bow_y + 0.28, bow_z), segments=6,
          round_r=0)
    if branch == "RapidFire":
        # Twin ballistae on the front flanks, splayed toward the road: big gold bows read at phone scale.
        for side in (-1, 1):
            yaw = side * 28
            ax, az = side * 0.50, -0.50
            y = deck + 0.30
            rbox(f"BallistaMount{side}", (ax, y - 0.10, az + 0.06), (0.16, 0.16, 0.16), "wood_dark", bevel=0.05,
                 segments=2)
            rbox(f"BallistaStock{side}", (ax, y, az), (0.13, 0.10, 0.66), "wood", bevel=0.04, segments=2,
                 rot=(0, yaw, 0))
            dx, dz = math.sin(math.radians(yaw)), -math.cos(math.radians(yaw))
            px, pz = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
            fx, fz = ax + dx * 0.24, az + dz * 0.24
            pts = []
            for t in (-1, -0.55, 0, 0.55, 1):
                back = 0.16 * t * t
                pts.append((fx + px * 0.40 * t - dx * back, y + 0.04, fz + pz * 0.40 * t - dz * back))
            tube(f"BallistaBow{side}", pts, 0.042, "gold")
            tube(f"BallistaBolt{side}", [(ax - dx * 0.2, y + 0.09, az - dz * 0.2), (fx + dx * 0.16, y + 0.09,
                                                                                    fz + dz * 0.16)], 0.026, "metal")
            tip = lathe(f"BallistaTip{side}", [(0, 0), (0.06, 0.0), (0, 0.17)], "metal", segments=6, round_r=0)
            tip.rotation_euler = (math.radians(90), 0, math.radians(yaw))
            tip.location = (fx + dx * 0.16, fz + dz * 0.16, y + 0.09)
    socket("Socket_Projectile", (0, deck + 0.3, -(par_r + 0.1)), 0.14)
    socket("Socket_Muzzle", (0, deck + 0.3, -(par_r + 0.1)), 0.14)
    common_sockets(top)


# --------------------------------------------------------------------------------------------- mage


def mage(level, branch=None):
    plinth()
    grow = 0.0 if level == 1 else 0.34
    body_top = 1.60 + grow
    lathe("Body", [(0, 0.24), (0.44, 0.24), (0.38, 0.70), (0.28, 1.40 + grow), (0.34, 1.50 + grow),
                   (0.40, body_top), (0, body_top)], "stone_cool", round_r=0.07, steps=4)
    ring("Sash", 0.44, 0.405, 0.12, "purple")
    ring("Sash2", 1.18, 0.31 - grow * 0.05, 0.06, "gold")
    rbox("Window", (0, 0.98, -0.33), (0.13, 0.26, 0.08), "glow_purple", bevel=0.06, segments=3, rot=(-8, 0, 0))
    if level >= 2:
        ring("Sash3", 1.42, 0.285, 0.10, "purple")
        ring("Course", 0.80, 0.36, 0.05, "stone_warm")
        rbox("Window2", (0, 1.68, -0.27), (0.11, 0.20, 0.08), "glow_purple", bevel=0.05, segments=3, rot=(-8, 0, 0))
    if level >= 2:
        bal = body_top - 0.42
        lathe("Balcony", [(0, bal), (0.30, bal), (0.46, bal + 0.06), (0.48, bal + 0.12), (0, bal + 0.12)],
              "stone_cream", round_r=0.03)
        torus("BalconyTrim", (0, bal + 0.12, 0), 0.46, 0.025, "gold")
        for i in range(8):
            a = math.radians(i * 45 + 22.5)
            lathe(f"BalconyPost{i}", [(0, bal + 0.12), (0.025, bal + 0.12), (0.025, bal + 0.26), (0, bal + 0.26)],
                  "gold", at=(math.cos(a) * 0.44, 0, math.sin(a) * 0.44), segments=6, round_r=0)
        torus("BalconyRail", (0, bal + 0.26, 0), 0.44, 0.02, "gold")
    cap_base = body_top - 0.03 + (0.04 if level >= 2 else 0.0)
    cap_r = 0.50 if level == 1 else 0.56
    cap_h = 0.73 if level == 1 else 0.86
    lathe("Cap", [(0, cap_base + cap_h), (0.10, cap_base + cap_h - 0.14), (cap_r * 0.64, cap_base + 0.23),
                  (cap_r, cap_base + 0.05), (cap_r - 0.04, cap_base), (0, cap_base)], "purple", round_r=0.05, steps=4)
    torus("CapTrim", (0, cap_base + 0.06, 0), cap_r - 0.03, 0.035, "gold")
    if level >= 2:
        ribs("CapRib", [(0.07, cap_base + cap_h - 0.12), (cap_r * 0.64, cap_base + 0.23), (cap_r - 0.04, cap_base + 0.07)],
             6, "gold", radius=0.02, lift=0.02, phase=math.pi / 6)
    if level >= 2:
        for i in range(6):
            a = math.radians(i * 60 + 30)
            lathe(f"CapStud{i}", [(0, 0), (0.05, 0.0), (0.05, 0.03), (0, 0.06)], "gold",
                  at=(math.cos(a) * (cap_r - 0.04), cap_base + 0.08, math.sin(a) * (cap_r - 0.04)), segments=10,
                  round_r=0.015)
    head = cap_base + cap_h - 0.12
    if branch == "Blast":
        lathe("Brazier", [(0, head), (0.10, head), (0.30, head + 0.18), (0.34, head + 0.26), (0.28, head + 0.24),
                          (0.10, head + 0.12), (0, head + 0.12)], "dark", round_r=0.03, segments=20)
        torus("BrazierRim", (0, head + 0.25, 0), 0.31, 0.03, "gold")
        lathe("Fire", [(0, head + 0.14), (0.25, head + 0.24), (0.27, head + 0.34), (0.15, head + 0.58),
                       (0.04, head + 0.86), (0, head + 0.92)], "glow_fire", round_r=0.05, segments=8, sharp=35,
              phase=0.0)
        for i in range(4):
            a = math.radians(45 + i * 90)
            gem(f"Flame{i}", (math.cos(a) * 0.17, head + 0.24, math.sin(a) * 0.17), 0.09, 0.46, "glow_fire",
                sides=5, rot=(-24 * math.sin(a), 0, 24 * math.cos(a)))
        for i in range(3):
            a = math.radians(30 + i * 120)
            gem(f"Ember{i}", (math.cos(a) * 0.30, head + 0.86 + i * 0.07, math.sin(a) * 0.30), 0.05, 0.14,
                "glow_fire", sides=5)
        crystal_y = head + 0.4
        top = head + 0.9
    elif branch == "Frost":
        gem("Crystal", (0, head + 0.04, 0), 0.22, 0.90, "glow_ice", sides=6)
        for i, (a, tilt) in enumerate(((200, 30), (340, 28), (90, 26), (270, 24))):
            r = math.radians(a)
            gem(f"Shard{i}", (math.cos(r) * 0.18, head + 0.02, math.sin(r) * 0.18), 0.11, 0.50, "glow_ice",
                sides=5, rot=(-tilt * math.sin(r), 0, tilt * math.cos(r)))
        torus("FrostRing", (0, head + 0.06, 0), 0.30, 0.04, "glow_ice", major_segments=24)
        torus("Orbit", (0, head + 0.52, 0), 0.40, 0.022, "metal", rot=(16, 0, 12))
        crystal_y = head + 0.4
        top = head + 0.85
    else:
        scale = 1.0 if level == 1 else 1.28
        for i in range(3):
            a = math.radians(90 + i * 120)
            ca, sa = math.cos(a), math.sin(a)
            tube(f"Prong{i}", [(ca * 0.08, head, sa * 0.08), (ca * 0.30 * scale, head + 0.18, sa * 0.30 * scale),
                               (ca * 0.26 * scale, head + 0.44 * scale, sa * 0.26 * scale),
                               (ca * 0.12 * scale, head + 0.58 * scale, sa * 0.12 * scale)], 0.03, "gold",
                 taper_end=0.5)
        gem("Crystal", (0, head + 0.16, 0), 0.18 * scale, 0.56 * scale, "glow_cyan", sides=6)
        torus("Orbit", (0, head + 0.44 * scale, 0), 0.36 * scale, 0.022, "gold", rot=(18, 0, 10))
        if level >= 2:
            torus("Orbit2", (0, head + 0.40 * scale, 0), 0.46, 0.018, "glow_cyan", rot=(-14, 0, -16))
        crystal_y = head + 0.44 * scale
        top = head + 0.72 * scale
    socket("Socket_Projectile", (0, crystal_y, -0.08), 0.13)
    socket("Socket_Crystal", (0, crystal_y, 0), 0.13)
    socket("Socket_Cast", (0, crystal_y + 0.12, 0), 0.13)
    common_sockets(top)


# --------------------------------------------------------------------------------------------- barracks


def barracks(level, branch=None):
    plinth()
    lathe("Drum", [(0, 0.24), (0.60, 0.24), (0.55, 1.16), (0.61, 1.22), (0.61, 1.30), (0, 1.30)],
          "stone_cream", round_r=0.05)
    ring("Band", 0.92, 0.575, 0.08, "steel_dark")
    outline("GateFrame", arch_points(0.44, 0.62), 0.06, "gold", at=(0, 0.25, -0.575), bevel=0.02)
    outline("Gate", arch_points(0.34, 0.54), 0.06, "dark", at=(0, 0.25, -0.60), bevel=0.02)
    if level == 1:
        roof_base, apex, roof_r = 1.27, 2.36, 0.74
    else:
        # Level 2 adds a whole second tier: skirt roof, upper keep drum and a taller cone.
        merlons("Merlon", 1.28, 0.555, 10, (0.17, 0.17, 0.12), "stone_cream", phase=math.pi / 10)
        ring("WalkTrim", 1.24, 0.60, 0.05, "steel_dark")
        for k, y in enumerate((0.50, 0.70)):
            ring(f"Course{k}", y, 0.585 - (y - 0.24) * 0.05, 0.045, "stone_warm")
        lathe("Upper", [(0, 1.40), (0.42, 1.40), (0.40, 1.92), (0.45, 1.97), (0.45, 2.04), (0, 2.04)],
              "stone_cream", round_r=0.04)
        ring("UpperBand", 1.66, 0.41, 0.06, "gold")
        rbox("UpperWindow", (0, 1.72, -0.40), (0.12, 0.20, 0.08), "glow_gold", bevel=0.05, segments=3)
        roof_base, apex, roof_r = 2.01, 2.96, 0.58
    lathe("Roof", [(0, apex), (0.14, apex - 0.16), (roof_r * 0.65, roof_base + 0.39), (roof_r - 0.04, roof_base + 0.11),
                   (roof_r, roof_base + 0.04), (roof_r - 0.10, roof_base), (0, roof_base)], "steel", round_r=0.06,
          steps=4)
    torus("RoofTrim", (0, roof_base + 0.06, 0), roof_r - 0.03, 0.04, "gold")
    if level >= 2:
        ribs("RoofRib", [(0.10, apex - 0.14), (roof_r * 0.65, roof_base + 0.39), (roof_r - 0.05, roof_base + 0.11)], 8,
             "gold_dark", phase=math.pi / 8)
    shield_y = roof_base + 0.09
    shield_z = -(roof_r - 0.10)
    outline("ShieldRim", shield_points(0.44, 0.50), 0.06, "gold", at=(0, shield_y, shield_z), rot=(-30, 0, 0),
            bevel=0.025)
    outline("Shield", shield_points(0.34, 0.40), 0.06, "cloth", at=(0, shield_y + 0.045, shield_z - 0.045),
            rot=(-30, 0, 0), bevel=0.025)
    if branch == "AssaultGuard":
        # Crossed spears in front of the cone with a hanging war banner between them.
        for side in (-1, 1):
            base = (side * -0.46, roof_base + 0.05, -0.40)
            tipp = (side * 0.40, apex + 0.42, -0.30)
            tube(f"Spear{side}", [base, tipp], 0.032, "wood_dark")
            d = Vector((tipp[0] - base[0], tipp[2] - base[2], tipp[1] - base[1])).normalized()
            tip = lathe(f"SpearTip{side}", [(0, 0), (0.07, 0.0), (0.05, 0.12), (0, 0.28)], "metal", segments=6,
                        round_r=0)
            tip.rotation_euler = d.to_track_quat("Z", "Y").to_euler()
            tip.location = (tipp[0], tipp[2], tipp[1])
        outline("WarBannerTrim", [(0, 0), (0.54, 0), (0.54, -0.09), (0, -0.09)], 0.04, "gold",
                at=(-0.27, apex - 0.02, -0.47), bevel=0.014, rot=(-14, 0, 0))
        outline("WarBanner", [(0, 0), (0.48, 0), (0.48, -0.56), (0.24, -0.42), (0, -0.56)], 0.03, "steel",
                at=(-0.24, apex - 0.06, -0.46), bevel=0.012, rot=(-14, 0, 0))
        outline("WarEmblem", shield_points(0.20, 0.24), 0.03, "gold", at=(0, apex - 0.42, -0.52),
                bevel=0.01, rot=(-14, 0, 0))
        top = apex + 0.65
    else:
        flag((0.0, apex - 0.10, 0.0), "steel", pole=0.70 if level == 1 else 0.62)
        top = apex + 0.6
    if level >= 2 and branch != "AssaultGuard":
        flag((0.52, 1.30, 0.30), "steel", length=0.32, height=0.2, pole=0.55)
    if branch == "ShieldGuard":
        for i, a in enumerate((-78, -44, 44, 78)):
            r = math.radians(a - 90)
            x, z = math.cos(r) * 0.70, math.sin(r) * 0.70
            outline(f"WallShieldRim{i}", shield_points(0.46, 0.66), 0.06, "gold", at=(x, 0.20, z),
                    rot=(-6, -a * 0.45, 0), bevel=0.022)
            outline(f"WallShield{i}", shield_points(0.36, 0.55), 0.06, "steel", at=(x + math.cos(r) * 0.03, 0.26,
                                                                                     z + math.sin(r) * 0.03),
                    rot=(-6, -a * 0.45, 0), bevel=0.022)
            boss = lathe(f"WallBoss{i}", [(0, 0), (0.06, 0), (0.05, 0.03), (0, 0.045)], "gold", segments=10,
                         round_r=0.01)
            boss.rotation_euler = (math.radians(90 - 6), 0, math.radians(-a * 0.45))
            boss.location = (x + math.cos(r) * 0.07, z + math.sin(r) * 0.07, 0.64)
    socket("Socket_Projectile", (0, 1.05, -0.62), 0.13)
    socket("Socket_Spawn", (0, 0.18, -0.74), 0.13)
    common_sockets(top)


BUILDERS = {"archer": archer, "mage": mage, "barracks": barracks}
BRANCHES = {
    "RapidFire": "archer", "Sharpshooter": "archer",
    "ShieldGuard": "barracks", "AssaultGuard": "barracks",
    "Blast": "mage", "Frost": "mage",
}
