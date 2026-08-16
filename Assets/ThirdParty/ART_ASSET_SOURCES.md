# Defender Chronicles third-party art sources

Acquired on: 2026-08-16

Only the files used by the production presentation catalog are stored in this
repository. All three packs are released under Creative Commons Zero 1.0; the
original license text is preserved beside each selected asset set.

## KayKit Adventurers Character Pack 1.0

- Source: https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0
- Source revision: `672074b73ba276876a19e8816ecdc5241817ab47`
- License: CC0 1.0 (`KayKit/Adventurers/LICENSE.txt`)
- Used source FBX SHA-256:
  - Knight: `b25e35cc7eaaa3c7c103f10a4f41fde1cbd81936fede9aa9ae4efbd29d40ba1b`
  - Barbarian: `902a7c0092714c21f148b42ca8921ba7ac66e74f16c8af5088a3d224663ed597`
  - Mage: `93b401d56e2547316f8841a4ac0086011c64d79185c9cb7623567c2dbf125c7d`
  - RogueHooded: `4a773f074695c6c089cc6f059eed85da080928a4a4137af4e38b177139d4cc27`

The source FBX files contain the pack's full animation library and are about
19–20 MB each. `ArtSource/Blender/optimize_kaykit_character.py` records the
repeatable conversion used here: it selects a readable pose, removes unused
props and animations, and exports a static mobile proxy. The atlas PNG files
are unmodified.

## KayKit Medieval Hexagon Pack 1.0

- Source: https://github.com/KayKit-Game-Assets/KayKit-Medieval-Hexagon-Pack-1.0
- Source revision: `84fa4e91af6a88989be7c99e0891cede11f2ca38`
- License: CC0 1.0 (`KayKit/MedievalHexagon/LICENSE.txt`)
- Selected files: the blue Unity FBX variants for tower A, archery range,
  tower base, barracks, tower B, church and castle.
- Shared atlas SHA-256: `5301a06866ba3be68dcc14cc429bd6d0f69004b72884283715d7e233f2e9782e`

## Kenney Fantasy UI Borders 1.0

- Source: https://kenney.nl/assets/fantasy-ui-borders
- Download SHA-256: `59532da3bd61195a425585455b40baa6cbf1eda8227c42adcef31a535a737769`
- License: CC0 1.0 (`Kenney/FantasyUiBorders/LICENSE.txt`)
- Selected file: `panel-transparent-border-025.png`, stored as
  `HudFrameBorder.png` (unmodified, SHA-256
  `5416e9342427aba83f6c9b790bd998129835ba334b8387efcdddd4bd96a7472a`).
