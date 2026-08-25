# VIS-812 Final Visual Evidence - iOS Simulator

Date: 2026-08-17; accepted set refreshed 2026-08-25

Status: Passed on iOS Simulator

Scope: VIS-812 final labelled PNG set from one accepted iOS Simulator build/profile. This is visual acceptance evidence for the current Simulator build; physical iPhone 13-class Profiler evidence remains under VIS-807.

## Automation

Run:

```bash
tools/run_ios_vis812_capture.sh
```

Evidence logs:

- `Builds/ios-vis812-final-evidence.log`
- `Builds/ios-vis812-final-evidence-defeat.log`

Pass markers:

- `[DC_VIS812_CAPTURE_PASS] states=preparation/ability/normal/dense/boss1/boss2/boss3/victory`
- `[DC_VIS812_CAPTURE_DEFEAT_PASS] state=defeat`

All accepted readable captures below are 2622 x 1206 landscape PNGs from one installed iPhone 17 Pro Simulator build, stamp `vis812-final-20260825-210559`.

## Captures

| State | Phase/log evidence | Accepted PNG |
|---|---|---|
| Preparation | `state=preparation phase=Preparing wave=0/7 gold=200 lives=20 enemies=0` | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-preparation-landscape-upright.png` |
| Ability | `state=ability phase=Preparing wave=0/7 gold=1440 lives=20 enemies=0` | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-ability-landscape-upright.png` |
| Normal wave | `state=normal phase=Wave wave=0/7 gold=1440 lives=20 enemies=8` | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-normal-landscape-upright.png` |
| Dense wave | `state=dense phase=Wave wave=0/7 gold=1530 lives=20 enemies=37` | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-dense-landscape-upright.png` |
| Boss phase 1 | `state=boss1 phase=Wave wave=0/7 gold=1772 lives=20 enemies=12` | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-boss1-landscape-upright.png` |
| Boss phase 2 | `state=boss2 phase=Wave wave=0/7 gold=1838 lives=20 enemies=6` | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-boss2-landscape-upright.png` |
| Boss phase 3 | `state=boss3 phase=Wave wave=0/7 gold=1942 lives=20 enemies=2` | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-boss3-landscape-upright.png` |
| Victory | `state=victory phase=Victory wave=7/7 gold=2453 lives=20 enemies=0` | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-victory-landscape-upright.png` |
| Defeat | `state=defeat phase=Defeat wave=0/7 gold=1440 lives=0 enemies=0` | `docs/evidence/runtime/defender-simulator-vis812-final-20260825-210559-defeat-landscape-upright.png` |

## Review Notes

- Victory and defeat overlays were visually inspected after capture and both show the expected result card over the battlefield.
- Preparation is a clean pre-wave opening state. Ability, normal, dense and boss captures use a deterministic upgraded-defense setup from the same installed build.
- Earlier 25 August capture attempts were discarded when slide transitions clipped labels or the result description overlapped the retry action. The accepted `210559` set includes the layout fix, per-state settling, heartbeat logs and script-managed termination.
