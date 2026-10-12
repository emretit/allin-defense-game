# ALLIN: Defense - VIS-300 Building Pipeline Index

Tarih: 2026-08-17

Bu indeks, ortak building pipeline maddelerinin mevcut kanıt durumunu ayırır. VIS-301-VIS-304 generated candidate ve catalog binding seviyesinde review'a hazırdır; final hard-fail socket validation, resmi PlayMode XML ve video/acceptance board kapıları açık kalır.

## Kanıt Haritası

| VIS | Durum | Ana kanıt | Kapsam |
|---|---|---|---|
| VIS-301 | İncelemede | `docs/BUILDING_PIPELINE_STANDARD_2026-08-16.md`, `docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json` | Pivot, footprint, level replacement ve runtime building socket contract standardı |
| VIS-302 | İncelemede | `docs/BUILDING_MATERIAL_IMPORT_STANDARD_2026-08-16.md`, `docs/BUILDING_GENERATED_CANDIDATE_PASS_2026-08-16.md`, `docs/evidence/visual-development/building-budget-report-2026-08-16.json` | Generated building candidates için <=3 material hedefi, role material language ve geometry/material soft budget geçişi |
| VIS-303 | İncelemede | `docs/BUILDING_GENERATED_CANDIDATE_PASS_2026-08-16.md`, `docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json` | Archer, Barracks ve Mage generated L1/L2 adaylarında common + role-specific `Socket_*` coverage |
| VIS-304 | İncelemede | `docs/BUILDING_GENERATED_CATALOG_BINDING_RUNTIME_PASS_2026-08-16.md`, `Assets/Scripts/Presentation/DefenderPresentationCatalog.cs`, `Builds/unity-visual-building-binding.log` | Six-entry outpost catalog coverage, visual-development warning layer ve generated catalog runtime binding kanıtı |

## Açık Kapılar

- VIS-301: final footprint/click target kabulü resmi PlayMode XML ve video kanıtı bekler.
- VIS-302: Unity texture import/compression profili ve platform profiler kanıtı VIS-346/VIS-807 altında açık kalır.
- VIS-303: generated candidates socket-complete; final runtime prefab sonrası missing socket warning'leri hard failure'a çevrilmelidir.
- VIS-304: catalog validation katmanı şu an visual-development warning raporu üretir; final generated runtime prefab acceptance board ve resmi PlayMode XML tamamlanmadan tam kabul değildir.

## Doğrulama

Bu indeks `DefenderPresentationBuildValidator.ValidateBuildingPipelineIndexCheckpoint()` ile doğrulanır. Checkpoint; gerekli standard dokümanlarını, generated FBX dosyalarını, metrics/budget/readability sheets'i, runtime binding screenshot/loglarını, altı outpost catalog entry'sini ve generated socket/material budget kanıtını kontrol eder.
