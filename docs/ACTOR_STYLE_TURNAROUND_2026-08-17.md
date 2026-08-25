# Actor Style Turnaround - 2026-08-17

## Status

Knight, Guard, Raider, Vanguard, Regenerator, Support and Siege now have re-runnable front/side/isometric visual proof sheets from the current generated proxy FBX files. These sheets support VIS-210, VIS-220, VIS-230, VIS-240, VIS-250, VIS-260 and VIS-270 visual review, but do not close the final runtime actor work.

## Evidence

| Actor | Evidence | Result |
|---|---|---|
| Knight | `docs/evidence/visual-development/knight-turnaround-2026-08-17.png` | Passed for proxy silhouette review |
| Guard | `docs/evidence/visual-development/guard-turnaround-2026-08-17.png` | Passed for proxy silhouette review |
| Raider | `docs/evidence/visual-development/raider-turnaround-2026-08-17.png` | Passed for proxy silhouette review |
| Vanguard | `docs/evidence/visual-development/vanguard-turnaround-2026-08-17.png` | Passed for proxy silhouette review |
| Regenerator | `docs/evidence/visual-development/regenerator-turnaround-2026-08-17.png` | Passed for proxy silhouette review with final differentiation notes |
| Support | `docs/evidence/visual-development/support-turnaround-2026-08-17.png` | Passed for proxy silhouette review with final differentiation notes |
| Siege | `docs/evidence/visual-development/siege-turnaround-2026-08-17.png` | Passed for proxy silhouette review |
| Render script | `tools/art_pipeline/render_actor_turnaround_sheet.py` | Re-runnable with Blender 5.1.2 |
| Metrics baseline | `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json` | Bounds, material and triangle counts captured |

## Review Notes

### Knight

- Wide body and shield make the defender role readable in front and isometric views.
- Blue/gold review colors separate the friendly unit from enemy red/dark accents.
- Side view confirms the shield creates meaningful profile mass, not only a front-facing detail.

### Raider

- Dark body block and red core separate the enemy role from the Knight.
- Narrower footprint and smaller bounds support a faster melee read.
- Current proxy is readable but not yet strongly forward-leaning; final VIS-231 model work should exaggerate agile posture and negative space.

### Guard

- Tall helmet, narrow body and upright spear separate the role from the Knight's wider shield-first mass.
- Blue/steel review colors keep the friendly-team read while making the Guard lighter than the Knight.
- Current proxy is readable for silhouette review; final VIS-221 model work should push the horizontal spear line and reduce shield similarity at gameplay zoom.

### Vanguard

- Wider shoulder mass, darker metal and heavier shield read as a stronger enemy than Raider.
- The side view keeps a thicker body block than Raider, but the current weapon line still overlaps Guard/Boss proxy language.
- Final VIS-241 model work should add a clearer two-handed heavy weapon and reduce the spear-like read.

### Regenerator

- Tall horned head and compact body are readable, but the current proxy is close to Support in posture and accessory language.
- 2026-08-17 20:43 refresh introduced the dark organic/green core material identity and the asymmetric back/totem mass.
- Final VIS-253-VIS-256 work must still prove animation, VFX, icon framing and gameplay-camera readability.

### Support

- Staff/lantern silhouette is readable in front, side and isometric views.
- 2026-08-17 21:03 refresh moved Support to neutral cloth plus cyan energy, with the cast focus on the staff/lantern instead of Regenerator's organic green core/totem language.
- Final VIS-263-VIS-266 work must still prove cast/heal animation, target-flow VFX, icon framing and gameplay-camera readability.

### Siege

- Low horizontal body, wheels and long attack apparatus make the role distinct from humanoid actors.
- The side and isometric views give a strong mechanical profile that should hold at gameplay zoom.
- Final VIS-271 model work should preserve separate pivotable attack parts while keeping the collider/path footprint stable.

## Remaining Before Actor Acceptance

- Runtime sockets, rig, animation clips, controller bindings and icon/marker/shadow acceptance remain open.
- Current sheets use generated static proxy FBX files and review materials; final gameplay-camera capture is still required after runtime actor integration.
