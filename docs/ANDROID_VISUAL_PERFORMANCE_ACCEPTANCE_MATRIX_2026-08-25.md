# ALLIN: Defense - Android Visual/Performance Acceptance Matrix

Date: 2026-08-25
Related TODO: `NEXT-205`

This matrix prepares Android acceptance after the iOS visual-slice closeout. It does not replace `VIS-816`, which remains iPhone physical-device gated.

## Scope

Android acceptance should verify that the existing Unity 6 + URP + uGUI vertical slice keeps the same visual hierarchy, touch behavior and performance budgets outside the iOS-first release gate.

Out of scope:

- New Android-only gameplay mechanics
- Store listing art
- Android monetization, billing or notification flows
- Replacing the accepted runtime frontend architecture

## Device matrix

| Tier | Target class | Screen/aspect focus | Purpose |
|---|---|---|---|
| Low | 4-6 GB RAM mid-range Android, Mali/Adreno class GPU | 19.5:9 to 20:9 | Verify Low profile readability and no memory/control failure |
| Medium | Recent mainstream Android phone | 20:9 | Verify default quality profile and sustained combat |
| Tablet | 10-11 inch Android tablet or foldable/tablet emulator | 16:10 or 4:3-ish large canvas | Verify HUD does not unnecessarily shrink battlefield |

## Scenario matrix

| Scenario | Required evidence | Acceptance |
|---|---|---|
| First launch/frontend | Screen recording or screenshots through Welcome → Main Menu → Campaign → Briefing | Safe area, buttons, readable typography and no clipped controls |
| Preparation | Screenshot + frame metrics | Build slots and route hierarchy are readable; target 60 FPS on Medium |
| Normal combat | 60-second profile note | No persistent GC spikes; combat VFX readable without covering path |
| Dense wave | 60-second profile note | Actor silhouettes remain readable; no low-memory warning |
| Boss phase 3 | 60-second profile note + screenshot | Boss telegraph and phase VFX remain distinct; floor should not stay below 30 FPS |
| Victory/defeat | Screenshot or short clip | Result UI respects safe area and does not hide final battlefield state |

## Quality-profile checks

| Profile | Android rule |
|---|---|
| Low | Disable expensive shadow paths and cap concurrent VFX first |
| Medium | Keep mobile-friendly lighting; additional point-light shadows remain off |
| High | Allow stronger polish only if frame pacing remains stable on the Medium target |

## Evidence package

Curated Android evidence should be stored under:

```text
docs/evidence/android-acceptance/
```

Recommended files:

- `android-device-matrix-YYYY-MM-DD.json`
- `android-frontend-flow-YYYY-MM-DD.png`
- `android-preparation-YYYY-MM-DD.png`
- `android-normal-combat-profile-YYYY-MM-DD.md`
- `android-dense-wave-profile-YYYY-MM-DD.md`
- `android-boss3-profile-YYYY-MM-DD.md`
- `android-result-screens-YYYY-MM-DD.png`

Do not commit APK/AAB builds, raw profiler dumps, crash dumps or generated Gradle cache files.

## Closeout rule

`NEXT-205` can be considered prepared when this matrix is linked from the next TODO and Android work can start without changing the iOS visual-slice release definition.
