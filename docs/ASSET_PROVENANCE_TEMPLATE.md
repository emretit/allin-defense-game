# Asset Provenance Template

Use this template for every external asset before processed files are accepted into the visual development pipeline.

## Required Fields

| Field | Value |
|---|---|
| Asset name |  |
| Source URL |  |
| Source author/publisher |  |
| License |  |
| License file path |  |
| Acquired on |  |
| Source version/revision |  |
| Source archive path |  |
| Source archive SHA-256 |  |
| Selected source file paths |  |
| Selected source file SHA-256 values |  |
| Processed output paths |  |
| Processing tool/version |  |
| Processing notes |  |
| Runtime usage |  |
| Reviewer |  |

## Example: Existing KayKit Adventurers Source

| Field | Value |
|---|---|
| Asset name | KayKit Adventurers Character Pack 1.0 |
| Source URL | https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0 |
| Source author/publisher | Kay Lousberg / KayKit |
| License | Creative Commons Zero 1.0 |
| License file path | `Assets/ThirdParty/KayKit/Adventurers/LICENSE.txt` |
| Acquired on | 2026-08-16 |
| Source version/revision | `672074b73ba276876a19e8816ecdc5241817ab47` |
| Source archive path | Not stored as a full archive in this repository |
| Source archive SHA-256 | Not applicable; selected source FBX hashes are stored instead |
| Selected source file paths | `Assets/ThirdParty/KayKit/Adventurers/Models/KnightLicensed.fbx`, `BarbarianLicensed.fbx`, `MageLicensed.fbx`, `RogueLicensed.fbx` |
| Selected source file SHA-256 values | Recorded in `Assets/ThirdParty/ART_ASSET_SOURCES.md` |
| Processed output paths | `Assets/Art/Models/Generated/Knight.fbx`, `Guard.fbx`, `Raider.fbx`, `Vanguard.fbx`, `Regenerator.fbx`, `Support.fbx`, `Boss.fbx` |
| Processing tool/version | Blender pipeline script: `ArtSource/Blender/optimize_kaykit_character.py` |
| Processing notes | Current stored FBX files are static mobile proxies; VIS-201 split runtime rigged actors from icon/LOD proxies conceptually, and VIS-202 must reacquire/store full rigged sources or create original rigged actor sources. |
| Runtime usage | Actor catalog baseline, not final role art |
| Reviewer | Visual development owner |
