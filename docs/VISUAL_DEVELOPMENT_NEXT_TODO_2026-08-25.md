# ALLIN: Defense - Next Visual TODO

Date: 2026-08-25
Branch: `codex/visual-slice-closeout`

Current closeout state: `VISUAL_DEVELOPMENT_TODO.md` has 165 completed rows and 23 blocked rows. The blocked rows are not open-ended production tasks; they need physical-device evidence or human review.

## A. Immediate unblock checklist

| ID | Priority | Status | Task | Acceptance |
|---|---:|---|---|---|
| NEXT-001 | P0 | Tamamlandı | Connect iPhone over USB, unlock it, keep screen awake, accept Trust/Developer prompts | `docs/evidence/visual-development/ios-physical-readiness-2026-08-26.json` returns `[DC_IOS_PHYSICAL_READY]` |
| NEXT-002 | P0 | Tamamlandı | Run physical iPhone development build | App installed and launched on the connected iPhone 13-class device; smoke automation reached `[DC_SMOKE_PASS]` |
| NEXT-003 | P0 | İncelemede | Capture physical Profiler scenarios: preparation, normal, dense, boss phase 3 | Automated physical performance run passed, but final 60-second Unity Profiler notes with FPS, CPU/GPU frame time, draw calls, triangles, GC, memory and thermal notes are still required |
| NEXT-004 | P0 | Blocked by NEXT-003 | Capture Frame/Rendering Debugger evidence | Top render costs, shadow casters, additional lights, overdraw and texture usage are recorded |
| NEXT-005 | P0 | İncelemede | Run physical 60-actor/VFX stress and 90-second soak | 90-second physical soak passed with no low-memory warning; 60-actor Profiler/overdraw evidence is still required |
| NEXT-006 | P0 | Blocked by human reviewer | Complete human audio/accessibility sign-off JSON | `tools/validate_visual_human_signoff.py` passes without `--allow-pending` |

## B. Close blocked VIS rows after unblock

| ID | Depends on | Task | Closes |
|---|---|---|---|
| NEXT-101 | NEXT-003/NEXT-004 | Convert static/proxy budgets into device-derived budgets | VIS-105, VIS-346, VIS-415 |
| NEXT-102 | NEXT-003/NEXT-005 | Record 60 actor + dense VFX physical stress report | VIS-292, VIS-515 |
| NEXT-103 | NEXT-003/NEXT-004 | Finalize Low/Medium/High quality profile tuning | VIS-801, VIS-802, VIS-803, VIS-804, VIS-805, VIS-806 |
| NEXT-104 | NEXT-006 | Attach blind-recognition and assistive-tech results | VIS-290, VIS-291, VIS-813, VIS-814 |
| NEXT-105 | NEXT-001–NEXT-104 | Update release gate and final handoff | VIS-006, VIS-007, VIS-008, VIS-807, VIS-816 |

## C. Optional quality improvements after release gate

These are intentionally outside the current vertical-slice closeout. Do not mix them into VIS-816 unless the product owner expands scope.

| ID | Priority | Task | Why it helps |
|---|---:|---|---|
| NEXT-201 | P1 | Prepared: actor rig migration decision | `docs/NEXT201_ACTOR_RIG_MIGRATION_DECISION_2026-08-25.md` and `docs/evidence/visual-development/actor-rig-source-inspection-2026-08-25.json` confirm current Generic rigs are acceptable until real authored Humanoid/skinned animation sources exist |
| NEXT-202 | P1 | Prepared: Frostfen Hollow second-biome paint-over and kit | `docs/FROSTFEN_HOLLOW_SECOND_BIOME_PLAN_2026-08-25.md` and `docs/evidence/visual-development/frostfen-hollow-paintover-plan-2026-08-25.*` prove the visual language can scale beyond Greenwatch Valley without changing runtime scope |
| NEXT-203 | P1 | Blocked by accepted brand/logo handoff | `tools/validate_brand_handoff.py` and `docs/BRAND_HANDOFF_ACCEPTANCE_STANDARD_2026-08-26.md` define the required accepted direction, changed files, Dynamic-Island-safe screenshot, frontend evidence and signoffs before final logo/splash variants can proceed |
| NEXT-204 | P2 | Prepared: cinematic boss/result framing contract | `Assets/Scripts/Presentation/PresentationCinematicFramingContract.cs` and `docs/CINEMATIC_BOSS_RESULT_FRAMING_PLAN_2026-08-25.md` define tested framing specs without changing runtime camera behavior |
| NEXT-205 | P2 | Prepared: Android visual/performance acceptance matrix | `docs/ANDROID_VISUAL_PERFORMANCE_ACCEPTANCE_MATRIX_2026-08-25.md` defines post-iOS device tiers, scenarios, profile rules and evidence package |

## Rules for the next session

- Do not close VIS-816 from Simulator evidence alone.
- Do not stage ignored raw screenshots, `Builds`, `Library`, `TestResults`, crash dumps or transient logs.
- Do stage curated docs/evidence that are directly linked from TODO, handoff or blocker reports.
- Re-run the exact acceptance validator before changing a blocked row to `Tamamlandı`.

## Execution helper

`tools/run_visual_closeout_acceptance.sh` now runs the remaining closeout chain in order once the physical device is ready. The detailed runbook is `docs/VISUAL_CLOSEOUT_ACCEPTANCE_RUNBOOK_2026-08-25.md`.

Latest local preflight:

- `python3 tools/check_ios_physical_readiness.py --output docs/evidence/visual-development/ios-physical-readiness-2026-08-26.json` returns `[DC_IOS_PHYSICAL_READY]` for the connected, wired, paired iPhone 13-class device.
- `tools/build_and_run_ios_device.sh` now selects the provisioning-profile team before falling back to the first Apple Development identity. This fixes the previous local mismatch where the certificate team and usable wildcard provisioning profile team differed.
- Physical iPhone smoke, automated performance and 90-second soak runs passed on 2026-08-26. Details and caveats are recorded in `docs/IOS_PHYSICAL_DEVICE_ACCEPTANCE_PROGRESS_2026-08-26.md`.
- `tools/validate_visual_human_signoff.py docs/evidence/visual-development/human-review-signoff-template-2026-08-25.json --allow-pending` lists the expected missing human-review approvals for NEXT-006.
- `tools/validate_visual_device_acceptance.py` and `docs/evidence/visual-development/physical-device-acceptance-template-2026-08-25.json` still define the required manual Unity Profiler, Frame Debugger and 60-actor stress evidence before NEXT-003/NEXT-004/NEXT-005 can be fully closed.
- Latest local Unity regression after NEXT-201/NEXT-202/NEXT-204/NEXT-205 preparation: EditMode `67/67` and PlayMode `12/12` passed; reports are under ignored `Builds/`.
- `tools/validate_brand_handoff.py` and `docs/evidence/visual-development/brand-handoff-template-2026-08-26.json` now prevent NEXT-203 from moving forward without the accepted logo/splash handoff and Dynamic-Island-safe evidence.
