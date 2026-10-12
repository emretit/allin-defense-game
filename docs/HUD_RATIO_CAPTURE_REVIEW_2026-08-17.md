# ALLIN: Defense — HUD Ratio Capture Review

Tarih: 2026-08-17
Kapsam: `VIS-612`, `VIS-613`, `VIS-809`
Durum: Oran capture seti geçti; polish açık

## Özet

Unity 6000.4.1f1 ile iOS Simulator Xcode projesi yeniden üretildi ve Debug simulator build'i kuruldu. 16:9 ve 20:9 telefon oranlarında oyun render ediyor, HUD okunuyor ve ekran yanlış dönmüyor. iPad Pro 13-inch (M5) için Simulator cihazı önce `Landscape Right` durumuna alınarak capture üretildi; readable kanıt artık doğru landscape yönde ve büyük siyah bant problemi tekrar üretim akışında saklanmıyor.

## Kanıtlar

| Oran | Cihaz | Sonuç | Kanıt |
|---|---|---|---|
| 16:9 | iPhone SE (3rd generation) | Geçti | `docs/evidence/runtime/defender-simulator-vis612-16x9-iphone-se-rebuilt-landscape-readable.png` |
| 20:9 | iPhone 17 Pro | Geçti | `docs/evidence/runtime/defender-simulator-vis612-20x9-iphone-17-pro-rebuilt-landscape-readable.png` |
| 4:3 sınıfı tablet | iPad Pro 13-inch (M5) | Geçti | `docs/evidence/runtime/defender-simulator-vis612-4x3-ipad-pro-13-m5-helper-rotated-landscape-readable.png` |

## Bulgular

- 16:9 telefonda battlefield, sekiz slot ve ana HUD kontrolleri görünür.
- 20:9 telefonda battlefield genişliği iyi kullanılıyor; safe-area kenarları korunuyor.
- Tablet capture'ında cihaz penceresini `Landscape Right` konumuna almadan ham screenshot portrait framebuffer olarak geliyor; yeni helper modu bu adımı tekrar üretilebilir hale getiriyor.
- `tools/capture_ios_simulator_screenshot.sh` artık `--rotate-device` moduyla Simulator cihazını `Landscape Right` yapıp portrait-shaped ham iPad screenshot'ından doğru landscape review kopyası üretir.
- `Assets/Editor/IosFirstBuildSettings.cs` içinde default iOS orientation `LandscapeLeft` yapıldı; telefon export'u landscape-only plist üretir, iPad capture için ayrıca Simulator cihaz yönü ayarlanır.

## Kapanış Kapıları

- iPad helper capture akışı future regression için korunmalı.
- 4:3/tablet HUD yerleşimi final polish pass'inde daha güçlü density/spacing kontrolünden geçirilmeli.
- Threat preview ve phase badge dar 16:9 ekranda birbirine yakın duruyor; final polish pass'inde boşluk/tipografi yeniden ayarlanmalı.
