"""Create a lightweight, posed Unity FBX from a licensed KayKit character.

Usage:
  blender -b --factory-startup --python optimize_kaykit_character.py -- \
    source.fbx output.fbx CharacterName profile

The original KayKit FBX contains a large reusable animation library. Defender
Chronicles animates its actor proxy at the presentation root, so the shipping
model only needs one readable idle pose and the meshes used by that profile.
"""

from pathlib import Path
import sys

import bpy


PROFILES = {
    "knight": {
        "action": "Rig|Idle",
        "frame": 9,
        "keep": ("Knight_", "Knight_Cape", "Badge_Shield", "1H_Sword"),
    },
    "barbarian": {
        "action": "Rig|2H_Melee_Idle",
        "frame": 9,
        "keep": ("Barbarian_", "Barbarian_Cape", "2H_Axe"),
    },
    "mage": {
        "action": "Rig|Spellcasting",
        "frame": 10,
        "keep": ("Mage_", "Mage_Cape", "2H_Staff"),
    },
    "rogue": {
        "action": "Rig|Idle",
        "frame": 9,
        "keep": ("Rogue_", "Rogue_Cape", "Knife", "Knife_Offhand"),
    },
}


def main() -> None:
    args = sys.argv[sys.argv.index("--") + 1 :]
    if len(args) != 4:
        raise SystemExit("Expected: source.fbx output.fbx CharacterName profile")
    source, output, character_name, profile_name = args
    profile = PROFILES[profile_name]

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=source)
    rig = next((obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"), None)
    if rig is None:
        raise RuntimeError(f"No armature found in {source}")

    action = bpy.data.actions.get(profile["action"])
    if action is None:
        action = bpy.data.actions.get("Rig|Idle")
    rig.animation_data_create()
    rig.animation_data.action = action
    bpy.context.scene.frame_set(profile["frame"])
    bpy.context.view_layer.update()

    keep_meshes = []
    for obj in list(bpy.context.scene.objects):
        if obj.type != "MESH":
            continue
        if not any(obj.name.startswith(prefix) for prefix in profile["keep"]):
            bpy.data.objects.remove(obj, do_unlink=True)
            continue
        keep_meshes.append(obj)

    if not keep_meshes:
        raise RuntimeError(f"Profile {profile_name} selected no meshes")

    # Freeze the selected animation frame before removing the heavy rig/actions.
    for obj in keep_meshes:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        for modifier in list(obj.modifiers):
            if modifier.type == "ARMATURE":
                bpy.ops.object.modifier_apply(modifier=modifier.name)
        world = obj.matrix_world.copy()
        obj.parent = None
        obj.matrix_world = world
        obj.select_set(False)

    bpy.data.objects.remove(rig, do_unlink=True)
    for obj in list(bpy.context.scene.objects):
        if obj.type not in {"MESH"}:
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in keep_meshes:
        if obj.name in bpy.context.scene.objects:
            obj.select_set(True)
    bpy.context.view_layer.objects.active = keep_meshes[0]
    bpy.ops.object.join()
    model = bpy.context.object
    model.name = character_name
    model.data.name = f"{character_name}_Mesh"

    output_path = Path(output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )
    print(f"[DC_KAYKIT_OPTIMIZED] {character_name}: {output_path}")


if __name__ == "__main__":
    main()
