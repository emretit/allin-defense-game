# Defender Chronicles Visual Art Bible

Status: M0 draft, ready for review after runtime screenshots and device profiling
Date: 2026-08-16
Decision source: `docs/VISUAL_DEVELOPMENT_RESEARCH_2026-08-16.md`
Baseline evidence: `docs/evidence/visual-development/baseline-index-2026-08-16.md`

## 1. Core Visual Sentence

Greenwatch Valley is a moonlit, readable isometric fantasy defense battlefield: cold blue stone, steel and cyan magic hold the line against warm red-orange invaders, with gold reserved for command, economy and decisive player actions.

Do:

- Keep the current night low-poly fantasy identity.
- Make every gameplay role readable by silhouette, shape, motion and timing before color.
- Keep the battlefield full-screen while safe area protects only interactive HUD content.
- Treat one polished map as the vertical slice before expanding content.

Avoid:

- Copying any reference game's composition, icons, character proportions or map.
- Solving role identity with only tint or scale.
- Adding texture noise that disappears into shimmer on mobile.
- Making the boss a larger normal Raider.
- Spending visual budget on decorative effects that compete with path, slots, threat and ability telegraphs.

## 2. Moodboard References

| # | Reference | What to use | What not to copy |
|---:|---|---|---|
| 1 | Current iPhone 17 Pro screenshot: `docs/evidence/defender-fullscreen-iphone17pro-2026-08-16.png` | Full-screen battlefield, cold night read, existing HUD placement | Current empty areas, repeated prop rhythm, low state contrast |
| 2 | Current catalog baseline: `docs/evidence/visual-development/catalog-baseline-2026-08-16.json` | Existing role list, scale, catalog structure | Shared role prefabs as final visual solution |
| 3 | Current asset inventory: `docs/evidence/visual-development/asset-inventory-2026-08-16.md` | Existing generated FBX and Blender production base | Treating generated proxy assets as final art |
| 4 | KayKit Medieval Hexagon GitHub | Stylized medieval modular readability and mobile-friendly low-poly density | Hex tile composition as-is |
| 5 | KayKit Medieval Hexagon local processed assets | Blue medieval building material family | One-to-one stock building silhouettes |
| 6 | KayKit Adventurers GitHub | Rigged/animated low-poly character base, accessories, clear weapon language | Four source characters as only four gameplay roles |
| 7 | KayKit Adventurers local sources | Existing CC0 actor base and atlas provenance | Treating stored static proxy FBX files as final runtime rigs |
| 8 | Kenney Fantasy UI Borders | Fantasy border/9-slice language, clean panel corners | Busy UI frames that reduce text contrast |
| 9 | Quaternius Medieval Village MegaKit | Optional modular village/prop candidates, CC0 environment variety | Importing mismatched scale/materials without reauthoring |
| 10 | Unity URP performance guidance | Conservative shadows, texture, additional light and render-feature budget | High-cost real-time shadows as default decoration |
| 11 | Unity URP shadow optimization guidance | Fewer shadow casters, shorter max distance, lower cascade/pass cost | Shadow-casting point lights on Low/Medium profiles |
| 12 | WCAG 2.2 Level AA target and contrast guidance | Minimum target/contrast floor for HUD controls and text | Color-only warnings, tiny touch targets, invisible focus/selection |

Source URLs used for external references:

- KayKit Medieval Hexagon: https://github.com/KayKit-Game-Assets/KayKit-Medieval-Hexagon-Pack-1.0
- KayKit Adventurers: https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0
- Kenney Fantasy UI Borders: https://kenney.nl/assets/fantasy-ui-borders
- Quaternius Medieval Village MegaKit: https://quaternius.com/packs/medievalvillagemegakit.html
- Unity URP performance: https://docs.unity3d.com/6000.1/Documentation/Manual/urp/configure-for-better-performance.html
- Unity URP shadows: https://docs.unity3d.com/6000.2/Documentation/Manual/shadows-optimization.html
- WCAG 2.2: https://www.w3.org/TR/WCAG22/

## 3. Palette

All authored colors should be checked in both lit gameplay and unlit thumbnail contexts. Hex values are art-direction anchors, not exact shader constants.

| Family | Hex | Linear RGB approx | Role |
|---|---|---|---|
| Night base | `#061020` | `0.002, 0.005, 0.014` | Deep background, HUD shadow wells |
| Valley ground | `#06382F` | `0.002, 0.039, 0.030` | Playfield ground, low saturation |
| Road earth | `#8B6A4A` | `0.258, 0.144, 0.068` | Main route and walkable path read |
| Moon edge | `#9BCBFF` | `0.328, 0.599, 1.000` | Rim light, path glints, ice/slow edge |
| Defender blue | `#2E75C9` | `0.027, 0.178, 0.585` | Friendly metal/cloth identity |
| Command gold | `#F2B84B` | `0.888, 0.479, 0.070` | Economy, CTA, important confirmation |
| Enemy red | `#B23A2E` | `0.445, 0.042, 0.027` | Enemy cloth, warning accents |
| Impact orange | `#F06A2B` | `0.871, 0.144, 0.024` | Impact heat, attack sparks |
| Arcane cyan | `#40D7FF` | `0.051, 0.680, 1.000` | Mage, shield, arcane VFX |
| Regen green | `#49E07B` | `0.067, 0.745, 0.198` | Heal/regen state, organic energy |
| Disrupt violet | `#8E5BFF` | `0.270, 0.105, 1.000` | Boss phase, disrupt, special danger |
| Text white | `#F4FAFF` | `0.905, 0.956, 1.000` | Primary HUD labels |

Rules:

- Gold is scarce; use it for currency, rewards and primary progression actions.
- Enemy red/orange is mostly motion and short-lived feedback, not persistent wall paint.
- Green is split by shape: Support heal flows from caster to target; Regenerator pulse radiates from the enemy body.
- Cyan magic must keep geometric shards/runes so it does not become generic glow.
- Information never relies on hue alone; pair with icon, outline, motion, shape or timing.

## 4. Value Hierarchy

| Layer | Target value | Rule |
|---|---|---|
| Background mountains/edge silhouettes | 5-15% | Frame the map without reading as playable space |
| Valley ground | 15-28% | Stay quieter than actors, slots and road |
| Road | 40-58% | Continuous path must survive grayscale and thumbnail tests |
| Build slots | 55-75% rim, 25-35% fill | Empty/selected/upgradeable states change shape and pulse |
| Friendly actors | 55-80% lit face/weapon | Defender units stand above ground and below CTA brightness |
| Enemy actors | 45-75% lit weapon/head | Red team stays readable on dark ground without becoming HUD-like |
| Boss telegraph | 65-85% edge, transparent fill | Danger area is visible before impact and does not hide actor silhouettes |
| HUD primary text | 85-100% | Minimum contrast target: 4.5:1 for normal text |
| HUD panels | 5-18% base, 30-50% frame | Decorative surfaces go full-screen; touch content stays safe-area aware |

Thumbnail tests:

- Actor thumbnails: 160 px and 64 px, color and grayscale.
- Building thumbnails: 220 px and 160 px, color and grayscale.
- HUD buttons: 1x and 0.75x simulated mobile scale.

## 5. Scale And Export Rules

Pending Unity Editor verification, these rules are the authoring standard:

| Asset type | Blender unit | Pivot | Forward | Runtime target |
|---|---|---|---|---|
| Actor | 1 Blender unit = 1 Unity meter | Feet center on ground | +Z forward | Catalog scale should stay near current values unless silhouette requires change |
| Building | 1 Blender unit = 1 Unity meter | Slot center at ground | +Z toward road/target | Click/slot footprint must not change between L1 and L2 |
| Prop | 1 Blender unit = 1 Unity meter | Contact center | Any, normalized on prefab | No collider unless gameplay explicitly needs it |
| VFX mesh | 1 Blender unit = 1 Unity meter | Emission origin | +Z forward for directional effects | Poolable prefab with no gameplay authority |
| UI sprite | Pixel authored at source size | Center unless 9-slice | N/A | Compression/PPU verified in Unity |

Export requirements:

- Keep editable `.blend` sources under `ArtSource/Blender`.
- Keep runtime FBX outputs under `Assets/Art/Models/Generated` or a future role-specific subfolder.
- Do not require manual transform correction after export.
- Store a provenance entry for any non-original source before processed output is accepted.

## 6. Geometry Budgets

These are provisional authoring ceilings until VIS-006/VIS-007 profiling can replace them with device-derived budgets.

| Asset | Initial ceiling | Material slots | Notes |
|---|---:|---:|---|
| Small actor | 1.5k triangles | 2 | Knight, Guard, Raider, Support, Regenerator |
| Heavy actor | 2.5k triangles | 2 | Vanguard, Siege |
| Boss | 4.5k triangles | 3 | Includes phase parts; hide inactive phase pieces |
| Building L1 | 2.5k triangles | 3 | Keep footprint stable |
| Building L2 | 4.0k triangles | 3 | Add mass, not noise |
| Environment prop | 300-900 triangles | 1 | Prefer atlas and instancing-friendly material |
| Hero/critical VFX mesh | 300-800 triangles | 1 | Transparency budget matters more than triangles |

## 7. Materials And Textures

| Topic | Standard |
|---|---|
| Shader | URP Lit or Simple Lit unless a measured reason exists |
| Actor slots | Target 2 or fewer materials per actor |
| Building slots | Target 3 or fewer materials per building |
| Atlas | Prefer shared gradient/flat atlas for low-poly surfaces |
| Emissive | Small, role-critical accents only; readable with bloom disabled |
| Texture noise | Avoid high-frequency texture detail in gameplay camera |
| Mobile import | Use platform overrides after visual review; large HUD PNGs are optimization candidates |
| Additional lights | Low/Medium profiles avoid shadow-casting point lights |
| Shadows | Reduce shadow distance/casters before lowering gameplay clarity |

## 8. Silhouette Rules

| Role | Required read at 160 px | Forbidden shortcut |
|---|---|---|
| Knight | Wide shield, grounded stance, short sword | Only blue/gold tint |
| Guard | Longer spear line, thinner body, smaller shield | Knight mesh scaled down |
| Raider | Forward lean, exposed agile limbs, light weapon | Boss mesh scaled down |
| Vanguard | Thick armor, wide body, heavy weapon anticipation | Raider with bigger weapon only |
| Regenerator | Asymmetric organic core/totem | Support mage with green material |
| Support | Staff/lantern, upright support posture | Regenerator with cyan material |
| Siege | Low wide mechanical or heavy profile | Humanoid with larger scale |
| Boss | Three phase parts, unique negative space and emissive core | Any normal enemy enlarged |

Siluet review board:

- One row per role: color render, grayscale render, 160 px crop, 64 px crop.
- One column per camera: gameplay camera, icon camera.
- Review passes only if a viewer can label each role without reading color labels.

## 9. Animation Contract

| Clip/state | Loop | Gameplay authority | Required event window |
|---|---|---|---|
| Idle | Yes | Visual only | None |
| Move | Yes | Code controls position | Foot rhythm may trigger SFX only |
| Attack | No | Code controls damage | Impact event aligns with already-decided gameplay hit |
| Hit | No | Code controls health/state | Short interrupt, never traps actor |
| Defeat | No | Code controls removal/pooling | Visual death can finish after gameplay state is resolved |
| Ability | No | Code controls ability result | VFX/SFX release event only |
| Phase | No | Boss phase system controls state | Telegraph and phase swap timing |
| BossAttack | No | Boss system controls danger/impact | Telegraph, release, impact windows |

Rules:

- Root motion must not move gameplay position.
- Animation events trigger presentation only, never damage/economy decisions.
- Rigged runtime actor and static proxy/icon exports are separate products.
- Any root-transform fallback must be marked in the catalog validation report.

## 10. VFX Families

| Family | Shape | Color | Duration target | Pool rule |
|---|---|---|---|---|
| Physical hit | Directional wedge/spark | White, steel, small orange | 0.12-0.25s | Common physical pool |
| Arrow | Thin trail + pin impact | Wood/gold | Projectile dependent | Projectile pool |
| Arcane | Rune/shard geometry | Cyan/violet | Cast, flight, impact split | Mage pool |
| Shield bash | Cone + broken ring | Blue/gold | 0.25-0.45s | Ability priority |
| Heal | Flowing particles from caster to target | Cyan/green | 0.6-1.2s | Limited concurrent beams |
| Regen | Body-centered pulse/spores | Green | 0.5-1.0s repeating | Enemy status pool |
| Slow | Foot-level broken ice/rune | Moon blue | Status duration | Low intensity persistent |
| Disrupt | Broken violet rune/glitch | Violet | 0.25-0.7s | High readability, short |
| Build/upgrade/sell | Dust + rising/falling shape | Blue-gold or neutral | 0.4-0.8s | Economy pool |
| Spawn/death | Faction-specific entrance/vanish | Team palette | 0.2-0.6s | Hides pooling |
| Boss phase | Area telegraph + core crack | Red/orange/violet | Gameplay telegraph length | Highest gameplay priority |

## 11. HUD Component States

| State | Visual rule |
|---|---|
| Normal | Dark fantasy panel, readable icon, no pulse |
| Pressed | Short inset/darken motion, icon remains readable |
| Selected | Shape/rim change plus world-slot echo |
| Disabled | Lower contrast and locked shape; not only opacity |
| Cooldown | Radial or directional fill with remaining-time rhythm |
| Unaffordable | Cost line, lock/rim pattern and short error feedback |
| Ready ability | Subtle command-gold pulse, capped frequency |
| Threat/boss | Red/orange shape and label hierarchy, not permanent screen tint |

Rules:

- Background and decorative rails may extend to physical screen edges.
- Interactive controls and text remain inside safe area.
- Do not shrink the battlefield just to make HUD panels comfortable.
- 16:9, 20:9 and tablet layouts must keep route, eight slots and keep visible.

## 12. Accessibility Checklist

| Area | Requirement |
|---|---|
| Text contrast | Normal HUD text targets 4.5:1; large/icon-text targets 3:1 minimum. |
| Touch target | Native mobile target goal is 44x44 pt or larger; absolute minimum follows WCAG 2.2 24x24 CSS px equivalent with spacing exception documented. |
| Color dependence | Every status uses shape, icon, motion, outline or timing in addition to hue. |
| Cooldowns | Cooldown progress is visible without relying on a color ramp. |
| Warnings | Threat and boss telegraphs show area and timing by shape/animation, not red tint alone. |
| Text scaling | Button labels may shrink within approved min sizes; text must not overlap icons or neighboring controls. |
| Motion | Repeating pulses are subtle and must not obscure critical state; camera shake should be reducible/disableable. |
| Focus/selection | Selected world slot and selected HUD button are visibly paired. |

## 13. M1 Style Proof Gate

The project can move from art bible into broad production only when Knight, Raider and Archer L1 prove the style together:

- Each has concept/silhouette, model, material, icon/marker and gameplay-camera evidence.
- The three assets are shown in one review board with identical light/camera.
- Thumbnail and grayscale tests pass.
- Catalog integration does not break the current route, eight build slots, HUD, safe area or camera behavior.
- Profiling captures or documented Editor limitations are attached to the evidence index.
