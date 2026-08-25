# Defender Chronicles — Build-In VFX Standard

Status: VIS-341 generated candidate pass
Date: 2026-08-16
Related TODO: VIS-341, VIS-345, VIS-500, VIS-607

## 1. Goal

The build-in effect should hide outpost prefab pop-in and confirm construction without changing the slot collider, tap target, economy, or pathing. It must read as “construction/placement,” not as attack damage, heal, sell, or upgrade.

This pass creates three generated mesh candidates that can later be played by the existing pooled presentation layer:

- `BuildVfx_Dust`
- `BuildVfx_Ring`
- `BuildVfx_Spark`

Runtime binding is intentionally pending until Unity Editor/PlayMode regression is available.

## 2. Timing Language

| Beat | Time window | Visual | Role |
|---|---:|---|---|
| Dust | 0.00–0.18s | Earth puffs and stone chips | Hides the first frame of the new outpost |
| Ring | 0.08–0.34s | Cyan construction/blueprint ring | Shows the slot center and build confirmation |
| Spark | 0.22–0.48s | Small upward cyan/gold shards | Ends the action with a light fantasy craft cue |

The full effect should remain below roughly half a second so it does not block tower readability or next input.

## 3. Generated Candidate Metrics

Evidence:

- `docs/evidence/visual-development/build-vfx-metrics-2026-08-16.json`
- `docs/evidence/visual-development/build-vfx-sheet-2026-08-16.png`
- `tools/art_pipeline/generate_lowpoly_assets.py`
- `tools/art_pipeline/inspect_build_vfx_metrics.py`
- `tools/art_pipeline/render_build_vfx_sheet.py`

| Asset | FBX | Size bytes | SHA-256 | Triangles | Materials | GUID |
|---|---|---:|---|---:|---:|---|
| BuildVfx_Dust | `Assets/Art/Models/Generated/BuildVfx_Dust.fbx` | 61996 | `0add5fc508b6ca1307b72de4b4558c58cb0d890e20bf1ba7c800e0f7fb947ad4` | 332 | 2 | `5034969c2f70421c9b0684b145ee8523` |
| BuildVfx_Ring | `Assets/Art/Models/Generated/BuildVfx_Ring.fbx` | 53068 | `b53e138e9931b22e0e03878f7fe21afe7731fe6a4455f81168e80edda61f96b3` | 280 | 3 | `cc148379648c46f09f244dade252954e` |
| BuildVfx_Spark | `Assets/Art/Models/Generated/BuildVfx_Spark.fbx` | 42508 | `232bbc799ee171d56120d3f5eed4d189a2fa42c852bde6369b891de06496f2fe` | 172 | 2 | `15071003f8024c26bea36144c75a9b02` |

Budget result:

- Total generated build VFX triangles: 784.
- Per-asset soft budget: ≤500 triangles.
- Per-asset material budget: ≤3 materials.
- Current generated candidates meet both file-inspected budgets.

## 4. Runtime Binding Rules

When Unity validation is available:

- Trigger from `PresentationCue.OutpostBuilt`.
- Use pooled objects; do not instantiate/destroy per build in combat.
- Play at the slot/outpost root position, not at HUD coordinates.
- Do not add colliders to VFX meshes.
- Do not block the existing slot click/tap target.
- Do not use shadow-casting point lights.
- Keep all pieces optional for Low quality; Dust-only is the fallback.

Suggested sequence:

1. Spawn Dust at slot center, scale from 0.55 → 1.05, fade or deactivate at ~0.18s.
2. Spawn Ring at slot center, scale from 0.75 → 1.15, rotate gently, deactivate at ~0.34s.
3. Spawn Spark at slot center + small upward offset, scale 0.65 → 1.0, deactivate at ~0.48s.

## 5. Open Gates

- Unity import validation is pending.
- Runtime pool binding is not implemented in this pass.
- Eight slot build regression must prove tap/click targets remain unchanged.
- Visual proof must be captured in preparation state and during active wave.
- Low/Medium quality fallback must be validated once Unity profiling is available.

## 6. Regeneration

```bash
blender -b --factory-startup --python tools/art_pipeline/generate_lowpoly_assets.py -- --output-assets Assets/Art/Models/Generated --source ArtSource/Blender --only BuildVfx_Dust BuildVfx_Ring BuildVfx_Spark
blender -b --factory-startup --python tools/art_pipeline/inspect_build_vfx_metrics.py -- --output docs/evidence/visual-development/build-vfx-metrics-2026-08-16.json
blender -b --factory-startup --python tools/art_pipeline/render_build_vfx_sheet.py -- --output docs/evidence/visual-development/build-vfx-sheet-2026-08-16.png
```
