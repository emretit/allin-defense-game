# Actor Runtime Animation/VFX Capture - 2026-08-17

Durum: In review

## Scope

This pass verifies the current Knight/Guard/Raider/Vanguard/Regenerator/Support runtime presentation path on iOS Simulator:

- Knight sword attack cue reaches the `Attack` animation trigger through the Knight role override and fire diamond projectile VFX family.
- Knight shield bash cue reaches the `Ability` animation trigger through the Knight role override and defender ring shockwave VFX family.
- Guard spawn cue reaches the defender ring spawn VFX family.
- Guard spear thrust cue reaches the `Attack` animation trigger through the Guard role override and defender diamond non-projectile VFX family.
- Guard block/parry cue reaches the `Ability` animation trigger through the Guard role override and defender ring VFX family.
- Raider spawn cue reaches the dark ring spawn VFX family.
- Raider hit cue reaches the `Hit` animation trigger through the Raider role override and enemy diamond impact VFX family.
- Raider defeat cue reaches the `Defeat` echo path through the Raider role override and enemy ring death VFX family.
- Vanguard spawn cue reaches the defender ring spawn VFX family.
- Vanguard heavy/disrupt cue reaches the `Hit` animation trigger through the Vanguard role override and enemy diamond non-projectile physical heavy VFX family.
- Regenerator spawn cue reaches the defender ring spawn VFX family.
- Regenerator regen pulse cue reaches the `Ability` animation trigger through the Regenerator role override and enemy ring core-pulse VFX family.

## Evidence

| Evidence | Path |
|---|---|
| iOS runtime log | `Builds/ios-actor-capture.log` |
| Support-refresh raw Simulator screenshot | `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-211833-raw.png` |
| Support-refresh landscape readable screenshot | `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-211833-landscape-upright.png` |
| Vanguard-refresh raw Simulator screenshot | `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-203449-raw.png` |
| Vanguard-refresh landscape readable screenshot | `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-203449-landscape-upright.png` |
| Guard-refresh raw Simulator screenshot | `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-201018-raw.png` |
| Guard-refresh landscape readable screenshot | `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-201018-landscape-upright.png` |
| Override-refresh raw Simulator screenshot | `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-193606-raw.png` |
| Override-refresh landscape readable screenshot | `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-193606-landscape-upright.png` |
| Raw Simulator screenshot | `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-manual-raw.png` |
| Landscape readable screenshot | `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-manual-landscape-upright.png` |
| Repro script | `tools/run_ios_actor_capture.sh` |

## Runtime Result

`Builds/ios-actor-capture.log` contains:

```text
[DC_ACTOR_CAPTURE_PASS] knight_attack=true knight_ability=true guard_spawn=true guard_thrust=true guard_block=true raider_spawn=true raider_hit=true raider_death=true vanguard_spawn=true vanguard_heavy=true regenerator_spawn=true regenerator_pulse=true support_spawn=true support_heal=true
```

Representative cue mapping lines from the same run:

```text
[DC_ACTOR_CAPTURE_SIGNAL] actor=Raider cue=UnitSpawned animation=none vfx=Dark/Ring projectile=False
[DC_ACTOR_CAPTURE_SIGNAL] actor=Raider cue=EnemyHit animation=Hit vfx=Enemy/Diamond projectile=True
[DC_ACTOR_CAPTURE_SIGNAL] actor=Knight cue=HeroAttacked animation=Attack vfx=Fire/Diamond projectile=True
[DC_ACTOR_CAPTURE_SIGNAL] actor=Raider cue=EnemyDefeated animation=Defeat vfx=Enemy/Ring projectile=False
[DC_ACTOR_CAPTURE_SIGNAL] actor=Knight cue=HeroAbility animation=Ability vfx=Defender/Ring projectile=False
[DC_ACTOR_CAPTURE_SIGNAL] actor=Guard cue=UnitSpawned animation=none vfx=Defender/Ring projectile=False
[DC_ACTOR_CAPTURE_SIGNAL] actor=Guard cue=OutpostAttacked animation=Attack vfx=Defender/Diamond projectile=False
[DC_ACTOR_CAPTURE_SIGNAL] actor=Guard cue=HeroAbility animation=Ability vfx=Defender/Ring projectile=False
[DC_ACTOR_CAPTURE_SIGNAL] actor=Vanguard cue=UnitSpawned animation=none vfx=Defender/Ring projectile=False
[DC_ACTOR_CAPTURE_SIGNAL] actor=Vanguard cue=OutpostDisrupted animation=Hit vfx=Enemy/Diamond projectile=False
[DC_ACTOR_CAPTURE_SIGNAL] actor=Regenerator cue=UnitSpawned animation=none vfx=Defender/Ring projectile=False
[DC_ACTOR_CAPTURE_SIGNAL] actor=Regenerator cue=HealPulse animation=Ability vfx=Enemy/Ring projectile=False
```

The 2026-08-17 20:56 local Regenerator-refresh run produced the pass line after `DC_Knight_Actors.overrideController`, `DC_Guard_Actors.overrideController`, `DC_Raider_Actors.overrideController`, `DC_Vanguard_Actors.overrideController` and `DC_Regenerator_Actors.overrideController` were generated. The accepted review screenshot is 1334 x 750 landscape and was visually inspected; the right platform contains the actor/VFX review cluster, with Regenerator pulse verified by log signal.

## Notes

- The first automated script attempt failed because no Simulator was booted. `tools/run_ios_actor_capture.sh` now boots an available Simulator and waits for boot completion before building/running.
- The raw Simulator capture needed a 180-degree review correction after landscape rotation. The accepted review image is the `*-landscape-upright.png` copy.
- This is a runtime presentation acceptance checkpoint, not final animation polish. Knight/Guard/Raider/Vanguard/Regenerator/Support generated override clips now exist. Hand-authored motion polish, socket-origin timing, dense-wave readability, blind recognition and physical-device review remain open.
