# Defender Chronicles Actor Runtime Pipeline

Status: M0/M1 implementation contract
Date: 2026-08-16
Related TODO: VIS-201, VIS-202, VIS-203, VIS-205, VIS-207, VIS-208

## 1. Decision

Actor production now has two separate outputs:

1. Runtime actor: keeps the rig, animation clips, sockets and role-specific accessories needed during gameplay.
2. Proxy/icon actor: lightweight static or simplified mesh used for icon renders, LOD proof, silhouette sheets and non-animated review boards.

The current catalog is no longer allowed to point multiple roles at the same stock character prefab. It now starts from eight role-specific generated proxy FBX files under `Assets/Art/Models/Generated`. These are only the baseline proxies; final runtime actors still need rigged model, material, animation, VFX, icon, shadow and catalog validation.

## 2. Current State

| Role | Catalog runtime baseline | Final runtime status |
|---|---|---|
| Knight | `Assets/Art/Models/Generated/Knight.fbx` | Needs heavy shield runtime rig, sword attack, shield bash, icon and VFX |
| Guard | `Assets/Art/Models/Generated/Guard.fbx` | Needs spear runtime rig, block/parry animation and role icon |
| Raider | `Assets/Art/Models/Generated/Raider.fbx` | Needs agile enemy runtime rig, fast attack and slash VFX |
| Vanguard | `Assets/Art/Models/Generated/Vanguard.fbx` | Needs heavy weapon profile, anticipation/recovery animation |
| Regenerator | `Assets/Art/Models/Generated/Regenerator.fbx` | Needs organic/asymmetric runtime parts and regen pulse |
| Support | `Assets/Art/Models/Generated/Support.fbx` | Needs staff/lantern support silhouette and heal cast |
| Siege | `Assets/Art/Models/Generated/Siege.fbx` | Needs mechanical attack setup/recoil and projectile/impact VFX |
| Boss | `Assets/Art/Models/Generated/Boss.fbx` | Needs unique three-phase body, phase pieces and boss telegraph VFX |

Evidence:

- `docs/evidence/visual-development/catalog-baseline-2026-08-16.json`
- `docs/evidence/visual-development/actor-proxy-contact-sheet-2026-08-16.png`
- `docs/evidence/visual-development/actor-proxy-metrics-2026-08-16.json`
- `docs/evidence/visual-development/actor-rig-source-inspection-2026-08-16.json`
- `docs/ACTOR_SOCKET_STANDARD_2026-08-16.md`
- `docs/ACTOR_ANIMATION_CONTRACT_2026-08-16.md`
- `docs/ACTOR_MARKER_SHADOW_STANDARD_2026-08-16.md`
- `docs/ACTOR_MATERIAL_IMPORT_STANDARD_2026-08-16.md`
- `docs/ACTOR_ICON_RENDER_STANDARD_2026-08-16.md`
- `docs/CATALOG_VISUAL_VALIDATION_2026-08-16.md`

Important source finding: the local `Assets/ThirdParty/KayKit/Adventurers/Models/*Licensed.fbx` files are static proxy meshes with no armature and no animation actions. The upstream KayKit pack is still a valid CC0 rig/animation candidate, but the full rigged sources are not currently stored in this repository.

## 3. Source Folders

| Purpose | Path | Rule |
|---|---|---|
| Editable original/procedural sources | `ArtSource/Blender` | Keep `.blend` files editable and role named |
| External CC0 source FBX/textures | `Assets/ThirdParty/...` | Never modify in place; record provenance |
| Runtime FBX outputs | `Assets/Art/Models/Generated` initially; may split to `Assets/Art/Models/Actors` later | Must be referenced by catalog or documented as candidate |
| Generated presentation materials | `Assets/Art/GeneratedPresentation` | Shared palette and licensed atlas materials |
| Evidence renders | `docs/evidence/visual-development` | Every acceptance pass links here |
| Pipeline scripts | `tools/art_pipeline` | Scripts must be rerunnable from repo root |

## 4. Output Types

| Output | Rig | Animations | Mesh detail | Used by | Acceptance |
|---|---|---|---|---|---|
| Runtime actor FBX/prefab | Preserved | Required role clips | Mobile gameplay quality | Catalog actor entry | Moves/attacks without root-motion authority |
| Proxy actor FBX | Optional or stripped | None | Simplified static silhouette | Review boards, icon/LOD | Role is readable in grayscale thumbnail |
| Icon render | None | None | Render output | HUD/selection UI | 64 px read passes |
| Contact sheet render | None | None | Render output | Evidence | Same camera/light; labels or order documented |

## 5. Rig And Animation Rules

- Shared clip, state and trigger names are defined in `Assets/Scripts/Presentation/PresentationActorAnimationContract.cs`.
- The readable animation contract lives in `docs/ACTOR_ANIMATION_CONTRACT_2026-08-16.md`.
- Runtime actors should keep the source rig when possible.
- Gameplay position remains code-driven; root motion is not authority.
- Animation events may trigger only presentation signals: SFX, VFX, camera feedback.
- Damage, economy, health and phase decisions stay in gameplay code.
- Every runtime actor must support the shared gameplay calls: idle, move, attack, hit, defeat and role special.
- Boss runtime actor must support phase state changes without swapping to a normal enemy mesh.

## 6. Socket Contract

Detailed scale, budget and role-specific socket rules live in `docs/ACTOR_SOCKET_STANDARD_2026-08-16.md`. This section is the compact runtime contract used by catalog validation.

| Socket | Required for | Notes |
|---|---|---|
| `Socket_Root` | Stable visual root, pooled debug/VFX parent | Prefer at actor origin |
| `Socket_Hand_R` | Sword, spear, staff, heavy weapon, attack VFX | Directional attack release |
| `Socket_Hand_L` | Shield, offhand, support prop | Shield bash or block/parry origin |
| `Socket_Head` | Portrait/icon aim, hit flash, status markers | Not a gameplay target |
| `Socket_Chest` | Impact center, heal/regen source, boss core | Use for generic status |
| `Socket_Back` | Cape, totem, banner, phase parts | Optional per role |
| `Socket_Ground` | Selection marker, blob shadow, foot VFX | Must align with world ground |
| `Socket_Weapon_Tip` | Slash trail, spear tip, staff release | Required for weapon actors |
| `Socket_Cast` | Heal, regen, boss special | Required for Support/Regenerator/Boss |
| `Socket_Core` | Boss, Regenerator, Siege energy/mechanical core | Required where role identity depends on a core |
| `Socket_Muzzle` | Siege projectile release | Required for Siege-style ranged body |

Generated proxy actors may omit these sockets, but final runtime actor validation must fail if required sockets are missing.

## 7. Catalog Rules

The builder in `Assets/Editor/DefenderPresentationAssetBuilder.cs` must remain the source of truth for generated catalog links.

Current accepted baseline:

- `Actor(PresentationActorKind.Knight, "Knight", 0.74f)`
- `Actor(PresentationActorKind.Guard, "Guard", 0.66f)`
- `Actor(PresentationActorKind.Raider, "Raider", 0.64f)`
- `Actor(PresentationActorKind.Vanguard, "Vanguard", 0.68f)`
- `Actor(PresentationActorKind.Regenerator, "Regenerator", 0.60f)`
- `Actor(PresentationActorKind.Support, "Support", 0.62f)`
- `Actor(PresentationActorKind.Siege, "Siege", 0.92f)`
- `Actor(PresentationActorKind.Boss, "Boss", 1.02f)`

Future runtime-prefab work may replace an FBX string with a prefab path or typed entry, but it must preserve one unique runtime visual per actor role.

## 8. Validation Gates

| Gate | How to prove |
|---|---|
| Unique actor visual entry | Catalog JSON has 8 actor roles and 8 unique actor prefab GUIDs |
| Proxy visual proof | `actor-proxy-contact-sheet-2026-08-16.png` is regenerated from the current FBX set |
| Proxy scale/geometry proof | `actor-proxy-metrics-2026-08-16.json` lists bounds, triangle/material counts and socket gaps |
| Source rig availability | `actor-rig-source-inspection-2026-08-16.json` or a successor report lists armatures and actions for runtime sources |
| Runtime rig proof | Unity import report lists required rig/avatar and no broken bones |
| Socket proof | Catalog validation reports all required sockets per role |
| Animation proof | EditMode/PlayMode tests confirm calls map to states/clips |
| Gameplay regression proof | Build slots, pathing, camera and safe-area tests pass |
| Performance proof | iPhone 13-class profiler capture passes target budgets |

Unity CLI is not available in the current shell, so Unity runtime rig/socket/PlayMode gates remain open until Unity Editor or CLI access is restored. In addition, full rigged actor sources are not currently present in the repository; VIS-202 must not be closed until that source gap is resolved.

## 9. Next M1 Work

The M1 style proof should use this order:

1. Knight: concept/silhouette, runtime model/accessory plan, material plan, proxy comparison.
2. Raider: agile silhouette, enemy material, slash/spawn/death VFX plan.
3. Archer L1: building progression anchor, tower mass and readable archer props.
4. Combined review board: same camera/light, color and grayscale.
5. Unity integration and runtime screenshot once Editor access is available.
