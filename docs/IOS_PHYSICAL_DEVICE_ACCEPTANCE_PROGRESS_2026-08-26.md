# Defender Chronicles — iOS Physical Device Acceptance Progress

Date: 2026-08-26
Branch: `codex/visual-slice-closeout`

## Outcome

The connected iPhone 13-class device is now usable for physical acceptance work. The app installed, launched and completed the automated smoke, performance and 90-second soak checks.

This does not close `VIS-816` yet. The final gate still requires manual Unity Profiler, Frame/Rendering Debugger, 60-actor overdraw/stress evidence and human audio/accessibility sign-off.

## Device readiness

- Evidence: `docs/evidence/visual-development/ios-physical-readiness-2026-08-26.json`
- Result: `[DC_IOS_PHYSICAL_READY]`
- Device class: physical iPhone 13-class, wired USB, paired, Developer Mode enabled.
- DDI probe: available.
- Sensitive identifiers are redacted from the committed evidence.

## Build/signing correction

The first automated device build selected the Apple Development certificate team id, but the usable local provisioning profile for the bundle id was under a different team. `tools/build_and_run_ios_device.sh` now:

- selects a valid matching provisioning-profile team for `com.emreaydin.defenderchronicles` before falling back to the first Apple Development identity;
- accepts a wired, paired, Developer Mode enabled iPhone as the target device;
- defaults `DC_IOS_CONNECT_PROFILER=0` for console automation so automated device runs do not inherit Profiler connection overhead;
- writes separate mode logs under `Builds/` for smoke, defeat smoke, performance and soak.

`Assets/Editor/IosFirstBuildSettings.cs` now only enables Unity `ConnectWithProfiler` when `DC_IOS_CONNECT_PROFILER=1`.

## Physical run results

| Run | Result | Key observation |
|---|---|---|
| Install + smoke | Passed | `[DC_SMOKE_PASS] Build, upgrade, hero, pause and all seven waves completed with victory.` |
| Automated performance | Passed | `[DC_PERFORMANCE_ACCEPTANCE_PASS] scenarios=preparation/normal/dense/boss median_fps=60.1 p95_frame_ms=16.9` |
| 90-second soak | Passed | `[DC_SOAK_ACCEPTANCE_PASS] cycles=9 duration_seconds=90 memory_growth_pct=7.1` |
| EditMode regression | Passed | `67/67` |
| PlayMode regression | Passed | `12/12` |

Representative physical metrics from the automated console run:

| Stage | Median FPS | p95 frame | Peak memory | Thermal | Low-memory warning |
|---|---:|---:|---:|---|---|
| Preparation | 60.1 | 16.8 ms | ~609 MB | fair | false |
| Normal | 60.1 | 16.8 ms | ~821 MB | fair | false |
| Dense | 60.1 | 16.9 ms | ~949 MB | fair | false |
| Boss | 60.1 | 16.9 ms | ~1015 MB | fair | false |
| Soak final | 60.1 | 21.1 ms | ~1089 MB | serious | false |

## Remaining evidence before closing VIS-816

`python3 tools/validate_visual_device_acceptance.py docs/evidence/visual-development/physical-device-acceptance-template-2026-08-25.json` still returns `[DC_VISUAL_DEVICE_ACCEPTANCE_PENDING]`, as expected. The missing fields are the manual Profiler, Frame/Rendering Debugger and 60-actor stress measurements below.

1. Capture at least 60 seconds per scenario in Unity Profiler:
   - preparation
   - normal combat
   - dense combat
   - boss phase 3
2. Record CPU/GPU frame time, draw calls, triangles, GC allocation, memory and thermal notes.
3. Capture Unity Frame/Rendering Debugger evidence:
   - top draw passes
   - shadow casters
   - additional lights
   - overdraw
   - texture usage
4. Record physical 60-actor/VFX stress evidence and connect it to `VIS-292` / `VIS-515`.
5. Fill and validate the physical device acceptance JSON with `tools/validate_visual_device_acceptance.py`.
6. Complete human audio/accessibility sign-off with `tools/validate_visual_human_signoff.py`.
