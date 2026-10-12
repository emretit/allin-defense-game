# ALLIN: Defense - Cinematic Boss/Result Framing Plan

Date: 2026-08-25
Related TODO: `NEXT-204`

This checkpoint adds a tested framing contract without yet moving the runtime camera. The goal is to make future boss/result camera polish deliberate and regression-safe.

## Contract

`PresentationCinematicFramingContract` defines optional framing specs for:

- `BossPhase`
- `BossAttack`
- `Victory`
- `Defeat`

The contract stores:

- normalized focus offset;
- orthographic size multiplier;
- blend-in, hold and blend-out timing;
- reduced-motion behavior through the existing `PresentationFeedbackIntensityMode`.

## Safety rule

The runtime camera is not yet animated by this checkpoint. That avoids changing:

- safe-area battlefield fitting;
- touch target alignment;
- existing shake restoration behavior;
- result screen layout.

Runtime binding should happen only after a PlayMode regression proves interrupted framing restores the adaptive camera rest pose exactly like the current shake test.

## Acceptance

`NEXT-204` is prepared when the contract and tests exist. Runtime camera movement remains a later production task and must not be used to close `VIS-816` without physical-device evidence.
