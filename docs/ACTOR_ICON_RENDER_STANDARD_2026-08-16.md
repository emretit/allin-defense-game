# ALLIN: Defense — Actor Icon Render Standard

Status: M1 icon render preset
Date: 2026-08-16
Related TODO: VIS-107, VIS-207, VIS-215, VIS-225, VIS-235, VIS-245, VIS-255, VIS-265, VIS-275, VIS-285

## 1. Decision

Actor icons should be generated from the same camera angle and lighting preset so HUD, selection and review-board assets remain comparable. The first repository preset is a Blender-based proxy icon renderer.

Tool:

- `tools/art_pipeline/render_actor_icon_set.py`
- `tools/art_pipeline/render_actor_icon_review_sheet.py`

Current output:

- `docs/evidence/visual-development/actor-icons-2026-08-16/Knight.png`
- `docs/evidence/visual-development/actor-icons-2026-08-16/Guard.png`
- `docs/evidence/visual-development/actor-icons-2026-08-16/Raider.png`
- `docs/evidence/visual-development/actor-icons-2026-08-16/Vanguard.png`
- `docs/evidence/visual-development/actor-icons-2026-08-16/Regenerator.png`
- `docs/evidence/visual-development/actor-icons-2026-08-16/Support.png`
- `docs/evidence/visual-development/actor-icons-2026-08-16/Siege.png`
- `docs/evidence/visual-development/actor-icons-2026-08-16/Boss.png`

2026-08-17 updated output:

- `docs/evidence/visual-development/actor-icons-2026-08-17/Knight.png`
- `docs/evidence/visual-development/actor-icons-2026-08-17/Guard.png`
- `docs/evidence/visual-development/actor-icons-2026-08-17/Raider.png`
- `docs/evidence/visual-development/actor-icons-2026-08-17/Vanguard.png`
- `docs/evidence/visual-development/actor-icons-2026-08-17/Regenerator.png`
- `docs/evidence/visual-development/actor-icons-2026-08-17/Support.png`
- `docs/evidence/visual-development/actor-icons-2026-08-17/Siege.png`
- `docs/evidence/visual-development/actor-icons-2026-08-17/Boss.png`
- `docs/evidence/visual-development/actor-icon-64px-review-2026-08-17.png`

## 2. Render Preset

| Setting | Current value |
|---|---|
| Resolution | 512 × 512 |
| Background | Transparent render film; viewer may show black behind alpha |
| Camera | Orthographic, shared isometric-ish actor angle |
| Lighting | Shared area key plus cool rim |
| Framing | Role auto-fit for icon readability |
| Temporary material | Current FBX material families remapped to fixed review colors |
| Source model root | `Assets/Art/Models/Generated` |

This output is for icon framing and silhouette review. It is not final HUD art and does not replace final role materials, portraits or hand-polished icons.

## 3. Regeneration

```bash
blender -b --factory-startup --python tools/art_pipeline/render_actor_icon_set.py -- --output-dir docs/evidence/visual-development/actor-icons-2026-08-17
```

```bash
python3 tools/art_pipeline/render_actor_icon_review_sheet.py --icons-dir docs/evidence/visual-development/actor-icons-2026-08-17 --output docs/evidence/visual-development/actor-icon-64px-review-2026-08-17.png
```

## 4. Acceptance Gates

| Gate | Required proof |
|---|---|
| Same preset | All eight icons produced by the same script and settings |
| No crop errors | Boss and tall actors have safe headroom |
| Readability | 64 px thumbnail review passes |
| Grayscale review | Color and grayscale rows are present in the 64 px sheet |
| HUD integration | Unity sprite import and uGUI usage verified |

Open status:

- Knight, Guard and Raider proxy icons have 512 px source renders and a color/grayscale 64 px review sheet from 2026-08-17.
- Unity sprite import and HUD binding are pending.
- Final hand-polished role icons remain per-role TODO work.
