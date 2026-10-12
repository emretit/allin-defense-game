# ALLIN: Defense — Actor Animation Contract

Status: M1 actor animation standard
Date: 2026-08-16
Related TODO: VIS-108, VIS-202, VIS-205, VIS-501, VIS-502

## 1. Decision

Actor animation names are now centralized in `Assets/Scripts/Presentation/PresentationActorAnimationContract.cs`. The catalog validator, presentation actor view and editor asset builder must use this contract instead of independent string lists.

This keeps the current root-transform fallback animation working while making room for role-specific rigged animation overrides.

## 2. Current Shared Runtime Parameters

| Type | Name | Used for |
|---|---|---|
| Bool | `Moving` | Idle/Move blend decision from presentation root movement |
| Trigger | `Attack` | Hero, unit or outpost attack presentation |
| Trigger | `Hit` | Enemy hit or disabled/disrupt fallback |
| Trigger | `Defeat` | Defeat echo and actor death fallback |
| Trigger | `Ability` | Hero ability or role special fallback |
| Trigger | `Victory` | Hero victory pose |
| Trigger | `Phase` | Boss phase presentation |
| Trigger | `BossAttack` | Boss special attack presentation |

Clip/state names use the same values:

- `Idle`
- `Move`
- `Attack`
- `Hit`
- `Defeat`
- `Ability`
- `Victory`
- `Phase`
- `BossAttack`

## 3. Current Gameplay Cue Mapping

| Presentation cue | Current actor call | Target final role behavior |
|---|---|---|
| `EnemyHit` | `Hit` | Short hit reaction or impact flash; no gameplay authority |
| `HeroAttacked` | `Attack` | Knight sword/weapon attack release |
| `OutpostAttacked` | `Attack` | Tower/outpost attack feedback; actor fallback remains generic |
| `OutpostDisrupted` | `Hit` | Distinct disrupt/stagger state in final polish |
| `HeroAbility` | `Ability` | Shield bash or hero special |
| `BossPhase` | `Phase` | Boss phase transition and piece/emissive update |
| `BossAttack` | `BossAttack` | Boss telegraph, release and recovery |
| `Victory` | `Victory` | Hero victory pose |
| `EnemyDefeated` / `UnitDefeated` | `Defeat` echo | Role-specific defeat clip plus pooled death VFX |

## 3.1 Runtime VFX Mapping Checkpoint

2026-08-17 direct Unity checkpoint: `Builds/unity-actor-vfx-visual-checkpoint.log`

| Role/cue | Runtime VFX family |
|---|---|
| Knight `HeroAttacked` | Fire material diamond projectile for sword/weapon release |
| Knight `HeroAbility` | Defender material blue ring shockwave for shield bash radius |
| Raider `UnitSpawned` | Dark material red ring spawn pulse |
| Raider `EnemyHit` | Enemy material diamond projectile for fast slash/readable impact |
| Raider `EnemyDefeated` | Enemy material red ring death burst |

## 3.2 Runtime Animation Mapping Checkpoint

2026-08-17 direct Unity checkpoint: `Builds/unity-actor-animation-visual-checkpoint.log`

| Role/cue | Shared animation trigger |
|---|---|
| Knight `HeroAttacked` | `Attack` |
| Knight `HeroAbility` | `Ability` |
| Raider `EnemyHit` | `Hit` |
| Raider `EnemyDefeated` | `Defeat` echo |

## 3.3 Action Timing Checkpoint

2026-08-17 direct Unity checkpoint: `Builds/unity-action-timing-checkpoint.log`

Full table: `docs/ACTION_ANIMATION_TIMING_CONTRACT_2026-08-17.md`

| Role/cue | Trigger | Impact time | Socket |
|---|---|---:|---|
| Knight `HeroAttacked` | `Attack` | 0.45 | `Socket_Weapon_Tip` |
| Knight `HeroAbility` | `Ability` | 0.18 | `Socket_Ground` |
| Raider `EnemyHit` | `Hit` | 0.05 | `Socket_Chest` |
| Raider `EnemyDefeated` | `Defeat` echo | 0.00 | `Socket_Ground` |
| Boss `BossAttack` | `BossAttack` | 0.55 | `Socket_Weapon_Tip` |
| Boss `BossPhase` | `Phase` | 0.15 | `Socket_Ground` |

## 3.4 Animator Override Checkpoint

2026-08-17 direct Unity checkpoint: `Builds/unity-actor-animator-override-vis223.log`

`ActorVisualEntry` has an optional `animatorOverride`. Runtime actor creation asks the catalog for `ActorAnimator(kind)`, which returns the role override when authored and otherwise returns the shared fallback controller. Current catalog state has generated role overrides for Knight, Guard and Raider, with the remaining roles still on explicit fallback:

| Check | Result |
|---|---|
| Actor roles checked | 8 |
| Role override roles | 3: Knight, Guard, Raider |
| Shared fallback roles | 5 |
| Override path | Passed |

Test Runner follow-up:

- `Builds/editmode-results-vis223.xml` passed 40/40 and includes the Knight/Guard/Raider override resolver and role-specific clip binding tests.
- `Builds/playmode-results-vis223.xml` passed 6/6 after the Guard override catalog update.

## 3.5 Animator Priority Checkpoint

2026-08-17 direct Unity checkpoint: `Builds/unity-actor-animator-priority-checkpoint.log`

The shared fallback controller now applies these transition rules:

| Rule | Result |
|---|---|
| Defeat AnyState transition | Immediate, no exit time, ordered interrupt enabled |
| Hit AnyState transition | Short blend, no self-transition spam |
| Victory AnyState transition | Short trigger-driven blend |
| Defeat state exit | Returns after full fallback clip |

## 3.6 Animation Event Relay Checkpoint

2026-08-17 direct Unity checkpoint: `Builds/unity-animation-event-relay-checkpoint.log`

`PresentationAnimationEventRelay` is attached to runtime actor motion objects. It accepts only presentation feedback cues and exposes no gameplay authority methods.

| Rule | Result |
|---|---|
| Allowed feedback | Combat/presentation VFX and SFX cues |
| Rejected feedback | `UiClick`, `UiError`, `WaveStarted` |
| Gameplay authority | False; no damage, gold, pathing or economy methods |

## 4. Runtime Rules

- Gameplay movement, damage, phase, health, economy and targeting remain code-driven.
- Root motion is not gameplay authority.
- Rigged runtime actors may visually move limbs/weapon arcs, but world position returns to the presentation root.
- Animation events may trigger only presentation effects: VFX, SFX, camera shake or UI feedback.
- Every final runtime actor must support idle, move, attack, hit and defeat.
- Role-special clips must map through the shared trigger set or a documented role override.

## 5. Role-Specific Target Clips

| Role | Required final clips |
|---|---|
| Knight | `Idle`, `Move`, `Attack`, `Hit`, `Defeat`, `Ability` as shield bash |
| Guard | `Idle`, `Move`, `Attack`, `Hit`, `Defeat`, `Ability` as block/parry placeholder |
| Raider | `Idle`, `Move`/`Run`, `Attack`, `Hit`, `Defeat` |
| Vanguard | `Idle`, `Move`, heavy `Attack`, `Hit`, stagger, `Defeat` |
| Regenerator | `Idle`, `Move`, `Attack`, regen pulse/special, `Hit`, `Defeat` |
| Support | `Idle`, `Move`, `Attack`, cast/heal, `Hit`, `Defeat` |
| Siege | `Move`, setup, heavy attack/recoil, `Hit`, `Defeat` |
| Boss | `Idle`, `Move`, `Attack`, `BossAttack`, `Hit`, `Phase`, `Defeat` |

If a role cannot share a humanoid rig, it may use a role-specific animator override, but the presentation calls above must still work.

## 6. Current Technical State

Current implementation:

- `PresentationActorAnimationContract.cs` stores the shared names.
- `DefenderPresentationAssetBuilder.CreateActorAnimator()` builds the fallback controller from the contract.
- `DefenderPresentationCatalog.ValidationErrors()` validates clips from the contract.
- `StylizedActorView` triggers the contract names.
- `DefenderPresentationCatalog.ActorAnimator(kind)` resolves a role-specific override or the shared fallback.

Open work:

- Full rigged KayKit or original rigged source files are still missing from the repository.
- Generated Animator Override Controller assets exist for Knight, Guard and Raider; final authored polish is still open.
- Final authored animation events still need gameplay-camera timing review after role-specific clips are produced.

## 7. Acceptance Gates

| Gate | Required proof |
|---|---|
| Shared contract compiles | Unity Editor compile or CI result |
| Fallback controller builds | Catalog rebuild creates all contract clips/states |
| Role override compatibility | Every role responds to the shared presentation calls |
| Event timing | Attack/heal/boss release events align with VFX sockets |
| Gameplay regression | Movement/pathing/damage unchanged with animation enabled |
