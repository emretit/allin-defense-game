# ALLIN: Defense — Greenwatch Valley Paint-over Plan

Tarih: 2026-08-16
TODO: VIS-401
Durum: Paint-over plan hazır; environment üretimi ve Unity doğrulaması sonraki görevlerde.

## Amaç

Greenwatch Valley'in görsel geliştirmesi rota, sekiz build slotu, knight/command noktaları, kale ve düşman girişi okunurluğunu bozmadan yapılacak. Bu belge `VIS-401` için üstten ve izometrik paint-over kararını sabitler; runtime pathing, collider, slot ve kamera davranışını değiştirmez.

## Kanıt dosyaları

| Kanıt | Yol | Açıklama |
|---|---|---|
| Paint-over plan PNG | `docs/evidence/visual-development/greenwatch-valley-paintover-plan-2026-08-16.png` | Üstten bölge/slot/rota planı + izometrik intent strip |
| Paint-over plan JSON | `docs/evidence/visual-development/greenwatch-valley-paintover-plan-2026-08-16.json` | Rota, slotlar, command points, bölgeler, landmark ve prop-band koordinatları |
| Üretim aracı | `tools/art_pipeline/render_environment_paintover_plan.py` | Plan PNG/JSON çıktısını tekrar üretir |

## Sabit oynanış geometrisi

### Rota

Rota korunacak kırıklı hat:

`(-2,2) → (4,2) → (4,8) → (10,8) → (10,3.5) → (16,3.5) → (16,9) → (24,9)`

Bu rota çevresinde yol kenarı, cobble, travel-wear ve alçak dekor kullanılabilir; fakat yolun merkez değeri ve düşman akış yönü okunmaz hale getirilmeyecek.

### Sekiz build slotu

| Kod | Slot ID | Pozisyon X/Z | Rol | Koruma kuralı |
|---|---|---:|---|---|
| S1 | `slot_valley_west_entry` | 2.5 / 2.1 | Frontline | İlk öğretici slot; çevresinde sıcak gate noise düşük tutulur |
| S2 | `slot_valley_west_ridge` | 5.5 / 2.8 | Ranged | Yol dönüşü yanında ama foreground props tarafından kapanmayacak |
| S3 | `slot_valley_north_bend` | 6.6 / 5.9 | Control | Mid-bend kontrol slotu; büyük kaya/ağaç ring içine girmeyecek |
| S4 | `slot_valley_mid_cross` | 10.0 / 3.3 | Frontline | Orta düğüm; slot ve road-edge değer kontrastı yüksek kalacak |
| S5 | `slot_valley_east_ridge` | 13.8 / 4.4 | Ranged | Son bölgeye geçiş nişancı slotu; boss/keep sinyaliyle karışmayacak |
| S6 | `slot_valley_gate_low` | 15.6 / 1.0 | Control | Alt foreground band'a yakın; foreground silhouette slotu kapatmayacak |
| S7 | `slot_valley_gate_high` | 18.2 / 4.6 | Ranged | Boss yaklaşımında boş alan ister; VFX/prop yoğunluğu sınırlı |
| S8 | `slot_valley_final_stand` | 19.2 / 7.9 | Frontline | Final stand slotu; keep ve boss okunurluğuna bağlı ana karar noktası |

## Bölge kararları

| Bölge | Ad | Görsel görev | Gürültü kuralı |
|---|---|---|---|
| A | West Entry Tutorial | Düşman kapısı, ilk yol ve ilk slotu öğretir | Sıcak kırmızı/turuncu portal, düşük kaya, az prop; S1 ve gate okunur |
| B | Mid Control Bend | Yolun ritmini ve kontrol kararını anlatır | Büyük zemin varyasyonu ve küçük prop kümeleri slot çemberleri dışında kalır |
| C | Final Stand / Boss Read | Kale, boss yaklaşımı ve son savunmayı yükseltir | Uzun siluetler keep arkasına/kenara; boss yaklaşımı ve S7/S8 temiz kalır |

## Landmark hiyerarşisi

| Landmark | Pozisyon | Renk/değer dili | Kullanım |
|---|---:|---|---|
| Enemy Gate | -2.4 / 2.0 | Sıcak kırmızı/turuncu, koyu metal | Dalga başlangıcını ve tehdit yönünü anlatır |
| Moon/Ridge | -2.8 / 13.1 | Soğuk cyan/mavi, uzak silhouette | Gece atmosferini ve üst çerçeveyi kurar |
| Defender Keep | 24.1 / 9.0 | Mavi taş, yüksek dost kütle | Savunma hedefi ve final stand odağı |

## Prop ve sınır bantları

- North frame prop band: üst sınırda pines, rocks, ridge glow ve ay silueti taşır.
- South foreground frame: düşük foreground silhouette ve parallax hissi verir; S6 ve yol alt kolunu kapatmaz.
- Rota ve slot çevresi no-clutter alanıdır: uzun ağaç, büyük kaya, parlak kristal ve sürekli VFX bu halkalara girmeyecek.
- Prop kümeleri tek tek serpilmek yerine 3–5 parçalık kontrollü kümelerle kullanılacak; boş alanlar zemin değer varyasyonu ile kırılacak.

## Sonraki üretim görevlerine etkisi

- `VIS-404`: zemin varyasyonu bölgeler A/B/C'ye göre boyanacak.
- `VIS-405–VIS-407`: ağaç/kaya/kristal/dekor kümeleri north/south band ve mid-bend boşluklarına dağıtılacak.
- `VIS-408`: düşman girişi ve defender keep landmark polish bu planın üç landmark hiyerarşisine uyacak.
- `VIS-409`: harita kenarı foreground/silhouette katmanları bu planın north/south frame bantlarını kullanacak.
- `VIS-412+`: ışık ve performans ölçümü, slot/rota okunurluğu bozulmadığı sürece yapılacak.

## Açık kapılar

- Bu plan runtime sahneye uygulanmadı; yalnız üretim yönünü sabitler.
- Unity capture ile 16:9, 20:9 ve 4:3 oranlarında rota, S1–S8 ve keep/gate görünürlüğü doğrulanmalı.
- Yeni prop üretiminden sonra pathing/collider/slot click regresyonu çalıştırılmalı.

## Yeniden üretim

```bash
python3 tools/art_pipeline/render_environment_paintover_plan.py \
  --output docs/evidence/visual-development/greenwatch-valley-paintover-plan-2026-08-16.png \
  --data docs/evidence/visual-development/greenwatch-valley-paintover-plan-2026-08-16.json
```
