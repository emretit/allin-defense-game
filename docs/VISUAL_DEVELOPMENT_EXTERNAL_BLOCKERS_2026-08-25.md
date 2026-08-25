# Visual Development External Blockers - 2026-08-25

This file is the current closeout blocker map for the visual TODO. It intentionally separates completed repository work from gates that require physical-device evidence or human review.

## Current TODO State

| Status | Count | Meaning |
|---|---:|---|
| Tamamlandı | 165 | Code, authoring, documentation and automated/simulator evidence are present. |
| Bloke | 23 | Final acceptance requires evidence that cannot be produced from the current repository alone. |
| İncelemede | 0 | No ambiguous review-only items remain in the current TODO. |

## Physical iPhone / Xcode Blockers

| IDs | Missing evidence | Current evidence | Unblock condition |
|---|---|---|---|
| VIS-006, VIS-007, VIS-807 | iPhone 13-class Profiler, Frame Debugger and Rendering Debugger captures | `docs/VIS807_PHYSICAL_READINESS_CHECK_2026-08-25.md`, `docs/evidence/visual-development/ios-physical-readiness-2026-08-25.json` | Install/use Xcode DeviceSupport compatible with iOS 26.6 beta or move to a supported iOS device, then record the four physical scenarios. |
| VIS-105, VIS-292, VIS-346, VIS-415, VIS-515 | Device-derived geometry, actor, building, environment and VFX stress budgets | Static/proxy reports and Simulator acceptance are present; physical DDI is blocked | Re-run physical Profiler/Frame Debugger after VIS-006/VIS-007 unblocks, then update budget reports from measured data. |
| VIS-801, VIS-802, VIS-803, VIS-804, VIS-805, VIS-806 | Final quality-profile, shadow, lighting, texture, geometry and VFX settings proven on physical device | Batch/simulator profile evidence exists; VIS-805 explicitly defers mesh changes until draw-pass proof | Use physical captures to either accept current settings or apply measured optimizations. |
| VIS-008, VIS-814, VIS-816 | Complete baseline/release acceptance including physical evidence | Final Simulator visual set exists; physical debug baseline is missing | Attach final physical evidence to the baseline index and release gate. |

## Human Review Blockers

| IDs | Missing evidence | Current evidence | Unblock condition |
|---|---|---|---|
| VIS-290 | Independent blind-recognition review for eight actors | Final 64 px icon/contact evidence exists | A human reviewer fills the recognition/sign-off fields in the human review template. |
| VIS-291 | Final actor budget acceptance after VIS-105 and VIS-290 | Actor metrics exist; device-derived budget and blind-recognition sign-off are missing | Close VIS-105 and VIS-290, then update actor budget decision. |
| VIS-706 | Human masking/loudness judgment for ambience | Automated Medium smoke and Low-off profile passed | Human listener approves masking/loudness in the sign-off JSON. |
| VIS-707, VIS-708 | Speaker/headphone, audio-only/visual-only and final video polish sign-off | 480-second automated audio-visual run passed | Human reviewer completes the listening and video review protocol. |
| VIS-813 | Real assistive-tech and blind-recognition accessibility sign-off | Final grayscale contact sheet and audit report exist | Human/assistive-tech review passes or issues are recorded and fixed. |

## Authoritative Sign-Off Files

- `docs/VIS706_708_813_814_HUMAN_REVIEW_GATE_2026-08-25.md`
- `docs/evidence/visual-development/human-review-signoff-template-2026-08-25.json`
- `tools/validate_visual_human_signoff.py`
- `tools/check_ios_physical_readiness.py`

## Do Not Close Without These

- Do not mark VIS-816 as complete from Simulator evidence alone.
- Do not treat Generic transform rigs as Humanoid migration; Humanoid is outside this vertical-slice closeout.
- Do not apply mesh combine, static batching, instancing or LOD only to make a TODO look complete; VIS-805 requires physical draw-pass proof first.
- Do not replace human speaker/headphone, VoiceOver or blind-recognition review with automated image/audio reports.
