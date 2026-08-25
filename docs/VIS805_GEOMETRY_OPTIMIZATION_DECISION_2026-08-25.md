# VIS-805 Geometry Optimization Decision

Date: 2026-08-25

Status: In review; no geometry mutation applied

Source report: `docs/evidence/visual-development/vis805-geometry-optimization-decision-2026-08-25.json`

## Decision

No mesh combine, static batching, instancing conversion or LOD authoring was applied in this checkpoint. The current static evidence does not prove a geometry bottleneck, and the remaining final proof requires the physical iPhone Profiler plus Frame/Rendering Debugger gate under VIS-006/VIS-007/VIS-807.

## Current static budget

| Area | Current evidence |
|---|---|
| Actors | 8 actors, 5,028 total triangles, maximum actor 800 triangles, no required socket gaps |
| Buildings | 6 generated buildings, 3,088 total triangles, no required socket gaps |
| Legacy building comparison | Generated building set is 80.1% lower triangle count than the old KayKit catalog baseline recorded in the 16 August metrics |
| Environment proxy | 16 renderers, 3,792 triangles, 16 material slots, 0 shadow casters, 0 colliders |
| VFX stress proxy | 108 signals, 0 pool growth, transparent cost 117.331 / 120 |

## Candidate decisions

| Candidate | Decision | Reason |
|---|---|---|
| Mesh combine | Defer | Environment renderer count is already at the soft budget; combine only non-interactive background clusters if device draw-pass evidence demands it |
| GPU instancing | Candidate | Repeated props and road stones may benefit, but only if Frame Debugger shows repeated material draw calls |
| Static batching | Candidate | Terrain, road and boundary dressing are plausible, but slots, actors, VFX and dynamic outpost states must stay untouched |
| LOD | Reject for current actor/building set | Actor/building triangle counts are low enough that LOD would add QA and silhouette risk without measured benefit |

## Regression rule

VIS-805 cannot become `Tamamlandı` from static evidence alone. Future geometry changes must include before/after draw call, triangle, interaction and camera/path regression proof. Build slots, touch targets, gameplay route, actor sockets and VFX sockets are explicitly out of scope for bulk mesh merging.
