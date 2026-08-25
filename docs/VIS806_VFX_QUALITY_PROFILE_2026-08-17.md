# VIS-806 VFX Quality Profile

Date: 2026-08-17

## Scope

VIS-806 moves VFX pool sizing, total playing cap, boss-impact concurrency and boss-telegraph concurrency into a quality-based runtime config.

## Runtime Config

| Quality | Prewarm pool | Max playing | BossImpact cap | BossTelegraph cap | Transparent budget |
|---|---:|---:|---:|---:|---:|
| Low | 72 | 72 | 2 | 1 | 82 |
| Medium | 96 | 96 | 3 | 2 | 104 |
| High | 128 | 128 | 3 | 2 | 120 |

`DefenderVfxPool.InitializeForCurrentQuality()` reads the active Unity quality profile and applies the matching VFX config. The pool also prewarms render components and material property blocks when items are created, so dense combat playback does not need to build VFX components on first use.

## Batch Evidence

- Checkpoint log: `Builds/unity-vfx-quality-profile-checkpoint.log`
- Machine report: `docs/evidence/visual-development/vfx-quality-profile-report-2026-08-17.json`
- Passing line: `[DC_VFX_QUALITY_PROFILE_CHECKPOINT] passed low=pool72/play72/boss2/1/budget79.484/82 medium=pool96/play96/boss3/2/budget101.788/104 high=pool128/play101/boss3/2/budget108.184/120 growth=0 criticalPreserved=true`

## Acceptance Status

Batch proxy acceptance is in review for VIS-806:

- Low/Medium/High pool sizes are profile-specific.
- Dense boss/status/projectile VFX does not grow the pool.
- Low reduces boss VFX concurrency to preserve mobile budget.
- Critical boss telegraph remains preserved.
- Final device Profiler and GC spike capture remain under VIS-807.
