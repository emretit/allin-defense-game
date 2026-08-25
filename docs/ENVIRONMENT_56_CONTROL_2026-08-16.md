# VIS-413/414 — 5.6 Final Kontrol (Environment)

Tarih: 2026-08-16
Güncelleme: 2026-08-17
Durum: VIS-413 kamera oran testi ve VIS-414 PlayMode regresyonu koşuldu; VIS-413 screenshot seti açık

## Amaç

Harita sınırları, slot/path/regresyon ve güvenli alan davranışını 16:9, 20:9, 4:3 ekran oranlarında 5.4 sonrası görselle karşılaştırmak.

## 1) Oran kontrolü (VIS-413)

1. Unity'de Production sahnesi açılır.
2. 16:9, 20:9, 4:3 viewport ile aynı hazırlık anı ekranı çekilir.
3. Aşağıdaki objeler her oran için görünür olmalı:
   - `Greenwatch Environment`
   - `Road Hand-Laid Stone Borders`
   - `Moon Disc`
   - `Scenic Battlefield Dressing`
   - Slot 1..8 markerları
4. Battlefield alanı safe area dışında gereksiz küçülmemeli (özellikle 20:9’da).

Otomatik test desteği:
- `Assets/Tests/EditMode/CoreRulesTests.cs`
  - `CameraFramingCoversVisualDevelopmentAspectRatios`
  - `UltrawideCameraUsesMoreOfTheScreenThanSixteenByNine`
  - `CameraFramingReservesUnsafeHorizontalInsets`

## 2) Gameplay regresyonu (VIS-414)

1. PlayMode’da normal tur açılır.
2. Aşağıdaki aksiyonlar tek testte denenir:
   - slot 1..8 üstünde build
   - upgrade / sell
   - yeni rota başlangıcı
   - dokunma ile slot seçim
3. Aşağıdaki hatalar olmamalı:
   - collider kaybı (`Outpost Slot` bulunamama)
   - slotın seçilemez olması
   - rota başlangıcının engellenmesi
   - harita içindeki path/çıkış davranışında beklenmeyen duraklama

## 3) Testte referans kod

- `Assets/Tests/PlayMode/GreyboxSceneTests.cs`
  - `SessionOpensWithCompleteGreyboxContent`
  - `ProductionPresentationPreservesEconomyAndEmitsTypedSignals`
  - `ProductionPresentationAllowsBuildUpgradeSellOnEveryFixedSlot`
- `Assets/Tests/EditMode/CoreRulesTests.cs`
  - `CameraFramingCoversVisualDevelopmentAspectRatios`
- `Assets/Scripts/Presentation/AdaptiveBattlefieldCamera.cs`
- `Assets/Scripts/Grid/OutpostSlotManager.cs`

## Kabul notu

- [ ] VIS-413 screenshot seti tamamlandı.
- [x] VIS-413 için 16:9, 20:9 ve 4:3 kamera framing matematiğini kapsayan EditMode testi eklendi.
- [x] VIS-414 için sekiz fixed slotta build/upgrade/sell/rebuild akışını kapsayan PlayMode regresyon testi eklendi.
- [x] VIS-414 PlayMode regresyon sonucu tutuldu: `Builds/playmode-results-vis414.xml`.
- [x] Rota ve slot davranışı baseline davranışı bozmuyor.

## 2026-08-16 uygulama notu

`CameraFramingCoversVisualDevelopmentAspectRatios` testi 16:9, 20:9 ve 4:3 ekran oranlarında `AdaptiveBattlefieldCamera.CalculateOrthographicSize` davranışını kilitler. 20:9 telefonlarda battlefield daha büyük kalmalı; 4:3 tablet oranında rota görünürlüğü için kamera kontrollü şekilde uzaklaşmalıdır.

`ProductionPresentationAllowsBuildUpgradeSellOnEveryFixedSlot` testi production catalog ile tek oturum açar, test bütçesini artırır ve slot 1..8 için sırayla build, upgrade, sell ve yeniden build/sell akışını doğrular. Test ayrıca slot collider'larının korunmasını, slot sayısının 8 kalmasını ve `Road Hand-Laid Stone Borders` ile `Moon Disc` runtime objelerinin sahnede kalmasını kontrol eder.

2026-08-17 koşusunda `Builds/editmode-results-vis413.xml` içinde `CameraFramingCoversVisualDevelopmentAspectRatios` 1/1 geçti. Aynı gün `Builds/playmode-results-vis414.xml` içinde `ProductionPresentationAllowsBuildUpgradeSellOnEveryFixedSlot` 1/1 geçti. VIS-413 kapanışı için hâlâ gerçek 16:9, 20:9 ve 4:3 hazırlık/yoğun screenshot seti beklenir.
