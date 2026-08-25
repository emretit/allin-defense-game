# VIS-804 Texture Import Profile

Date: 2026-08-17

## Scope

VIS-804 applies and verifies the mobile texture import profile for actor atlases, building/environment atlas use, UI sprites and the VFX helper texture.

## Import Profile

| Family | Textures | Max size | Mipmaps | iPhone format |
|---|---:|---:|---|---|
| Actor atlases | 4 | 512 | On | ASTC_6x6 |
| Building/environment atlas | 1 | 1024 | On | ASTC_8x8 |
| HUD panel/icon sprites | 9 | 256-1024 | Off | ASTC_4x4 |
| HUD frame sprite | 1 | 64 | Off | RGBA32 |
| VFX helper | 1 | 32 | Off | RGBA32 |

Runtime VFX currently use generated mesh shapes and material colors instead of particle flipbook atlases, so VIS-804 tracks the shared `White32` helper and leaves future flipbook import rules explicit if authored VFX textures are added.

## Batch Evidence

- Checkpoint log: `Builds/unity-texture-import-profile-checkpoint.log`
- Machine report: `docs/evidence/visual-development/texture-import-profile-report-2026-08-17.json`
- Passing line: `[DC_TEXTURE_IMPORT_PROFILE_CHECKPOINT] passed textures=16 actor=4 environmentBuilding=1 ui=10 vfxHelper=1 iosOverrides=16 formats=ASTC_4x4/ASTC_6x6/ASTC_8x8/RGBA32 mipmaps=atlasOnly`

## Acceptance Status

Batch proxy acceptance is in review for VIS-804:

- iPhone platform overrides are present on all tracked runtime texture families.
- Actor and environment/building atlases keep mipmaps for gameplay camera distance changes.
- UI and VFX helper textures keep mipmaps off to avoid unnecessary memory.
- Final gameplay zoom comparison and device memory delta remain under VIS-807/VIS-812.
