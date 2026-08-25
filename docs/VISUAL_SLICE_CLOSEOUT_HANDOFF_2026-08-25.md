# Defender Chronicles — Visual Slice Closeout Handoff

Date: 2026-08-25
Branch: `codex/visual-slice-closeout`
Release gate: Blocked, not failed

## Delivered

- Premium medieval runtime frontend and brand flow retained from Welcome through Result.
- Eight unique low-poly actor roles, role materials, sockets, icons, animation overrides and pooled VFX bindings.
- Boss uses a unique model with three phase-specific armour/core/telegraph states; it is not a scaled Raider.
- Six generated building appearances for Archer, Barracks and Mage L1/L2 are production catalog sources.
- Greenwatch Valley road, entrance, keep, slot, boundary, atmosphere and HUD framing polish is integrated.
- Responsive HUD/input tests cover 16:9, 20:9 and 4:3 without changing the eight build slots or gameplay route.
- iOS performance, soak, state capture and ratio capture workflows are repeatable from `tools/`.
- The 22-cue audio policy, critical-event ducking, ten-voice budget, six music transitions and Silent/Low/Normal acceptance runner are integrated.

## Verified

| Check | Result |
|---|---|
| Unity EditMode | 65/65 passed |
| Unity PlayMode | 12/12 passed |
| Actor catalog | 8/8 unique, 5,028 triangles total, no required socket gaps |
| Building catalog | 6/6 unique, 3,088 triangles total, no required socket gaps |
| Simulator four-scenario run | 60 FPS median, boss p95 17.1 ms, longest continuous under-30 interval 0.10 s, no low-memory warning |
| Simulator 90-second soak | 60 FPS median, 17.1 ms p95, 8.9% post-warm-up memory growth, no low-memory warning |
| Simulator 8-minute audio-visual combat | 22/22 cues, 6/6 music states, 3/3 review levels, 40/40 combat cycles, pause snapshot passed, 10/10 peak SFX voices |
| Final-source ambience smoke | Medium enabled, 4 wind/leaf/crystal details; Low-off/Medium/High profile contract passed |
| Safe-area ratios | 16:9, 20:9 and 4:3 selected evidence visually inspected |

Raw builds, XML, profiler logs and crash output stay under ignored `Builds/` or `TestResults/`. Curated evidence is under `docs/evidence/`.

## Rig Decision

The current actor library uses seven-bone Generic transform rigs with visible rigid parts parented to bones. Role override clips contain bone curves and keep the existing trigger contract. This is the accepted architecture for the current modular low-poly vertical slice. The original Humanoid-retarget idea is not reported as completed; it becomes a later upgrade only if a fully skinned source library is introduced.

## External Asset Decision

KayKit Medieval Hexagon/Adventurers and Kenney Fantasy UI provenance remains recorded in `Assets/ThirdParty/ART_ASSET_SOURCES.md`. Quaternius was researched but intentionally not imported, so there is no invented archive hash or unused third-party payload. New generated/original models are project-owned derivatives built on the documented CC0/art-direction rules.

## Open Gates

1. Physical iPhone Profiler, Frame Debugger and Rendering Debugger capture. The connected iPhone 13 runs iOS 26.6 beta, but installed Xcode supplies iOS 26.5 DeviceSupport/DDI; developer disk image mount fails before installation.
2. Physical-device 60-actor/VFX stress and final 90-second soak.
3. Human speaker/headphone listening, audio-only/visual-only and final video sign-off. The automated Silent/Low/Normal matrix and eight-minute combat run passed.
4. Optional human blind-recognition sign-off for the eight actor silhouettes; automated color/grayscale/64 px evidence exists.

Do not close VIS-816 until items 1–3 pass or the product owner explicitly waives them. Simulator FPS must not be presented as physical GPU or thermal acceptance.

## Reproduction

```sh
tools/build_and_run_ios_simulator.sh
DC_SIMULATOR_ID=<iphone-17-pro-udid> DC_REUSE_INSTALLED_BUILD=1 tools/run_ios_performance_acceptance.sh
DC_SIMULATOR_ID=<iphone-17-pro-udid> tools/run_ios_soak_acceptance.sh
DC_SIMULATOR_ID=<iphone-17-pro-udid> DC_REUSE_INSTALLED_BUILD=1 tools/run_ios_vis812_capture.sh
DC_SIMULATOR_ID=<iphone-17-pro-udid> DC_REUSE_INSTALLED_BUILD=1 DC_AUDIO_ACCEPTANCE_SECONDS=480 tools/run_ios_audio_visual_acceptance.sh
tools/run_ios_ratio_acceptance.sh
```

For physical acceptance, install an Xcode/DeviceSupport build compatible with iOS 26.6 beta, then run:

```sh
DC_IOS_TEAM_ID=YL5HQCW4RY DC_IOS_PERFORMANCE_ACCEPTANCE=1 tools/build_and_run_ios_device.sh
```
