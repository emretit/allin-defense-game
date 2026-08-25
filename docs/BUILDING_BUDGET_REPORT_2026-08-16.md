# Defender Chronicles — Building Budget Report

Tarih: 2026-08-16
TODO: VIS-346
Kapsam: generated building candidate seti + generated build/upgrade/sell action VFX.
Final durum: generated mesh bütçesi geçiyor; Unity import/profile kapıları nedeniyle tamamlanmadı.

## Kanıt dosyaları

| Kanıt | Yol | Açıklama |
|---|---|---|
| Makine bütçe raporu | `docs/evidence/visual-development/building-budget-report-2026-08-16.json` | Building ve action VFX triangle/material/hash/meta özetleri |
| Kaynak building metrikleri | `docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json` | Altı generated building candidate için Blender ölçümü |
| Build VFX metrikleri | `docs/evidence/visual-development/build-vfx-metrics-2026-08-16.json` | BuildVfx Dust/Ring/Spark |
| Upgrade VFX metrikleri | `docs/evidence/visual-development/upgrade-vfx-metrics-2026-08-16.json` | UpgradeVfx Anchor/Column/Crest |
| Sell VFX metrikleri | `docs/evidence/visual-development/sell-vfx-metrics-2026-08-16.json` | SellVfx Ring/Chips/Glint |
| Üretim aracı | `tools/art_pipeline/build_building_budget_report.py` | Yukarıdaki metrikleri tek budget gate raporuna derler |

## Budget kapıları

| Alan | Eşik | Sonuç |
|---|---:|---|
| Building per-asset triangle soft budget | 800 | Geçti; 6/6 asset ≤800 |
| Building per-asset material budget | 3 | Geçti; 6/6 asset ≤3 material |
| Building total triangle soft budget | 4500 | Geçti; toplam 3088 |
| Action VFX per-asset triangle soft budget | 500 | Geçti; 9/9 asset ≤500 |
| Action VFX per-asset material budget | 3 | Geçti; 9/9 asset ≤3 material |
| Action VFX total triangle soft budget | 2500 | Geçti; toplam 2176 |

## Building özeti

| Asset | Triangle | Material | Socket gap | Durum |
|---|---:|---:|---:|---|
| Archer_L1 | 580 | 3 | 0 | Geçti |
| Archer_L2 | 792 | 3 | 0 | Geçti |
| Barracks_L1 | 332 | 3 | 0 | Geçti |
| Barracks_L2 | 636 | 3 | 0 | Geçti |
| Mage_L1 | 364 | 3 | 0 | Geçti |
| Mage_L2 | 384 | 3 | 0 | Geçti |

## Action VFX özeti

Build, upgrade ve sell için toplam 9 generated FBX ölçüldü. Hiçbiri per-asset triangle veya material soft budget aşmıyor. Bu yalnız generated mesh budget sonucudur; runtime pooling bağlantısı ve eşzamanlı VFX cost'u Unity/Profiler kanıtı bekler.

## Texture ve runtime notu

Generated FBX building candidate'ları şu an flat material diliyle çalışıyor ve dış texture dosyası referanslamıyor. Bu yüzden texture import/compression profili için Unity tarafında final atlas veya texture seti bağlanana kadar “tamamlandı” denemez.

Sürekli VFX tarafında Mage veya ambient building efektleri henüz final runtime prefab olarak profillenmedi. Bu kapı VIS-346 final kabulünden önce Unity Profiler/Frame Debugger kanıtı ister.

## Kabul için kalanlar

- Unity sonrası final catalog/prefab ölçümünde triangle sayılarının aynı budget içinde kaldığını doğrula.
- Unity import ayarlarında mesh compression, scale, material remap ve texture/atlas profili doğrula.
- Build/upgrade/sell VFX runtime pool bağlantısı ve concurrency sınırını ölç.
- Sürekli Mage/ambient VFX varsa düşük/orta kalite profillerindeki maliyetini kaydet.
- Catalog bağlantısından sonra aynı raporu final prefab/görüntü kanıtlarıyla tekrar üret.

## Yeniden üretim

```bash
python3 tools/art_pipeline/build_building_budget_report.py \
  --output docs/evidence/visual-development/building-budget-report-2026-08-16.json
```
