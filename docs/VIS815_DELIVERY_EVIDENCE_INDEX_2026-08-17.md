# VIS-815 Delivery Evidence Index

Date: 2026-08-17

Status: Delivered; VIS-816 physical release gate remains blocked

Scope: Current visual vertical-slice delivery map for art bible, provenance, catalog, QA and evidence. This is a handoff index for another developer to reproduce and inspect the accepted visual-development work.

## 2026-08-25 Closeout Update

This update supersedes the older test counts and release-risk summary below while preserving the 17 August history.

| Area | Current result |
|---|---|
| Branch | `codex/visual-slice-closeout` |
| Frontend + brand | Integrated; Welcome → Main Menu → Campaign → Briefing → Gameplay → Result remains the accepted runtime flow. |
| Actor/catalog | 8/8 unique generated actor models, role overrides, sockets and icons; 5,028 total triangles, 800 maximum for one actor. |
| Rig decision | 8/8 use a seven-bone Generic transform rig for this slice. Humanoid migration is explicitly not claimed. |
| Buildings | 6/6 generated Archer/Barracks/Mage L1/L2 entries; 3,088 total triangles, unique catalog GUIDs and required sockets. |
| Automated QA | EditMode 65/65; PlayMode 12/12. Raw XML/log output remains outside version control. |
| Ratio QA | Selected 16:9, 20:9 and 4:3 preparation screenshots under `docs/evidence/runtime/`. |
| Simulator performance | Preparation/normal/dense/boss median 60 FPS, boss p95 17.1 ms and longest continuous under-30 interval 0.10 s; 90-second soak median 60 FPS, p95 17.1 ms, 8.9% post-warm-up memory growth, no low-memory warning, nominal thermal proxy. |
| Physical iPhone | Blocked: attached iPhone 13 is paired/wired with Developer Mode enabled, but `docs/evidence/visual-development/ios-physical-readiness-2026-08-25.json` reports DDI services unavailable and no iOS 26.6 DeviceSupport match. |
| Audio-visual polish | Eight-minute Simulator combat acceptance passed: 22/22 cues, 6/6 music states, 3/3 review levels and 40/40 stress cycles. Human listening/video sign-off is now standardized in `docs/VIS706_708_813_814_HUMAN_REVIEW_GATE_2026-08-25.md`. |
| Release decision | VIS-816 remains blocked; Simulator approval is not a substitute for physical Profiler/Frame Debugger or human speaker/headphone polish review. |

Selected final authoring evidence:

- `docs/evidence/visual-development/actor-icon-64px-review-final-2026-08-25.png`
- `docs/evidence/visual-development/boss-phase-sheet-final-2026-08-25.png`
- `docs/evidence/visual-development/building-readability-final-2026-08-25.png`
- `docs/evidence/visual-development/vis813-accessibility-grayscale-contact-sheet-2026-08-25.png`
- `docs/evidence/visual-development/vis814-before-after-contact-sheet-2026-08-25.png`
- `docs/evidence/runtime/defender-simulator-final-ratio-16x9-20260825.png`
- `docs/evidence/runtime/defender-simulator-final-ratio-20x9-20260825.png`
- `docs/evidence/runtime/defender-simulator-final-ratio-4x3-20260825.png`

## Core Documents

| Area | Evidence |
|---|---|
| Master visual TODO | `docs/VISUAL_DEVELOPMENT_TODO.md` |
| Active remaining handoff | `docs/ACTIVE_REMAINING_TODO_2026-08-17.md` |
| Baseline/evidence index | `docs/evidence/visual-development/baseline-index-2026-08-16.md` |
| Art bible standards | `docs/VIS100_ART_BIBLE_STANDARD_INDEX_2026-08-17.md`, `docs/VISUAL_ART_BIBLE_2026-08-16.md` |
| Provenance template | `docs/ASSET_PROVENANCE_TEMPLATE.md` |
| Catalog validation standard | `docs/CATALOG_VISUAL_VALIDATION_2026-08-16.md` |

## Current P0 QA Evidence

| VIS ID | Status | Evidence |
|---|---|---|
| VIS-801/VIS-802/VIS-803 | Blocked | `docs/VIS800_QUALITY_PROFILE_MATRIX_2026-08-17.md`, `Builds/playmode-results-vis801-803.xml`, `Builds/unity-quality-profile-matrix-checkpoint.log`; matrix is implemented, final profile tuning waits for physical Profiler/Frame Debugger |
| VIS-804 | Blocked | `docs/VIS804_TEXTURE_IMPORT_PROFILE_2026-08-17.md`, `Builds/unity-texture-import-profile-checkpoint.log`, `docs/evidence/visual-development/texture-import-profile-report-2026-08-17.json`; import profile is implemented, final memory delta waits for physical device |
| VIS-805 | Blocked | `docs/VIS805_GEOMETRY_OPTIMIZATION_DECISION_2026-08-25.md`, `docs/evidence/visual-development/vis805-geometry-optimization-decision-2026-08-25.json`; no geometry mutation applied before physical Frame Debugger evidence |
| VIS-806 | Blocked | `docs/VIS806_VFX_QUALITY_PROFILE_2026-08-17.md`, `Builds/unity-vfx-quality-profile-checkpoint.log`, `docs/evidence/visual-development/vfx-quality-profile-report-2026-08-17.json`; config is implemented, final GC/overdraw capture waits for physical device |
| VIS-807 | Blocked | `docs/VIS807_PERFORMANCE_ACCEPTANCE_SIMULATOR_2026-08-17.md`, `Builds/ios-performance-acceptance.log`, `tools/run_ios_performance_acceptance.sh` |
| VIS-807 physical readiness | Blocked | `docs/VIS807_PHYSICAL_READINESS_CHECK_2026-08-25.md`, `docs/evidence/visual-development/ios-physical-readiness-2026-08-25.json`, `tools/check_ios_physical_readiness.py` |
| VIS-221/VIS-222 | Passed | `ArtSource/Blender/Guard.blend`, `Assets/Art/Models/Generated/Guard.fbx`, `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json`, `Builds/unity-actor-catalog-visual-vis221222.log`, `Builds/editmode-results-vis221222.xml` passed 40/40 |
| VIS-213/VIS-223/VIS-233 | Passed | `Assets/Art/GeneratedPresentation/DC_Knight_Actors.overrideController`, `Assets/Art/GeneratedPresentation/DC_Guard_Actors.overrideController`, `Assets/Art/GeneratedPresentation/DC_Raider_Actors.overrideController`, `Builds/unity-actor-animator-override-vis223.log`, `Builds/editmode-results-vis223.xml` passed 40/40, `Builds/playmode-results-vis223.xml` passed 6/6 |
| VIS-224/VIS-507 | Passed | `Builds/unity-actor-vfx-visual-vis224.log`, `Builds/unity-physical-vfx-family-vis224.log`, `Builds/unity-arrow-vfx-family-vis224.log`, `Builds/editmode-results-vis224.xml` passed 40/40 |
| VIS-225 | Passed | `docs/evidence/visual-development/actor-icons-2026-08-17/Guard.png`, `docs/evidence/visual-development/actor-icon-64px-review-2026-08-17.png`, `tools/art_pipeline/render_actor_icon_review_sheet.py`, `Builds/unity-actor-pipeline-index-vis225.log` |
| VIS-226 | Passed | `Builds/editmode-results-vis226.xml` passed 40/40, `Builds/playmode-results-vis226.xml` passed 6/6, `Builds/ios-actor-capture.log` passed with Guard spawn/thrust/block, `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-201018-landscape-upright.png` |
| VIS-241/VIS-242 | Passed | `ArtSource/Blender/Vanguard.blend`, `Assets/Art/Models/Generated/Vanguard.fbx`, `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json`, `Builds/unity-actor-catalog-visual-vis241242.log` passed with `vanguardMaterialWarnings=0` |
| VIS-243 | Passed | `Assets/Art/GeneratedPresentation/DC_Vanguard_Actors.overrideController`, `Builds/unity-actor-animator-override-vis243.log`, `Builds/unity-actor-animation-visual-vis243.log`, `Builds/editmode-results-vis243.xml` passed 40/40, `Builds/playmode-results-vis243.xml` passed 6/6 |
| VIS-244/VIS-506 | Passed | `Builds/unity-actor-vfx-visual-vis244.log`, `Builds/unity-physical-vfx-family-vis244.log`, `Builds/editmode-results-vis244.xml` passed 40/40, `Builds/playmode-results-vis244.xml` passed 6/6 |
| VIS-245 | Passed | `docs/evidence/visual-development/actor-icons-2026-08-17/Vanguard.png`, `docs/evidence/visual-development/actor-icon-64px-review-2026-08-17.png`, `tools/art_pipeline/render_actor_icon_review_sheet.py`, `Builds/unity-actor-pipeline-index-vis245.log` |
| VIS-246 | Passed | `Builds/editmode-results-vis246.xml` passed 40/40, `Builds/playmode-results-vis246.xml` passed 6/6, `Builds/ios-actor-capture.log`, `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-203449-landscape-upright.png` |
| VIS-251/VIS-252 | Passed | `ArtSource/Blender/Regenerator.blend`, `Assets/Art/Models/Generated/Regenerator.fbx`, `docs/evidence/visual-development/regenerator-turnaround-2026-08-17.png`, `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json`, `Builds/unity-actor-catalog-visual-vis251252.log` passed with `regeneratorMaterialWarnings=0`, `Builds/editmode-results-vis251252.xml` passed 40/40 |
| VIS-253 | Passed | `Assets/Art/GeneratedPresentation/DC_Regenerator_Actors.overrideController`, `Builds/unity-actor-animator-override-vis253.log`, `Builds/unity-actor-animation-visual-vis253.log`, `Builds/editmode-results-vis253.xml` passed 40/40, `Builds/playmode-results-vis253.xml` passed 6/6 |
| VIS-254/VIS-509/VIS-264 | Passed | `Builds/unity-actor-vfx-visual-vis254.log`, `Builds/unity-heal-regen-vfx-family-vis264.log`, `Builds/editmode-results-vis264.xml` passed 40/40, `Builds/playmode-results-vis264.xml` passed 6/6 |
| VIS-255 | Passed | `docs/evidence/visual-development/actor-icons-2026-08-17/Regenerator.png`, `docs/evidence/visual-development/actor-icon-64px-review-2026-08-17.png`, `tools/art_pipeline/render_actor_icon_review_sheet.py`, `Builds/unity-actor-pipeline-index-vis255.log` |
| VIS-256 | Passed | `Builds/editmode-results-vis256.xml` passed 40/40, `Builds/playmode-results-vis256.xml` passed 6/6, `Builds/ios-actor-capture.log`, `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-205613-landscape-upright.png` |
| VIS-261/VIS-262 | Passed | `ArtSource/Blender/Support.blend`, `Assets/Art/Models/Generated/Support.fbx`, `docs/evidence/visual-development/support-turnaround-2026-08-17.png`, `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json`, `Builds/unity-actor-catalog-visual-vis261262.log` passed with `supportMaterialWarnings=0`, `Builds/editmode-results-vis261262.xml` passed 40/40 |
| VIS-263 | Passed | `Assets/Art/GeneratedPresentation/DC_Support_Actors.overrideController`, `Builds/unity-actor-animator-override-vis263.log`, `Builds/unity-actor-animation-visual-vis263.log`, `Builds/editmode-results-vis263.xml` passed 40/40, `Builds/playmode-results-vis263.xml` passed 6/6 |
| VIS-265 | Passed | `docs/evidence/visual-development/actor-icons-2026-08-17/Support.png`, `docs/evidence/visual-development/actor-icon-64px-review-2026-08-17.png`, `tools/art_pipeline/render_actor_icon_review_sheet.py`, `Builds/unity-actor-pipeline-index-vis265.log` |
| VIS-271/VIS-272 | Passed | `ArtSource/Blender/Siege.blend`, `Assets/Art/Models/Generated/Siege.fbx`, `docs/evidence/visual-development/siege-turnaround-2026-08-17.png`, `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json`, `Builds/unity-actor-catalog-visual-vis271272.log` passed with `siegeMaterialWarnings=0`, `Builds/editmode-results-vis271272.xml` passed 40/40 |
| VIS-273 | Passed | `Assets/Art/GeneratedPresentation/DC_Siege_Actors.overrideController`, `Builds/unity-build-presentation-vis273.log`, `Builds/unity-actor-animator-override-vis273.log` passed with `roleOverrides=7 sharedFallback=1 siegeOverride=true`, `Builds/unity-actor-animation-visual-vis273.log` passed with `siegeRecoil=Hit`, `Builds/editmode-results-vis273.xml` passed 40/40, `Builds/playmode-results-vis273.xml` passed 6/6 |
| VIS-274 | Passed | `Assets/Scripts/Presentation/DefenderVfxPool.cs`, `Builds/unity-actor-vfx-visual-vis274.log` passed with `siegeImpact=DarkDiamond/Socket_Muzzle`, `Builds/unity-physical-vfx-family-vis274.log` passed with `siegeHeavy=Diamond/Muzzle`, `Builds/unity-action-timing-vis274.log` passed with `siegeRecoil=Hit@0.1/Muzzle`, `Builds/editmode-results-vis274.xml` passed 40/40, `Builds/playmode-results-vis274.xml` passed 6/6 |
| VIS-275 | Passed | `docs/evidence/visual-development/actor-icons-2026-08-17/Siege.png`, `docs/evidence/visual-development/actor-icon-64px-review-2026-08-17.png`, `tools/art_pipeline/render_actor_icon_set.py`, `tools/art_pipeline/render_actor_icon_review_sheet.py`, `Builds/unity-actor-pipeline-index-vis275.log` passed with `icons=8 contactShadow=Off/receiveFalse catalogActors=8` |
| VIS-276 | Passed | `Assets/Scripts/Core/GameSessionController.cs`, `Builds/editmode-results-vis276.xml` passed 40/40, `Builds/playmode-results-vis276.xml` passed 6/6, `Builds/ios-actor-capture.log` passed with `siege_spawn=true siege_heavy=true`, `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-213948-landscape-upright.png` |
| VIS-266 | Passed | `Builds/editmode-results-vis266.xml` passed 40/40, `Builds/playmode-results-vis266.xml` passed 6/6, `Builds/ios-actor-capture.log`, `docs/evidence/runtime/defender-simulator-actor-runtime-vfx-animation-20260817-211833-landscape-upright.png` |
| VIS-701–705 | Passed | `docs/VIS700_AUDIO_VISUAL_POLISH_ACCEPTANCE_2026-08-25.md`, `docs/evidence/runtime/audio-visual-acceptance-20260825.txt`; 480-second run passed 22/22 cues and 40/40 combat cycles. |
| VIS-706 | Blocked | Procedural wind/leaf/crystal layer integrated; final-source 60-second Medium smoke played 4 details and Low-off profile test passed. Human masking/loudness review is prepared in `docs/VIS706_708_813_814_HUMAN_REVIEW_GATE_2026-08-25.md`. |
| VIS-707/VIS-708 | Blocked | Automated matrix/full-run passed; human audio-only/visual-only, speaker/headphone and final video sign-off are prepared in `docs/VIS706_708_813_814_HUMAN_REVIEW_GATE_2026-08-25.md`. |
| VIS-810 | Passed | Current integrated EditMode suite passed 65/65; raw XML intentionally remains ignored. |
| VIS-811 | Passed | Current integrated PlayMode suite passed 12/12, including audio voice-budget and result layout regressions; raw XML intentionally remains ignored. |
| VIS-812 | Passed on Simulator | `docs/VIS812_FINAL_VISUAL_EVIDENCE_SIMULATOR_2026-08-17.md`, accepted stamp `vis812-final-20260825-210559`, `Builds/ios-vis812-final-evidence.log`, `Builds/ios-vis812-final-evidence-defeat.log` |
| VIS-813/VIS-814 | Blocked | `docs/VIS813_814_ACCESSIBILITY_COMPARISON_2026-08-17.md`, `docs/evidence/visual-development/vis813-accessibility-grayscale-contact-sheet-2026-08-25.png`, `docs/evidence/visual-development/vis814-before-after-contact-sheet-2026-08-25.png`, `docs/evidence/visual-development/vis813-814-visual-audit-report-2026-08-25.json`, `docs/VIS706_708_813_814_HUMAN_REVIEW_GATE_2026-08-25.md`; automated evidence is current, human/assistive-tech sign-off is missing |

## Accepted Runtime Visual Set

Accepted stamp: `vis812-final-20260825-210559`

| State | Evidence |
|---|---|
| Preparation | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-preparation-landscape-upright.png` |
| Ability | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-ability-landscape-upright.png` |
| Normal wave | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-normal-landscape-upright.png` |
| Dense wave | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-dense-landscape-upright.png` |
| Boss phase 1 | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-boss1-landscape-upright.png` |
| Boss phase 2 | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-boss2-landscape-upright.png` |
| Boss phase 3 | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-boss3-landscape-upright.png` |
| Victory | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-victory-landscape-upright.png` |
| Defeat | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-defeat-landscape-upright.png` |

## Reproduction Commands

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/emreaydin/DefenderChronicles -runTests -testPlatform EditMode -testResults /Users/emreaydin/DefenderChronicles/Builds/editmode-results-vis810.xml -logFile /Users/emreaydin/DefenderChronicles/Builds/editmode-vis810.log
```

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/emreaydin/DefenderChronicles -runTests -testPlatform PlayMode -testResults /Users/emreaydin/DefenderChronicles/Builds/playmode-results-vis811.xml -logFile /Users/emreaydin/DefenderChronicles/Builds/playmode-vis811.log -burst-disable-compilation
```

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/emreaydin/DefenderChronicles -runTests -testPlatform EditMode -testResults /Users/emreaydin/DefenderChronicles/Builds/editmode-results-vis223.xml -logFile /Users/emreaydin/DefenderChronicles/Builds/editmode-vis223.log
```

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/emreaydin/DefenderChronicles -runTests -testPlatform PlayMode -testResults /Users/emreaydin/DefenderChronicles/Builds/playmode-results-vis223.xml -logFile /Users/emreaydin/DefenderChronicles/Builds/playmode-vis223.log -burst-disable-compilation
```

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/emreaydin/DefenderChronicles -runTests -testPlatform EditMode -testResults /Users/emreaydin/DefenderChronicles/Builds/editmode-results-vis226.xml -logFile /Users/emreaydin/DefenderChronicles/Builds/editmode-vis226.log
```

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/emreaydin/DefenderChronicles -runTests -testPlatform PlayMode -testResults /Users/emreaydin/DefenderChronicles/Builds/playmode-results-vis226.xml -logFile /Users/emreaydin/DefenderChronicles/Builds/playmode-vis226.log -burst-disable-compilation
```

```bash
tools/run_ios_actor_capture.sh
```

```bash
tools/run_ios_vis812_capture.sh
```

```bash
python3 tools/art_pipeline/render_vis813_814_evidence.py
```

## Remaining Release-Risk Items

| Item | Why It Remains Open |
|---|---|
| VIS-006/VIS-007/VIS-807 physical-device profiling | Simulator metrics passed, but final iPhone 13-class Profiler, Frame Debugger and Rendering Debugger evidence are still missing. |
| VIS-706 environment ambience | Runtime layer and quality controls passed; subjective masking/loudness remains part of the human audio polish review gate. |
| VIS-707/VIS-708 human polish sign-off | Eight-minute automation passed, but speaker/headphone fatigue, loudness, pop/click and audio-only/visual-only video review need a human listener through `docs/evidence/visual-development/human-review-signoff-template-2026-08-25.json`. |
| Real assistive-tech review | VIS-813 final 25 August visual/grayscale evidence is current, but VoiceOver or assistive-tech behavior was not validated. |
| Role-specific authored asset polish | Knight/Guard/Raider generated override clips now exist and pass regression, but final hand-authored motion polish and many actor/building P1/P2 production tasks remain open. |

## Handoff Decision

The Simulator visual evidence path is now reproducible and indexed. Do not mark the art vertical slice fully complete until the physical-device performance/debugger gate is captured or explicitly waived.
