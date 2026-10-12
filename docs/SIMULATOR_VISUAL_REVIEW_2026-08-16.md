# ALLIN: Defense — Simulator Visual Review

Tarih: 2026-08-16
Kapsam: iPhone 17 Pro Simulator, iOS 26.5, Development iOS Simulator build

## Build ve çalışma durumu

- Unity iOS Simulator Xcode project build geçti.
- Xcode `iphonesimulator` build geçti.
- `com.emreaydin.allindefense` Simulator'a kuruldu ve açıldı.
- İlk screenshot yükleme/ilk frame sırasında koyu göründü; geç screenshot'ta oyun sahnesi render edildi.

## Evidence

| Kanıt | Yol | Not |
|---|---|---|
| İlk launch screenshot | `docs/evidence/runtime/defender-simulator-run-2026-08-16.png` | İlk frame/karanlık launch anı |
| Runtime screenshot | `docs/evidence/runtime/defender-simulator-run-late-2026-08-16.png` | Oyun sahnesi görünür |
| Rotate denemesi | `docs/evidence/runtime/defender-simulator-run-landscape-attempt-2026-08-16.png` | Framebuffer hâlâ 1206x2622 |
| Okunabilir landscape review | `docs/evidence/runtime/defender-simulator-run-late-landscape-review-readable-2026-08-16.png` | Raw screenshot 270 derece döndürülerek QA için okunabilir hale getirildi |
| HUD readability pass | `docs/evidence/runtime/defender-simulator-hud-readability-no-glyph-late-2026-08-16-landscape-readable.png` | Üst bar ve button state okunabilirlik polish'i sonrası kabul edilen görüntü |
| HUD top banner pass | `docs/evidence/runtime/defender-simulator-hud-top-banner-pass-landscape-readable.png` | Threat preview ve mesaj banner'ı üst bilgi alanından ayrıldı |
| HUD threat preview pass | `docs/evidence/runtime/defender-simulator-hud-threat-preview-pass-prep-landscape-readable.png` | İlk dalga tehdidi artık hazırlık ekranında kart olarak görünür |
| HUD phase badge pass | `docs/evidence/runtime/defender-simulator-hud-phase-badge-pass-prep-relaunch-landscape-readable.png` | Üst bardaki state alanı `DURUM / DALGA ÖNCESİ` rozeti olarak okunur |
| HUD ability state pass | `docs/evidence/runtime/defender-simulator-hud-ability-state-cooldown-pass-landscape-readable.png` | Kalkan Darbesi button'ı cooldown state'ine güvenilir biçimde düşer (`8 sn` örneği) |
| HUD pause overlay pass | `docs/evidence/runtime/defender-simulator-hud-pause-overlay-pass-landscape-readable.png` | Pause state merkez overlay + üst bar state ailesiyle birlikte okunur |
| HUD accessibility audit pass | `docs/evidence/runtime/defender-simulator-hud-pause-overlay-pass-landscape-readable.png` | Pause overlay yardımcı metni kırpılmadan okunur; küçük metin pass'i sonrası accessibility baseline |
| Selected panel visual pass | `docs/evidence/runtime/defender-simulator-selected-panel-pass3-landscape-readable.png` | Seçili karakol paneli başlık + SV + stat kartı düzenine taşındı |
| HUD material/frame pass | `docs/evidence/runtime/defender-simulator-hud-material-pass-landscape-readable.png` | Ray ve paneller artık sprite tabanlı yüzey/çerçeve dili taşıyor |
| Reddedilen HUD pass | `docs/evidence/runtime/defender-simulator-hud-readability-pass-late-2026-08-16-landscape-readable.png` | TMP font kare glyph fallback'i nedeniyle kabul edilmedi |
| Environment value pass | `docs/evidence/runtime/defender-simulator-environment-value-pass-adjusted-late-2026-08-16-landscape-readable.png` | Zemin value breakup ve düşük kontrast landmark read pool ayarlı pass |
| Reddedilen environment value pass | `docs/evidence/runtime/defender-simulator-environment-value-pass-late-2026-08-16-landscape-readable.png` | İlk zemin/read pool ayarı fazla lekesel göründü |
| Road surface runtime pass | `docs/evidence/runtime/defender-simulator-road-organic-surface-normals-late-2026-08-16-landscape-readable.png` | Eski blok yol yerine normal düzeltilmiş organic runtime surface |
| Reddedilen road surface pass | `docs/evidence/runtime/defender-simulator-road-organic-surface-late-2026-08-16-landscape-readable.png` | İlk organic road surface siyah çıktı |
| Road border rhythm pass | `docs/evidence/runtime/defender-simulator-road-cobble-rhythm-late-2026-08-16-landscape-readable.png` | Yol kenar taş ritmi daha düzensiz ve daha az mekanik |
| Lighting/material balance pass | `docs/evidence/runtime/defender-simulator-lighting-material-balance-late-2026-08-16-landscape-readable.png` | Taş/yol/zemin ve gece ışığı daha sakin value bandına çekildi |
| Generated building catalog binding pass | `docs/evidence/runtime/defender-simulator-building-generated-catalog-binding-upgraded-state-landscape-readable.png` | Generated Archer/Barracks/Mage setinin production catalog üzerinden runtime'da göründüğü kabul screenshot'ı |
| Building slot runtime regression | `Builds/ios-slot-regression-smoke.log` | iOS Simulator runtime'ında slot 1..8 build/upgrade/sell/rebuild zinciri PASS log'ları |

## Görsel kalite notu

Bu görüntü hedef kalite değildir. Mevcut durum, oynanabilir vertical slice iskeletinin Simulator'da açıldığını kanıtlar; sanat kalitesi hâlâ proxy/geliştirme seviyesindedir.

Öne çıkan problemler:

| Alan | Gözlem | Etki | Bağlı TODO |
|---|---|---|---|
| Orientation/capture | Screenshot 1206x2622 portre framebuffer'da; oyun landscape render ettiği için görüntü yan dönmüş algılanıyor | UI değerlendirmesi ve screenshot karşılaştırması güvenilir değil | VIS-413, VIS-601, VIS-808 |
| HUD | Üst bar, threat kartı, phase badge, ability cooldown ve pause overlay state'i önceye göre daha okunur; ama panel materyali, final selected-state polish, metinsiz cooldown read ve 4:3 oran kapanışı eksik | Genel HUD hissi gelişti ama final kaliteye ulaşmadı | VIS-602-VIS-610, VIS-611 |
| Environment | Yol ve zemin okunuyor ama büyük düz renk alanları, keskin blok tekrarları ve sınırlı prop çeşitliliği var | Harita hâlâ prototip/puzzle board hissi veriyor | VIS-404-VIS-411 |
| Lighting/value | Gece atmosferi var fakat actor, yol, kristal ve prop değer hiyerarşisi yeterince sinematik değil | Sahne düz ve düşük kaliteli algılanıyor | VIS-412, VIS-802, VIS-803 |
| Buildings | Generated building set runtime'da görünüyor ve sekiz slot davranış zinciri smoke log ile doğrulandı; ancak tam altı görünüm acceptance board'u, build/upgrade/sell video ve resmi PlayMode XML raporu hâlâ açık | Building pipeline büyük ölçüde ilerledi ama final kalite kapısı kapanmadı | VIS-317, VIS-326, VIS-336, VIS-345 |
| Units | Knight görünüyor; diğer unit sınıfları ve boss runtime görüntüleri bu capture'da yok | Sekiz role ait siluet kabulü kanıtlanmadı | VIS-200, VIS-290 |
| VFX/animation | Hazırlık ekranı statik; combat, ability, boss faz ve yoğun VFX kanıtı yok | Oyunun kalite hissi henüz ana aksiyon üzerinden değerlendirilemiyor | VIS-500, VIS-700 |

## HUD readability pass notu

`docs/HUD_READABILITY_PASS_2026-08-16.md` ilk HUD okunabilirlik polish'ini kaydeder. Bu pass'te üst bar sayı hiyerarşisi, build button maliyetleri, ability ready/cooldown label'ı ve button state şeritleri iyileştirildi. İlk denemede dekoratif unicode marker'lar kare glyph'e düştüğü için reddedildi; ikinci build'de bu marker'lar kaldırıldı.

Sonuç: HUD önceki baseline'a göre daha okunur, fakat final kalite değildir. Sonraki mini-pass ile threat preview ve geçici mesaj banner'ı üst bar rayından ayrıldı; kabul kanıtı `docs/evidence/runtime/defender-simulator-hud-top-banner-pass-landscape-readable.png` dosyasıdır. Yeni threat preview pass'i ile ilk dalga bilgisi hazırlık ekranında da görünür hale geldi; kabul kanıtı `docs/evidence/runtime/defender-simulator-hud-threat-preview-pass-prep-landscape-readable.png` dosyasıdır. Panel materyali hâlâ gerçek optimize 9-slice set değil; alt bar hâlâ fazla ağır; 16:9 ve 4:3 görüntüleri, cooldown fill/motion, selected/unaffordable feedback ve touch/contrast denetimleri açık.

## HUD ability state pass notu

`docs/HUD_ABILITY_STATE_PASS_2026-08-16.md` Kalkan Darbesi button'ının ready/cooldown/blocked state pass'ini kaydeder. Kabul edilen runtime kanıtı `docs/evidence/runtime/defender-simulator-hud-ability-state-cooldown-pass-landscape-readable.png` dosyasıdır; bu görüntüde ability button sahne açıldıktan sonra güvenilir biçimde cooldown state'ine düşer ve hero statüsüyle aynı anda senkron görünür.

Açık problem: fill/motion dili hâlâ final kalite seviyesinde değil; daha güçlü metinsiz okunurluk ve 4:3/tablet QA açık.

## HUD overlay state pass notu

`docs/HUD_OVERLAY_STATE_PASS_2026-08-16.md` pause, victory ve defeat overlay ailesinin ilk pass'ini kaydeder. Kabul edilen runtime kanıtı `docs/evidence/runtime/defender-simulator-hud-pause-overlay-pass-landscape-readable.png` dosyasıdır; bu görüntüde pause state yalnız button etiketiyle değil, phase badge, ability state ve merkez overlay kartıyla birlikte okunur.

Açık problem: victory/defeat için ayrı acceptance capture'ları, dar oran QA'sı ve final alpha/input polish hâlâ açık.

## HUD accessibility audit pass notu

`docs/HUD_ACCESSIBILITY_AUDIT_2026-08-16.md` production HUD üzerinde ilk küçük metin/kontrast/touch-target audit'ini kaydeder. Bu pass sonrası phase hint, preview title, hero status ve yardımcı hint satırları büyütüldü; pause overlay açıklaması artık kırpılmadan tam okunuyor. Ana foreground/background çiftleri `6.44:1` ile `17.46:1` arasında kontrast veriyor ve kritik buton hedefleri `58 px` üstünde kalıyor.

Açık problem: 4:3/tablet oran acceptance, victory/defeat accessibility capture ve gerçek assistive-tech akışı doğrulaması açık.

## HUD runtime resize/safe-area pass notu

`docs/HUD_RUNTIME_RESIZE_SAFEAREA_PASS_2026-08-16.md` runtime resize, orientation ve safe-area regresyon pass'ini kaydeder. `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs` içine `CalculateLayoutMetrics(...)` saf hesap metodu eklendi; `Assets/Tests/EditMode/CoreRulesTests.cs` içinde sekiz yeni EditMode testiyle safe-area inset, preview/message genişliği, selected panel sınırı ve 16:9/20:9/4:3 farkları kilitlendi. iOS Simulator runtime kanıtı `docs/evidence/runtime/defender-simulator-vis612-safearea-runtime-landscape-readable.png` dosyasındadır.

Açık problem: 4:3 tablet runtime QA'sı, dar oran capture seti ve final responsive motion polish hâlâ açık.

## HUD runtime resize/safe-area iPad 4:3 capture notu

`docs/evidence/runtime/defender-simulator-vis612-ipad-safearea-landscape-readable.png` iPad Pro 11-inch (M5) 4:3 runtime kanıtıdır. Bu görüntüde HUD rail'leri fiziksel ekran kenarlarına kadar genişlerken içerik safe-area sınırı içinde kalıyor.

Açık problem: final responsive motion polish hâlâ açık.


## HUD threat preview pass notu

`docs/HUD_THREAT_PREVIEW_PASS_2026-08-16.md` yaklaşan tehdit kartının hazırlık ekranına taşındığını kaydeder. Bu pass'te ilk dalga artık butona basılmadan önce de okunur; kart başlığı ve gövdesi ayrılaşır; preview ile geçici mesaj satırı daha az çakışır.

Açık problem: kart yüzeyi biraz fazla geniş; boss state için ayrı ikon/motion dili yok; combat-state ve 4:3/tablet QA hâlâ açık.

## HUD phase badge pass notu

`docs/HUD_PHASE_BADGE_PASS_2026-08-16.md` üst bardaki phase alanını küçük state badge olarak güçlendirir. Hazırlık ekranında `DURUM / DALGA ÖNCESİ` rozetinin görünmesi kabul kanıtı olarak `docs/evidence/runtime/defender-simulator-hud-phase-badge-pass-prep-relaunch-landscape-readable.png` dosyasında tutulur.

Açık problem: badge transition/motion hâlâ yok; üst bilgi ailesi preview kartı ile birlikte daha fazla rafine edilebilir; 4:3/tablet QA açık.

## Selected panel visual pass notu

`docs/SELECTED_PANEL_VISUAL_PASS_2026-08-16.md` seçili yapı panelinin ilk kartlaştırma pass'ini kaydeder. Kabul edilen görüntü `docs/evidence/runtime/defender-simulator-selected-panel-pass3-landscape-readable.png` dosyasıdır; bu görüntüde yapı türü, seviye ve temel statlar önceki düz açıklama bloğuna göre daha net ayrışır.

Açık problem: panel hâlâ gerçek materyal seti, motion state ve final erişilebilirlik denetimini almadı.

## HUD material/frame pass notu

`docs/HUD_MATERIAL_FRAME_PASS_2026-08-16.md` panelSprite ve uiFrameSprite'ın production HUD'a runtime'da bağlandığını kaydeder. `docs/evidence/runtime/defender-simulator-hud-material-pass-landscape-readable.png` görüntüsünde özellikle selected panel ve HUD rayları önceki düz tint görünümünden daha katmanlı okunur.

Açık problem: frame overlay selected panelde hâlâ biraz güçlü duruyor; final border/alpha tuning ve oran QA açık kaldı.

## Environment value pass notu

`docs/ENVIRONMENT_VALUE_RUNTIME_PASS_2026-08-16.md` production scenic dressing'e düşük kontrast zemin value mesh'leri ekler. İlk ayar fazla büyük boya lekeleri gibi göründüğü için reddedildi; ikinci ayarda patch boyutları küçültüldü ve renkler koyulaştırıldı. Son görüntü önceki baseline'a göre düz zemin hissini azaltıyor, ancak hâlâ final environment kalite kapısı değildir.

Açık kalan ana problem: yol ve zemin hâlâ gerçek materyal/atlas polish seviyesinde değil; lighting/value, 16:9/4:3 capture ve profiler gate tamamlanmadan VIS-404/VIS-405 kapanmaz.

## Road surface runtime pass notu

`docs/ROAD_SURFACE_RUNTIME_PASS_2026-08-16.md` eski blok yol renderer'larını gizleyip `Road Organic Earth Surface` runtime mesh'ini bağlar. İlk üç denemede yol surface siyah/yanlış aydınlandı; kabul edilen denemede mesh normalleri yukarı sabitlendi. Son görüntü eski dikdörtgen yol hissini azaltıyor, fakat bordür taş ritmi ve final materyal kalitesi hâlâ açık.

## Road border rhythm pass notu

`docs/ROAD_BORDER_RHYTHM_PASS_2026-08-16.md` yol kenar taşlarının sabit aralık tekrarını kırar. Son görüntüde path okunurluğu korunurken taşlar daha az metronom gibi diziliyor. Açık problem: taş rengi/ışığı hâlâ biraz fazla soğuk ve parlak; lighting/material pass'te dengelenmeli.

## Lighting/material balance pass notu

`docs/LIGHTING_MATERIAL_BALANCE_RUNTIME_PASS_2026-08-16.md` taş, yol, zemin patch ve gece ışık/post-process değerlerini birlikte dengeler. Son görüntüde yol kenarı taşları önceki kadar mavi/parlak değil ve sahne daha sakin bir gece value bandına oturuyor. Açık problem: hazırlık ekranı dışında yoğun combat, boss fazı, 16:9/4:3 oran ve profiler kanıtı hâlâ eksik.

## Generated building catalog binding pass notu

`docs/BUILDING_GENERATED_CATALOG_BINDING_RUNTIME_PASS_2026-08-16.md` generated Archer/Barracks/Mage L1/L2 setini production catalog'a bağlar. Kabul screenshot'ı `docs/evidence/runtime/defender-simulator-building-generated-catalog-binding-upgraded-state-landscape-readable.png` iPhone 17 Pro Simulator smoke akışında build+upgrade sonrası yeni setin gerçekten oyunda göründüğünü kanıtlar.

Açık problem: tek screenshot bütün altı görünümü aynı anda kanıtlamıyor; `VIS-345` için resmi PlayMode XML/video kanıtı ve final acceptance board hâlâ eksik.

## Building slot runtime regression notu

`docs/BUILDING_SLOT_RUNTIME_REGRESSION_2026-08-16.md` production catalog altındaki sekiz fixed slot için gerçek iOS Simulator runtime smoke kanıtını kaydeder. `Builds/ios-slot-regression-smoke.log` içindeki `[DC_SLOT_REGRESSION_PASS]` satırları slot 1..8 için build, L1→L2 upgrade, sell, Archer rebuild ve ikinci sell zincirinin geçtiğini gösterir.

Bu güçlü bir davranış kanıtıdır, fakat Unity batch test runner güncel XML üretmediği için resmi PlayMode report kapısı hâlâ açıktır.

## Kısa karar

1. Bu screenshot seti VIS-001/VIS-601 için kısmi baseline olarak saklanır.
2. VIS-413 kapanmadan önce Simulator landscape capture problemi çözülür.
3. Görsel kalite polish sırası: HUD panel materyali/overlay state kapanışı, building acceptance board + slot regression kanıtı, zemin/yol micro-variation, combat VFX.
4. Bu kanıt, "oyun çalışıyor" kapısını destekler; "hedef kalite" kapısını desteklemez.

## Tekrar üretim

Boot edilmiş Simulator'da raw ve okunabilir landscape review screenshot almak için:

```bash
./tools/capture_ios_simulator_screenshot.sh 20260816-review
```

## Sonraki çalışma bloğu

Önce HUD ve orientation/capture güvenilirliği ele alınmalı. Sebep: Şu an görsel kaliteyi doğru tartmak için yatay cihaz görüntüsüne ve okunabilir HUD'a ihtiyacımız var. Ardından terrain/road/lighting polish'e geçmek daha sağlıklı olur.
