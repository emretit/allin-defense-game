# Action Animation Timing Contract - 2026-08-17

Durum: In review

## Scope

This document records the first machine-checked action timing contract for presentation cues. Gameplay authority remains in combat/session code; animation timing can trigger only presentation feedback such as VFX, SFX, camera or UI feedback.

Unity checkpoint: `Builds/unity-action-timing-checkpoint.log`

## Checked Runtime Rows

| Role/cue | Animation trigger | Impact time | Impact socket | VFX family |
|---|---|---:|---|---|
| Knight `HeroAttacked` | `Attack` | 0.45 | `Socket_Weapon_Tip` | Fire diamond projectile |
| Knight `HeroAbility` | `Ability` | 0.18 | `Socket_Ground` | Defender ring shockwave |
| Raider `EnemyHit` | `Hit` | 0.05 | `Socket_Chest` | Enemy diamond impact |
| Raider `EnemyDefeated` | `Defeat` echo | 0.00 | `Socket_Ground` | Enemy ring death burst |
| Boss `BossAttack` | `BossAttack` | 0.55 | `Socket_Weapon_Tip` | Enemy projectile |
| Boss `BossPhase` | `Phase` | 0.15 | `Socket_Ground` | Dark phase ring |

## Checkpoint Result

```text
[DC_ACTION_TIMING_CHECKPOINT] passed knightSlash=Attack@0.45/WeaponTip knightShield=Ability@0.18/Ground raiderHit=Hit@0.05/Chest raiderDeath=Defeat@0/Ground bossAttack=BossAttack@0.55/WeaponTip bossPhase=Phase@0.15/Ground
```

## Open Gates

- This is a contract/checkpoint for timing targets, not authored final animation clips.
- Remaining actor roles need the same final authored clip timing after their model/rig polish.
- Final acceptance still needs gameplay-camera motion capture and dense-wave readability for authored clips.
