# Defender Chronicles — Building Pipeline, Footprint And Socket Standard

Status: VIS-301 implementation standard
Date: 2026-08-16
Related TODO: VIS-301, VIS-302, VIS-303, VIS-304, VIS-310–VIS-346

## 1. Decision

Building production now follows the same proxy/runtime split as actors:

1. Catalog-current building: what the playable catalog references today.
2. Generated candidate building: lower-poly or processed FBX candidates under `Assets/Art/Models/Generated`.
3. Final runtime building prefab: slot-safe, socket-complete, material-budgeted production output.

The current catalog still references KayKit Medieval Hexagon licensed FBX files for the six outpost visuals. Generated candidate FBX files also exist, but they are not currently the catalog source of truth.

Evidence:

- `docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json`
- `docs/evidence/visual-development/asset-inventory-2026-08-16.md`
- `Assets/Editor/DefenderPresentationAssetBuilder.cs`
- `Assets/Resources/Presentation/DefenderPresentationCatalog.asset`

## 2. Current Catalog vs Generated Candidate Baseline

| Building | Catalog triangles | Generated triangles | Catalog materials | Generated materials | Footprint delta X/Z |
|---|---:|---:|---:|---:|---|
| Archer L1 | 2,138 | 580 | 1 | 3 | +0.230 / -0.044 |
| Archer L2 | 3,819 | 792 | 1 | 3 | -0.101 / +0.511 |
| Barracks L1 | 1,538 | 332 | 1 | 3 | +0.347 / +0.375 |
| Barracks L2 | 4,007 | 636 | 1 | 3 | +0.257 / +0.577 |
| Mage L1 | 2,418 | 364 | 1 | 3 | +0.076 / -0.367 |
| Mage L2 | 1,601 | 384 | 1 | 3 | +0.216 / +0.255 |

Interpretation:

- Catalog-current buildings are atlas-friendly but not visually differentiated enough for the target quality bar.
- Generated candidates are mobile-light on geometry and now all meet the generated-candidate ≤3 material target.
- All six generated candidates now include runtime socket empties; catalog-current KayKit FBX files still do not.
- Some generated candidates change the scaled footprint enough that they must not replace catalog visuals blindly.

## 3. Footprint And Pivot Rules

| Rule | Standard |
|---|---|
| Root pivot | Slot center on world ground |
| Gameplay footprint | Must stay inside the current outpost slot click/selection footprint |
| L1/L2 upgrade | Visual mass may grow upward and inward/outward cosmetically, but slot center and click collider do not move |
| Projectile range | Visual height and projectile socket do not change gameplay range |
| Sell/upgrade | Replacing the visual must preserve the existing `GreyboxOutpost` root and slot occupant relationship |
| Foundation | Every final building has a readable base/foundation that hides minor terrain mismatch |
| Safe replacement | A generated candidate cannot replace catalog-current output until Unity scene click/range regression passes |

## 4. Runtime Building Socket Contract

Every final runtime building prefab must expose stable child transforms with these exact names.

| Socket | Required for | Placement |
|---|---|---|
| `Socket_Root` | Visual root and debug tools | Building origin |
| `Socket_Ground` | Build/sell dust, base aura | Slot center on ground |
| `Socket_Target` | Aim reference and look-at target | Main visible body or turret center |
| `Socket_Projectile` | Arrow/magic/projectile release | Weapon tip, crystal, muzzle or roof archer point |
| `Socket_VFX` | Build/upgrade idle VFX | Visually central, not hidden by roof |
| `Socket_Banner` | Team/banner attachment | Readable side or front |
| `Socket_Upgrade` | Upgrade burst and level marker | Upper mass, visible from gameplay camera |
| `Socket_Spawn` | Barracks soldier spawn | Ground-side exit point, Barracks only |
| `Socket_Crystal` | Mage arcane source | Crystal/altar top, Mage only |
| `Socket_Muzzle` | Archer/Siege-like projectile source | Archer only if distinct from `Socket_Projectile` |

Proxy FBX files may omit these sockets. Final runtime prefab validation must fail if required sockets are missing.

## 5. Role-Specific Building Identity

| Building | L1 target | L2 target |
|---|---|---|
| Archer | Thin wooden watchtower with bow/target language | Taller covered archer post with stronger parapet, arrows and banner |
| Barracks | Low guard post with shield/spear rack | Fortified barracks mass with reinforced base, roof/tent and larger weapon rack |
| Mage | Crystal shrine/tower with small rune base | High arcane sanctuary with larger crystal, vertical glow and rune/banners |

Level changes must be readable through height, mass, silhouette and props; not only scale or color.

## 6. Material And Geometry Targets

| Building group | Target triangles | Target materials |
|---|---:|---:|
| L1 outpost | 700–1,800 | ≤3 |
| L2 outpost | 1,200–2,800 | ≤3 |
| Slot/foundation props | 200–900 | ≤2 |
| VFX-only helper meshes | 50–300 | 1 |

Current catalog and generated candidate metrics are baseline evidence, not final acceptance.

## 7. Validation Gates

| Gate | Current status |
|---|---|
| Six catalog-current building visuals exist | Passed |
| Six generated candidate building FBX files exist | Passed |
| Pivot/bounds/triangle/material baseline captured | Passed |
| Runtime building sockets | Pending; current proxies are not socket-complete |
| Unity click/slot regression | Pending; Unity CLI/Editor unavailable |
| Upgrade replacement screenshot | Pending |
| Material budget after final prefab consolidation | Pending |

## 8. Next Implementation Step

VIS-302 should define the shared building atlas/material import preset, then VIS-303 should add Unity-side socket validation for final runtime building prefabs. Archer L1/L2 should be the first style proof because Archer is part of the M1 review board.
