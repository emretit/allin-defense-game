# VIS-807 Performance Acceptance Simulator Run

Date: 2026-08-17; final Simulator rerun 2026-08-25

## Scope

VIS-807 requires final iPhone 13-class performance profiling for preparation, normal combat, dense combat and boss combat. This pass adds the repeatable simulator acceptance run and captures the same four scenario labels from a Development iOS Simulator build.

This is not the final physical-device Profiler capture. It is the automated pre-device gate used to verify that the instrumentation, scenario setup and log parsing are ready.

## Automation

- Runtime probe: `DeviceAcceptanceProbe`
- Scenario runner: `DC_PERFORMANCE_ACCEPTANCE=1`
- Script: `tools/run_ios_performance_acceptance.sh`
- Log: `Builds/ios-performance-acceptance.log`

## Captured Metrics

| Scenario | Median FPS | P95 frame | Peak memory | Max under 30 FPS | Thermal | Low memory |
|---|---:|---:|---:|---:|---|---|
| Preparation | 60.0 | 16.7 ms | 580.8 MB | 0.00 s | nominal | False |
| Normal | 60.0 | 17.2 ms | 795.5 MB | 0.10 s | nominal | False |
| Dense | 60.0 | 17.1 ms | 924.5 MB | 0.10 s | nominal | False |
| Boss | 60.0 | 17.1 ms | 991.3 MB | 0.10 s | nominal | False |

Passing line:

`[DC_PERFORMANCE_ACCEPTANCE_PASS] scenarios=preparation/normal/dense/boss median_fps=60.0 p95_frame_ms=17.1`

The same accepted Simulator app also passed the 90-second dense soak: 9 cycles, 60 FPS median, 17.1 ms p95, 8.9% post-warm-up memory growth, no low-memory warning and nominal thermal proxy.

## Acceptance Status

VIS-807 is in progress:

- The four required performance scenarios are now automated and labeled.
- The Development build emits `[DC_DEVICE_METRICS]` rows for each scenario.
- Simulator proxy metrics pass the 60 FPS/30 FPS smoke thresholds.
- Final iPhone 13-class device Profiler capture and device memory confirmation remain open.
