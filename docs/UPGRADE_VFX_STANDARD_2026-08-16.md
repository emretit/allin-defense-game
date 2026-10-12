# ALLIN: Defense — Upgrade VFX Standard

Status: VIS-342 generated candidate pass
Date: 2026-08-16
Related TODO: VIS-342, VIS-345, VIS-500, VIS-609

## 1. Goal

The L1→L2 upgrade effect should make the level change feel deliberate while proving that the new building appears from the same slot center. It must not look like initial construction dust, sell/demolition, damage, or heal.

This pass creates three generated mesh candidates:

- `UpgradeVfx_Anchor`
- `UpgradeVfx_Column`
- `UpgradeVfx_Crest`

Runtime binding is intentionally pending until Unity Editor/PlayMode regression is available.

## 2. Timing Language

| Beat | Time window | Visual | Role |
|---|---:|---|---|
| Anchor | 0.00–0.22s | Blue-gold ground ring and ticks | Holds attention on the unchanged slot center |
| Column | 0.10–0.42s | Vertical cyan/gold lift column | Communicates level-up and replacement height |
| Crest | 0.32–0.58s | Small crown flash | Marks L2 arrival without blocking building readability |

The full effect should stay under roughly 0.6 seconds. Low quality may skip Crest or use Anchor-only.

## 3. Generated Candidate Metrics

Evidence:

- `docs/evidence/visual-development/upgrade-vfx-metrics-2026-08-16.json`
- `docs/evidence/visual-development/upgrade-vfx-sheet-2026-08-16.png`
- `tools/art_pipeline/generate_lowpoly_assets.py`
- `tools/art_pipeline/inspect_upgrade_vfx_metrics.py`
- `tools/art_pipeline/render_upgrade_vfx_sheet.py`

| Asset | FBX | Size bytes | SHA-256 | Triangles | Materials | GUID |
|---|---|---:|---|---:|---:|---|
| UpgradeVfx_Anchor | `Assets/Art/Models/Generated/UpgradeVfx_Anchor.fbx` | 45628 | `538ffddedb4bf7eb7673848e97d427e6a48154163a7d398614d1358cb5605b6d` | 248 | 3 | `8d94d7a0d3b5476d870804878de0aff6` |
| UpgradeVfx_Column | `Assets/Art/Models/Generated/UpgradeVfx_Column.fbx` | 48332 | `b21143b4f60c87b4eb91c9e640bbebb5f343efeb92fa06f134e0c3c2af196762` | 208 | 3 | `8d3fbc78535d4f7f87ec1bc038a15f64` |
| UpgradeVfx_Crest | `Assets/Art/Models/Generated/UpgradeVfx_Crest.fbx` | 43628 | `9fb1fc9aa5c7c0732b97e97afc78f287ece426519c438ba5b5b3d5073519f345` | 176 | 3 | `c0d35e3635174b25a91131ea2923afca` |

Budget result:

- Total generated upgrade VFX triangles: 632.
- Per-asset soft budget: ≤500 triangles.
- Per-asset material budget: ≤3 materials.
- Center-drift flags: none in file-inspected metrics.

## 4. Runtime Binding Rules

When Unity validation is available:

- Trigger from `PresentationCue.OutpostUpgraded`.
- Use pooled objects; do not instantiate/destroy per upgrade.
- Spawn all beats at the outpost/slot root center.
- Do not add colliders to VFX meshes.
- Do not move the building root to fit the effect.
- Do not use shadow-casting point lights.
- Play before or during visual replacement, but verify the final L2 footprint and click target are unchanged.

Suggested sequence:

1. Spawn Anchor at slot center, scale 0.80 → 1.08, deactivate at ~0.22s.
2. Spawn Column at same center, scale 0.75 → 1.0, deactivate at ~0.42s.
3. Spawn Crest slightly above the building socket/upgrade socket, scale 0.60 → 1.0, deactivate at ~0.58s.

## 5. Open Gates

- Unity import validation is pending.
- Runtime pool binding is not implemented in this pass.
- L1→L2 catalog replacement screenshots are pending for Archer, Barracks and Mage.
- Eight slot upgrade regression must prove tap/click targets remain unchanged.
- Low/Medium quality fallback must be validated once Unity profiling is available.

## 6. Regeneration

```bash
blender -b --factory-startup --python tools/art_pipeline/generate_lowpoly_assets.py -- --output-assets Assets/Art/Models/Generated --source ArtSource/Blender --only UpgradeVfx_Anchor UpgradeVfx_Column UpgradeVfx_Crest
blender -b --factory-startup --python tools/art_pipeline/inspect_upgrade_vfx_metrics.py -- --output docs/evidence/visual-development/upgrade-vfx-metrics-2026-08-16.json
blender -b --factory-startup --python tools/art_pipeline/render_upgrade_vfx_sheet.py -- --output docs/evidence/visual-development/upgrade-vfx-sheet-2026-08-16.png
```
