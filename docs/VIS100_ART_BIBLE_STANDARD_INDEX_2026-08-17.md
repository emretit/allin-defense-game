# ALLIN: Defense - VIS-100 Art Bible Standard Index

Tarih: 2026-08-17
Son mutabakat: 2026-08-25

Bu indeks, VIS-101-VIS-111 arasındaki art bible ve üretim kuralı teslimatlarını tek yerden izler. Amaç mevcut belgeleri yeniden yazmak değil, hangi kabul kanıtının hangi VIS maddesini desteklediğini açıkça bağlamaktır.

## Kanıt Haritası

| VIS | Durum | Ana kanıt | Kapsam |
|---|---|---|---|
| VIS-101 | Tamamlandı | `docs/VISUAL_ART_BIBLE_2026-08-16.md` | Core visual sentence, 12 referanslı moodboard, kullanılacak/kopyalanmayacak nitelikler |
| VIS-102 | Tamamlandı | `docs/VISUAL_ART_BIBLE_2026-08-16.md` | Savunmacı, düşman, doğa, yol, büyü, kaynak, tehlike ve HUD metin paleti; hex ve linear RGB tabloları |
| VIS-103 | Tamamlandı | `docs/VISUAL_ART_BIBLE_2026-08-16.md` | Dünya, yol, slot, actor, boss telegraph ve HUD için value hiyerarşisi ve thumbnail test hedefleri |
| VIS-104 | Tamamlandı | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/ACTOR_SOCKET_STANDARD_2026-08-16.md`, `docs/BUILDING_PIPELINE_STANDARD_2026-08-16.md` | Blender-Unity ölçek, pivot, forward axis, actor/building/socket export kuralları |
| VIS-105 | Bloke | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json`, `docs/evidence/visual-development/building-budget-report-2026-08-16.json`, `docs/VIS807_PHYSICAL_READINESS_CHECK_2026-08-25.md` | Provisional actor, building, prop ve VFX geometri/material bütçeleri mevcut; device-derived bütçe için fiziksel iPhone Profiler + Frame/Rendering Debugger kapısı bloke |
| VIS-106 | Tamamlandı | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/ACTOR_MATERIAL_IMPORT_STANDARD_2026-08-16.md`, `docs/BUILDING_MATERIAL_IMPORT_STANDARD_2026-08-16.md` | URP shader, atlas, material slot, emissive ve mobile import kuralları; actor <=2 ve building <=3 material hedefleri |
| VIS-107 | Tamamlandı | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/ACTOR_ICON_RENDER_STANDARD_2026-08-16.md`, `docs/evidence/visual-development/building-qa-sheet-2026-08-16.png`, `docs/evidence/visual-development/actor-icon-64px-review-final-2026-08-25.png` | Actor/building color, grayscale, 160-220 px ve 64 px thumbnail review şablonları |
| VIS-108 | Tamamlandı | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/ACTOR_ANIMATION_CONTRACT_2026-08-16.md`, `docs/ACTION_ANIMATION_TIMING_CONTRACT_2026-08-17.md` | Clip adları, loop/root-motion kuralları, presentation-only event sorumluluğu ve impact window tablosu |
| VIS-109 | Tamamlandı | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/BUILD_VFX_STANDARD_2026-08-16.md`, `docs/UPGRADE_VFX_STANDARD_2026-08-16.md`, `docs/SELL_VFX_STANDARD_2026-08-16.md` | Fiziksel, ok, arcane, heal, regen, slow, disrupt, build/upgrade/sell, spawn/death ve boss VFX aileleri |
| VIS-110 | Tamamlandı | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/UI_FULL_SCREEN_RESEARCH_2026-08-16.md`, `docs/HUD_MATERIAL_FRAME_PASS_2026-08-16.md` | HUD normal/pressed/selected/disabled/cooldown/unaffordable, 9-slice ve safe-area state kuralları |
| VIS-111 | Tamamlandı | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/HUD_ACCESSIBILITY_AUDIT_2026-08-16.md`, `docs/evidence/visual-development/vis813-accessibility-grayscale-contact-sheet-2026-08-25.png` | Kontrast, touch target, color-independence, cooldown, warning, motion ve selection checklist'i |

## Açık Kapı

- VIS-105 bütçeleri şu an authoring/proxy bütçeleridir; iPhone 13 sınıfı gerçek cihaz Profiler ve Frame/Rendering Debugger baseline'ı geldikten sonra device-derived bütçe tablosuna çevrilmelidir.

## Downstream Kontroller

- VIS-106 materyal/import standardı kapandı; runtime texture/platform override optimizasyonu ayrıca VIS-804 altında izlenir.
- VIS-107 thumbnail standardı kapandı; bağımsız kör tanıma imzası ayrıca VIS-290/VIS-813 altında izlenir.
- VIS-108/VIS-109 animasyon ve VFX sözleşmeleri kapandı; final yoğun savaş ve cihaz maliyeti ayrıca VIS-515/VIS-807 altında izlenir.
- VIS-110/VIS-111 HUD ve erişilebilirlik standartları kapandı; gerçek assistive-tech/human review kapısı ayrıca VIS-813 altında izlenir.

## Doğrulama

Bu indeks `DefenderPresentationBuildValidator.ValidateArtBibleStandardIndexCheckpoint()` ile doğrulanır. Checkpoint; ana art bible bölümlerini, bağlı standart dosyalarını, 12 referans eşiğini ve bu indeksin VIS-101-VIS-111 satırlarını kontrol eder.
