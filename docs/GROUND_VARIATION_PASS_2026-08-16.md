# Defender Chronicles — Greenwatch Ground Variation Pass

Tarih: 2026-08-16
TODO: VIS-404
Durum: Generated ground variation candidate seti hazır; runtime environment bağlantısı ve Unity doğrulaması beklemede.

## Amaç

Greenwatch Valley'in büyük düz zemin alanlarını, rota ve sekiz slot okunurluğunu bozmadan kırmak. Bu pass alçak relief ve renk/value overlay adayları üretir; collider, pathing, slot ve kamera davranışı değiştirmez.

## Üretilen assetler

| Bölge | FBX | Blender source | Görsel görev |
|---|---|---|---|
| West Entry Tutorial | `Assets/Art/Models/Generated/GreenwatchGround_West.fbx` | `ArtSource/Blender/GreenwatchGround_West.blend` | Düşman girişi ve S1/S2 çevresinde sıcak gate scuff + düşük yeşil değer kırılımı |
| Mid Control Bend | `Assets/Art/Models/Generated/GreenwatchGround_Mid.fbx` | `ArtSource/Blender/GreenwatchGround_Mid.blend` | S3/S4 çevresinde büyük ama düşük kontrastlı kontrol-bend varyasyonu |
| Final Stand / Boss Read | `Assets/Art/Models/Generated/GreenwatchGround_Final.fbx` | `ArtSource/Blender/GreenwatchGround_Final.blend` | S5–S8 ve keep yaklaşımında soğuk gölge/grass/value alanları |

## Kanıt dosyaları

| Kanıt | Yol | Açıklama |
|---|---|---|
| Metrik raporu | `docs/evidence/visual-development/ground-variation-metrics-2026-08-16.json` | Üç FBX için triangle/material/hash/meta ve collider import gate raporu |
| Contact sheet | `docs/evidence/visual-development/ground-variation-sheet-2026-08-16.png` | Ground patch adayları, rota ve 8 slot overlay'iyle üstten görünüm |
| Üretim script'i | `tools/art_pipeline/generate_lowpoly_assets.py` | `GreenwatchGround_West/Mid/Final` generated assetlerini üretir |
| Metrik script'i | `tools/art_pipeline/inspect_ground_variation_metrics.py` | Blender üzerinden FBX ölçümü yapar |
| Render script'i | `tools/art_pipeline/render_ground_variation_sheet.py` | Ground variation contact sheet üretir |

## Bütçe sonucu

| Asset | Triangle | Material | Collider import | Durum |
|---|---:|---:|---|---|
| GreenwatchGround_West | 36 | 4 | Kapalı | Geçti |
| GreenwatchGround_Mid | 45 | 4 | Kapalı | Geçti |
| GreenwatchGround_Final | 45 | 4 | Kapalı | Geçti |

Toplam: 126 triangle. Her bölge 120 triangle soft budget ve 4 material soft budget içinde.

## Tasarım kuralları

- Zemin varyasyonu yolun ana earth değerini bastırmayacak.
- Slot no-clutter ringleri patch üstünde de okunacak.
- Patch'ler büyük dekor yerine düşük relief/color value kırılımı olarak kalacak.
- Tall prop, collider, Update script veya gameplay object bu pass kapsamında yok.
- Final runtime bağlantısında material slotları gerekiyorsa atlas veya shared palette üzerinden konsolide edilecek.

## Açık kapılar

- Generated FBX'ler production environment prefabına bağlanmadı.
- Unity 16:9, 20:9 ve 4:3 screenshot doğrulaması bekliyor.
- Slot click/tap, route/pathing ve collider regression bekliyor.
- Profiler/Frame Debugger ile environment draw-call/material etkisi ölçülmedi.

## Yeniden üretim

```bash
blender -b --factory-startup --python tools/art_pipeline/generate_lowpoly_assets.py -- \
  --output-assets Assets/Art/Models/Generated \
  --source ArtSource/Blender \
  --only GreenwatchGround_West GreenwatchGround_Mid GreenwatchGround_Final

blender -b --factory-startup --python tools/art_pipeline/inspect_ground_variation_metrics.py -- \
  --output docs/evidence/visual-development/ground-variation-metrics-2026-08-16.json

blender -b --factory-startup --python tools/art_pipeline/render_ground_variation_sheet.py -- \
  --output docs/evidence/visual-development/ground-variation-sheet-2026-08-16.png
```
