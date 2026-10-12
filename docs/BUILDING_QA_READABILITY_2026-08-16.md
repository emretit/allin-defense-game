# ALLIN: Defense — Building QA Readability

Tarih: 2026-08-16
TODO: VIS-344
Kapsam: generated building candidate seti; production catalog bağlantısı ve Unity runtime doğrulaması beklemede.

## Amaç

Altı yapı görünümünün oyun kamerası mantığında, renkli görünümde, gri ton/siluet görünümünde ve küçük thumbnail ölçeğinde birbirinden ayrılıp ayrılmadığını kanıtlamak için tekrar üretilebilir bir QA seti hazırlandı.

Bu belge final kabul sonucu değildir. `VIS-344` için gerekli kör tanıma altyapısını ve görsel kanıtı oluşturur; insan kör tanıma sonucu, catalog binding ve sekiz slot runtime regresyonu hâlâ açık kapıdır.

## Üretilen kanıtlar

| Kanıt | Yol | Açıklama |
|---|---|---|
| QA contact sheet | `docs/evidence/visual-development/building-qa-sheet-2026-08-16.png` | Altı generated building candidate için renkli, gri ton, küçük thumbnail ve A-F kör quiz satırı |
| Kör tanıma cevap anahtarı | `docs/evidence/visual-development/building-blind-recognition-key-2026-08-16.json` | A-F kodlarının yapı türü, level, SHA-256 ve Unity `.meta` GUID kaydı |
| Üretim aracı | `tools/art_pipeline/render_building_qa_sheet.py` | Blender ile QA sheet ve answer-key çıktısını yeniden üretir |

## Kör quiz dizilimi

Kör quiz satırı deterministic seed `20260816` ile karıştırıldı. Testi yapacak kişi önce PNG'deki A-F satırını okuyup her kod için `Archer/Barracks/Mage` ve `L1/L2` tahmini vermeli; aşağıdaki anahtar yalnız sonuç karşılaştırması için kullanılmalı.

| Kod | Yapı | Beklenen okuma |
|---|---|---|
| A | Archer L1 | İnce gözetleme kulesi, düşük kütle, bow/target dili |
| B | Mage L2 | Dikey arcane sanctuary, çoklu kristal ve yüksek siluet |
| C | Archer L2 | Daha yüksek/çatılı okçu karakolu, daha büyük banner/weapon profili |
| D | Barracks L1 | Alçak muhafız noktası, yatay kütle, kalkan/mızrak dili |
| E | Barracks L2 | Güçlendirilmiş kışla, ağır çatı/kule/weapon-rack kütlesi |
| F | Mage L1 | Açık kristal tapınak, rune halka ve tek kristal odağı |

## Ön değerlendirme

| Kontrol | Durum | Not |
|---|---|---|
| Renkli role ayrımı | Hazır | Archer mavi/ahşap/okçu aparatları, Barracks mavi/taş/kalkan-kütle, Mage lacivert/cyan kristal diliyle ayrılıyor |
| Gri ton / siluet | Hazır | Archer kule, Barracks yatay/çatı kütlesi, Mage dikey kristal gövde olarak ayrılıyor |
| Thumbnail ölçeği | Hazır | 160–220 px aralığını temsil eden küçük row üretildi; insan kör tanıma sonucu bekliyor |
| Level ayrımı | Hazır aday | L2 görünümler L1'den yalnız renk/ölçekle değil yükseklik, ek parça, roof/banner/kristal/kütle farkıyla ayrılıyor |
| Catalog binding | Bekliyor | Production catalog hâlâ generated candidate'lara bağlanmadı |
| Unity runtime | Bekliyor | Slot hizası, click target, build/upgrade/sell davranışı ve PlayMode regression henüz çalıştırılmadı |

## Kabul için kalanlar

- Bir kişi PNG'deki kör quiz satırını answer-key'e bakmadan çözmeli ve sonuç bu belgeye eklenmeli.
- Production outpost catalog/prefab bağlantısı Unity üzerinden yapılmalı.
- L1→L2 upgrade sırasında slot merkezi, projectile/cast socket ve click target korunmalı.
- Sekiz build slotunda build/upgrade/sell regresyon testi çalıştırılmalı.
- Unity screenshots: hazırlık, build, upgrade ve sell akışı aynı kalite profilinde alınmalı.

## Yeniden üretim

```bash
blender -b --factory-startup --python tools/art_pipeline/render_building_qa_sheet.py -- \
  --output docs/evidence/visual-development/building-qa-sheet-2026-08-16.png \
  --answer-key docs/evidence/visual-development/building-blind-recognition-key-2026-08-16.json
```
