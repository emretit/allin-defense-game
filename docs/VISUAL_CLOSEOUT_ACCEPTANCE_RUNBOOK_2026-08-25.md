# ALLIN: Defense - Visual Closeout Acceptance Runbook

Date: 2026-08-25
Branch: `codex/visual-slice-closeout`

This runbook turns `VISUAL_DEVELOPMENT_NEXT_TODO_2026-08-25.md` into an executable order of operations for the remaining release gates.

## Current gate state

The next TODO is now blocked on the remaining manual review inputs:

- A human reviewer must complete the audio/accessibility sign-off JSON.
- Unity Profiler and Frame/Rendering Debugger summaries must be captured from the physical iPhone run.

The 2026-08-26 readiness probe returns `[DC_IOS_PHYSICAL_READY]`, and the physical iPhone development build installs and launches. The automated smoke, performance and 90-second soak paths pass, but they do not replace the manual Profiler/Frame Debugger evidence needed for the final release gate.

## One-command closeout path

After the phone is physically connected and unlocked:

```bash
tools/run_visual_closeout_acceptance.sh
```

The script runs:

1. `tools/check_ios_physical_readiness.py`
2. `tools/build_and_run_ios_device.sh` with `DC_IOS_SMOKE=1`
3. `tools/build_and_run_ios_device.sh` with `DC_IOS_PERFORMANCE_ACCEPTANCE=1`
4. `tools/build_and_run_ios_device.sh` with `DC_IOS_SOAK_ACCEPTANCE=1`
5. `tools/validate_visual_device_acceptance.py`
6. `tools/validate_visual_human_signoff.py`

Build logs are written under `Builds/` and remain uncommitted. Smoke, defeat smoke, performance and soak modes now write separate log files so one run does not overwrite another. Curated acceptance summaries should be copied into `docs/evidence` only after review.

Physical Profiler and Frame/Rendering Debugger summaries must be copied into `docs/evidence/visual-development/physical-device-acceptance-2026-08-25.json` and validated with:

```bash
python3 tools/validate_visual_device_acceptance.py docs/evidence/visual-development/physical-device-acceptance-2026-08-25.json
```

## TODO mapping

| NEXT ID | Covered by | Remaining manual evidence |
|---|---|---|
| NEXT-001 | `check_ios_physical_readiness.py` | None after `[DC_IOS_PHYSICAL_READY]` |
| NEXT-002 | device smoke phase | Confirm install/launch on the target physical iPhone |
| NEXT-003 | performance phase | Attach 60-second Profiler notes for preparation, normal, dense and boss phase 3 |
| NEXT-004 | not automated | Capture Frame/Rendering Debugger screenshots/notes manually from Unity and pass `validate_visual_device_acceptance.py` |
| NEXT-005 | soak phase | Attach 60-actor/VFX stress and 90-second soak evidence |
| NEXT-006 | sign-off validator | Human reviewer must fill `human-review-signoff-2026-08-25.json` |

## Closeout rules

- Do not close `VIS-816` from Simulator evidence alone.
- Do not commit `Builds`, `Library`, `TestResults`, raw screenshots or crash dumps.
- Only mark a TODO row `Tamamlandı` when acceptance criteria, command result and curated evidence all exist.
- If the closeout script stops at readiness, the next action is physical: plug the phone in over USB, unlock it, keep it awake and accept trust/developer prompts.
