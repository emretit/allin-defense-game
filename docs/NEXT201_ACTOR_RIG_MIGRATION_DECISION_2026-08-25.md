# Defender Chronicles - NEXT-201 Actor Rig Migration Decision

Date: 2026-08-25
Related TODO: `NEXT-201`

## Decision

Do not replace the current runtime actor rigs in this closeout branch.

The eight current runtime actors already have lightweight seven-bone Generic transform rigs and role-specific presentation contracts. This is acceptable for the current modular low-poly vertical slice.

## Evidence

| Evidence | Result |
|---|---|
| `docs/evidence/visual-development/actor-rig-source-inspection-2026-08-25.json` | All eight runtime actors exist and each has one armature with seven bones |
| KayKit comparison FBXs | Present, but static comparison meshes with no armatures/actions |
| EditMode `67/67` on 2026-08-25 | Cinematic/feedback/presentation contract additions compile and pass |

## Migration rule

Only migrate to Humanoid/skinned sources when there is a real authored animation library to retarget.

Recommended future split:

| Actor | Current acceptable rig | Future candidate |
|---|---|---|
| Knight | Generic seven-bone transform rig | Humanoid/skinned when hand-authored sword/shield set exists |
| Guard | Generic seven-bone transform rig | Humanoid/skinned when spear block/parry set exists |
| Raider | Generic seven-bone transform rig | Humanoid/skinned when agile run/attack set exists |
| Vanguard | Generic seven-bone transform rig | Humanoid/skinned when heavy attack/stagger set exists |
| Support | Generic seven-bone transform rig | Humanoid/skinned when cast/heal staff set exists |
| Regenerator | Generic seven-bone transform rig | Role-specific Generic remains preferred |
| Siege | Generic seven-bone transform rig | Role-specific Generic remains preferred |
| Boss | Generic seven-bone transform rig | Role-specific Generic remains preferred for phase armor/core parts |

## Acceptance note

`NEXT-201` is prepared as a migration decision, not a runtime rig replacement. Replacing the rigs now would risk animation/socket regressions without improving the currently accepted vertical slice.
