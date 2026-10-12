# ALLIN: Defense - VIS-200 Actor Pipeline Index

Tarih: 2026-08-17

Bu indeks, ortak actor pipeline maddelerinin mevcut kanıt durumunu ayırır. VIS-202 bilinçli olarak açık kalır: repository içindeki yerel KayKit actor FBX'leri static proxy olduğu için full rig/armature import kararı henüz kanıtlanmış değildir.

## Kanıt Haritası

| VIS | Durum | Ana kanıt | Kapsam |
|---|---|---|---|
| VIS-204 | İncelemede | `docs/ACTOR_MARKER_SHADOW_STANDARD_2026-08-16.md`, `Assets/Scripts/Presentation/StylizedPresentationFactory.cs` | Capsule actor root için `Actor Contact Shadow` runtime katmanı; boss/defender aura ayrımı; shadow casting ve receive shadows kapalı |
| VIS-205 | İncelemede | `docs/ACTOR_ANIMATION_CONTRACT_2026-08-16.md`, `Builds/unity-actor-animation-visual-vis223.log`, `Builds/unity-actor-animator-override-vis223.log`, `Builds/editmode-results-vis223.xml`, `Builds/playmode-results-vis223.xml`, `Builds/unity-actor-animator-priority-checkpoint.log` | Shared 9 clip/state contract, gameplay cue mapping, Knight/Guard/Raider role override resolver ve transition priority kuralları |
| VIS-206 | İncelemede | `docs/ACTOR_MATERIAL_IMPORT_STANDARD_2026-08-16.md`, `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json`, `Builds/unity-actor-catalog-visual-vis221222.log` | Actor atlas/material import standardı, final actor <=2 material hedefi, Knight/Guard/Raider proxy material consolidation kanıtı |
| VIS-207 | İncelemede | `docs/ACTOR_ICON_RENDER_STANDARD_2026-08-16.md`, `docs/evidence/visual-development/actor-icons-2026-08-17/`, `docs/evidence/visual-development/actor-icon-64px-review-2026-08-17.png` | Ortak Blender icon camera/light preset, 8 role icon output ve 64 px review sheet |
| VIS-208 | İncelemede | `docs/CATALOG_VISUAL_VALIDATION_2026-08-16.md`, `Builds/unity-actor-catalog-visual-checkpoint.log` | Catalog visual-development warning layer; actor role coverage, unique prefab, socket/material/animator validation gates |

## Açık Kapılar

- VIS-202 açık: full rigli runtime source veya özgün rig üretimi ve Unity import/test sahnesi gerekiyor.
- VIS-204 contact shadow/aura katmanı runtime factory içinde var; final selected/status marker dili VFX/status sistemiyle ayrı kalır.
- VIS-205 shared fallback ve timing contract hazır; Knight/Guard/Raider generated role override controller assetleri üretildi. `Builds/editmode-results-vis223.xml` 40/40 ve `Builds/playmode-results-vis223.xml` 6/6 geçti. Diğer role-specific authored clip placement işleri final actor üretimine bağlıdır.
- VIS-206 material/import standardı hazır; final platform texture overrides ve device memory proof VIS-804/VIS-807 altında doğrulanacaktır.
- VIS-207 proxy icon preset ve outputs hazır; final hand-polished role icons per-role asset üretiminden sonra değişebilir.
- VIS-208 şu an visual-development warning layer olarak çalışır; final runtime prefabs geldikten sonra gerekli actor hataları hard failure'a çevrilmelidir.
- VIS-211/VIS-231 model closeout için güncel üretim handoff'u `docs/VIS211_231_RUNTIME_ACTOR_CLOSEOUT_PLAN_2026-08-17.md` dosyasıdır. Knight/Raider Blender source ve FBX export'ları 2026-08-17'de yeniden üretildi; proxy baseline'ları kabul edilir ama Unity import/prefab validation, authored clip proof ve gameplay-camera evidence olmadan final runtime actor kabulü yapılmaz.

## Doğrulama

Bu indeks `DefenderPresentationBuildValidator.ValidateActorPipelineIndexCheckpoint()` ile doğrulanır. Checkpoint; gerekli dokümanları, 8 icon çıktısını, 64 px review sheet'i, direct Unity actor checkpoint loglarını, catalog role coverage'ını ve geçici capsule actor üzerinde contact shadow/aura katmanını kontrol eder.
