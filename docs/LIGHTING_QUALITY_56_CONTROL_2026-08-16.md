# VIS-412 — 5.6 Final Kontrol (Lighting)

Tarih: 2026-08-16
Durum: Hazır, çalıştırma bekliyor (editor cihaz/CI gerekli)

Bu kontrol, 5.4 ışık geçişinden sonra 5.6 kalite-kapısı kapanışı için hazırlanmıştır.

## 1) Kod doğrulama

- [x] `Assets/Scripts/Presentation/DefenderSceneLook.cs`
  - [x] `GetQualityTier()` kalite adını low/medium/high olarak sınıflandırıyor.
  - [x] Low/Medium için key ışık gölgesinin kapandığı doğrulanıyor.
  - [x] Fill ışıkların `LightShadows.None` olduğu doğrulanıyor.
  - [x] Low profilde fill intensity/range attenuation değerleri beklenen oranlarda.

- [x] `Assets/Tests/PlayMode/GreyboxSceneTests.cs`
  - [x] `SceneLookRespectsQualityLightingProfile` testi mevcut.
  - [x] Test, low/medium/high kalite adlarının sırası doğruysa koşu alıyor.
  - [ ] Test sonuçları green.

## 2) Unity Editor PlayMode kontrol listesi

1. `Assets/Scenes/Game.unity` açılıp Production görünümüne geçilir.
2. `Window > General > Test Runner > PlayMode`
3. `SceneLookRespectsQualityLightingProfile` testi çalıştırılır.
4. Aşağıdaki kamera ve objelerin kontrolü:
   - `Greenwatch Moon Key` tipi Directional olmalı.
   - `Moon Fill`, `Gate Ember Fill` var olmalı ve point-light shadow kapalı olmalı.
   - Low/Medium’da key gölge `None`, High’da `Soft`.

## 3) 5.6 profil geçiş görseli

Oyunu 5.4 sonrası baseline savaş kadrajında çalıştırıp:
- Low / Medium / High kaliteye geçişte ışık görsel farkları tek ekranda karşılaştırılır.
- iPhone 13 ve iPhone 17 Pro simülatöründe 20:9 ve 16:9 için HUD-battlefield oranı bozulmamalı.
- Actor vs zemin kontrast farkının düşmemesi doğrulanır.

## 4) Kabul listesine taşınacak deliller

- Unit/PlayMode test sonuç ekran görüntüsü veya log.
- 5.6 profil geçiş screenshot seti: `low/medium/high_2026-08-16.png`.
- Gerekirse kısa not: hangi cihaz ve Unity versiyonu ile alındı.

Kaynaklar:
- [Assets/Scripts/Presentation/DefenderSceneLook.cs](/Users/emreaydin/DefenderChronicles/Assets/Scripts/Presentation/DefenderSceneLook.cs)
- [Assets/Tests/PlayMode/GreyboxSceneTests.cs](/Users/emreaydin/DefenderChronicles/Assets/Tests/PlayMode/GreyboxSceneTests.cs)
- [docs/LIGHTING_PASS_2026-08-16.md](/Users/emreaydin/DefenderChronicles/docs/LIGHTING_PASS_2026-08-16.md)
