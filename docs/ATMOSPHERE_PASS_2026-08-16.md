# VIS-411 — Atmosphere Pass

Tarih: 2026-08-16
TODO: VIS-411
Durum: Atmosphere adayları generator’a eklendi; metrik kaydı üretim adımında.

## Amaç

Tek haritada oyun okunurluğunu koruyarak gece atmosferini tek tek güçlendirmek;
silüetleri bozmayacak, gameplay alanını kapatmayacak, low-cost bir arka plan katmanı elde etmek.

## Üretilen adaylar

| Aday | Rol | Dosya | Kaynak |
|---|---|---|---|
| `Atmosphere_MoonDisc` | Uzak atmosfer kimliği + krater okuyucu dil | `Assets/Art/Models/Generated/Atmosphere_MoonDisc.fbx` | `ArtSource/Blender/Atmosphere_MoonDisc.blend` |
| `Atmosphere_FogRibbon` | Ufuk şerit havası + derinlik ayırıcısı | `Assets/Art/Models/Generated/Atmosphere_FogRibbon.fbx` | `ArtSource/Blender/Atmosphere_FogRibbon.blend` |
| `Atmosphere_DustVeil` | Lane ön planını bozmayan toz tabakası | `Assets/Art/Models/Generated/Atmosphere_DustVeil.fbx` | `ArtSource/Blender/Atmosphere_DustVeil.blend` |

## Kural seti (VIS-411)

- Ana savaş oku ve hero odakları okunabilir kalmalı.
- Toplam overdraw ve alpha doluluk oranı, oyuncu odaklı 1.3 m² kamera viewport’unun üzerinde olmamalı.
- `Atmosphere_DustVeil` Low profilinde kapatılabilir olmalı; High/Medium’de düşük yoğunluk ile açık kalmalı.
- Point light ile değil, mevcut emisyon/ton profilinde okunaklılık korunmalı.
- Collider eklenmeyecek, pathing/gövdeye dokunulmayacak.

## Kanıt dosyaları

| Kanıt | Yol | Açıklama |
|---|---|---|
| Üretim script’i | `tools/art_pipeline/generate_lowpoly_assets.py` | `ASSETS` sözlüğüne `Atmosphere_MoonDisc`, `Atmosphere_FogRibbon`, `Atmosphere_DustVeil` eklendi |
| Metrik script’i | `tools/art_pipeline/inspect_atmosphere_assets_metrics.py` | Triangle/material/hash/meta/mesh-bounds raporu |
| Metrik JSON | `docs/evidence/visual-development/atmosphere-asset-metrics-2026-08-16.json` | VIS-411 adaylarının teknik kabul listesi |
| Manifest ve prefab kararı | `docs/evidence/visual-development/atmosphere-asset-metrics-2026-08-16.json` | Runtime'a geçmeden önce prefab slot/quality kapıları için başvuru |

## Geçiş öncesi kabul kriterleri

- Üç atmosphere varlığı birlikte veya ayrı açıldığında oyun merkezini kapatmamalı.
- Siluetlerde sadece doku değil, kontur ve boyut farkıyla da ayrım var.
- Şekiller Low/Medium/High profilinde kapatma-bekleme listesine uygun (Dust ve fill layer) olmalı.
- `.meta` dosyasında `addColliders` ve `importMaterial` gibi ayarlar güvenli olmalı.

## Yeniden üretim

```bash
blender -b --factory-startup --python tools/art_pipeline/generate_lowpoly_assets.py -- \
  --output-assets Assets/Art/Models/Generated \
  --source ArtSource/Blender \
  --only Atmosphere_MoonDisc Atmosphere_FogRibbon Atmosphere_DustVeil

blender -b --factory-startup --python tools/art_pipeline/inspect_atmosphere_assets_metrics.py -- \
  --output docs/evidence/visual-development/atmosphere-asset-metrics-2026-08-16.json
```
