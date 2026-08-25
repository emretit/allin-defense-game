# Defender Chronicles — Sell/Demolition VFX Standard

Status: VIS-343 generated candidate pass
Date: 2026-08-16
Related TODO: VIS-343, VIS-345, VIS-500, VIS-704

## 1. Goal

The sell/demolition effect should communicate “this outpost was sold and refunded” without looking like initial construction, upgrade, unit death, enemy damage, victory, or defeat.

This pass creates three generated mesh candidates:

- `SellVfx_Ring`
- `SellVfx_Chips`
- `SellVfx_Glint`

Runtime binding is intentionally pending until Unity Editor/PlayMode regression is available.

## 2. Timing Language

| Beat | Time window | Visual | Role |
|---|---:|---|---|
| Ring | 0.00–0.18s | Neutral/gold refund ring | Confirms the economic action at the slot |
| Chips | 0.06–0.34s | Downward stone/wood dismantle chips | Reads as removal/demolition, not combat defeat |
| Glint | 0.22–0.44s | Small gold coin glint | Closes with a short refund cue |

The full effect should stay under roughly 0.45 seconds and leave the slot visually readable immediately afterward.

## 3. Generated Candidate Metrics

Evidence:

- `docs/evidence/visual-development/sell-vfx-metrics-2026-08-16.json`
- `docs/evidence/visual-development/sell-vfx-sheet-2026-08-16.png`
- `tools/art_pipeline/generate_lowpoly_assets.py`
- `tools/art_pipeline/inspect_sell_vfx_metrics.py`
- `tools/art_pipeline/render_sell_vfx_sheet.py`

| Asset | FBX | Size bytes | SHA-256 | Triangles | Materials | GUID |
|---|---|---:|---|---:|---:|---|
| SellVfx_Ring | `Assets/Art/Models/Generated/SellVfx_Ring.fbx` | 44252 | `4d47a51e4863b1bdf414ba26991289af1c9c7e5c049ea799d0b51619761f6cac` | 284 | 2 | `7ead109f21e643b19f45765bf876a153` |
| SellVfx_Chips | `Assets/Art/Models/Generated/SellVfx_Chips.fbx` | 54668 | `10108877f15f34a487252f3c111685aed4a9fd8486141169530da8e8d03d2468` | 220 | 3 | `b0e59dea4406478e9e54ad8e62e17b0e` |
| SellVfx_Glint | `Assets/Art/Models/Generated/SellVfx_Glint.fbx` | 46620 | `a94ce52b5bc83cd3a6b49049e70685d6a8c091ce82241ea9cd10e0a608d1bcc2` | 256 | 1 | `b764357a1ca1402da833fa622549e146` |

Budget result:

- Total generated sell VFX triangles: 760.
- Per-asset soft budget: ≤500 triangles.
- Per-asset material budget: ≤3 materials.
- Enemy/death red material flags: none in file-inspected metrics.

## 4. Runtime Binding Rules

When Unity validation is available:

- Trigger from `PresentationCue.OutpostSold`.
- Use pooled objects; do not instantiate/destroy per sell in combat.
- Play at the outpost/slot root center before or while the tower visual is removed.
- Do not add colliders to VFX meshes.
- Do not block the slot click/tap target after sale.
- Do not use enemy red or defeat/victory screen colors.
- Do not use shadow-casting point lights.
- Low quality may use Ring-only or Glint-only.

Suggested sequence:

1. Spawn Ring at slot center, scale 0.95 → 1.08, deactivate at ~0.18s.
2. Spawn Chips at same center, scale 0.9 → 0.65 or drop/downward fade, deactivate at ~0.34s.
3. Spawn Glint slightly above slot center, scale 0.55 → 0.85, deactivate at ~0.44s.

## 5. Open Gates

- Unity import validation is pending.
- Runtime pool binding is not implemented in this pass.
- Sell action screenshot/video is pending.
- Eight slot sell regression must prove tap/click targets and rebuild after sell remain unchanged.
- Audio/VFX/economy synchronization remains open under VIS-704.

## 6. Regeneration

```bash
blender -b --factory-startup --python tools/art_pipeline/generate_lowpoly_assets.py -- --output-assets Assets/Art/Models/Generated --source ArtSource/Blender --only SellVfx_Ring SellVfx_Chips SellVfx_Glint
blender -b --factory-startup --python tools/art_pipeline/inspect_sell_vfx_metrics.py -- --output docs/evidence/visual-development/sell-vfx-metrics-2026-08-16.json
blender -b --factory-startup --python tools/art_pipeline/render_sell_vfx_sheet.py -- --output docs/evidence/visual-development/sell-vfx-sheet-2026-08-16.png
```
