# Visual Development Asset Inventory

Captured on: 2026-08-16
Scope: Current presentation, generated art, editable Blender sources, and recorded third-party bases.

## Summary

| Area | Count | Evidence |
|---|---:|---|
| Editable Blender sources | 18 `.blend` files + 18 `.blend1` backups + 1 conversion script | `ArtSource/Blender` |
| Generated runtime FBX models | 17 | `Assets/Art/Models/Generated` |
| Generated presentation materials | 11 shared + 5 licensed atlas materials | `Assets/Art/GeneratedPresentation` |
| Actor animation clips | 9 | `Assets/Art/GeneratedPresentation/DC_*.anim` |
| HUD art files | 9 PNG files | `Assets/Art/UI` |
| Production HUD prefab | 1 | `Assets/Art/GeneratedPresentation/DefenderHud.prefab` |
| Production environment prefab | 1 | `Assets/Art/GeneratedPresentation/LicensedEnvironment.prefab` |
| Third-party art/audio provenance files | 132 non-meta files across checked folders | `Assets/ThirdParty`, `Assets/Art`, `Assets/Audio` |

## Actor Sources

| Role | Catalog source | Current processed source | Size bytes | SHA-256 | Baseline flag |
|---|---|---|---:|---|---|
| Knight | `Assets/Art/Models/Generated/Knight.fbx` | `Assets/Art/Models/Generated/Knight.fbx` | 74844 | `9bf51b953804907c8308a2b9a2a0345988ad3d51734bfd6042bd4c29d4d72e4e` | Role-specific generated proxy connected; final art still open |
| Guard | `Assets/Art/Models/Generated/Guard.fbx` | `Assets/Art/Models/Generated/Guard.fbx` | 68972 | `81c4852de49e2527e30fcdd9cf7e497c4c399588c42a1c4435c16d02981d123b` | Role-specific generated proxy connected; final art still open |
| Raider | `Assets/Art/Models/Generated/Raider.fbx` | `Assets/Art/Models/Generated/Raider.fbx` | 66140 | `d43664065dd9b26d3b8456197b13dd605a0cbb062599c9ed40f4bbb40c7ef0f0` | Role-specific generated proxy connected; final art still open |
| Vanguard | `Assets/Art/Models/Generated/Vanguard.fbx` | `Assets/Art/Models/Generated/Vanguard.fbx` | 69084 | `899d87d884efd7d9687885afe20f101d528f31dbd417caf48be50b3e259b96f4` | Role-specific generated proxy connected; final art still open |
| Regenerator | `Assets/Art/Models/Generated/Regenerator.fbx` | `Assets/Art/Models/Generated/Regenerator.fbx` | 65868 | `462a6a23dabc3a0111040872083ba9c32f711e89092537d4df22d1ba94942c3d` | Role-specific generated proxy connected; final art still open |
| Support | `Assets/Art/Models/Generated/Support.fbx` | `Assets/Art/Models/Generated/Support.fbx` | 65980 | `e3f6b0e85ab7ef3ab8201b593a9e71205cc85ee236a265350fb6155f9e64afa4` | Role-specific generated proxy connected; final art still open |
| Siege | `Assets/Art/Models/Generated/Siege.fbx` | `Assets/Art/Models/Generated/Siege.fbx` | 45708 | `f5102bbd0b1bdeecc59d901ff48c9ecad6d9018bd903c2dde703b88d285b43de` | Unique generated asset |
| Boss | `Assets/Art/Models/Generated/Boss.fbx` | `Assets/Art/Models/Generated/Boss.fbx` | 74588 | `bec979c54016acf03850aa425c391ec85c2dbca0f6be34b166b5e5a17b519881` | Role-specific generated proxy connected; final art still open |

## Building Sources

| Visual | Catalog source | Current processed source | Size bytes | SHA-256 | Baseline flag |
|---|---|---|---:|---|---|
| Archer L1 | `building_tower_A_blue.fbx` | `Assets/Art/Models/Generated/Archer_L1.fbx` | 72044 | `787fcb832cdc2da386c56f58bd3f195c52080cecfe179d5b3662e246619b9447` | Generated candidate now has bow/target/arrow language, runtime sockets and 3-material target; Unity validation open |
| Archer L2 | `building_archeryrange_blue.fbx` | `Assets/Art/Models/Generated/Archer_L2.fbx` | 93244 | `bdb8494ae7cf6418e1f57e0c3f8ee1bfdfd4f278d4b9bcb437bd6609a7c07441` | Generated candidate now has stronger height/banner/arrow progression, runtime sockets and 3-material target; Unity validation open |
| Barracks L1 | `building_tower_base_blue.fbx` | `Assets/Art/Models/Generated/Barracks_L1.fbx` | 48444 | `459e50e51f1ac463ab983bf1fc322be3fd577ea45bd5dcb343d9e82358672c2c` | Generated candidate now has runtime sockets and 3-material target; final art/Unity validation open |
| Barracks L2 | `building_barracks_blue.fbx` | `Assets/Art/Models/Generated/Barracks_L2.fbx` | 77916 | `85fe767be514a86fcb6d24204ff00e18ffaa823530842c80bbf0c4fd5d0a3df2` | Generated candidate now has stronger L2 tower/weapon silhouette, runtime sockets and 3-material target; Unity validation open |
| Mage L1 | `building_tower_B_blue.fbx` | `Assets/Art/Models/Generated/Mage_L1.fbx` | 64940 | `f1c3a7d23acaee18d402e21187263dbd0179e01737de10e17fabefdc23412ec1` | Generated candidate now has crystal/cast sockets and 3-material target; final art/Unity validation open |
| Mage L2 | `building_church_blue.fbx` | `Assets/Art/Models/Generated/Mage_L2.fbx` | 67660 | `363e3dbc104862e7fd0538dc378721fb9e0c57a3401ddefc02750f15c790160d` | Generated candidate now has crystal/cast sockets and 3-material target; final art/Unity validation open |

## Environment And HUD

| Asset | Size bytes | SHA-256 | Usage | Baseline flag |
|---|---:|---|---|---|
| `Assets/Art/Models/Generated/Environment.fbx` | 479916 | `c31b83baaf9e3ce82727328e049eb917bda44fe44b97bebe642c0582395606ee` | Base map geometry | Needs prop variety, edge framing, route polish |
| `Assets/Art/Models/Generated/OutpostSlot.fbx` | 33420 | `4b5859f80eb8f831ba7a20853830f3a401c4cf1af58a5f140a15b1a94a72f457` | Build slot marker | Base marker; state language now has generated candidates below; Unity binding open |
| `Assets/Art/Models/Generated/OutpostSlot_Empty.fbx` | 46540 | `0c9a70db8b34c2e51bc5b724c6d5d3c7c2d81634775ceeba27576b59f963337f` | Empty slot state candidate | Calm low rim; runtime binding open |
| `Assets/Art/Models/Generated/OutpostSlot_Selected.fbx` | 57404 | `ebba88d6c96d0de43fcc5ba37638aadb69f1f116cfbbb1636c1b8c41ef3a0b3e` | Selected slot state candidate | Raised focus and corner brackets; runtime binding open |
| `Assets/Art/Models/Generated/OutpostSlot_Available.fbx` | 48252 | `d294a13ccee03fe7c41222d8f5788ee29d407a1c5aa9456973f0d9c0cad6c354` | Available slot state candidate | Upward shard/pulse language; runtime binding open |
| `Assets/Art/Models/Generated/OutpostSlot_Insufficient.fbx` | 54444 | `dcba668848d19331447d448e06c70e1aeac49ebb964f8ab3d540b14fd0c103b4` | Insufficient-resource slot state candidate | Broken shard/slash language; runtime binding open |
| `Assets/Art/Models/Generated/OutpostSlot_Locked.fbx` | 49788 | `dc12d7643995c1b3d371b18776765ca458306295e2ecb0a6ad8cf4632efc4323` | Locked slot state candidate | Post/crossbar language; runtime binding open |
| `Assets/Art/Models/Generated/CommandPoint.fbx` | 26620 | `465f1e5f478c8dba703a9d7c17fb4f10b4e47f316405a1fb5697ece8df2eb708` | Defender keep/command point | Needs landmark hierarchy |
| `Assets/Art/GeneratedPresentation/DefenderHud.prefab` | 147731 | `cf741e1cc0c0a94b589c5be1112dec07588a60ef47e9858a348bc1931b4f50fe` | Runtime HUD | Needs state polish, contrast, fantasy material language |
| `Assets/Art/UI/DefenderHudPanel.png` | 2321770 | `a60f7c284d459b57e48b3722710aa960c3b0e900a406b057fbbc1799b1967861` | HUD panel art | Large PNG; review atlas/compression in VIS-600/VIS-800 |
| `Assets/Art/UI/DefenderIconAtlas.png` | 2028252 | `d5f17e283342a24b6e1e58f74feedd86035867139547d99f53f337530a6b7957` | HUD icon atlas | Large PNG; review slicing and compression |

## Animation And VFX

| Asset family | Files | Baseline flag |
|---|---|---|
| Shared actor animator | `Assets/Art/GeneratedPresentation/DC_Actors.controller` | One shared controller covers all actor roles; role-specific overrides are not present yet. |
| Root-transform clips | `DC_Idle`, `DC_Move`, `DC_Attack`, `DC_Hit`, `DC_Defeat`, `DC_Ability`, `DC_Victory`, `DC_Phase`, `DC_BossAttack` | Clips are generic and do not prove rig-based role animation. |
| VFX pool script | `Assets/Scripts/Presentation/DefenderVfxPool.cs` | Pooling infrastructure exists; role-specific visual families still need production. |
| Build-in VFX candidates | `BuildVfx_Dust`, `BuildVfx_Ring`, `BuildVfx_Spark` | Generated mesh candidates exist; runtime pool binding and PlayMode regression remain open. |
| Upgrade VFX candidates | `UpgradeVfx_Anchor`, `UpgradeVfx_Column`, `UpgradeVfx_Crest` | Generated mesh candidates exist; runtime pool binding and L1→L2 regression remain open. |
| Sell VFX candidates | `SellVfx_Ring`, `SellVfx_Chips`, `SellVfx_Glint` | Generated mesh candidates exist; runtime pool binding and sell/rebuild regression remain open. |

## Build-In VFX Candidates

| Asset | Path | Size bytes | SHA-256 | Triangles | Materials | Baseline flag |
|---|---|---:|---|---:|---:|---|
| BuildVfx_Dust | `Assets/Art/Models/Generated/BuildVfx_Dust.fbx` | 61996 | `0add5fc508b6ca1307b72de4b4558c58cb0d890e20bf1ba7c800e0f7fb947ad4` | 332 | 2 | Dust beat for build pop-in masking; runtime binding open |
| BuildVfx_Ring | `Assets/Art/Models/Generated/BuildVfx_Ring.fbx` | 53068 | `b53e138e9931b22e0e03878f7fe21afe7731fe6a4455f81168e80edda61f96b3` | 280 | 3 | Blueprint/slot confirmation beat; runtime binding open |
| BuildVfx_Spark | `Assets/Art/Models/Generated/BuildVfx_Spark.fbx` | 42508 | `232bbc799ee171d56120d3f5eed4d189a2fa42c852bde6369b891de06496f2fe` | 172 | 2 | Final craft spark beat; runtime binding open |

## Upgrade VFX Candidates

| Asset | Path | Size bytes | SHA-256 | Triangles | Materials | Baseline flag |
|---|---|---:|---|---:|---:|---|
| UpgradeVfx_Anchor | `Assets/Art/Models/Generated/UpgradeVfx_Anchor.fbx` | 45628 | `538ffddedb4bf7eb7673848e97d427e6a48154163a7d398614d1358cb5605b6d` | 248 | 3 | Ground anchor beat; runtime binding open |
| UpgradeVfx_Column | `Assets/Art/Models/Generated/UpgradeVfx_Column.fbx` | 48332 | `b21143b4f60c87b4eb91c9e640bbebb5f343efeb92fa06f134e0c3c2af196762` | 208 | 3 | Centered lift/level-up column; runtime binding open |
| UpgradeVfx_Crest | `Assets/Art/Models/Generated/UpgradeVfx_Crest.fbx` | 43628 | `9fb1fc9aa5c7c0732b97e97afc78f287ece426519c438ba5b5b3d5073519f345` | 176 | 3 | Final L2 crest flash; runtime binding open |

## Sell VFX Candidates

| Asset | Path | Size bytes | SHA-256 | Triangles | Materials | Baseline flag |
|---|---|---:|---|---:|---:|---|
| SellVfx_Ring | `Assets/Art/Models/Generated/SellVfx_Ring.fbx` | 44252 | `4d47a51e4863b1bdf414ba26991289af1c9c7e5c049ea799d0b51619761f6cac` | 284 | 2 | Neutral/gold refund ring; runtime binding open |
| SellVfx_Chips | `Assets/Art/Models/Generated/SellVfx_Chips.fbx` | 54668 | `10108877f15f34a487252f3c111685aed4a9fd8486141169530da8e8d03d2468` | 220 | 3 | Dismantle chips, no enemy red; runtime binding open |
| SellVfx_Glint | `Assets/Art/Models/Generated/SellVfx_Glint.fbx` | 46620 | `a94ce52b5bc83cd3a6b49049e70685d6a8c091ce82241ea9cd10e0a608d1bcc2` | 256 | 1 | Refund glint beat; runtime binding open |

## External Asset Provenance

| Source | License | Recorded revision/SHA evidence | Processed path |
|---|---|---|---|
| KayKit Adventurers Character Pack 1.0 | CC0 1.0 | `Assets/ThirdParty/ART_ASSET_SOURCES.md` records revision and source FBX hashes | `Assets/Art/Models/Generated/*actor*.fbx`, licensed materials |
| KayKit Medieval Hexagon Pack 1.0 | CC0 1.0 | `Assets/ThirdParty/ART_ASSET_SOURCES.md` records revision and atlas hash | `Assets/Art/Models/Generated/*building*.fbx`, `LicensedEnvironment.prefab` |
| Kenney Fantasy UI Borders 1.0 | CC0 1.0 | `Assets/ThirdParty/ART_ASSET_SOURCES.md` records download and selected-file hashes | `Assets/ThirdParty/Kenney/FantasyUiBorders/HudFrameBorder.png` |
| Noto fonts | Open Font License | Catalog provenance records source URL and acquired date | `Assets/Art/Fonts`, generated TMP assets |
| OpenAI-generated HUD art | OpenAI generated project asset | Catalog provenance records source and acquired date | `Assets/Art/UI` |

## Inventory Conclusions

- No actor role can be accepted as final art yet; the catalog now uses eight role-specific generated proxies, but model quality, materials, animation and VFX are still open.
- The repository has enough editable/generated sources to start the M1 style proof without adding paid assets.
- Full performance budgets cannot be accepted from file inspection alone; Unity Editor, Frame Debugger, and iPhone 13-class device captures remain required.
