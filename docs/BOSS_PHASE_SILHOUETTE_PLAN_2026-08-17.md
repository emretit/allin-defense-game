# Boss Phase Silhouette Plan - 2026-08-17

## Status

VIS-280 is ready for visual review from the current generated Boss proxy plus an authored three-phase planning overlay. This is not the final Boss runtime model; it defines the readable phase language that VIS-281-VIS-286 should preserve.

## Evidence

| Item | Evidence | Result |
|---|---|---|
| Phase sheet | `docs/evidence/visual-development/boss-phase-sheet-2026-08-17.png` | Passed for phase silhouette and telegraph review |
| Render script | `tools/art_pipeline/render_boss_phase_sheet.py` | Re-runnable with Blender 5.1.2 |
| Boss proxy | `Assets/Art/Models/Generated/Boss.fbx` | Used as the current review body |

## Phase Language

| Phase | Visual change | Gameplay read |
|---|---|---|
| Phase 1 | Warm core wake and small ground telegraph | Boss is active but still close to normal enemy pressure |
| Phase 2 | Brighter shoulder shards and larger ground ring | Phase break is visible before the next pressure step |
| Phase 3 | Largest hot shards, brightest core and widest telegraph | Critical threat is visible without relying on color alone |

## Review Notes

- The Boss keeps a wider, heavier body mass than Raider and Vanguard proxies.
- The telegraph ring grows by phase, so threat escalation is visible in shape and scale.
- Hot phase parts are intentionally placed high on the silhouette to remain readable behind crowds.
- Final VIS-281 work should replace the proxy body with original phaseable geometry and avoid sharing Raider mesh pieces.

## Remaining Before Boss Acceptance

- VIS-281 must produce the final editable Boss body and controlled phase pieces.
- VIS-282 must bind final black steel/hot core material behavior without material instance leaks.
- VIS-283-VIS-286 must verify animation, VFX, boss UI/world feedback, catalog binding and runtime three-phase capture.
