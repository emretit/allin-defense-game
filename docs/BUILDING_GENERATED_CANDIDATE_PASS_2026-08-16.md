# Defender Chronicles — Building Generated Candidate Socket/Material Pass

Status: VIS-302/VIS-303 generated candidate pass
Date: 2026-08-16
Related TODO: VIS-301–VIS-304, VIS-321, VIS-322, VIS-324, VIS-325, VIS-331, VIS-332, VIS-334, VIS-335

## 1. Decision

All six generated building candidates now meet the current generated-candidate socket and material-count target:

- Required building runtime sockets are present.
- Generated material count is ≤3 per building.
- Generated building set is now bound into the playable production catalog; runtime slot/click regression still remains open.

Evidence:

- `docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json`
- `docs/evidence/visual-development/building-proxy-contact-sheet-2026-08-16.png`
- `docs/evidence/visual-development/building-readability-sheet-2026-08-16.png`
- `tools/art_pipeline/generate_lowpoly_assets.py`
- `tools/art_pipeline/render_building_readability_sheet.py`

## 2. Current Generated Candidate Metrics

| Building | Triangles | Materials | Socket status |
|---|---:|---:|---|
| Archer L1 | 580 | 3 | Complete |
| Archer L2 | 792 | 3 | Complete |
| Barracks L1 | 332 | 3 | Complete |
| Barracks L2 | 636 | 3 | Complete |
| Mage L1 | 364 | 3 | Complete |
| Mage L2 | 384 | 3 | Complete |

Generated total: 3,088 triangles.

## 3. Socket Coverage

All generated candidates include the common building sockets:

- `Socket_Root`
- `Socket_Ground`
- `Socket_Target`
- `Socket_Projectile`
- `Socket_VFX`
- `Socket_Banner`
- `Socket_Upgrade`

Role-specific sockets:

- Archer: `Socket_Muzzle`
- Barracks: `Socket_Spawn`
- Mage: `Socket_Crystal`, `Socket_Cast`

## 4. Readability Pass

`docs/evidence/visual-development/building-readability-sheet-2026-08-16.png` renders the generated candidates in three rows:

- Color / candidate material
- Grayscale / silhouette value
- Tiny thumbnail / role read

Findings:

- Archer L1/L2 read as the tallest watchtower family, and L2 gains stronger roof/banner/arrow mass.
- Mage L1/L2 read as the vertical crystal/rune family, and L2 gains the strongest arcane silhouette.
- Barracks L1/L2 now have a socket/material-complete generated pass; L2 gained a clearer upper mass, side turrets, taller roof, front brace and weapon-rack silhouette after the thumbnail check exposed that it was too close to L1.

## 5. Open Gates

- Unity import validation is pending.
- Runtime catalog binding and gameplay screenshot are now recorded in `docs/BUILDING_GENERATED_CATALOG_BINDING_RUNTIME_PASS_2026-08-16.md`.
- Formal PlayMode XML result for slot center, click target and upgrade replacement regression is still pending.
- Final art direction for Barracks/Mage can still be improved beyond this generated candidate pass.

## 6. Regeneration

```bash
blender -b --factory-startup --python tools/art_pipeline/generate_lowpoly_assets.py -- --output-assets Assets/Art/Models/Generated --source ArtSource/Blender --only Archer_L1 Archer_L2 Barracks_L1 Barracks_L2 Mage_L1 Mage_L2
blender -b --factory-startup --python tools/art_pipeline/inspect_building_proxy_metrics.py -- --output docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json
blender -b --factory-startup --python tools/art_pipeline/render_building_proxy_contact_sheet.py -- --output docs/evidence/visual-development/building-proxy-contact-sheet-2026-08-16.png
blender -b --factory-startup --python tools/art_pipeline/render_building_readability_sheet.py -- --output docs/evidence/visual-development/building-readability-sheet-2026-08-16.png
```
