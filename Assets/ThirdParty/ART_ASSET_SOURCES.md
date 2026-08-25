# Defender Chronicles third-party art sources

Acquired on: 2026-08-16

Only the files used by the production presentation catalog are stored in this
repository. All three packs are released under Creative Commons Zero 1.0; the
original license text is preserved beside each selected asset set.

## KayKit Adventurers Character Pack 1.0

- Source: https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0
- Source revision: `672074b73ba276876a19e8816ecdc5241817ab47`
- License: CC0 1.0 (`KayKit/Adventurers/LICENSE.txt`)
- Original upstream FBX SHA-256 values recorded at acquisition time:
  - Knight: `b25e35cc7eaaa3c7c103f10a4f41fde1cbd81936fede9aa9ae4efbd29d40ba1b`
  - Barbarian: `902a7c0092714c21f148b42ca8921ba7ac66e74f16c8af5088a3d224663ed597`
  - Mage: `93b401d56e2547316f8841a4ac0086011c64d79185c9cb7623567c2dbf125c7d`
  - RogueHooded: `4a773f074695c6c089cc6f059eed85da080928a4a4137af4e38b177139d4cc27`
- Stored selected FBX SHA-256:
  - `BarbarianLicensed.fbx`: `27bc8c90e083ea7baa36bec17307448692475d49351b63967e152ac3d3f72eba`
  - `KnightLicensed.fbx`: `4bbf89d11272c8238f313e474a07cedd59f46e70fe7e012ca1a66819110c71a1`
  - `MageLicensed.fbx`: `699ccd357b798d8a045281df4b25d1f1fcacaebca0c49d7a3a648e44d28b7153`
  - `RogueLicensed.fbx`: `66cfe7a84bed5765c27a72c28a5f21d00cf6325984d3310806900a595b212820`

The upstream KayKit package contains rigged and animated character sources, but
the selected FBX files stored in this repository are the optimized static proxy
outputs, not the full rigged sources. `ArtSource/Blender/optimize_kaykit_character.py`
records the repeatable conversion used to create that proxy class: it selects a
readable pose, removes unused props and animations, and exports a static mobile
proxy. Runtime rig production must reacquire/store the full rigged CC0 sources
or create original rigged actor sources before VIS-202 can close. The atlas PNG
files are unmodified.

## KayKit Medieval Hexagon Pack 1.0

- Source: https://github.com/KayKit-Game-Assets/KayKit-Medieval-Hexagon-Pack-1.0
- Source revision: `84fa4e91af6a88989be7c99e0891cede11f2ca38`
- License: CC0 1.0 (`KayKit/MedievalHexagon/LICENSE.txt`)
- Selected files: the blue Unity FBX variants for tower A, archery range,
  tower base, barracks, tower B, church and castle; plus the Unity FBX nature
  variants `trees_A_small`, `trees_B_small`, `trees_A_medium`,
  `rock_single_A` and `rock_single_B`.
- Shared atlas SHA-256: `5301a06866ba3be68dcc14cc429bd6d0f69004b72884283715d7e233f2e9782e`

## Kenney Fantasy UI Borders 1.0

- Source: https://kenney.nl/assets/fantasy-ui-borders
- Download SHA-256: `59532da3bd61195a425585455b40baa6cbf1eda8227c42adcef31a535a737769`
- License: CC0 1.0 (`Kenney/FantasyUiBorders/LICENSE.txt`)
- Selected file: `panel-transparent-border-025.png`, stored as
  `HudFrameBorder.png` (unmodified, SHA-256
  `5416e9342427aba83f6c9b790bd998129835ba334b8387efcdddd4bd96a7472a`).

## Cinzel SemiBold

- Source: https://github.com/google/fonts/tree/main/ofl/cinzel
- Upstream project: https://github.com/NDISCOVER/Cinzel
- Source revision recorded by Google Fonts metadata: `8271e162a7c6064ff1e539ba86011b16b6a6184f`
- License: SIL Open Font License 1.1 (`Cinzel/OFL.txt`)
- Production file: `Assets/Art/Fonts/Cinzel-SemiBold.ttf`
- The static 600-weight instance was produced from the official
  `Cinzel[wght].ttf` variable font with fontTools 4.60.2.
- Production SHA-256: `3d603512f54ae6dd1e80a2a69301d26e6e8b7d7d1ea0322bc8f6c419f406e7ab`

## Greenwatch frontend artwork

- Generator: OpenAI image generation, project session 2026-08-25
- License/provenance classification: project-authorized AI-generated artwork
- `Assets/Resources/Presentation/Frontend/GreenwatchWelcome.png`
  - Prompt intent: premium stylized medieval mobile-game welcome background;
    moonlit Greenwatch castle, enemy torches, mist and banners, with clear UI
    negative space and no baked-in text.
  - SHA-256: `c15fac27984a6cbb3b27cd9baac976a1e502ff088f2289fde6ea9c8acdb33c2d`
- `Assets/Resources/Presentation/Frontend/GreenwatchWarTable.png`
  - Prompt intent: premium stylized 2.5D medieval campaign war table; a readable
    Greenwatch route, miniature terrain, forts, bridges, mist and no baked-in text.
  - SHA-256: `3a6b7b451aaa83e0996bf7553da90a4c9d9837b9f0f7627429e2a9a9c65faece`
