# ALLIN: Defense - VIS-413/414 Environment QA Index

Tarih: 2026-08-17

Bu indeks, environment oran kadrajı ve slot/path regresyon kapılarının güncel kanıt durumunu ayırır. VIS-414 bugünkü PlayMode XML sonucu ile incelemeye hazırdır. VIS-413 için kamera framing matematiği doğrulandı; oran başına gerçek hazırlık/yoğun screenshot seti hâlâ açık kalır.

## Kanıt Haritası

| VIS | Durum | Ana kanıt | Kapsam |
|---|---|---|---|
| VIS-413 | Yapılıyor | `Assets/Tests/EditMode/CoreRulesTests.cs`, `Builds/editmode-results-vis413.xml` | 16:9, 20:9 ve 4:3 için `AdaptiveBattlefieldCamera.CalculateOrthographicSize` davranışı açık testle doğrulandı |
| VIS-414 | İncelemede | `Assets/Tests/PlayMode/GreyboxSceneTests.cs`, `Builds/playmode-results-vis414.xml` | Production catalog ile slot 1..8 build/upgrade/sell/rebuild akışı, slot collider'ları, slot sayısı ve kritik environment objeleri doğrulandı |

## Açık Kapılar

- VIS-413: 16:9, 20:9 ve 4:3 için gerçek Editor/Simulator hazırlık ve yoğun savaş screenshot seti eksik.
- VIS-414: video kanıtı hâlâ final release polish kapısında üretilebilir; bugünkü resmi PlayMode XML regression kanıtı mevcuttur.
- VIS-415: environment profiler/Frame Debugger ölçümü bu indeksin dışında açık kalır.

## Doğrulama

Bu indeks `DefenderPresentationBuildValidator.ValidateEnvironmentQaIndexCheckpoint()` ile doğrulanır. Checkpoint; VIS-413 EditMode XML'inde `CameraFramingCoversVisualDevelopmentAspectRatios` testinin geçtiğini, VIS-414 PlayMode XML'inde `ProductionPresentationAllowsBuildUpgradeSellOnEveryFixedSlot` testinin geçtiğini, TODO durumlarının doğru ayrıldığını ve screenshot/profiler kapılarının açık kaldığını kontrol eder.
