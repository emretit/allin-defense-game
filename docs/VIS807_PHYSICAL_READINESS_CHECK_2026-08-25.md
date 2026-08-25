# VIS-807 Physical iOS Readiness Check

Date: 2026-08-25

Status: Blocked before install/profiling

Readiness report: `docs/evidence/visual-development/ios-physical-readiness-2026-08-25.json`

## Current Result

The iPhone 13-class device is visible to CoreDevice and has Developer Mode enabled. CoreDevice has a usable iOS DDI, but the phone is currently visible over local network instead of wired USB and the DDI mount probe reports the device as locked. It is still not ready for the physical Profiler, Frame Debugger or Rendering Debugger acceptance pass.

| Check | Result |
|---|---|
| Xcode | Xcode 26.6, build 17F113 |
| Device | iPhone 13 (`iPhone14,5`) |
| iOS | 26.6 beta (`23G5028e`) |
| Pairing | paired |
| Transport | localNetwork |
| Developer Mode | enabled |
| DDI services | unavailable |
| Preferred iOS DDI | usable, build 17F113 |
| DDI mount probe | blocked by device locked |

## Blocker

The physical acceptance gate is blocked by device connection/lock state, not by the game project:

- `ddiServicesAvailable` is `false`.
- CoreDevice reports a usable preferred iOS DDI at `/Library/Developer/DeveloperDiskImages/iOS_DDI/`.
- The direct `ddiServices` probe attempted to mount the DDI and failed because the device is locked.
- `devicectl` currently sees the phone over `localNetwork`, not wired USB.

## Next Gate

Connect the phone over USB, unlock it, keep the screen awake, accept any trust/developer prompts, then rerun:

```sh
python3 tools/check_ios_physical_readiness.py
```

Only when that report returns ready should the team run the physical iPhone Profiler, Frame/Rendering Debugger, 60-actor VFX stress and final 90-second soak gates.
