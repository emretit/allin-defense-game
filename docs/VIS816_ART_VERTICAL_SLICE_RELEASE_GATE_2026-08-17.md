# VIS-816 Art Vertical Slice Release Gate

Date: 2026-08-17

Last audit: 2026-08-25

Status: Not closed

Decision: The art vertical slice is not fully releasable yet because physical iPhone 13-class Profiler and Frame/Rendering Debugger evidence is still missing. Simulator visual acceptance and automated QA evidence are in review.

## Gate Checklist

| Gate | Status | Evidence / Reason |
|---|---|---|
| Quality profile matrix | In review | `docs/VIS800_QUALITY_PROFILE_MATRIX_2026-08-17.md`, `Builds/unity-quality-profile-matrix-checkpoint.log` |
| Texture import profile | In review | `docs/VIS804_TEXTURE_IMPORT_PROFILE_2026-08-17.md`, `Builds/unity-texture-import-profile-checkpoint.log` |
| Geometry optimization decision | In review | `docs/VIS805_GEOMETRY_OPTIMIZATION_DECISION_2026-08-25.md`; no mesh mutation applied without physical draw-pass proof |
| VFX quality profile | In review | `docs/VIS806_VFX_QUALITY_PROFILE_2026-08-17.md`, `Builds/unity-vfx-quality-profile-checkpoint.log` |
| Simulator performance acceptance | In review | `docs/VIS807_PERFORMANCE_ACCEPTANCE_SIMULATOR_2026-08-17.md`, `Builds/ios-performance-acceptance.log` |
| Physical device Profiler | Open | Required for VIS-006/VIS-807; readiness report `docs/evidence/visual-development/ios-physical-readiness-2026-08-25.json` is blocked before install/profiling. |
| Frame/Rendering Debugger | Open | Required for VIS-007; final draw pass, shadow caster, overdraw and texture screens are missing. |
| Catalog QA | Passed | Current integrated EditMode suite passed 65/65. Raw XML is intentionally ignored. |
| Gameplay QA | Passed | Current integrated PlayMode suite passed 12/12. Raw XML is intentionally ignored. |
| Automated audio-visual combat | Passed on Simulator | `docs/VIS700_AUDIO_VISUAL_POLISH_ACCEPTANCE_2026-08-25.md`; 480 seconds, 22/22 cues, 6/6 music states, 3/3 levels and 40/40 combat cycles. |
| Human audio polish | Open | Speaker/headphone fatigue, loudness, pop/click, audio-only/visual-only and final video review are prepared in `docs/VIS706_708_813_814_HUMAN_REVIEW_GATE_2026-08-25.md`, but not signed off. |
| Final visual evidence | Passed on Simulator | `docs/VIS812_FINAL_VISUAL_EVIDENCE_SIMULATOR_2026-08-17.md`, accepted stamp `vis812-final-20260825-210559` |
| Accessibility visual audit | In review | `docs/VIS813_814_ACCESSIBILITY_COMPARISON_2026-08-17.md`, `docs/evidence/visual-development/vis813-accessibility-grayscale-contact-sheet-2026-08-25.png` |
| Before/after comparison | In review | `docs/evidence/visual-development/vis814-before-after-contact-sheet-2026-08-25.png` |
| Delivery index | In review | `docs/VIS815_DELIVERY_EVIDENCE_INDEX_2026-08-17.md` |

## Accepted Simulator Evidence

- EditMode QA: current integrated suite passed 65/65.
- PlayMode QA: current integrated suite passed 12/12.
- Audio-visual combat: the eight-minute Simulator runner passed all 22 cues, six music states, three review levels, 40 stress cycles and the ten-voice cap.
- Visual state set: `vis812-final-20260825-210559` includes preparation, ability, normal, dense, boss1, boss2, boss3, victory and defeat from the same installed build.
- Grayscale accessibility and before/after sheets are regenerated from accepted stamp `vis812-final-20260825-210559` under `docs/evidence/visual-development/`.
- Simulator performance proxy reports preparation/normal/dense/boss metrics with median 60 FPS, boss p95 17.1 ms and no low-memory warning; the 90-second soak also passed.

## Required To Close

1. Capture iPhone 13-class physical-device Profiler evidence for preparation, normal/dense and boss scenarios.
2. Capture Unity Frame Debugger and Rendering Debugger evidence for draw passes, shadow casters, overdraw and texture budget.
3. Update `docs/VIS807_PERFORMANCE_ACCEPTANCE_SIMULATOR_2026-08-17.md` or create a physical-device successor report.
4. Complete the human speaker/headphone audio-only/visual-only polish, VoiceOver/blind-recognition review and final video sign-off using `docs/VIS706_708_813_814_HUMAN_REVIEW_GATE_2026-08-25.md`.
5. Revisit this checklist and mark VIS-816 complete only if no critical visual/performance regression remains.

## Current Device Availability Check

2026-08-25 discovery finds the iPhone 13-class device paired, wired and in Developer Mode. `tools/check_ios_physical_readiness.py` writes a sanitized report at `docs/evidence/visual-development/ios-physical-readiness-2026-08-25.json`; installation/profiling remains blocked before launch because DDI services are unavailable:

- Device: `Emre iPhone’u (2)`, iPhone 13 (`iPhone14,5`), iOS 26.6 beta (`23G5028e`).
- Installed toolchain: Xcode 26.6 (`17F113`); the visible DeviceSupport set does not include iOS 26.6.
- Failure: DDI services unavailable; matching iOS 26.6 DeviceSupport/DDI is not available to the installed toolchain.

The actionable unblock is installing an Xcode/DeviceSupport build that includes the iOS 26.6 beta DDI, or returning the phone to a supported iOS version. Pairing, cable trust and Developer Mode are no longer the blockers.

## Current Release Note

The current branch has a strong Simulator acceptance package and can be handed off for review. It should not be called final art vertical-slice completion until the physical-device performance/debugger gate is resolved.
