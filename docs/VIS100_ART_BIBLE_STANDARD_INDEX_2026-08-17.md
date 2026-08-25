# Defender Chronicles - VIS-100 Art Bible Standard Index

Tarih: 2026-08-17

Bu indeks, VIS-101-VIS-111 arasındaki art bible ve üretim kuralı teslimatlarını tek yerden izler. Amaç mevcut belgeleri yeniden yazmak değil, hangi kabul kanıtının hangi VIS maddesini desteklediğini açıkça bağlamaktır.

## Kanıt Haritası

| VIS | Durum | Ana kanıt | Kapsam |
|---|---|---|---|
| VIS-101 | İncelemede | `docs/VISUAL_ART_BIBLE_2026-08-16.md` | Core visual sentence, 12 referanslı moodboard, kullanılacak/kopyalanmayacak nitelikler |
| VIS-102 | İncelemede | `docs/VISUAL_ART_BIBLE_2026-08-16.md` | Savunmacı, düşman, doğa, yol, büyü, kaynak, tehlike ve HUD metin paleti; hex ve linear RGB tabloları |
| VIS-103 | İncelemede | `docs/VISUAL_ART_BIBLE_2026-08-16.md` | Dünya, yol, slot, actor, boss telegraph ve HUD için value hiyerarşisi ve thumbnail test hedefleri |
| VIS-104 | İncelemede | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/ACTOR_SOCKET_STANDARD_2026-08-16.md`, `docs/BUILDING_PIPELINE_STANDARD_2026-08-16.md` | Blender-Unity ölçek, pivot, forward axis, actor/building/socket export kuralları |
| VIS-105 | İncelemede | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json`, `docs/evidence/visual-development/building-budget-report-2026-08-16.json` | Provisional actor, building, prop ve VFX geometri/material bütçeleri; gerçek cihaz profiler bütçesi VIS-006/VIS-007 sonrası yenilenecek |
| VIS-106 | İncelemede | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/ACTOR_MATERIAL_IMPORT_STANDARD_2026-08-16.md`, `docs/BUILDING_MATERIAL_IMPORT_STANDARD_2026-08-16.md` | URP shader, atlas, material slot, emissive ve mobile import kuralları; actor <=2 ve building <=3 material hedefleri |
| VIS-107 | İncelemede | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/ACTOR_ICON_RENDER_STANDARD_2026-08-16.md`, `docs/evidence/visual-development/building-qa-sheet-2026-08-16.png` | Actor/building color, grayscale, 160-220 px ve 64 px thumbnail review şablonları |
| VIS-108 | İncelemede | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/ACTOR_ANIMATION_CONTRACT_2026-08-16.md`, `docs/ACTION_ANIMATION_TIMING_CONTRACT_2026-08-17.md` | Clip adları, loop/root-motion kuralları, presentation-only event sorumluluğu ve impact window tablosu |
| VIS-109 | İncelemede | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/BUILD_VFX_STANDARD_2026-08-16.md`, `docs/UPGRADE_VFX_STANDARD_2026-08-16.md`, `docs/SELL_VFX_STANDARD_2026-08-16.md` | Fiziksel, ok, arcane, heal, regen, slow, disrupt, build/upgrade/sell, spawn/death ve boss VFX aileleri |
| VIS-110 | İncelemede | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/UI_FULL_SCREEN_RESEARCH_2026-08-16.md`, `docs/HUD_MATERIAL_FRAME_PASS_2026-08-16.md` | HUD normal/pressed/selected/disabled/cooldown/unaffordable, 9-slice ve safe-area state kuralları |
| VIS-111 | İncelemede | `docs/VISUAL_ART_BIBLE_2026-08-16.md`, `docs/HUD_ACCESSIBILITY_AUDIT_2026-08-16.md` | Kontrast, touch target, color-independence, cooldown, warning, motion ve selection checklist'i |

## Açık Kapılar

- VIS-105 bütçeleri şu an authoring/proxy bütçeleridir; iPhone 13 sınıfı gerçek cihaz profiler ve Frame Debugger baseline'ı geldikten sonra device-derived bütçe tablosuna çevrilmelidir.
- VIS-106 import kuralları yazılıdır; final texture/platform override optimizasyonu VIS-804 altında doğrulanacaktır.
- VIS-107 thumbnail şablonları hazırdır; bütün sekiz actor ve altı yapı final assetleri üretildikten sonra VIS-290/VIS-344/VIS-813 altında tekrar kabul edilecektir.
- VIS-108 ve VIS-109 aile sözleşmeleri hazırdır; role-specific authored clip/event placement ve final dense-combat video kanıtı hâlâ üretim fazına bağlıdır.
- VIS-110/VIS-111 HUD sözleşmeleri güçlü bir tabana sahiptir; final victory/defeat accessibility ve assistive-tech kapıları VIS-813 altında kalır.

## Doğrulama

Bu indeks `DefenderPresentationBuildValidator.ValidateArtBibleStandardIndexCheckpoint()` ile doğrulanır. Checkpoint; ana art bible bölümlerini, bağlı standart dosyalarını, 12 referans eşiğini ve bu indeksin VIS-101-VIS-111 satırlarını kontrol eder.
