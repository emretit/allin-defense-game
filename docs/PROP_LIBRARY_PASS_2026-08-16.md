# VIS-408 — Greenwatch Prop Library Pass

Tarih: 2026-08-16
Kapsam: Greenwatch Valley için generated ağaç, kaya, çalı, ot ve kristal prop varyant kütüphanesi.
Durum: İncelemede — Unity import/runtime prefab bağlantısı ve sahne yerleşim doğrulaması açık.

## Amaç

Tek haritalık vertical slice için tekrar etmeyen, okunur ve mobil dostu bir prop kütüphanesi hazırlamak. Bu görev sahneye toplu set dressing yapmaz; yalnız `VIS-409` yerleşim pass'inde kullanılacak temel varyant setini üretir.

## Üretilen aileler

| Aile | Varyantlar | Dosyalar |
|---|---|---|
| Tree | A, B | `Prop_PineTall_A`, `Prop_PineTall_B` |
| Rock | A, B | `Prop_RockCluster_A`, `Prop_RockCluster_B` |
| Bush | A, B | `Prop_BushRound_A`, `Prop_BushRound_B` |
| Grass | A, B | `Prop_GrassPatch_A`, `Prop_GrassPatch_B` |
| Crystal | A, B | `Prop_CrystalCluster_A`, `Prop_CrystalCluster_B` |

Tüm kaynak `.blend` dosyaları `ArtSource/Blender`, tüm runtime aday FBX çıktıları `Assets/Art/Models/Generated` altına yazıldı.

## Tasarım kararları

- Tree ailesi north frame için uzun, okunur conifer siluetleri taşır.
- Rock ailesi alçak ve orta yükseklikte iki boundary kütlesi sunar.
- Bush ve grass aileleri yol/slot çevresinde gürültüyü büyütmeden küçük varyasyon sağlar.
- Crystal ailesi hem soğuk keep tarafı hem sıcak gate geçişi için iki farklı vurgu dili taşır.
- FBX import meta dosyalarında collider import kapalı tutuldu; bunlar görsel adaydır, path/touch collider üretmemeli.
- Kütüphane yalnız geometri ve materyal içerir; update script/davranış taşımaz.

## Metrikler

Kaynak: `docs/evidence/visual-development/prop-library-metrics-2026-08-16.json`

| Özellik | Sonuç |
|---|---|
| Toplam asset | 10 |
| Aile sayısı | 5 |
| Her ailede en az 2 varyant | Evet |
| Toplam triangle | 856 |
| Triangle overage | Yok |
| Material overage | Yok |
| Eksik meta | Yok |
| Collider import açık asset | Yok |
| Eksik kaynak `.blend` | Yok |

Kristal ailesi tek materyal prop kuralından biraz daha zengin yorumlandı; taş + kristal vurgu dili için aile bazlı soft budget kullanıldı.

## Görsel kanıt

- Contact sheet: `docs/evidence/visual-development/prop-library-sheet-2026-08-16.png`
- Ölçüm raporu: `docs/evidence/visual-development/prop-library-metrics-2026-08-16.json`

Contact sheet her aileyi iki varyantla birlikte gösterir. Bu görevte amaç thumbnail düzeyinde “aile ve varyant farkı”nı kanıtlamaktır; gerçek harita içi tekrar ritmi ve slot/güzergâh güvenliği `VIS-409` ile doğrulanacaktır.

## Açık kapılar

- Unity Editor/CLI mevcut shell’de bulunmadığı için import revalidation çalıştırılamadı.
- Runtime environment prefab bağlantısı yapılmadı.
- Prop'ların gerçek rota/slot çevresine dağıtımı ve tekrar kuralları `VIS-409`da yapılacak.
- Path/touch/collider regresyonu sahne yerleşimi sonrası PlayMode ile doğrulanacak.
