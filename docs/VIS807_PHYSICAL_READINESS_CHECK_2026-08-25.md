# VIS-807 Physical iOS Readiness Check

Date: 2026-08-25

Status: Blocked before install/profiling

Readiness report: `docs/evidence/visual-development/ios-physical-readiness-2026-08-25.json`

## Current Result

The iPhone 13-class device is visible to CoreDevice, paired over wired transport and has Developer Mode enabled. It is still not ready for the physical Profiler, Frame Debugger or Rendering Debugger acceptance pass.

| Check | Result |
|---|---|
| Xcode | Xcode 26.6, build 17F113 |
| Device | iPhone 13 (`iPhone14,5`) |
| iOS | 26.6 beta (`23G5028e`) |
| Pairing | paired |
| Transport | wired |
| Developer Mode | enabled |
| DDI services | unavailable |
| DeviceSupport match | iOS 26.6 not present |

## Blocker

The physical acceptance gate is blocked by device/toolchain support, not by the game project:

- `ddiServicesAvailable` is `false`.
- The visible Xcode DeviceSupport set does not include iOS 26.6.
- `xctrace` sees the iPhone 13 in the physical device sections, but profiling cannot be treated as ready until DDI services are available.

## Next Gate

After installing an Xcode/DeviceSupport/DDI build compatible with iOS 26.6 beta, or moving the phone to a supported iOS version, rerun:

```sh
python3 tools/check_ios_physical_readiness.py
```

Only when that report returns ready should the team run the physical iPhone Profiler, Frame/Rendering Debugger, 60-actor VFX stress and final 90-second soak gates.
