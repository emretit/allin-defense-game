# ALLIN: Defense — HUD Readability Pass

Tarih: 2026-08-16
Kapsam: iPhone 17 Pro Simulator, mevcut responsive HUD korunarak yapılan ilk okunabilirlik polish'i.

## Amaç

Bu pass'in amacı HUD'u final görsel kaliteye taşımak değil; mevcut ekranda ilk bakışta okunması gereken bilgileri daha net hale getirmektir:

- Altın, kale canı, dalga ve faz bilgisinin üst barda daha hızlı taranması.
- Build, ability, pause, next wave, upgrade ve sell butonlarının ortak state diline yaklaşması.
- Yetersiz kaynak / cooldown / hazır durumlarının yalnız renge bağlı kalmaması.
- Safe-area ve mevcut tam ekran battlefield yaklaşımının korunması.

## Yapılan değişiklikler

Kod: `Assets/Scripts/Presentation/DefenderHudController.cs`, `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs`

- Üst bar metinlerinde label ve sayı hiyerarşisi ayrıldı.
- Faz metni daha kısa ve yüksek kontrastlı hale getirildi.
- Panel/rail renkleri gece fantasy tonuna daha yakın, daha koyu bir kontrasta çekildi.
- Bütün ana butonlara ortak renk state'i ve alt durum şeridi eklendi.
- Build butonları artık uygun/yetersiz kaynak durumunu metin ve şerit ile ayırıyor.
- Kalkan Darbesi butonu ready/cooldown/returning durumunu kısa iki satırlı metinle gösteriyor.
- İlk denemede kullanılan dekoratif unicode marker'lar bazı TMP fontlarında kare glyph'e düştüğü için kaldırıldı.
- Threat preview ve geçici mesaj banner'ı üst raydan ayrılarak top-center bildirim bandına taşındı; böylece wave/faz bilgisiyle çakışma azaltıldı.

## Kanıt

| Kanıt | Yol | Not |
|---|---|---|
| İlk HUD pass, glyph sorunlu | `docs/evidence/runtime/defender-simulator-hud-readability-pass-late-2026-08-16-landscape-readable.png` | Buton/faz marker'ları kare glyph'e düştü; kabul edilmedi |
| Düzeltilmiş HUD pass | `docs/evidence/runtime/defender-simulator-hud-readability-no-glyph-late-2026-08-16-landscape-readable.png` | Kare glyph yok; state metin + şerit ile okunuyor |
| Top banner overlap pass | `docs/evidence/runtime/defender-simulator-hud-top-banner-pass-landscape-readable.png` | Threat preview ile mesaj artık üst bar metinlerinin üstüne binmiyor |
| Raw düzeltilmiş capture | `docs/evidence/runtime/defender-simulator-hud-readability-no-glyph-late-2026-08-16-raw.png` | Simulator ham framebuffer çıktısı |
| Compile log | `Logs/hud-readability-no-glyph-compile.log` | Unity batch compile hata vermeden tamamlandı |

## Kalite değerlendirmesi

Bu pass önceki baseline'a göre okunabilirliği iyileştirir:

- Altın ve kale değerleri daha hızlı fark ediliyor.
- Dalga ve hazırlık bilgisi sıkışık metin yerine daha net ayrılıyor.
- Threat preview / geçici mesaj akışı üst barla daha az yarışıyor.
- Build butonları maliyet/rol bilgisini daha düzenli gösteriyor.
- Kalkan Darbesi ready durumu artık buton üzerinde açık.

Hâlâ hedef kalite değil:

- HUD panel materyali hâlâ gerçek 9-slice art set değil; runtime tint ve mevcut sprite üstünden gidiyor.
- Alt bar oyun alanından hâlâ fazla görsel ağırlık çalıyor.
- Threat preview, cooldown fill, selected/unaffordable motion ve küçük dokunma erişilebilirlik ölçümü hâlâ eksik.
- Threat preview artık daha temiz yerleşiyor ama ayrı badge/motion dili hâlâ eksik.
- 16:9 ve 4:3 cihaz görüntüleri henüz alınmadı.

## TODO etkisi

- VIS-603, VIS-604, VIS-607 ve VIS-608 artık `Yapılıyor`.
- VIS-602 hâlâ açık; çünkü gerçek panel/rail materyal seti ve optimize 9-slice sprite üretimi yapılmadı.
- VIS-611, VIS-612 ve VIS-613 kapanış kapıları için bu pass yalnız ara kanıttır.
