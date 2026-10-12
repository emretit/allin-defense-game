# ALLIN: Defense — Archer Building Progression Pass

Status: M1 generated candidate pass
Date: 2026-08-16
Related TODO: VIS-310, VIS-311, VIS-312, VIS-313, VIS-314, VIS-315, VIS-316, VIS-317

## 1. Decision

Archer L1/L2 generated candidates now carry a clearer archer identity and the first socket-complete building candidate pass.

Source script:

- `tools/art_pipeline/generate_lowpoly_assets.py`

Generated outputs:

- `Assets/Art/Models/Generated/Archer_L1.fbx`
- `Assets/Art/Models/Generated/Archer_L2.fbx`
- `ArtSource/Blender/Archer_L1.blend`
- `ArtSource/Blender/Archer_L2.blend`

Evidence:

- `docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json`
- `docs/evidence/visual-development/building-proxy-contact-sheet-2026-08-16.png`

## 2. Visual Changes

| Building | Added read | Purpose |
|---|---|---|
| Archer L1 | Bow silhouettes, target board, arrow rack, lower watchtower mass | Read as an archer watchtower rather than a generic tower |
| Archer L2 | Taller platform, larger roof/canopy, banner, additional arrows, stronger ring/platform | Read as upgraded archer post through height, mass and props |

The improvement is still a generated candidate, not final accepted catalog art.

## 3. Current Metrics

| Building | Triangles | Materials | Runtime sockets | Scaled bounds XYZ |
|---|---:|---:|---|---|
| Archer L1 generated | 580 | 3 | Complete for current building socket contract | 0.945 × 1.012 × 1.534 |
| Archer L2 generated | 792 | 3 | Complete for current building socket contract | 1.102 × 1.066 × 1.801 |

Socket set present on both generated candidates:

- `Socket_Root`
- `Socket_Ground`
- `Socket_Target`
- `Socket_Projectile`
- `Socket_VFX`
- `Socket_Banner`
- `Socket_Upgrade`
- `Socket_Muzzle`

## 4. Open Issues

- The production catalog still references KayKit catalog-current FBX files, not these generated Archer candidates.
- Material count now meets the ≤3 generated-candidate target; Unity material import validation is still pending.
- Unity import, prefab hierarchy, click target, slot center and upgrade replacement tests are pending.
- Gameplay screenshot and grayscale/thumbnail review are pending.

## 5. Regeneration

```bash
blender -b --factory-startup --python tools/art_pipeline/generate_lowpoly_assets.py -- --output-assets Assets/Art/Models/Generated --source ArtSource/Blender --only Archer_L1 Archer_L2
blender -b --factory-startup --python tools/art_pipeline/inspect_building_proxy_metrics.py -- --output docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json
blender -b --factory-startup --python tools/art_pipeline/render_building_proxy_contact_sheet.py -- --output docs/evidence/visual-development/building-proxy-contact-sheet-2026-08-16.png
```

## 6. Next Acceptance Step

VIS-313/VIS-317 can next decide whether to connect the generated Archer candidates to the catalog or wrap them in final runtime prefabs with Unity-side socket validation. Do not connect them until slot/click regression can be run in Unity.
