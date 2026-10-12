# ALLIN: Defense — Actor Material And Import Standard

Status: M1 actor material/import standard
Date: 2026-08-16
Related TODO: VIS-106, VIS-206, VIS-212, VIS-222, VIS-232, VIS-242, VIS-252, VIS-262, VIS-272, VIS-282, VIS-291

## 1. Decision

Final runtime actors should use role-readable low-poly materials while staying mobile-safe. The current generated proxy FBX files may carry more source material slots, but final runtime prefabs should consolidate materials through shared palette/atlas rules.

Implementation currently touches:

- `Assets/Editor/DefenderPresentationAssetBuilder.cs`
- `Assets/Scripts/Presentation/DefenderPresentationCatalog.cs`
- `Assets/Scripts/Presentation/StylizedPresentationFactory.cs`

## 2. Current Import Baseline

Existing editor import configuration:

| Asset type | Current rule |
|---|---|
| KayKit texture atlases | Default texture, sRGB on, mipmaps on |
| KayKit Medieval Hexagon atlas | Max 1024, CompressedHQ |
| KayKit Adventurers actor atlas | Max 512, CompressedHQ |
| HUD icons | Sprite, no mipmaps, max 256, compressed |
| HUD panel | Sprite 9-slice, no mipmaps, max 1024, compressed |
| Kenney UI frame | Sprite 9-slice, no mipmaps, max 64, uncompressed for crisp borders |

Current actor proxy metric reports show:

- 2026-08-16 baseline actor proxies were 4–5 materials per actor.
- 2026-08-17 regenerated Knight, Guard and Raider proxy FBX files meet the normal humanoid target at 2 materials each.
- The remaining actor roles still need final material consolidation before runtime acceptance.

## 3. Final Runtime Actor Targets

| Actor group | Target material slots | Texture target |
|---|---:|---|
| Normal humanoid | ≤2 | Shared palette/atlas, max 512 |
| Siege/mechanical | ≤2 | Shared metal/wood/enemy atlas, max 512 |
| Boss | ≤3 | Shared dark/core atlas, max 1024 only if phase pieces justify it |
| Icon/proxy output | Temporary source materials allowed | Not a final runtime budget proof |

Exceptions must be recorded in the relevant TODO item and justified by a gameplay-camera screenshot, grayscale/thumbnail test and profiler evidence.

## 4. Role Material Language

| Role | Primary read | Secondary read |
|---|---|---|
| Knight | Defender blue armor/cloth | Gold shield trim and clean steel |
| Guard | Lighter defender steel | Long spear line and smaller shield |
| Raider | Dark leather/red enemy accent | Thin fast silhouette |
| Vanguard | Dark steel/burgundy mass | Heavy weapon face/read |
| Regenerator | Deep organic green | Core/totem emissive shape |
| Support | Neutral cloth/cyan-green magic | Staff/lantern source |
| Siege | Dark metal/wood/red enemy accent | Muzzle/core pieces |
| Boss | Black steel/warm core | Phase emissive and detachable silhouette parts |

Role readability cannot depend on color alone. Shape, material value and VFX timing must also carry the role.

## 5. Acceptance Gates

| Gate | Required proof |
|---|---|
| Import settings applied | Unity AssetImporter report or Editor screenshot |
| Material slot budget | Metrics report for final runtime prefabs or explicitly accepted proxy candidates |
| Atlas source recorded | Provenance entry with URL/license/SHA/path |
| Gameplay readability | Color and grayscale contact sheet |
| Mobile cost | Frame Debugger/profiler capture |

Open status:

- Unity AssetImporter validation and Frame Debugger proof remain pending.
- `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json` records Knight at `DC_Defender` + `DC_Fire` and Raider at `DC_Enemy` + `DC_Dark`, both with 2 material slots and required sockets present.
- Current proxy metrics are still not full runtime acceptance for rig, animation, prefab hierarchy or gameplay-camera capture.
