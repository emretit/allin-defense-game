# VIS-409 — Greenwatch Environment Dressing Pass

Tarih: 2026-08-16
Kapsam: Greenwatch Valley için prop kümelerinin dönüşümü, tekrarı ve yerleşim kuralları.
Durum: İncelemede — Haritaya uygulanabilir adaylar hazır, runtime prefab bağlaması ve PlayMode regresyonu bekliyor.

## Amaç

`VIS-408` ile hazırlanmış 10 adet prop varyantından yararlanarak rota, slot ve landmark okuma
bozulmadan kontrolü yüksek bir dağılım üretmek. Bu pass, sahneye doğrudan uygulanabilir bir “dressing manifestosu” sunar.

## Kapsam

- Bu görev yalnız yerleşim manifestosu ve aday transformlar üretir; yeni geometri üretimi yapmaz.
- Mevcut gameplay davranışı, rota, collider, slot tap alanı ve kamera ayarları değişmez.
- Giriş/slot/path çevresinde “aynı prop tekrarı” ve “bloklama” kontrolüne öncelik verilir.

## Yerleşim kuralları (VIS-409)

### Genel kurallar

- `path_clear_radius` = 1.30
  Her propun merkezinden yol polylines’ına minimum 1.30 birim mesafe.
- `slot_clear_radius` = 1.25
  Her slot merkezinden min 1.25 birim + varlık yarıçapı (`radius_class`) kadar uzaklık.
- `slot_hit_margin` = 0.25
  Dokunma güvenliği için slot çevresine ilave pay.
- `no_repeat_radius` = 1.30
  Aynı asset ID’leri bu mesafeden yakınsa ardışık tekrar olarak kabul edilir.
- `north_frame_min_z` = 10.9 / `south_frame_max_z` = 1.05
  Çerçeve/silhouette birimleri bu bantlarda yoğunlaştırılır.
- `max_cluster_size` = 4
  Tek bir kümeyi aşırı kalabalık yapmamak için.

### Rotation/scale kuralı

- Her yerleşimde rota/slot temizliğini bozmayacak şekilde +\-30° ve +\-90° arası rotasyon kullanılır.
- Ağaç (Tree): `scale 0.90–1.20`
- Kaya (Rock): `scale 0.72–1.10`
- Çalı (Bush): `scale 0.60–0.95`
- Ot (Grass): `scale 0.50–0.85`
- Kristal (Crystal): `scale 0.60–0.90`

## Manifesto (manifest + varyantlar)

- Aileler sırası her yama için karışık tutularak arka arkaya aynı formdan kaçınıldı.
- Ağaç ve kaya kümeleri foreground / north frame’de, çalı/ot kümeleri kontrollü yan zonlarda,
  kristaller S5–S8 geçiş alanı ve frame geçişlerinde kullanıldı.
- Her asset ID en az bir kez yer alır; her family içinde ikişer temel varyant kullanılmaya devam eder.

## Kümeler ve aday transformlar

Yerleşim girişi: `docs/evidence/visual-development/environment-dressing-layout-2026-08-16.json`

| Küme | Bölge | Aile/Varyant | X | Z | Rot (°) | Scale |
|---|---|---|---:|---:|---:|---:|
| C01 – North Pine 1 | A | `Prop_PineTall_A` | -1.5 | 12.1 | -18 | 1.02 |
| C01 – North Pine 2 | A | `Prop_PineTall_B` | 1.8 | 11.9 | 34 | 0.97 |
| C01 – North Rock 1 | A | `Prop_RockCluster_A` | 4.8 | 11.6 | 54 | 1.02 |
| C02 – North Rock 2 | A | `Prop_RockCluster_B` | 7.6 | 11.8 | -38 | 0.90 |
| C02 – North Crystal | A | `Prop_CrystalCluster_B` | 10.2 | 11.9 | 16 | 0.74 |
| C02 – North Grass | A | `Prop_GrassPatch_A` | 13.4 | 11.4 | 72 | 0.62 |
| C03 – Mid Bush 1 | B | `Prop_BushRound_B` | 4.7 | 4.5 | 18 | 0.75 |
| C03 – Mid Grass 1 | B | `Prop_GrassPatch_B` | 6.3 | 5.2 | -26 | 0.64 |
| C03 – Mid Bush 2 | B | `Prop_BushRound_A` | 7.3 | 6.0 | 56 | 0.82 |
| C03 – Mid Rock | B | `Prop_RockCluster_B` | 8.9 | 6.2 | -12 | 0.96 |
| C03 – Mid Grass 2 | B | `Prop_GrassPatch_A` | 11.2 | 5.8 | 34 | 0.58 |
| C03 – Mid Bush 3 | B | `Prop_BushRound_B` | 14.9 | 5.8 | 11 | 0.68 |
| C04 – West Frame | A | `Prop_PineTall_A` | 2.9 | 0.6 | 5 | 0.92 |
| C04 – West Bush | A | `Prop_BushRound_B` | 4.4 | 1.8 | -74 | 0.63 |
| C05 – Final Rock 1 | C | `Prop_RockCluster_A` | 19.1 | 10.6 | -54 | 0.77 |
| C05 – Final Pine | C | `Prop_PineTall_B` | 21.6 | 10.4 | 21 | 1.06 |
| C05 – Final Crystal 1 | C | `Prop_CrystalCluster_A` | 20.2 | 7.4 | 22 | 0.82 |
| C05 – Final Crystal 2 | C | `Prop_CrystalCluster_B` | 23.8 | 10.9 | 31 | 0.68 |

Toplam yerleştirme adedi: 18.

## Metri̇k / kanıt dosyaları

- Yerleşim manifestosu: `docs/evidence/visual-development/environment-dressing-layout-2026-08-16.json`
- Yerleşim denetim raporu: `docs/evidence/visual-development/environment-dressing-layout-metrics-2026-08-16.json`
- Görsel plan: `docs/evidence/visual-development/environment-dressing-layout-2026-08-16.png`

Yerleşim manifestosu, `VIS-401` rota/slot/sinir bandı kararlarıyla uyumludur; pathing, slot ve collider regresyonu sahne seviyesinde run edilmelidir.

## Açık kapılar

- Unity Editor/CLI bu shell’de bulunmadığı için runtime prefab binding ve pathing/collider regresyonu bekler.
- Slot click/tap, kamera oranı ve route readability testleri kapı olarak açık.
- `route/readability` ve `VIS-414` için PlayMode regresyonu beklenir.
