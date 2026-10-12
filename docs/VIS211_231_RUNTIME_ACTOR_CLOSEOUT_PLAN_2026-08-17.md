# VIS-211 / VIS-231 Runtime Actor Closeout Plan

Date: 2026-08-17

Status: In progress, not complete

Scope: Knight final runtime model/accessories and Raider final runtime model/accessories.

## Current Evidence

| Role | Current proxy | Current proof | Decision |
|---|---|---|---|
| Knight | `ArtSource/Blender/Knight.blend`, `Assets/Art/Models/Generated/Knight.fbx` | `docs/evidence/visual-development/knight-turnaround-2026-08-17.png`, `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json` | Accepted as editable style/proxy baseline only |
| Raider | `ArtSource/Blender/Raider.blend`, `Assets/Art/Models/Generated/Raider.fbx` | `docs/evidence/visual-development/raider-turnaround-2026-08-17.png`, `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json` | Accepted as editable style/proxy baseline only |

The current proxy metrics report records Knight at 556 triangles, 2 materials and 7/7 required sockets, and Raider at 452 triangles, 2 materials and 6/6 required sockets. Both entries still report runtime readiness as not accepted because final rig, authored clips and gameplay-camera animation proof are not complete.

## 2026-08-17 Source/Export Refresh

Knight and Raider were regenerated from the existing Blender production script with:

`/opt/homebrew/bin/blender --background --python tools/art_pipeline/generate_lowpoly_assets.py -- --output-assets Assets/Art/Models/Generated --source ArtSource/Blender --only Knight Raider`

Then the actor proxy metrics report was regenerated with:

`/opt/homebrew/bin/blender --background --factory-startup --python tools/art_pipeline/inspect_actor_proxy_metrics.py -- --output docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json`

Unity actor catalog validation was also re-run with:

`/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/emreaydin/AllinDefense -executeMethod AllinDefense.Editor.DefenderPresentationBuildValidator.ValidateActorCatalogVisualDevelopmentCheckpoint -logFile Builds/unity-actor-catalog-visual-checkpoint-vis211231.log`

Refresh result:

| Role | Editable source | Runtime export | Triangles | Materials | Required sockets |
|---|---|---|---:|---:|---|
| Knight | `ArtSource/Blender/Knight.blend` | `Assets/Art/Models/Generated/Knight.fbx` | 556 | 2 | 7/7 present |
| Raider | `ArtSource/Blender/Raider.blend` | `Assets/Art/Models/Generated/Raider.fbx` | 452 | 2 | 6/6 present |

Unity catalog result: `[DC_ACTOR_CATALOG_VISUAL_CHECKPOINT] passed warnings=6 actorSocketWarnings=0 knightMaterialWarnings=0 raiderMaterialWarnings=0 sharedActorPrefabs=0 outpostSocketWarnings=0`.

Animation override result: `Builds/unity-actor-animator-override-vis213233.log` first passed with Knight/Raider role overrides, then `Builds/unity-actor-animator-override-vis223.log` passed after Guard joined the override catalog. The current follow-up is `Builds/unity-actor-animator-override-vis263.log`, which passes with `roles=8 roleOverrides=6 sharedFallback=2 knightOverride=true guardOverride=true raiderOverride=true vanguardOverride=true regeneratorOverride=true supportOverride=true`. `Builds/unity-actor-animation-visual-vis263.log` also passed the shared cue mapping checkpoint after Support heal joined the override catalog.

Runtime override-refresh result: `tools/run_ios_actor_capture.sh` first produced the Knight/Raider override capture `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-193606-landscape-upright.png`. The current Support-inclusive run produced `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-211833-landscape-upright.png`, and `Builds/ios-actor-capture.log` now passes `knight_attack=true knight_ability=true guard_spawn=true guard_thrust=true guard_block=true raider_spawn=true raider_hit=true raider_death=true vanguard_spawn=true vanguard_heavy=true regenerator_spawn=true regenerator_pulse=true support_spawn=true support_heal=true`.

Test Runner result: `Builds/editmode-results-vis226.xml` passed 40/40 and includes `ActorAnimatorOverridePathUsesAuthoredKnightGuardRaiderOverridesAndFallbacks` plus `KnightGuardAndRaiderAnimatorOverridesBindRoleSpecificClips`. `Builds/playmode-results-vis226.xml` passed 6/6 after the Guard catalog/runtime capture update.

Important command note: Unity Test Runner must be run without `-quit`; `-runTests` exits the Editor after writing the XML. Earlier attempts with `-quit` compiled/imported but did not produce `Builds/editmode-results-vis213233.xml`.

Generated role-specific animation assets:

- `Assets/Art/GeneratedPresentation/DC_Knight_Actors.overrideController`
- `Assets/Art/GeneratedPresentation/DC_Raider_Actors.overrideController`
- `Assets/Art/GeneratedPresentation/DC_Knight_Idle.anim`, `DC_Knight_Move.anim`, `DC_Knight_Attack.anim`, `DC_Knight_Hit.anim`, `DC_Knight_Defeat.anim`, `DC_Knight_Ability.anim`
- `Assets/Art/GeneratedPresentation/DC_Raider_Idle.anim`, `DC_Raider_Move.anim`, `DC_Raider_Attack.anim`, `DC_Raider_Hit.anim`, `DC_Raider_Defeat.anim`

This advances VIS-211/VIS-231 from waiting to active production, advances VIS-213/VIS-233 from shared fallback proof to generated role override plus runtime capture proof, and advances VIS-223 to generated Guard override proof. It does not close the final runtime actor tasks: the generated source is still a proxy/runtime candidate until final hand-authored motion polish and final runtime prefab review are attached.

## Final Outputs Required

| VIS | Output | Required asset/evidence |
|---|---|---|
| VIS-211 | Knight editable source | Existing `ArtSource/Blender/Knight.blend`, or a documented `Knight_Runtime.blend` successor if the final source is split from the proxy generator |
| VIS-211 | Knight runtime export | Runtime FBX/prefab with shield, sword, stable pivot, final sockets and no weapon/body clipping |
| VIS-211 | Knight closeout evidence | Front/side/isometric update, gameplay-camera screenshot and socket/import validation |
| VIS-231 | Raider editable source | Existing `ArtSource/Blender/Raider.blend`, or a documented `Raider_Runtime.blend` successor if the final source is split from the proxy generator |
| VIS-231 | Raider runtime export | Runtime FBX/prefab with agile posture, light weapon, readable negative space, stable pivot and final sockets |
| VIS-231 | Raider closeout evidence | Front/side/isometric update, gameplay-camera screenshot and socket/import validation |

## Acceptance Gates

| Gate | Knight requirement | Raider requirement |
|---|---|---|
| Pivot/scale | Ground pivot stays stable; catalog scale does not compensate for bad source scale | Ground pivot stays stable; agile height/width remains distinct from Boss/Vanguard |
| Accessory sockets | Shield anchors to `Socket_Hand_L`; sword release reaches `Socket_Weapon_Tip` | Weapon anchors to `Socket_Hand_R`; slash release reaches `Socket_Weapon_Tip` |
| Clipping | Shield, sword, cape and body do not intersect at idle/attack/ability review poses | Arms, legs and weapon preserve readable open negative space at gameplay zoom |
| Material budget | Final gameplay actor targets 2 material slots unless exception is measured | Final gameplay actor targets 2 material slots unless exception is measured |
| Animation handoff | `Attack` and `Ability` authored poses preserve existing cue contract | `Run`, `Attack`, `Hit` and `Defeat` authored poses preserve existing cue contract |
| Gameplay authority | No root-motion gameplay movement; code remains authority | No root-motion gameplay movement; code remains authority |
| Runtime proof | Catalog validation, gameplay-camera screenshot and regression test evidence attached | Catalog validation, gameplay-camera screenshot and regression test evidence attached |

## Do Not Close Until

1. Editable source and runtime export exist for each role.
2. Unity import/prefab validation confirms required sockets in the imported hierarchy; the 2026-08-17 actor catalog checkpoint is green for the current proxy exports, but final runtime prefab validation must be repeated after any authored source replacement.
3. Authored animation or override controller proof exists for the role-specific clips; the current generated override proof is present for Knight/Guard/Raider, but final authored animation polish and gameplay-camera review are still required.
4. Gameplay-camera evidence shows the final runtime actor during preparation/combat, not only a proxy turnaround.
5. Existing VIS-213/VIS-214 and VIS-233/VIS-234 cue/VFX contracts still pass.

## Next Production Step

Start with Knight because it is the hero readability anchor. Replace the proxy with an editable runtime source that preserves the current heavy shield silhouette, then re-run the actor proxy metrics and catalog validation before moving Raider out of proxy status.
