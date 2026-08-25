# Defender Chronicles — Greenwatch Road Variation Pass

Tarih: 2026-08-16
TODO: VIS-405
Durum: Generated road surface/edge/wear candidate seti hazır; runtime environment bağlantısı ve Unity doğrulaması beklemede.

## Amaç

Mevcut Greenwatch Valley rotasını değiştirmeden yolun mekanik dikdörtgen tekrarını azaltmak. Bu pass yol yüzeyi, taş kenar ve travel-wear/iz katmanlarını ayrı generated FBX adayları olarak üretir; collider, pathing, slot ve kamera davranışı değiştirmez.

## Üretilen assetler

| Katman | FBX | Blender source | Görsel görev |
|---|---|---|---|
| Road surface | `Assets/Art/Models/Generated/GreenwatchRoad_Surface.fbx` | `ArtSource/Blender/GreenwatchRoad_Surface.blend` | Kırıklı rota boyunca daha organik genişlik ve köşe blend'i |
| Road edges | `Assets/Art/Models/Generated/GreenwatchRoad_Edges.fbx` | `ArtSource/Blender/GreenwatchRoad_Edges.blend` | Yol kenarında tekdüze ray yerine hand-laid taş ritmi |
| Road wear | `Assets/Art/Models/Generated/GreenwatchRoad_Wear.fbx` | `ArtSource/Blender/GreenwatchRoad_Wear.blend` | Hafif iz, scuff ve omuz aşınması; rota yönünü güçlendirir |

## Kanıt dosyaları

| Kanıt | Yol | Açıklama |
|---|---|---|
| Metrik raporu | `docs/evidence/visual-development/road-variation-metrics-2026-08-16.json` | Üç FBX için triangle/material/hash/meta/collider import raporu |
| Contact sheet | `docs/evidence/visual-development/road-variation-sheet-2026-08-16.png` | Road surface/edge/wear adayları, ground variation ve 8 slot overlay'iyle üstten görünüm |
| Üretim script'i | `tools/art_pipeline/generate_lowpoly_assets.py` | `GreenwatchRoad_Surface/Edges/Wear` generated assetlerini üretir |
| Metrik script'i | `tools/art_pipeline/inspect_road_variation_metrics.py` | Blender üzerinden FBX ölçümü yapar |
| Render script'i | `tools/art_pipeline/render_road_variation_sheet.py` | Road variation contact sheet üretir |

## Bütçe sonucu

| Asset | Triangle | Material | Collider import | Durum |
|---|---:|---:|---|---|
| GreenwatchRoad_Surface | 50 | 1 | Kapalı | Geçti |
| GreenwatchRoad_Edges | 1128 | 2 | Kapalı | Geçti |
| GreenwatchRoad_Wear | 165 | 2 | Kapalı | Geçti |

Toplam: 1343 triangle. Her katman kendi soft budget'ı içinde; edge taşları en yüksek maliyetli parça olduğu için runtime binding sonrası profiler/draw-call kontrolü gerektirir.

## Tasarım kuralları

- Yol mekanik olarak aynı kırıklı hattı takip eder: `(-2,2) → (4,2) → (4,8) → (10,8) → (10,3.5) → (16,3.5) → (16,9) → (24,9)`.
- Surface layer yolu kesintisiz ve ana okunurluk katmanı olarak kalır.
- Edge stones yol sınırını anlatır ama slot ringleriyle rekabet etmez.
- Wear patches saldırı/tehlike VFX'i gibi okunmayacak kadar düşük kontrastlıdır.
- Yeni collider, script veya runtime gameplay object yoktur.

## Açık kapılar

- Generated FBX'ler production environment prefabına bağlanmadı.
- Unity 16:9, 20:9 ve 4:3 screenshot doğrulaması bekliyor.
- Slot click/tap, route/pathing ve collider regression bekliyor.
- Road edge stones için runtime draw-call/material etkisi Profiler/Frame Debugger ile ölçülmeli.

## Yeniden üretim

```bash
blender -b --factory-startup --python tools/art_pipeline/generate_lowpoly_assets.py -- \
  --output-assets Assets/Art/Models/Generated \
  --source ArtSource/Blender \
  --only GreenwatchRoad_Surface GreenwatchRoad_Edges GreenwatchRoad_Wear

blender -b --factory-startup --python tools/art_pipeline/inspect_road_variation_metrics.py -- \
  --output docs/evidence/visual-development/road-variation-metrics-2026-08-16.json

blender -b --factory-startup --python tools/art_pipeline/render_road_variation_sheet.py -- \
  --output docs/evidence/visual-development/road-variation-sheet-2026-08-16.png
```
