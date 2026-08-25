# VIS-700 Audio-Visual Polish Acceptance

Date: 2026-08-25

Status: VIS-701–705 completed; VIS-706 waiting; VIS-707/VIS-708 in review

## Delivered Contract

`PresentationAudioPolicy` is the single runtime contract for cue concurrency, importance, impact delay and music ducking. `DefenderAudioController` now receives the full `PresentationSignal`, protects critical voices from low-priority combat chatter, limits active SFX voices to ten, supports deterministic Silent/Low/Normal review gains and uses state-specific music transitions.

| Cue | Group | Priority | Limit | Delay | Music duck |
|---|---|---:|---:|---:|---:|
| UiClick | UI | High | 2 | 0.00 s | — |
| UiError | UIError | High | 1 | 0.00 s | — |
| OutpostBuilt | Economy | High | 2 | 0.00 s | — |
| OutpostUpgraded | Economy | High | 2 | 0.00 s | — |
| OutpostSold | Economy | High | 2 | 0.00 s | — |
| UnitSpawned | Spawn | Normal | 3 | 0.00 s | — |
| UnitDefeated | DefeatImpact | Normal | 3 | 0.00 s | — |
| EnemyHit | CombatLight | Normal | 4 | 0.00 s | — |
| EnemyDefeated | DefeatImpact | Normal | 3 | 0.00 s | — |
| HeroMoved | Movement | Ambient | 2 | 0.00 s | — |
| HeroAttacked | HeroAction | High | 2 | 0.12 s | — |
| HeroAbility | HeroAbility | Critical | 1 | 0.05 s | 42% / 0.48 s |
| OutpostAttacked | Projectile | Normal | 4 | 0.00 s | — |
| MageImpact | MagicImpact | High | 3 | 0.00 s | — |
| HealPulse | Heal | High | 3 | 0.00 s | — |
| OutpostDisrupted | Status | High | 2 | 0.00 s | — |
| SlowApplied | Status | High | 2 | 0.00 s | — |
| BossPhase | BossPhase | Critical | 1 | 0.05 s | 55% / 0.75 s |
| BossAttack | BossImpact | Critical | 2 | 0.20 s | 48% / 0.55 s |
| WaveStarted | Wave | High | 1 | 0.00 s | 24% / 0.32 s |
| Victory | Result | Critical | 1 | 0.08 s | 62% / 1.15 s |
| Defeat | Result | Critical | 1 | 0.08 s | 62% / 1.15 s |

Music crossfade targets are Preparation 1.10 s, Combat 0.85 s, Intense 0.70 s, Boss 0.50 s, Victory 0.35 s and Defeat 0.28 s. Pause uses the existing pause mixer snapshot and mutes SFX/music sources; resume returns to the normal snapshot in 0.18 s.

Review gains are Silent 0%, Low 25% and Normal 100%. These are acceptance controls, not a new player-facing settings screen.

## Automated Acceptance

The iOS Simulator acceptance runner starts gameplay directly, builds the eight-slot defence layout and holds real combat load for the whole run. Every 12 seconds it replaces the stress group with 16 enemies; every fourth cycle includes a Boss. In parallel it exercises all 22 cues, all six music states, the three review gains and the pause snapshot.

| Check | Result |
|---|---|
| EditMode | 64/64 passed |
| PlayMode | 12/12 passed |
| 60-second integration smoke | 22/22 cues, 6/6 music states, 3/3 levels, 5/5 combat cycles, 10/10 maximum voices |
| 480-second full combat run | 22/22 cues, 6/6 music states, 3/3 levels, 40/40 combat cycles, 10/10 maximum voices |
| Mixer snapshot | normal → pause → normal passed |
| Runtime failure marker | none |

Curated evidence: `docs/evidence/runtime/audio-visual-acceptance-20260825.txt`

Raw runtime log: ignored `Builds/ios-audio-visual-acceptance.log`

## Acceptance Decision

- VIS-701 is complete: all 22 cues have explicit timing and grouping policy, backed by full enum-coverage tests.
- VIS-702 is complete: per-clip and per-group concurrency, four importance levels, ten-voice cap and priority-based stealing are integrated and stressed.
- VIS-703 is complete: all six music states have deterministic crossfade targets; pause/normal snapshot transition passed.
- VIS-704 is complete: build/upgrade/sell remain success-only gameplay emissions and share the protected Economy feedback group.
- VIS-705 is complete: HeroAbility, BossPhase, BossAttack, Victory and Defeat are Critical and duck music.
- VIS-706 remains waiting: a dedicated low-profile wind/foliage/crystal environment SFX layer has not been authored.
- VIS-707 is in review: automated Silent/Low/Normal coverage passed, but the requested video and human audio-only/visual-only sign-off are not present.
- VIS-708 is in review: the eight-minute combat run passed with timestamped evidence, but subjective speaker/headphone fatigue, loudness balance, pop/click and perceived synchronization still require a listening pass.

## Reproduction

```sh
tools/build_and_run_ios_simulator.sh
DC_SIMULATOR_ID=<udid> DC_REUSE_INSTALLED_BUILD=1 DC_AUDIO_ACCEPTANCE_SECONDS=60 tools/run_ios_audio_visual_acceptance.sh
DC_SIMULATOR_ID=<udid> DC_REUSE_INSTALLED_BUILD=1 DC_AUDIO_ACCEPTANCE_SECONDS=480 tools/run_ios_audio_visual_acceptance.sh
```

The Simulator run is valid for state, coverage, timing policy and pool/concurrency regression. It is not physical-speaker, headphone, thermal or physical-GPU acceptance.
