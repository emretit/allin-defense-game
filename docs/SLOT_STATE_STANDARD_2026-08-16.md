# ALLIN: Defense — World-Space Slot State Standard

Status: VIS-340 generated candidate pass
Date: 2026-08-16
Related TODO: VIS-340, VIS-345, VIS-607

## 1. Goal

Build slots must communicate state from the gameplay camera without relying only on color. The current generated pass creates five world-space FBX candidates:

- Empty
- Selected
- Available
- Insufficient
- Locked

These are generated art candidates, not yet runtime-bound catalog assets. The existing playable slot behavior remains unchanged until Unity click/tap regression can run.

## 2. Shape Language

| State | Color cue | Non-color cue | Gameplay meaning |
|---|---|---|---|
| Empty | Dark inner disc, subtle blue ticks | Calm flat center and low four-tick rim | Buildable location exists but is not actively targeted |
| Selected | Cyan/blue focus | Raised inner disc and four bracket/corner marks | Player is choosing or inspecting this slot |
| Available | Green/forest pulse | Four upward shard/arrow shapes | Current build choice can be placed here |
| Insufficient | Gold/orange warning | Broken shards and slash bars | Slot is valid but player lacks resource/cost requirement |
| Locked | Dark bars | Four posts and crossed blocking bars | Slot should not accept current action |

## 3. Generated Candidate Metrics

Evidence:

- `docs/evidence/visual-development/slot-state-metrics-2026-08-16.json`
- `docs/evidence/visual-development/slot-state-sheet-2026-08-16.png`
- `tools/art_pipeline/generate_lowpoly_assets.py`
- `tools/art_pipeline/inspect_slot_state_metrics.py`
- `tools/art_pipeline/render_slot_state_sheet.py`

| Slot state | FBX | Size bytes | SHA-256 | Triangles | Materials |
|---|---|---:|---|---:|---:|
| OutpostSlot_Empty | `Assets/Art/Models/Generated/OutpostSlot_Empty.fbx` | 46540 | `0c9a70db8b34c2e51bc5b724c6d5d3c7c2d81634775ceeba27576b59f963337f` | 440 | 3 |
| OutpostSlot_Selected | `Assets/Art/Models/Generated/OutpostSlot_Selected.fbx` | 57404 | `ebba88d6c96d0de43fcc5ba37638aadb69f1f116cfbbb1636c1b8c41ef3a0b3e` | 404 | 3 |
| OutpostSlot_Available | `Assets/Art/Models/Generated/OutpostSlot_Available.fbx` | 48252 | `d294a13ccee03fe7c41222d8f5788ee29d407a1c5aa9456973f0d9c0cad6c354` | 372 | 3 |
| OutpostSlot_Insufficient | `Assets/Art/Models/Generated/OutpostSlot_Insufficient.fbx` | 54444 | `dcba668848d19331447d448e06c70e1aeac49ebb964f8ab3d540b14fd0c103b4` | 360 | 3 |
| OutpostSlot_Locked | `Assets/Art/Models/Generated/OutpostSlot_Locked.fbx` | 49788 | `dc12d7643995c1b3d371b18776765ca458306295e2ecb0a6ad8cf4632efc4323` | 432 | 2 |

Budget result:

- Total generated slot-state triangles: 2,008.
- Per-state soft budget: ≤500 triangles.
- Per-state material budget: ≤3 materials.
- Current generated candidates meet both file-inspected budgets.

## 4. Runtime Binding Rules

When Unity validation is available, these candidates should be bound as visual state overlays or replacement visuals without changing slot ownership, collider size, tap target, or route occupancy.

Required runtime states:

- No tower selected: Empty.
- Tower/build option selected and slot valid: Available.
- Slot actively targeted or tapped: Selected.
- Tower/build option selected but resource is insufficient: Insufficient.
- Tutorial, future map lock, or non-buildable override: Locked.

HUD build-button states should reuse the same visual language: blue bracket for selected, green upward mark for available, gold shard/slash for insufficient, dark crossbar for locked.

## 5. Open Gates

- Unity import validation is pending.
- Runtime state switching is not implemented in the current pass.
- Eight existing slot click/tap targets must be proven unchanged.
- Slot visuals must be checked in 16:9, 20:9 and tablet framing.
- HUD button-state alignment remains open under VIS-607.

## 6. Regeneration

```bash
blender -b --factory-startup --python tools/art_pipeline/generate_lowpoly_assets.py -- --output-assets Assets/Art/Models/Generated --source ArtSource/Blender --only OutpostSlot_Empty OutpostSlot_Selected OutpostSlot_Available OutpostSlot_Insufficient OutpostSlot_Locked
blender -b --factory-startup --python tools/art_pipeline/inspect_slot_state_metrics.py -- --output docs/evidence/visual-development/slot-state-metrics-2026-08-16.json
blender -b --factory-startup --python tools/art_pipeline/render_slot_state_sheet.py -- --output docs/evidence/visual-development/slot-state-sheet-2026-08-16.png
```
