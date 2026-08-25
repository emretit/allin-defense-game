# Defender Chronicles - VIS-310-336 Building Family Index

Tarih: 2026-08-17

Bu indeks, Archer, Barracks ve Mage L1/L2 building family maddelerinin mevcut kanıt durumunu ayırır. Altı generated candidate review'a hazırdır ve production catalog binding kanıtı vardır; resmi PlayMode XML, final acceptance board, video ve daha ileri art-direction polish açık kalır.

## Kanıt Haritası

| VIS | Durum | Ana kanıt | Kapsam |
|---|---|---|---|
| VIS-310 | İncelemede | `docs/ARCHER_BUILDING_PROGRESSION_2026-08-16.md`, `docs/evidence/visual-development/building-readability-sheet-2026-08-16.png` | Archer L1/L2 progression sheet ve level readability |
| VIS-311 | İncelemede | `Assets/Art/Models/Generated/Archer_L1.fbx`, `docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json` | Archer L1 generated runtime candidate model, sockets ve footprint baseline |
| VIS-312 | İncelemede | `docs/BUILDING_MATERIAL_IMPORT_STANDARD_2026-08-16.md`, `docs/evidence/visual-development/building-budget-report-2026-08-16.json` | Archer L1 material/banner language ve <=3 generated material target |
| VIS-315 | İncelemede | `Assets/Art/Models/Generated/Archer_L2.fbx`, `docs/evidence/visual-development/building-readability-sheet-2026-08-16.png` | Archer L2 higher covered progression candidate |
| VIS-316 | İncelemede | `docs/BUILDING_GENERATED_CANDIDATE_PASS_2026-08-16.md`, `docs/evidence/visual-development/building-budget-report-2026-08-16.json` | Archer L2 material/banner target ve <=3 material evidence |
| VIS-320 | İncelemede | `docs/BUILDING_GENERATED_CANDIDATE_PASS_2026-08-16.md`, `docs/evidence/visual-development/building-readability-sheet-2026-08-16.png` | Barracks L1/L2 progression, grayscale ve thumbnail readability |
| VIS-321 | İncelemede | `Assets/Art/Models/Generated/Barracks_L1.fbx`, `docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json` | Barracks L1 generated runtime candidate model ve spawn socket coverage |
| VIS-322 | İncelemede | `docs/BUILDING_MATERIAL_IMPORT_STANDARD_2026-08-16.md`, `docs/evidence/visual-development/building-budget-report-2026-08-16.json` | Barracks L1 reinforced material/banner target ve <=3 material evidence |
| VIS-324 | İncelemede | `Assets/Art/Models/Generated/Barracks_L2.fbx`, `docs/evidence/visual-development/building-readability-sheet-2026-08-16.png` | Barracks L2 fortified mass, roof/turret/weapon-rack silhouette |
| VIS-325 | İncelemede | `docs/BUILDING_GENERATED_CANDIDATE_PASS_2026-08-16.md`, `docs/evidence/visual-development/building-budget-report-2026-08-16.json` | Barracks L2 material/banner/weapon-rack target ve <=3 material evidence |
| VIS-330 | İncelemede | `docs/BUILDING_GENERATED_CANDIDATE_PASS_2026-08-16.md`, `docs/evidence/visual-development/building-readability-sheet-2026-08-16.png` | Mage L1/L2 crystal/rune progression, grayscale ve thumbnail readability |
| VIS-331 | İncelemede | `Assets/Art/Models/Generated/Mage_L1.fbx`, `docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json` | Mage L1 generated runtime candidate model, crystal/cast socket coverage |
| VIS-332 | İncelemede | `docs/BUILDING_MATERIAL_IMPORT_STANDARD_2026-08-16.md`, `docs/evidence/visual-development/building-budget-report-2026-08-16.json` | Mage L1 stone/cyan material target ve bloom-free crystal read |
| VIS-334 | İncelemede | `Assets/Art/Models/Generated/Mage_L2.fbx`, `docs/evidence/visual-development/building-readability-sheet-2026-08-16.png` | Mage L2 arcane sanctuary candidate and stronger vertical crystal mass |
| VIS-335 | İncelemede | `docs/BUILDING_GENERATED_CANDIDATE_PASS_2026-08-16.md`, `docs/evidence/visual-development/building-budget-report-2026-08-16.json` | Mage L2 material/energy column target and continuous VFX profiler gap |
| VIS-313, VIS-317, VIS-323, VIS-326, VIS-333, VIS-336 | İncelemede | `docs/BUILDING_GENERATED_CATALOG_BINDING_RUNTIME_PASS_2026-08-16.md`, `Builds/unity-visual-building-binding.log` | Production catalog binding and runtime generated building screenshot evidence |

## Açık Kapılar

- Resmi PlayMode XML ve build/upgrade/sell video kanıtı VIS-345 altında açık kalır.
- Final six-building acceptance board ve insan kör tanıma sonucu VIS-344 altında açık kalır.
- Barracks ve Mage final art-direction polish yapılabilir; mevcut generated candidates review'a hazır baseline olarak kabul edilir.
- Mage continuous/idle VFX ve platform profiler kanıtı VIS-335/VIS-346/VIS-807 altında açık kalır.

## Doğrulama

Bu indeks `DefenderPresentationBuildValidator.ValidateBuildingFamilyIndexCheckpoint()` ile doğrulanır. Checkpoint; altı generated FBX dosyasını, metrics/budget/readability/QA kanıtlarını, production catalog L1/L2 binding'ini, runtime building binding logunu ve indeksin VIS-310-VIS-336 coverage'ını kontrol eder.
