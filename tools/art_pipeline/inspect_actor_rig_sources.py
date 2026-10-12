"""Inspect actor rig source FBX files and write a compact JSON report.

Usage:
  blender -b --factory-startup --python tools/art_pipeline/inspect_actor_rig_sources.py -- \
    --output docs/evidence/visual-development/actor-rig-source-inspection-2026-08-16.json
"""

from __future__ import annotations

import argparse
import datetime
import json
import sys
from pathlib import Path

import bpy


SOURCES = {
    **{
        name: (f"Assets/Art/Models/Generated/{name}.fbx", "runtime", "Generic transform rig")
        for name in ("Knight", "Guard", "Raider", "Vanguard", "Regenerator", "Support", "Siege", "Boss")
    },
    "KnightLicensed": ("Assets/ThirdParty/KayKit/Adventurers/Models/KnightLicensed.fbx", "comparison", "Static CC0 reference"),
    "BarbarianLicensed": ("Assets/ThirdParty/KayKit/Adventurers/Models/BarbarianLicensed.fbx", "comparison", "Static CC0 reference"),
    "MageLicensed": ("Assets/ThirdParty/KayKit/Adventurers/Models/MageLicensed.fbx", "comparison", "Static CC0 reference"),
    "RogueLicensed": ("Assets/ThirdParty/KayKit/Adventurers/Models/RogueLicensed.fbx", "comparison", "Static CC0 reference"),
}


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)


def inspect_source(name: str, path: Path, purpose: str, rig_target: str) -> dict[str, object]:
    reset_scene()
    bpy.ops.import_scene.fbx(filepath=str(path))

    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    actions = sorted(action.name for action in bpy.data.actions)
    materials = sorted({slot.material.name for obj in meshes for slot in obj.material_slots if slot.material})

    return {
        "name": name,
        "path": str(path),
        "purpose": purpose,
        "rig_target": rig_target,
        "exists": path.exists(),
        "armature_count": len(armatures),
        "armatures": [
            {
                "name": armature.name,
                "bone_count": len(armature.data.bones),
                "root_bones": [bone.name for bone in armature.data.bones if bone.parent is None],
            }
            for armature in armatures
        ],
        "mesh_count": len(meshes),
        "mesh_names": sorted(obj.name for obj in meshes)[:80],
        "material_count": len(materials),
        "material_names": materials[:80],
        "action_count": len(actions),
        "action_names": actions[:160],
        "has_rig": len(armatures) > 0,
        "has_actions": len(actions) > 0,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])

    report = {
        "schema": "allin-defense.visual-development.actor-rig-source-inspection.v2",
        "captured_on": datetime.date.today().isoformat(),
        "sources": [inspect_source(name, Path(path), purpose, target)
                    for name, (path, purpose, target) in SOURCES.items()],
        "decision": {
            "unity_import_rig_type": "Generic transform rigs for this modular low-poly vertical slice",
            "reason": "Every runtime actor now owns an original lightweight armature and bone-parented modular geometry. The stored KayKit files remain static CC0 comparison sources and are not runtime catalog actors.",
            "animation_authority": "Animator events may time presentation only; gameplay remains authoritative in combat code.",
            "unity_validation_status": "Covered by EditMode catalog, override, bone-curve and visible-geometry tests.",
            "humanoid_follow_up": "A future fully skinned animation library may migrate Knight, Guard, Raider, Vanguard and Support to Unity Humanoid avatars; it is not required by the current rigid low-poly runtime clips.",
        },
    }

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"[DC_ACTOR_RIG_SOURCE_INSPECTION] {output}")


if __name__ == "__main__":
    main()
