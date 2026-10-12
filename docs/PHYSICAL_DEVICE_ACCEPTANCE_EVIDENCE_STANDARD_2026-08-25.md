# ALLIN: Defense - Physical Device Acceptance Evidence Standard

Date: 2026-08-25
Related TODO: `NEXT-003`, `NEXT-004`, `NEXT-005`, `NEXT-101`, `NEXT-102`, `NEXT-103`

This standard prevents the remaining physical-device VIS rows from being closed with partial evidence.

## Validator

```bash
python3 tools/validate_visual_device_acceptance.py docs/evidence/visual-development/physical-device-acceptance-2026-08-25.json
```

The template can be checked in pending mode:

```bash
python3 tools/validate_visual_device_acceptance.py docs/evidence/visual-development/physical-device-acceptance-template-2026-08-25.json --allow-pending
```

## Required evidence

The final physical acceptance JSON must include:

- accepted build id, device class, model, iOS version, capture date and reviewer;
- four 60-second Profiler scenarios: preparation, normal combat, dense wave and boss phase 3;
- FPS, CPU/GPU frame time, draw calls, triangle count, GC allocation, memory and thermal state for every scenario;
- Frame/Rendering Debugger notes for top render costs, shadow casters, additional lights, overdraw and texture use;
- curated Frame Debugger screenshots under `docs/evidence`;
- 60 actor/VFX stress and 90-second soak evidence;
- explicit false values for persistent GC spike, low-memory warning and control loss.

## Acceptance floors

| Gate | Minimum |
|---|---|
| Preparation/normal/dense median FPS | 55 FPS or higher |
| Boss phase 3 floor | 30 FPS or higher |
| Scenario duration | At least 60 seconds each |
| Soak duration | At least 90 seconds |
| Actor stress count | At least 60 actors |

The release gate remains blocked if this validator does not print `[DC_VISUAL_DEVICE_ACCEPTANCE_PASS]`.
