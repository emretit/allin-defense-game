"""Render tower v3 previews in the gameplay camera angle (43 deg ortho) with the battle key/fill light.

Usage: blender -b --factory-startup --python render_tower_v3_sheet.py -- --out DIR --set a1
"""

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import tower_v3_kit as kit  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
PIXELS_PER_UNIT_960 = 960 / (8.9 * 2)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def setup_render(scene, size, transparent=False, samples=64):
    scene.render.engine = "CYCLES"
    prefs = bpy.context.preferences.addons["cycles"].preferences
    try:
        prefs.compute_device_type = "METAL"
        prefs.get_devices()
        for device in prefs.devices:
            device.use = True
        scene.cycles.device = "GPU"
    except Exception:
        pass
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 3
    scene.cycles.diffuse_bounces = 1
    scene.render.resolution_x = size[0]
    scene.render.resolution_y = size[1]
    scene.render.film_transparent = transparent
    scene.view_settings.view_transform = "ACES 1.3"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.15


def setup_world(scene):
    world = bpy.data.worlds.new("Night")
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.075, 0.105, 0.15, 1.0)
    bg.inputs["Strength"].default_value = 1.0
    scene.world = world


def add_lights():
    # Unity key: Euler(48, -38, 0), colour lerp(Moon, white, .52), intensity 1.58.
    fwd_unity = Vector((math.sin(math.radians(-38)) * math.cos(math.radians(48)), -math.sin(math.radians(48)),
                        math.cos(math.radians(-38)) * math.cos(math.radians(48))))
    fwd = Vector((fwd_unity.x, fwd_unity.z, fwd_unity.y))
    sun_data = bpy.data.lights.new("Key", "SUN")
    sun_data.color = (0.80, 0.90, 1.0)
    sun_data.energy = 4.6
    sun_data.angle = math.radians(6)
    sun = bpy.data.objects.new("Key", sun_data)
    sun.rotation_euler = fwd.to_track_quat("-Z", "Y").to_euler()
    bpy.context.collection.objects.link(sun)
    rim = bpy.data.lights.new("Rim", "SUN")
    rim.color = (0.55, 0.75, 1.0)
    rim.energy = 0.9
    rim.use_shadow = False
    rim_obj = bpy.data.objects.new("Rim", rim)
    rim_obj.rotation_euler = Vector((0.5, 0.6, -0.6)).to_track_quat("-Z", "Y").to_euler()
    bpy.context.collection.objects.link(rim_obj)


def add_ground(colour=(0.022, 0.046, 0.058), catcher=False):
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0))
    plane = bpy.context.object
    mat = bpy.data.materials.new("Ground")
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*colour, 1.0)
    mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.95
    plane.data.materials.append(mat)
    plane.is_shadow_catcher = catcher
    return plane


def add_camera(target_height, ortho_scale):
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho_scale
    cam = bpy.data.objects.new("Cam", cam_data)
    pitch = math.radians(43)
    target = Vector((0, 0, target_height))
    cam.location = target + Vector((0, -math.cos(pitch), math.sin(pitch))) * 20
    cam.rotation_euler = (math.radians(90 - 43), 0, 0)
    bpy.context.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    return cam


def build(builder, scale=1.0):
    reset()
    mat = kit.palette_material(ROOT / "Builds" / "towers-v3" / "_palette", emission_strength=0.7)
    builder()
    body = kit.finish_tower("Tower", mat)
    body.scale = (scale, scale, scale)
    return body


def render_closeup(builder, path, size=640):
    build(builder)
    scene = bpy.context.scene
    setup_render(scene, (size, size))
    setup_world(scene)
    add_lights()
    add_ground()
    add_camera(1.7, 4.6)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def render_game_scale(builder, path, scale, ppu=PIXELS_PER_UNIT_960, extent=4.4):
    build(builder, scale)
    scene = bpy.context.scene
    px = int(round(extent * ppu))
    setup_render(scene, (px, px), transparent=True, samples=96)
    setup_world(scene)
    add_lights()
    add_ground(catcher=True)
    add_camera(0.0, extent)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True)
    parser.add_argument("--set", default="a1")
    parser.add_argument("--only", nargs="*")
    parser.add_argument("--game-only", action="store_true")
    parser.add_argument("--scale", type=float, default=0.8)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    if args.set == "a1":
        import tower_v3_styles as styles
        for style, kinds in styles.STYLES.items():
            for kind, builder in kinds.items():
                tag = f"s{style}_{kind}"
                if args.only and tag not in args.only:
                    continue
                if not args.game_only:
                    render_closeup(builder, out / f"{tag}.png")
                render_game_scale(builder, out / f"{tag}_game960.png", args.scale)
                render_game_scale(builder, out / f"{tag}_game720.png", args.scale, ppu=720 / (8.9 * 2))
                print(f"Rendered {tag}")
    if args.set == "a2":
        import tower_v3_models as models
        variants = [(k, 1, None) for k in ("archer", "mage", "barracks")] + \
                   [(k, 2, None) for k in ("archer", "mage", "barracks")] + \
                   [(kind, 2, branch) for branch, kind in models.BRANCHES.items()]
        for kind, level, branch in variants:
            tag = f"{kind}_{branch or f'L{level}'}"
            if args.only and tag not in args.only:
                continue
            builder = (lambda k=kind, l=level, b=branch: models.BUILDERS[k](l, b))
            if not args.game_only:
                render_closeup(builder, out / f"{tag}.png")
            render_game_scale(builder, out / f"{tag}_game960.png", args.scale)
            render_game_scale(builder, out / f"{tag}_game720.png", args.scale, ppu=720 / (8.9 * 2))
            print(f"Rendered {tag}")


if __name__ == "__main__":
    main()
