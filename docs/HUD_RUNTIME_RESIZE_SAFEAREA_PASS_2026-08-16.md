# Defender Chronicles — HUD Runtime Resize / Orientation / Safe-Area Pass

Tarih: 2026-08-16
Kapsam: `VIS-612`
Durum: İnceleme, test kapısı yeşil

## Özet

`DefenderHudResponsiveLayout.CalculateLayoutMetrics(...)` saf bir EditMode hesaplama metodu olarak eklenmiştir. Metot; root boyutu, safe-area rect ve çözünürlüğü alarak safe-area içi içerik sınırlarını, önizleme/mesaj genişliklerini ve konumlarını hesaplar.

Bu pass'in amacı: runtime resize, orientation ve safe-area değişimlerinde HUD'ın beklenen sınırları koruyup korumadığını testlenebilir hale getirmek.

## Eklenen değişiklikler

- `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs`
  - `LayoutMetrics` struct
  - `CalculateLayoutMetrics(Vector2 rootSize, Rect safeArea, Vector2Int screenSize)`

## Test kapsamı

`Assets/Tests/EditMode/CoreRulesTests.cs` içine `VIS-612` testleri eklendi:

- `SafeAreaInsetsAreClampedBetweenZeroAndOne`
- `PreviewAndMessageWidthsDoNotExceedSafeWidth`
- `TopContentReservesTopInset`
- `BottomContentReservesBottomInset`
- `SelectedPanelStaysInsideSafeArea`
- `SixteenByNineHasWiderSafeContentThanUltrawide`
- `TwentyByNineHasWiderSafeContentThanSixteenByNine`
- `FourByThreeHasNarrowerSafeContentThanSixteenByNine`
- `CameraFramingReservesUnsafeHorizontalInsets`

## Kanıt

| Tür | Yol |
|---|---|
| iPhone runtime kanıt | `docs/evidence/runtime/defender-simulator-vis612-safearea-runtime-landscape-readable.png` |
| iPad 4:3 runtime kanıt | `docs/evidence/runtime/defender-simulator-vis612-ipad-safearea-landscape-readable.png` |
| 2026-08-17 ratio review | `docs/HUD_RATIO_CAPTURE_REVIEW_2026-08-17.md` |
| 16:9 rebuilt kanıt | `docs/evidence/runtime/defender-simulator-vis612-16x9-iphone-se-rebuilt-landscape-readable.png` |
| 20:9 rebuilt kanıt | `docs/evidence/runtime/defender-simulator-vis612-20x9-iphone-17-pro-rebuilt-landscape-readable.png` |
| 4:3 tablet rebuilt kanıt | `docs/evidence/runtime/defender-simulator-vis612-4x3-ipad-pro-13-m5-helper-rotated-landscape-readable.png` |
| EditMode test sonucu | `Builds/editmode-results.xml` — 2026-08-17, `20/20 geçti` |
| PlayMode test sonucu | `Builds/playmode-results.xml` — 2026-08-17, `5/5 geçti` |

## Notlar

- `CalculateLayoutMetrics` mevcut runtime davranışını değiştirmiyor; sadece layout hesabını tek metotta topluyor.
- Mevcut env hook'lar korunuyor:
  - `DC_ABILITY_CAPTURE`
  - `DC_PAUSE_CAPTURE`
  - `DC_SLOT_REGRESSION`
- 2026-08-17 yeniden üretiminde 16:9, 20:9 ve 4:3 tablet sınıfı capture'lar geçti. iPad Pro 13-inch (M5) için `tools/capture_ios_simulator_screenshot.sh ... --rotate-device` kullanılmalı; bu Simulator cihazını `Landscape Right` konumuna alıp readable review kopyasını doğru landscape yönde üretir.
