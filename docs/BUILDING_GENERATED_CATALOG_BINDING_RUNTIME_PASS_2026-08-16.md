# Defender Chronicles — Building Generated Catalog Binding Runtime Pass

Status: VIS-313 / VIS-317 / VIS-323 / VIS-326 / VIS-333 / VIS-336 runtime binding pass
Date: 2026-08-16
Related TODO: VIS-304, VIS-313, VIS-317, VIS-323, VIS-326, VIS-333, VIS-336, VIS-345

## 1. Decision

Production `DefenderPresentationCatalog` artık KayKit building FBX kayıtları yerine generated building setini kullanır:

- Archer L1/L2
- Barracks L1/L2
- Mage L1/L2

Bu değişiklik `Assets/Resources/Presentation/DefenderPresentationCatalog.asset` içinde yapıldı. `TryBuildOutpost()` artık build anında generated L1 görsellerini, `ReplaceOutpostVisual()` ise upgrade sonrası generated L2 görsellerini spawn eder.

## 2. Runtime evidence

Kanıt dosyaları:

- `docs/evidence/runtime/defender-simulator-building-generated-catalog-binding-landscape-readable.png`
- `docs/evidence/runtime/defender-simulator-building-generated-catalog-binding-upgraded-state-landscape-readable.png`
- `Builds/unity-visual-building-binding.log`

İkinci screenshot kabul edilen runtime kanıtıdır. Bu görüntü iPhone 17 Pro Simulator smoke akışında build + upgrade sonrası generated building setinin sahnede göründüğünü doğrular.

## 3. Validation summary

- Unity batch compile geçti.
- iOS Simulator build geçti.
- App Simulator'a kuruldu ve açıldı.
- `DC_SMOKE=1` ile otomatik smoke çalıştı.
- Smoke sırasında build/upgrade zinciri sahnede görünür oldu ve runtime screenshot alındı.

## 4. Remaining open gates

- `VIS-345` için resmi PlayMode XML sonucu hâlâ açık. `ProductionPresentationAllowsBuildUpgradeSellOnEveryFixedSlot` testi mevcut, ancak bu turdaki batch test çağrısı sonuç XML'i üretmedi.
- İnsan kör tanıma sonucu ve final building acceptance board açık.
- Barracks/Mage final art direction polish ve build/upgrade/sell video kanıtı açık.

## 5. Practical outcome

Bu pass ile building üretim zincirindeki en büyük kopukluk kapandı: generated candidate'lar artık yalnız belge içinde değil, gerçek oyunda da production catalog üzerinden oynanabilir durumda.
