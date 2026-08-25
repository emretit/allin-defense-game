# Defender Chronicles — Building Material And Import Standard

Status: VIS-302 material/import standard
Date: 2026-08-16
Related TODO: VIS-302, VIS-312, VIS-316, VIS-322, VIS-325, VIS-332, VIS-335, VIS-346

## 1. Decision

Final runtime buildings should preserve the atlas-friendly simplicity of the current KayKit catalog assets while gaining stronger type and level readability.

The current baseline shows two useful but incomplete sources:

- Catalog-current KayKit building FBX files: one material, higher triangle count.
- Generated building candidates: low triangle count, 3 generated material slots per building, and footprint deltas that still need Unity click/slot review. All six generated candidates now have socket-complete outputs and meet the ≤3 generated-candidate material target.

Evidence:

- `docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json`
- `docs/BUILDING_PIPELINE_STANDARD_2026-08-16.md`

## 2. Import Targets

| Asset type | Target |
|---|---|
| Shared building atlas | sRGB on, mipmaps on, compressed, max 1024 |
| Small building decals/icons | Sprite or texture by use case, no unnecessary mipmaps |
| Runtime material slots | ≤3 per final building |
| Emissive pieces | Prefer one shared material family; do not require bloom to read |
| L1/L2 variants | Reuse atlas/material families; distinguish level through mass, height, props and silhouette |

## 3. Role Material Language

| Building | Main language | Accent |
|---|---|---|
| Archer | Blue-team wood/stone, bow/target props | Warm arrow/torch hints |
| Barracks | Stone/wood reinforced mass, shield/spear racks | Blue banner and metal accents |
| Mage | Stone/dark base, arcane crystal/rune pieces | Cyan/magic emissive shapes |

## 4. Validation Gates

| Gate | Required proof |
|---|---|
| Material slot target | Catalog/Prefab metric report or Unity validation |
| Atlas use | Importer/material evidence |
| Gameplay readability | Color and grayscale building review sheet |
| Upgrade readability | L1/L2 thumbnail comparison |
| Mobile cost | Frame Debugger/profiler evidence |

Open status:

- Unity import preset verification is pending because Unity CLI/Editor is not available in this shell.
- Generated candidates are not final accepted runtime prefabs until footprint and material slot validation pass.
