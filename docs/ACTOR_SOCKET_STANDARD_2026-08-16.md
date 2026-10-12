# ALLIN: Defense — Actor Scale, Budget And Socket Standard

Status: M1 actor production standard
Date: 2026-08-16
Related TODO: VIS-104, VIS-105, VIS-108, VIS-203, VIS-205, VIS-207, VIS-208, VIS-291

## 1. Purpose

This document defines the minimum runtime shape contract for all eight actor roles. It separates the current static proxy baseline from the final runtime actor requirement.

The current generated FBX files are acceptable as visual proxies for review boards and contact sheets, but they are not final gameplay-ready runtime actors. Final runtime actors must include stable scale, pivot, rig, animation clips, sockets, material budgets, icon framing and catalog validation.

Evidence:

- `docs/evidence/visual-development/actor-proxy-metrics-2026-08-16.json`
- `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json`
- `docs/evidence/visual-development/actor-proxy-contact-sheet-2026-08-16.png`
- `docs/evidence/visual-development/actor-rig-source-inspection-2026-08-16.json`
- `docs/ACTOR_RUNTIME_PIPELINE_2026-08-16.md`

## 2. Coordinate And Export Rules

| Rule | Standard |
|---|---|
| Ground pivot | Actor origin at center of contact point on world ground |
| Forward axis | Unity forward is +Z after import |
| Up axis | Unity up is +Y; Blender Z-up exports must be converted consistently by FBX preset |
| Gameplay movement | Code controls world position; root motion is not gameplay authority |
| Visual height range | Normal humanoid actors target 1.35–2.10 proxy-height band before catalog scale; Boss may exceed it |
| Scale edits | Prefer mesh/source scale correction before catalog multiplier changes |
| Collider/path rule | New visuals must not change pathing, lane, target point or eight build slots |
| Unity import check | Final acceptance requires Unity import validation; current shell has no Unity CLI/Editor access |

## 3. Current Proxy Metrics

The original 2026-08-16 generated proxies were intentionally small and mobile-friendly, but were not socket-complete. The 2026-08-17 regenerated actor proxies now include role-specific `Socket_*` empties in both Blender source and generated FBX exports. They remain animation/runtime-prefab candidates, not final accepted runtime actors.

| Role | Triangles | Materials | Bounds size XYZ | Runtime sockets |
|---|---:|---:|---|---|
| Knight | 556 | 2 | 1.278 × 0.960 × 2.088 | 7/7 required present |
| Guard | 492 | 2 | 0.997 × 0.749 × 1.463 | 7/7 required present |
| Raider | 452 | 2 | 0.729 × 0.691 × 1.354 | 6/6 required present |
| Vanguard | 492 | 5 | 1.214 × 0.912 × 1.783 | 7/7 required present |
| Regenerator | 452 | 5 | 1.093 × 1.037 × 2.029 | 7/7 required present |
| Support | 456 | 5 | 0.921 × 0.806 × 1.575 | 7/7 required present |
| Siege | 384 | 4 | 1.700 × 2.745 × 1.568 | 5/5 required present |
| Boss | 532 | 5 | 1.981 × 1.488 × 2.920 | 10/10 required present |

Current proxy summary:

- Expected actors found: 8/8.
- Unique actor meta GUIDs: 8/8.
- Total current proxy triangles: 3,816.
- Highest current proxy triangle count: Knight, 556.
- Current proxy material slots exceed final actor target; final runtime actors should consolidate toward the art bible material budget.
- 2026-08-17 proxy metrics report `roles_missing_runtime_sockets: []`.

## 4. Runtime Socket Contract

Every final runtime actor prefab must expose stable child transforms with these exact names. Names are case-sensitive.

| Socket | Required | Placement | Primary use |
|---|---|---|---|
| `Socket_Root` | All runtime actors | Actor origin or root visual parent | Stable parent for scale, pooled VFX and debug tools |
| `Socket_Ground` | All runtime actors | Center of ground contact | Selection marker, blob shadow, foot dust |
| `Socket_Chest` | All runtime actors | Readable torso/impact center | Hit flash, generic impact, heal/regen center, boss core fallback |
| `Socket_Head` | All runtime actors except Siege if non-humanoid | Above head/readable top point | Status marker, portrait aim, overhead effects |
| `Socket_Hand_R` | Humanoid/weapon actors | Right hand or main weapon grip | Sword, spear, staff, heavy weapon, attack release |
| `Socket_Hand_L` | Humanoid/offhand actors | Left hand or shield grip | Shield, block/parry, offhand VFX |
| `Socket_Back` | Role-dependent | Upper back or rear equipment mount | Banner, cape, totem, phase parts |
| `Socket_Weapon_Tip` | Weapon/projectile actors | Tip of blade/spear/cannon/staff | Slash trail, projectile spawn, impact timing |
| `Socket_Cast` | Support, Mage-like, Regenerator, Boss | Spell source point | Heal, regen pulse, boss telegraph source |
| `Socket_Core` | Regenerator, Boss, Siege optional | Energy core/mechanical core | Phase/core emissive, regeneration or disrupt hit |
| `Socket_Muzzle` | Siege, Archer-like actors if used | Projectile exit | Projectile origin and recoil VFX |

Generated proxy actors may omit these sockets. Final runtime actor validation must fail if required sockets are missing for a role.

## 5. Role-Specific Required Sockets

| Role | Required role sockets beyond common set |
|---|---|
| Knight | `Socket_Hand_R`, `Socket_Hand_L`, `Socket_Weapon_Tip` |
| Guard | `Socket_Hand_R`, `Socket_Hand_L`, `Socket_Weapon_Tip` |
| Raider | `Socket_Hand_R`, `Socket_Weapon_Tip` |
| Vanguard | `Socket_Hand_R`, `Socket_Hand_L`, `Socket_Weapon_Tip` |
| Regenerator | `Socket_Back`, `Socket_Cast`, `Socket_Core` |
| Support | `Socket_Hand_R`, `Socket_Cast`, `Socket_Weapon_Tip` |
| Siege | `Socket_Ground`, `Socket_Chest`, `Socket_Muzzle`, `Socket_Core` |
| Boss | `Socket_Chest`, `Socket_Head`, `Socket_Hand_R`, `Socket_Hand_L`, `Socket_Weapon_Tip`, `Socket_Cast`, `Socket_Core`, `Socket_Back` |

Common set means `Socket_Root`, `Socket_Ground` and `Socket_Chest` for all actors, plus `Socket_Head` for humanoid actors.

## 6. Animation And VFX Timing Contract

Animation clips may move the mesh visually, but they must not become gameplay authority.

| Event | Timing rule | Socket |
|---|---|---|
| `AttackAnticipation` | Wind-up begins; no damage yet | Role weapon/cast socket |
| `AttackRelease` | VFX/projectile/impact request may fire | `Socket_Weapon_Tip`, `Socket_Muzzle` or `Socket_Cast` |
| `HitReact` | Visual-only reaction; gameplay health already decided | `Socket_Chest` |
| `DefeatStart` | Death VFX and audio start | `Socket_Chest` and `Socket_Ground` |
| `SpecialRelease` | Shield bash/heal/regen/boss special | Role-specific socket |

Gameplay scripts should request named presentation events; animation events may only trigger presentation side effects such as VFX, SFX and camera feedback.

## 7. Budget Targets

These are target budgets for final runtime actors, not a claim that every current proxy already complies.

| Actor group | Target triangles | Target materials | Notes |
|---|---:|---:|---|
| Normal humanoid | 800–1,800 | ≤2 | Knight/Guard/Raider/Support/Vanguard/Regenerator |
| Siege/mechanical | 1,200–2,500 | ≤2 | Moving parts may be separate meshes but should share atlas/materials |
| Boss | 2,000–4,000 | ≤3 | Phase pieces allowed; document any exception |
| Icon/proxy mesh | ≤1,000 | Any temporary source material count acceptable | Must not be mistaken for final runtime budget |

Final budget acceptance still needs Unity/device profiling. The current report is a baseline, not final approval.

## 8. Validation Gates

| Gate | Current status |
|---|---|
| Eight unique actor proxy files | Passed |
| Proxy bounds and triangle counts captured | Passed |
| Runtime rig source availability | Failed for local KayKit selected FBXs; full rigged sources must be reacquired or recreated |
| Required runtime sockets | Passed for generated proxy FBX files on 2026-08-17; final runtime prefab validation still pending |
| Unity import/prefab transform validation | Pending; Unity CLI/Editor unavailable in this shell |
| Animator/controller compatibility | Pending |
| Icon/contact sheet acceptance | Proxy contact sheet exists; final icon render scene pending |

## 9. Next Implementation Step

VIS-203 should proceed by adding a Unity-side catalog/prefab validation rule for required socket names and checking the imported prefab hierarchy. New Blender/runtime actor exports must keep this socket naming standard and the proxy metrics report should be regenerated after each actor replacement.
