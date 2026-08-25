# M1 Style Proof Board - 2026-08-17

## Status

VIS-113 is accepted as the M1 style proof baseline. The shared board was rendered from the generated FBX candidates with one camera, one lighting setup and fixed review colors mapped from the project material families. The 25 August closeout evidence supersedes the original proxy caveats: Knight/Raider runtime catalog work, final icon review and gameplay-camera VIS-812 captures now exist, so this board is no longer an open blocker for vertical-slice asset production.

## Evidence

| Evidence | Path | Result |
|---|---|---|
| M1 style proof board | `docs/evidence/visual-development/m1-style-proof-board-2026-08-17.png` | Passed for same-camera/same-light visual review |
| Render script | `tools/art_pipeline/render_m1_style_proof_board.py` | Re-runnable with Blender 5.1.2 |
| Actor metrics | `docs/evidence/visual-development/actor-proxy-metrics-2026-08-16.json` | Knight and Raider proxy bounds/material/triangle baseline captured |
| Building metrics | `docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json` | Archer_L1 generated candidate is present, socket-complete and within material target |
| Actor contact sheet | `docs/evidence/visual-development/actor-proxy-contact-sheet-2026-08-16.png` | Wider actor proxy set remains available for silhouette comparison |
| Building readability sheet | `docs/evidence/visual-development/building-readability-sheet-2026-08-16.png` | Building color/grayscale/tiny-thumbnail readability evidence exists |
| Final actor 64 px review | `docs/evidence/visual-development/actor-icon-64px-review-final-2026-08-25.png` | Supersedes proxy-only actor readability caveat |
| Final gameplay-camera evidence | `docs/VIS812_FINAL_VISUAL_EVIDENCE_SIMULATOR_2026-08-17.md` | Accepted Simulator stamp `vis812-final-20260825-210559` covers preparation, combat, boss, victory and defeat |

## Review Notes

- Knight reads as the friendly heavy defender through wider body mass, shield/sword silhouette and blue/gold review material family.
- Raider reads as a smaller hostile melee unit through narrower proportions, darker value block and red enemy accent.
- Archer L1 reads separately from both actors as a vertical tower form with roof, raised platform and blue team roof treatment.
- This proof board uses normalized review height so the three assets can be judged side by side; production scale is still tracked through the metric JSON and Unity catalog.

## Resolved After M1

- The board remains the historical style proof for Knight, Raider and Archer L1.
- Later actor/runtime work resolved the original static-proxy limitation.
- Later gameplay-camera capture resolved the original “final Unity capture needed” limitation.

## Not Covered By VIS-113

- Independent blind-recognition review remains tracked under VIS-290/VIS-813.
- Real assistive-tech and physical-device release evidence remain tracked under VIS-813/VIS-816.
