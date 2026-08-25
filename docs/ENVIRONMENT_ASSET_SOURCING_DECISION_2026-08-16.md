# Defender Chronicles — Environment Asset Sourcing Decision

Tarih: 2026-08-16
TODO: VIS-402
Kapsam: Greenwatch Valley çevre üretimi için Quaternius Medieval Village MegaKit ile mevcut KayKit Medieval Hexagon kullanımını stil, ölçek, mobil maliyet ve entegrasyon riski açısından karşılaştırmak.
Karar: Quaternius bu vertical slice için doğrudan import edilmeyecek; mevcut KayKit + özgün/generated environment parçaları ana kaynak olarak kalacak.

## Doğrulanan kaynaklar

| Kaynak | URL | Lisans/format notu | Kontrol tarihi |
|---|---|---|---|
| Quaternius Medieval Village Pack | https://quaternius.com/packs/medievalvillage.html | CC0; FBX/OBJ/Blend; 44 model; kişisel/ticari kullanım bilgisi kaynak sayfada yer alıyor | 2026-08-16 |
| Quaternius Medieval Village MegaKit | https://quaternius.itch.io/medieval-village-megakit | CC0; 300+ modular parça; FBX/OBJ/glTF; Source version Unity URP/Godot/Unreal setup içeriyor | 2026-08-16 |
| KayKit Medieval Hexagon GitHub | https://github.com/KayKit-Game-Assets/KayKit-Medieval-Hexagon-Pack-1.0 | CC0; 200 low-poly model; tek gradient atlas; Unity/Godot/Unreal uyumlu | 2026-08-16 |
| KayKit Medieval Hexagon itch.io | https://kaylousberg.itch.io/kaykit-medieval-hexagon | CC0; 200+ low-poly model; tek atlas; ücretsiz standard sürüm | 2026-08-16 |
| Mevcut repo provenance | `Assets/ThirdParty/ART_ASSET_SOURCES.md` | KayKit selected files, revision, SHA-256 ve license kayıtları mevcut | 2026-08-16 |

Not: Bu görevde yeni dış dosya indirilmedi ve repository'ye yeni üçüncü taraf asset eklenmedi. Bu yüzden SHA-256/provenance yalnız mevcut KayKit dosyaları için zaten kayıtlı; Quaternius için import yapılırsa aynı değişiklikte lisans, URL, sürüm, edinme tarihi, SHA-256, kaynak yol ve işlenmiş yol kaydedilecek.

## Karşılaştırma

| Kriter | Mevcut KayKit Medieval Hexagon | Quaternius Medieval Village MegaKit | Karar |
|---|---|---|---|
| Lisans | CC0; repo içinde license korunuyor | CC0; kaynak sayfaları CC0 belirtiyor | İkisi de uygun |
| Mevcut entegrasyon | Zaten `Assets/ThirdParty/KayKit/MedievalHexagon` altında seçilmiş modeller var | Projede yok; indirme/import/provenance gerekir | KayKit düşük risk |
| Stil uyumu | Mevcut low-poly gece fantasy diliyle uyumlu; castle/tree/rock zaten sahnede | Daha modular village/duvar/çatı odaklı; ölçek ve renk dili işlenmeden karışabilir | Quaternius referans/işlenmiş aday |
| Mobil maliyet | Tek atlas ve az seçilmiş model; draw-call tahmini daha kontrollü | 300+ parça; modular kullanım draw-call/atlas/material planı ister | Doğrudan import edilmesin |
| VIS-401 planına uyum | North/south frame band, keep/gate ve küçük prop kümelerine zaten uygun parçalar var | Village kit parçaları rota/slot çevresini fazla mimari gürültüyle doldurabilir | Sadece özel ihtiyaçta seç |
| Üretim hızı | Var olan kaynaklardan hızlı polish veya generated varyant üretilebilir | Asset seçme, ayıklama, stil/ölçek remap ve Unity import gerekir | Vertical slice için KayKit/generated |
| Risk | Aynı prop tekrarları ve sınırlı çeşitlilik | Ölçek, collision, material/atlas, modular yoğunluk riski | Quaternius şimdilik beklet |

## Seçim

### Kullan

- Mevcut KayKit nature parçaları: `trees_A_small`, `trees_B_small`, `trees_A_medium`, `rock_single_A`, `rock_single_B`.
- Mevcut/generated environment parçaları: road cobbles, road wear, cliff skirt, night backdrop, moon/ridge, generated grass patches.
- Özgün/generated küçük prop varyantları: slot/rota çevresine düşük gürültülü grass, stone, crystal, stump/marker gibi kontrollü parçalar.

### Beklet

- Quaternius Medieval Village MegaKit: ücretsiz/CC0 olduğu için uygun aday, fakat vertical slice için doğrudan import edilmeyecek.
- Eğer `VIS-408` sırasında KayKit/generated prop çeşitliliği yetmezse Quaternius'tan yalnız düşük riskli parçalar seçilecek: küçük taş, fıçı/kasa, düşük duvar kırığı, ahşap parça gibi slotu kapatmayan objeler.

### Kullanma

- Quaternius modular house/wall/roof kitinin büyük blokları bu tek haritada doğrudan kullanılmayacak. Sebep: Greenwatch Valley bir savunma rotası; village duvar/çatı kalabalığı S1–S8 slot okunurluğunu ve boss/keep hiyerarşisini zayıflatabilir.
- Source/Pro sürümündeki hazır Unity/URP setup bu aşamada alınmayacak; ücretli kaynak veya büyük third-party import kapsamı bu görsel vertical slice için gereksiz.

## Uygulama kuralı

1. Önce VIS-401 paint-over bandlarına göre existing KayKit/generated prop varyantları üret.
2. Rota ve slot çevresi no-clutter alanlarını koru.
3. Her prop ailesini 3–5 parçalık küçük kümelerle kullan; tek tek mekanik tekrar görünüyorsa generated varyant üret.
4. Quaternius gerekiyorsa tek tek seç, Blender'da ölçek/stil/material remap yap, dış texture/material sayısını azalt ve provenance kaydı ekle.
5. Runtime sahneye koymadan önce collider yokluğu, draw-call/material budget ve slot click regression kapılarını açık tut.

## VIS-402 sonucu

VIS-402 için karar raporu hazır: Quaternius aday olarak belgelendi ama doğrudan import reddedildi; mevcut KayKit + generated/original environment üretimi bu vertical slice için birincil yol olarak seçildi.

## Kaynak bağlantıları

- [Quaternius Medieval Village Pack](https://quaternius.com/packs/medievalvillage.html)
- [Quaternius Medieval Village MegaKit](https://quaternius.itch.io/medieval-village-megakit)
- [KayKit Medieval Hexagon GitHub](https://github.com/KayKit-Game-Assets/KayKit-Medieval-Hexagon-Pack-1.0)
- [KayKit Medieval Hexagon itch.io](https://kaylousberg.itch.io/kaykit-medieval-hexagon)
