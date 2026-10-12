# ALLIN: Defense - Frostfen Hollow Second Biome Plan

Date: 2026-08-25
Related TODO: `NEXT-202`

Frostfen Hollow is a second-biome preproduction pass for the existing vertical-slice language. It intentionally reuses the Greenwatch route and eight-slot readability rules so the test answers one question: can the art direction scale to a different biome without losing gameplay clarity?

## Evidence

| Artifact | Path |
|---|---|
| Paint-over PNG | `docs/evidence/visual-development/frostfen-hollow-paintover-plan-2026-08-25.png` |
| Paint-over JSON/kit manifest | `docs/evidence/visual-development/frostfen-hollow-paintover-plan-2026-08-25.json` |
| Generator | `tools/art_pipeline/render_second_biome_paintover_plan.py` |

## Direction

Frostfen Hollow keeps the same night-fantasy foundation but shifts the material language from green valley to cold marsh/glacier:

- icy cyan road edges and cracked frozen stone;
- dark teal reeds and snow clumps as low filler;
- frost pines and glacier silhouettes as map frame;
- green ghost lanterns for mid-map control landmarks;
- warm enemy runes inside a broken ice gate so threat direction remains readable.

## Modular kit

| Asset | Role | Budget |
|---|---|---|
| `FrostPineTall` | North/south frame silhouette | ≤420 tris, 1 material |
| `IceShardCluster` | Boss/final-stand landmark accent | ≤280 tris, emissive optional |
| `FenLanternPost` | Mid-bend control readability | ≤220 tris, no shadow-casting point light on Low/Medium |
| `FrozenRoadStone` | Road-edge rhythm | ≤120 tris, atlas material |
| `ReedSnowClump` | Low filler outside touch rings | ≤90 tris, no collider |
| `BrokenIceArch` | Enemy gate variant | ≤700 tris, 2 materials |

## Acceptance rule

This completes a preproduction version of `NEXT-202`; it does not create a second playable map. Runtime implementation would be a separate production task after the iOS release gate.
