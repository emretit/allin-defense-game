"""Tower v3 modelling kit: one shared palette atlas, lathe silhouettes, rounded boxes, single-mesh output.

Coordinates in the public helpers follow the Unity convention used by generate_lowpoly_assets.py:
(x, y-up, z) with the gameplay camera looking along +Z, so the tower front faces -Z.
"""

import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

PALETTE_NAME = "tower_palette"
SWATCH_COLS = 8
SWATCH_ROWS = 8
SWATCH_PX = 32


def _hex(value):
    value = value.lstrip("#")
    return tuple(int(value[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


# name: (top sRGB, bottom sRGB, glow). Gradients run light at the top of a part to darker at its foot,
# which gives every piece a baked ambient-occlusion read without vertex colours or extra materials.
SWATCHES = {
    "stone_cream": ("#F1E9DA", "#A69A88", False),
    "stone_warm": ("#D2C4AE", "#857764", False),
    "stone_cool": ("#C4CBD3", "#6C7684", False),
    "stone_dark": ("#7E838C", "#40444D", False),
    "wood": ("#C27E45", "#6E4022", False),
    "wood_dark": ("#7A4C2B", "#3A2314", False),
    "gold": ("#FFE38A", "#C9892A", False),
    "gold_dark": ("#DDA746", "#8A5A1E", False),
    "metal": ("#E6EDF3", "#8796A5", False),
    "dark": ("#2B3242", "#121721", False),
    "green": ("#8CD16F", "#2E7D46", False),
    "green_dark": ("#55985A", "#22502E", False),
    "purple": ("#B48CFF", "#5A33BE", False),
    "purple_dark": ("#7350D2", "#2D1A6E", False),
    "steel": ("#8DB6EC", "#355A98", False),
    "steel_dark": ("#4D74B0", "#1D335E", False),
    "cloth": ("#FFFFFF", "#C3CEDA", False),
    "cyan": ("#9CF7FF", "#2DB2D4", False),
    "navy": ("#3A4F78", "#18243A", False),
    "leather": ("#94603F", "#4E3020", False),
    "roof_gold": ("#FFD36B", "#D7862C", False),
    "teal": ("#5FC8C0", "#1F6F73", False),
    "glow_cyan": ("#5FE8FF", "#0A86D8", True),
    "glow_gold": ("#FFE9A0", "#FFA62E", True),
    "glow_fire": ("#FFD86A", "#FF4A10", True),
    "glow_ice": ("#8FDFFF", "#1F6DF0", True),
    "glow_purple": ("#E2C8FF", "#9A5CFF", True),
    "glow_white": ("#FFFFFF", "#E2ECFF", True),
}
SWATCH_INDEX = {name: index for index, name in enumerate(SWATCHES)}
BASE_EMISSION_LIFT = 0.10
# Mobile budget caps: smooth normals hide the facets at gameplay scale.
MAX_LATHE_SEGMENTS = 14
MAX_ROUND_STEPS = 1
MAX_TORUS_SEGMENTS = (16, 5)


def swatch_uv_rect(name):
    index = SWATCH_INDEX[name]
    col = index % SWATCH_COLS
    row = index // SWATCH_COLS
    u0 = col / SWATCH_COLS
    v1 = 1.0 - row / SWATCH_ROWS
    return u0, v1 - 1.0 / SWATCH_ROWS, u0 + 1.0 / SWATCH_COLS, v1


def write_palette_textures(directory):
    """Write the albedo atlas and the emission mask atlas (same layout) as PNG files."""
    directory = Path(directory)
    directory.mkdir(parents=True, exist_ok=True)
    size_x = SWATCH_COLS * SWATCH_PX
    size_y = SWATCH_ROWS * SWATCH_PX
    albedo = [0.0] * (size_x * size_y * 4)
    emission = [0.0] * (size_x * size_y * 4)
    for i in range(size_x * size_y):
        albedo[i * 4 + 3] = 1.0
        emission[i * 4 + 3] = 1.0
    for name, (top, bottom, glow) in SWATCHES.items():
        u0, v0, u1, v1 = swatch_uv_rect(name)
        x0, x1 = round(u0 * size_x), round(u1 * size_x)
        y0, y1 = round(v0 * size_y), round(v1 * size_y)
        top_c, bottom_c = _hex(top), _hex(bottom)
        for y in range(y0, y1):
            # Margin rows repeat the end colours so bilinear sampling never bleeds between swatches.
            t = min(1.0, max(0.0, (y - y0 - 2) / max(1, (y1 - y0 - 5))))
            colour = tuple(bottom_c[k] + (top_c[k] - bottom_c[k]) * t for k in range(3))
            glow_scale = 1.0 if glow else BASE_EMISSION_LIFT
            for x in range(x0, x1):
                p = (y * size_x + x) * 4
                albedo[p:p + 3] = colour
                emission[p:p + 3] = tuple(channel * glow_scale for channel in colour)
    paths = []
    for suffix, pixels in (("", albedo), ("_emission", emission)):
        image_name = f"TowerPalette{suffix}"
        image = bpy.data.images.get(image_name) or bpy.data.images.new(image_name, size_x, size_y, alpha=False)
        image.colorspace_settings.name = "sRGB"
        image.pixels = pixels
        path = directory / f"{image_name}.png"
        image.filepath_raw = str(path)
        image.file_format = "PNG"
        image.save()
        paths.append(path)
    return paths


def palette_material(texture_dir=None, emission_strength=1.0):
    mat = bpy.data.materials.get(PALETTE_NAME)
    if mat is not None:
        return mat
    mat = bpy.data.materials.new(PALETTE_NAME)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    bsdf.inputs["Roughness"].default_value = 0.62
    bsdf.inputs["Specular IOR Level"].default_value = 0.35
    if texture_dir is not None:
        albedo_path, emission_path = write_palette_textures(texture_dir)
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(str(albedo_path), check_existing=True)
        tex.interpolation = "Linear"
        links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        emit = nodes.new("ShaderNodeTexImage")
        emit.image = bpy.data.images.load(str(emission_path), check_existing=True)
        links.new(emit.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat


def U(value):
    """Unity (x, y-up, z) -> Blender (x, y, z-up)."""
    return Vector((value[0], value[2], value[1]))


# ---------------------------------------------------------------------------------------------
# Parts. Every part is a separate object until finish_tower() paints its UVs and joins it.


def _link(name, bm):
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def _tag(obj, swatch, sharp=40, grad=(0.0, 1.0), zrange=None, flat=False):
    obj["tv3_swatch"] = swatch
    obj["tv3_sharp"] = float(sharp)
    obj["tv3_grad"] = list(grad)
    obj["tv3_zrange"] = list(zrange) if zrange else []
    obj["tv3_flat"] = bool(flat)
    return obj


def round_profile(points, radius, steps=3):
    """Round interior corners of a (r, h[, radius]) profile with quadratic arcs."""
    if len(points) < 3:
        return [(p[0], p[1]) for p in points]
    out = [Vector((points[0][0], points[0][1]))]
    for i in range(1, len(points) - 1):
        a = Vector(points[i - 1][:2])
        p = Vector(points[i][:2])
        b = Vector(points[i + 1][:2])
        corner = points[i][2] if len(points[i]) > 2 else radius
        da, db = a - p, b - p
        d = min(corner, da.length * 0.48, db.length * 0.48)
        if d < 1e-4:
            out.append(p)
            continue
        p1 = p + da.normalized() * d
        p2 = p + db.normalized() * d
        for s in range(steps + 1):
            t = s / steps
            out.append(p1 * (1 - t) ** 2 + p * 2 * (1 - t) * t + p2 * t * t)
    out.append(Vector(points[-1][:2]))
    return [(max(0.0, q.x), q.y) for q in out]


def lathe(name, profile, swatch, at=(0, 0, 0), segments=24, round_r=0.03, steps=3, phase=None,
          scale_x=1.0, scale_z=1.0, sharp=40, grad=(0.0, 1.0), zrange=None, flat=False, caps=True):
    """Revolve a (radius, height) profile around the local up axis."""
    segments = min(segments, MAX_LATHE_SEGMENTS)
    steps = min(steps, MAX_ROUND_STEPS)
    pts = round_profile(profile, round_r, steps) if round_r > 0 else [(p[0], p[1]) for p in profile]
    if phase is None:
        phase = math.pi / segments if segments <= 8 else 0.0
    bm = bmesh.new()
    rings = []
    base = U(at)
    for r, h in pts:
        if r < 1e-5:
            rings.append([bm.verts.new((base.x, base.y, base.z + h))])
            continue
        ring = []
        for i in range(segments):
            angle = phase + i * 2 * math.pi / segments
            ring.append(bm.verts.new((base.x + math.cos(angle) * r * scale_x,
                                      base.y + math.sin(angle) * r * scale_z, base.z + h)))
        rings.append(ring)
    for ra, rb in zip(rings, rings[1:]):
        if len(ra) == 1 and len(rb) == 1:
            continue
        if len(ra) == 1:
            for i in range(segments):
                bm.faces.new((ra[0], rb[i], rb[(i + 1) % segments]))
        elif len(rb) == 1:
            for i in range(segments):
                bm.faces.new((ra[i], ra[(i + 1) % segments], rb[0]))
        else:
            for i in range(segments):
                j = (i + 1) % segments
                bm.faces.new((ra[i], ra[j], rb[j], rb[i]))
    if caps and len(rings[0]) > 1:
        bm.faces.new(list(reversed(rings[0])))
    if caps and len(rings[-1]) > 1:
        bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return _tag(_link(name, bm), swatch, sharp, grad, zrange, flat)


def rbox(name, at, size, swatch, bevel=0.04, segments=2, rot=(0, 0, 0), sharp=40, grad=(0.0, 1.0),
         zrange=None, flat=False, taper=1.0):
    """Rounded box. size/at in Unity axes; rot = (tilt X, yaw, roll) degrees."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    sx, sy, sz = size[0], size[2], size[1]
    for v in bm.verts:
        top = v.co.z > 0
        k = taper if top else 1.0
        v.co = Vector((v.co.x * sx * k, v.co.y * sy * k, v.co.z * sz))
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=min(bevel, min(sx, sy, sz) * 0.45), offset_type="OFFSET",
                        segments=min(segments, 2), profile=0.5, affect="EDGES", clamp_overlap=True)
    obj = _link(name, bm)
    _place(obj, at, rot)
    return _tag(obj, swatch, sharp, grad, zrange, flat)


def _place(obj, at, rot):
    # rot = (tilt about X, yaw about the up axis, roll about the depth axis) in degrees.
    rx, ry, rz = (math.radians(a) for a in rot)
    m = Matrix.Rotation(ry, 4, "Z") @ Matrix.Rotation(rx, 4, "X") @ Matrix.Rotation(rz, 4, "Y")
    obj.matrix_world = Matrix.Translation(U(at)) @ m


def outline(name, points, thickness, swatch, at=(0, 0, 0), rot=(0, 0, 0), bevel=0.015, segments=1,
            sharp=40, grad=(0.0, 1.0), zrange=None, flat=False):
    """Extrude a 2D outline drawn in the XY (Unity X right, Y up) plane toward -Z (the camera)."""
    bm = bmesh.new()
    front = [bm.verts.new((x, -thickness * 0.5, y)) for x, y in points]
    face = bm.faces.new(front)
    result = bmesh.ops.extrude_face_region(bm, geom=[face])
    moved = [e for e in result["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=(0, thickness, 0), verts=moved)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=min(bevel, thickness * 0.45), offset_type="OFFSET",
                        segments=segments, profile=0.5, affect="EDGES", clamp_overlap=True)
    obj = _link(name, bm)
    _place(obj, at, rot)
    return _tag(obj, swatch, sharp, grad, zrange, flat)


def tube(name, points, radius, swatch, resolution=2, sides=6, sharp=60, grad=(0.0, 1.0), zrange=None, flat=False,
         taper_end=1.0):
    curve = bpy.data.curves.new(f"{name}_Curve", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = max(0, sides // 4 - 1)
    curve.use_fill_caps = True
    curve.resolution_u = resolution
    spline = curve.splines.new("NURBS")
    spline.points.add(len(points) - 1)
    for p, value in zip(spline.points, points):
        v = U(value)
        p.co = (v.x, v.y, v.z, 1.0)
    for i, p in enumerate(spline.points):
        t = i / max(1, len(points) - 1)
        p.radius = 1.0 + (taper_end - 1.0) * t
    spline.use_endpoint_u = True
    spline.order_u = min(4, len(points))
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    for other in bpy.context.selected_objects:
        other.select_set(False)
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.view_layer.objects.active
    obj.name = name
    return _tag(obj, swatch, sharp, grad, zrange, flat)


def torus(name, at, major, minor, swatch, major_segments=32, minor_segments=8, rot=(0, 0, 0), sharp=60,
          grad=(0.0, 1.0), zrange=None, flat=False, scale_y=1.0):
    major_segments = min(major_segments, MAX_TORUS_SEGMENTS[0])
    minor_segments = min(minor_segments, MAX_TORUS_SEGMENTS[1])
    bm = bmesh.new()
    for i in range(major_segments):
        a = i * 2 * math.pi / major_segments
        for j in range(minor_segments):
            b = j * 2 * math.pi / minor_segments
            r = major + minor * math.cos(b)
            bm.verts.new((math.cos(a) * r, math.sin(a) * r, minor * math.sin(b) * scale_y))
    bm.verts.ensure_lookup_table()
    for i in range(major_segments):
        for j in range(minor_segments):
            a = i * minor_segments + j
            b = ((i + 1) % major_segments) * minor_segments + j
            c = ((i + 1) % major_segments) * minor_segments + (j + 1) % minor_segments
            d = i * minor_segments + (j + 1) % minor_segments
            bm.faces.new((bm.verts[a], bm.verts[b], bm.verts[c], bm.verts[d]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    obj = _link(name, bm)
    _place(obj, at, rot)
    return _tag(obj, swatch, sharp, grad, zrange, flat)


def gem(name, at, radius, height, swatch, sides=6, waist=0.55, top=0.35, rot=(0, 0, 0), grad=(0.15, 1.0)):
    """Faceted crystal: sharp facets, pointed both ends."""
    obj = lathe(name, [(0, 0), (radius * 0.9, height * waist * 0.5), (radius, height * waist),
                       (radius * top, height * 0.92), (0, height)], swatch, at=(0, 0, 0), segments=sides,
                round_r=0, sharp=15, grad=grad, phase=0.0)
    _place(obj, (at[0], at[1], at[2]), rot)
    return obj


def socket(name, location, size=0.12):
    empty = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(empty)
    empty.empty_display_type = "SPHERE"
    empty.empty_display_size = size
    empty.location = U(location)
    return empty


def arch_points(width, height, segments=8):
    half = width * 0.5
    spring = height - half
    pts = [(-half, 0.0), (half, 0.0), (half, spring)]
    for i in range(1, segments):
        a = i * math.pi / segments
        pts.append((math.cos(a) * half, spring + math.sin(a) * half))
    pts.append((-half, spring))
    return pts


def shield_points(width, height, segments=6):
    half = width * 0.5
    pts = [(-half, height), (half, height), (half, height * 0.45)]
    for i in range(1, segments):
        t = i / segments
        pts.append((half * (1 - t) ** 1.4, height * 0.45 * (1 - t)))
    pts.append((0.0, 0.0))
    for i in range(segments - 1, 0, -1):
        t = i / segments
        pts.append((-half * (1 - t) ** 1.4, height * 0.45 * (1 - t)))
    pts.append((-half, height * 0.45))
    return pts


def pennant_points(length, height, notch=0.35):
    return [(0, 0), (length, height * 0.5 - height * 0.12), (length * (1 - notch), height * 0.5),
            (length, height * 0.5 + height * 0.12), (0, height)]


def ring(name, y, r, h, swatch, segments=24, out=0.02, round_r=0.015):
    return lathe(name, [(r - out, y), (r + out, y), (r + out, y + h), (r - out, y + h), (r - out, y)], swatch,
                 segments=segments, round_r=round_r, caps=False)


# ---------------------------------------------------------------------------------------------


def _paint(obj):
    swatch = obj["tv3_swatch"]
    u0, v0, u1, v1 = swatch_uv_rect(swatch)
    u = (u0 + u1) * 0.5
    pad = (v1 - v0) * 0.12
    g0, g1 = obj["tv3_grad"]
    mesh = obj.data
    world = obj.matrix_world
    zs = [(world @ v.co).z for v in mesh.vertices]
    zrange = list(obj["tv3_zrange"])
    if zrange:
        zmin, zmax = zrange
    else:
        zmin, zmax = min(zs), max(zs)
    span = max(1e-5, zmax - zmin)
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    uv = mesh.uv_layers.active.data
    for loop in mesh.loops:
        if obj["tv3_flat"]:
            t = (g0 + g1) * 0.5
        else:
            t = g0 + (g1 - g0) * min(1.0, max(0.0, (zs[loop.vertex_index] - zmin) / span))
        uv[loop.index].uv = (u, v0 + pad + (v1 - v0 - 2 * pad) * t)


def finish_tower(name, mat, location=(0, 0, 0)):
    """Paint every tagged part, smooth it with per-part sharp edges, and join into one mesh object."""
    bpy.context.view_layer.update()
    parts = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and "tv3_swatch" in obj]
    for obj in parts:
        obj.data.materials.clear()
        obj.data.materials.append(mat)
        _paint(obj)
        obj.data.shade_smooth()
        obj.data.set_sharp_from_angle(angle=math.radians(obj["tv3_sharp"]))
    for other in bpy.context.selected_objects:
        other.select_set(False)
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    body = bpy.context.view_layer.objects.active
    body.name = name
    body.data.name = f"{name}_Mesh"
    for key in ("tv3_swatch", "tv3_sharp", "tv3_grad", "tv3_zrange", "tv3_flat"):
        if key in body:
            del body[key]
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    body.location = U(location)
    return body
