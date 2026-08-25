# VIS-410 — Greenwatch Boundary Dressing Pass

Tarih: 2026-08-16
TODO: VIS-410
Durum: Harita kenarı ve uzak siluet adayları üretildi; manifest + kontrol + görsel kanıt üretimi tamamlandı (runtime entegrasyon ve cihaz/performans doğrulaması açık).

## Amaç

Tek haritalık vertical slice’da harita çerçevesi ve uzak okuma katmanı bozulmadan kalmalı; gameplay yolu, slotlar ve savaş alanı boşluğu korunurken kuzey-kırağı, güney foreground ve uzak siluetlerin ekranı çerçevelemesi eklenmelidir.

## Üretilmesi planlanan boundary adayları

| Aday | Rol | Dosya | Kaynak |
|---|---|---|---|
| `GreenwatchBoundary_RidgeWall_A` | Kuzey ridge/wall ana aday | `Assets/Art/Models/Generated/GreenwatchBoundary_RidgeWall_A.fbx` | `ArtSource/Blender/GreenwatchBoundary_RidgeWall_A.blend` |
| `GreenwatchBoundary_RidgeWall_B` | Kuzey ridge/wall alternatif | `Assets/Art/Models/Generated/GreenwatchBoundary_RidgeWall_B.fbx` | `ArtSource/Blender/GreenwatchBoundary_RidgeWall_B.blend` |
| `GreenwatchBoundary_ForegroundStrip_A` | South foreground alt katman A | `Assets/Art/Models/Generated/GreenwatchBoundary_ForegroundStrip_A.fbx` | `ArtSource/Blender/GreenwatchBoundary_ForegroundStrip_A.blend` |
| `GreenwatchBoundary_ForegroundStrip_B` | South foreground alt katman B | `Assets/Art/Models/Generated/GreenwatchBoundary_ForegroundStrip_B.fbx` | `ArtSource/Blender/GreenwatchBoundary_ForegroundStrip_B.blend` |
| `GreenwatchBoundary_SilhouetteWall` | Dış ridge/mountain silhouette | `Assets/Art/Models/Generated/GreenwatchBoundary_SilhouetteWall.fbx` | `ArtSource/Blender/GreenwatchBoundary_SilhouetteWall.blend` |
| `GreenwatchBoundary_ForestSilhouette` | Uzak orman-kıtalar silüeti | `Assets/Art/Models/Generated/GreenwatchBoundary_ForestSilhouette.fbx` | `ArtSource/Blender/GreenwatchBoundary_ForestSilhouette.blend` |

## Yerleşim kuralı (VIS-410)

- North frame ve south frame bantları, rota ve slot çemberlerinin okunabilirliğini gölgede bırakmayacak.
- Dış silhouette, oyunun ana alanı ile görsel olarak karıştırılmayacak; yalnız kenar çerçeve gibi okunacak.
- Aynı boundary adayının ardışık tekrarları en az `1.50` birim arayla bırakılacak.
- Path clear: en az `1.20`.
- Slot clear: en az `1.05 + 0.25` (hit margin dahil).
- Ekran kenarına göre çok uzun/tekdüze yatay striplerden kaçınılacak; her kümeye `max_items` ve `family_min` uygulanacak.

## Kural ve manifest dosyası

Kanıt kaydı: `docs/evidence/visual-development/boundary-layout-2026-08-16.json`
Bu belge içinde:

- `clusters`: N01 (north), S01 (south), W01 (west), E01 (east), F01 (far silhouette)
- 10 adet placement
- asset/variant listesi ve ölçü (scale + rotation) kayıtları
- path/slot/world/repeat kontrollerini çalıştırabilmek için kural sayıları

## Kanıt dosyaları

| Kanıt | Yol | Açıklama |
|---|---|---|
| Layout JSON | `docs/evidence/visual-development/boundary-layout-2026-08-16.json` | Placement manifest |
| Layout metric | `docs/evidence/visual-development/boundary-layout-metrics-2026-08-16.json` | Path/slot/repeat/bounds/cluster validasyonu |
| Layout sheet | `docs/evidence/visual-development/boundary-layout-2026-08-16.png` | Üstten boundary adaylarının harita bantları üstündeki preview |
| Üretim script’i | `tools/art_pipeline/inspect_boundary_layout.py` | VIS-410 manifest denetimi |
| Render script’i | `tools/art_pipeline/render_boundary_layout.py` | Boundary sheet render |
| Asset metrics | `docs/evidence/visual-development/boundary-asset-metrics-2026-08-16.json` | Altı boundary FBX için triangle/material/hash/meta ve import-risk raporu |

## Açık kapılar

- Unity prefab entegrasyonu, 16:9/20:9/tablet capture ve touch/pathing regresyonu bekliyor.
- Harita kenarı için runtime’da parallax/LOD planı sonraki görsel QA adımında netleştirilecek.

## Yeniden üretim

```bash
python3 tools/art_pipeline/inspect_boundary_layout.py \
  --layout docs/evidence/visual-development/boundary-layout-2026-08-16.json \
  --output docs/evidence/visual-development/boundary-layout-metrics-2026-08-16.json

python3 tools/art_pipeline/render_boundary_layout.py \
  --layout docs/evidence/visual-development/boundary-layout-2026-08-16.json \
  --paintover-data docs/evidence/visual-development/greenwatch-valley-paintover-plan-2026-08-16.json \
  --output docs/evidence/visual-development/boundary-layout-2026-08-16.png
```
