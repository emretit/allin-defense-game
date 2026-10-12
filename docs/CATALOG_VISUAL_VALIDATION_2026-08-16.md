# ALLIN: Defense — Catalog Visual Development Validation

Status: VIS-208 implementation scaffold
Date: 2026-08-16
Related TODO: VIS-201, VIS-203, VIS-205, VIS-206, VIS-208, VIS-810

## 1. Decision

The production catalog now has two validation layers:

1. `ValidationErrors()` — production technical completeness. This must stay empty for the current playable baseline.
2. `VisualDevelopmentValidationWarnings()` — visual-production gates for the upgraded art pipeline. This reports known proxy/runtime gaps without breaking the current playable build.

This split is intentional. The current generated actor FBX files are accepted as static proxy baseline, but they are not final runtime actors. The visual gate makes those gaps explicit.

## 2. Current Code Touchpoints

| File | Purpose |
|---|---|
| `Assets/Scripts/Presentation/DefenderPresentationCatalog.cs` | Adds `VisualDevelopmentValidationWarnings()` |
| `Assets/Tests/EditMode/CoreRulesTests.cs` | Adds tests proving current proxy gaps are visible while accepted actor gates stay protected |
| `Assets/Scripts/Presentation/PresentationActorAnimationContract.cs` | Provides shared animation clip/trigger names used by validation |
| `Assets/Editor/DefenderPresentationBuildValidator.cs` | Provides `ValidateActorCatalogVisualDevelopmentCheckpoint()` for batch-mode catalog evidence |

## 3. VIS-208 Gates Covered

| Gate | Current behavior |
|---|---|
| Actor count and role coverage | Reports if the catalog does not contain exactly one entry per actor kind |
| Unique actor visual entries | Reports if multiple actor roles share the same prefab |
| Runtime socket coverage | Reports missing role-specific `Socket_*` transforms |
| Material slot target | Reports when current actor source material count exceeds final target |
| Shared animation clips | Reports missing shared contract clips |
| Shared actor controller | Reports missing shared actor controller |
| Outpost role/level coverage | Reports missing or duplicate building level entries |
| Outpost runtime socket coverage | Reports missing building `Socket_*` transforms |
| Outpost material slot target | Reports building entries over the final material target |

## 4. Expected Current Warnings

The 2026-08-16 generated proxy actors were expected to report VIS-203 socket warnings because they did not contain runtime socket transforms. The 2026-08-17 actor proxy regeneration added role-specific sockets and consolidated Knight/Raider materials.

Current accepted checkpoint:

- `Builds/unity-actor-catalog-visual-checkpoint.log`
- `[DC_ACTOR_CATALOG_VISUAL_CHECKPOINT] passed warnings=6 actorSocketWarnings=0 knightMaterialWarnings=0 raiderMaterialWarnings=0 sharedActorPrefabs=0 outpostSocketWarnings=0`

The remaining six visual-development warnings are still allowed proxy/runtime gaps for actor roles outside the Knight/Raider M1 proof path or later animation/runtime acceptance. They should not be treated as a regression until those final runtime actor prefabs are introduced.

After final runtime actors are produced, VIS-208 should be tightened from warning/report mode into failing acceptance tests for required runtime pieces.

## 5. Open Validation

Unity batch mode successfully compiled and ran the catalog visual checkpoint method on 2026-08-17. The Unity Test Runner did not create a fresh filtered XML in this pass, so the direct checkpoint log is the current actor catalog evidence.

Required follow-up:

1. Restore or replace the filtered Unity Test Runner XML flow.
2. Run the full EditMode suite again and confirm `ProductionPresentationCatalogIsComplete` plus `VisualDevelopmentCatalogGateReportsCurrentProxyGaps`.
3. Once final runtime actor prefabs exist, update the visual gate acceptance from warnings to hard failures.
