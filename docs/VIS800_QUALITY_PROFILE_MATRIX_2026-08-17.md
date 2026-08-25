# Defender Chronicles - VIS-801/802/803 Quality Profile Matrix

Tarih: 2026-08-17

Bu belge Low/Medium/High kalite profillerinin runtime ışık ve shadow davranışını kaydeder. Final cihaz FPS/Profiler kabulü VIS-807 altında açık kalır; bu indeks profil matrisinin batch-mode doğrulamasıdır.

## Kanıt Haritası

| VIS | Durum | Ana kanıt | Kapsam |
|---|---|---|---|
| VIS-801 | Bloke | `ProjectSettings/QualitySettings.asset`, `Builds/playmode-results-vis801-803.xml`, `docs/evidence/visual-development/quality-profile-matrix-report-2026-08-17.json` | Low, Medium ve High profilleri runtime profile matrix içinde ayrı doğrulandı; final kapanış fiziksel ölçüm bekler |
| VIS-802 | Bloke | `Assets/Scripts/Presentation/DefenderSceneLook.cs`, `Assets/Settings/UniversalRP.asset`, `docs/evidence/visual-development/quality-profile-matrix-report-2026-08-17.json` | Low/Medium key shadow kapalı, High key soft shadow, additional point shadow kapalı; final maliyet kabulü fiziksel ölçüm bekler |
| VIS-803 | Bloke | `Builds/playmode-results-vis801-803.xml`, `docs/evidence/visual-development/environment-budget-proxy-report-2026-08-17.json`, `docs/evidence/visual-development/vfx-profiler-overdraw-report-2026-08-17.json` | Runtime look üç ışıkla sınırlı: 1 key + 2 point fill, point shadow 0, environment/VFX proxy bütçeleri bağlı; final ışık bütçesi fiziksel Profiler/Frame Debugger bekler |

## Profil Matrisi

| Profil | Key shadow | Point light count | Point shadow caster | Fill davranışı |
|---|---|---:|---:|---|
| Low | None | 2 | 0 | Moon/Gate fill intensity ve range düşürülür |
| Medium | None | 2 | 0 | Fill ışıkları normal intensity/range ile kalır |
| High | Soft | 2 | 0 | Yalnız key light soft shadow kullanır |

## Açık Kapılar

- VIS-807: iPhone sınıfı cihazda hazırlık/normal/yoğun/boss FPS ve Profiler capture seti hâlâ açık.
- VIS-801/802/803 final kabulü, cihaz profilinde FPS, shadow distance/cascade ve light maliyeti ölçümüyle desteklenmelidir. Bu yüzden bu maddeler 2026-08-25 TODO mutabakatında `Bloke` tutulur.
- VIS-804 texture import ve VIS-806 VFX quality config ayrıca ele alınacaktır.

## Doğrulama

Bu indeks `DefenderPresentationBuildValidator.ValidateQualityProfileMatrixCheckpoint()` ile doğrulanır. Checkpoint; PlayMode profile XML'ini, URP additional-light shadow ayarını, Low/Medium/High kalite adlarını, key/fill ışık davranışını ve JSON rapor üretimini kontrol eder.
