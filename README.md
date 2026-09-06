# Defender Chronicles

Bağımsız, mobil öncelikli stilize 3B savunma oyununun sanatsal dikey dilimi.

Bu proje Age of Arena veya FutbolQuiz’den kod, model, ses, font, ikon ya da UI dosyası kullanmaz. Production sunumu özgün Blender kaynakları, paylaşılan URP paleti, kaynağı/lisansı repoda tutulan CC0 sesler ve Noto fontlarından oluşur.

## Mevcut oynanabilir kapsam

- XZ zemin üzerinde yatay hizalanmış, sabit izometrik 3B harita
- Görünür rota, düşman kapısı, kale ve 8 sabit karakol platformu
- 200 başlangıç altını ve 20 kale canı
- Okçu (75), Kışla (100) ve Büyü (125) karakolları
- Tek seviye yükseltme ve toplam yatırımın %60’ı satış iadesi
- Kışladan çıkan, düşmanı durduran ve yeniden doğan iki birlik
- Kale yanında sabit duran; saldırı, bayılma, 10 saniyede dönüş ve Kalkan Darbesi olan Knight
- Hızlı, tank, rejenerasyon, destek, kuşatma ve üç safhalı boss düşmanları
- 6 normal dalga + 1 boss dalgası
- Hazırlık, savaş, 8 saniyelik mola, pause, zafer, yenilgi ve restart durumları
- Safe-area içinde yatay mobil HUD
- Gece mavisi/turuncu stilize çevre, aktör ve karakol seti
- Anında değişen Türkçe/İngilizce TextMeshPro arayüz, havuzlu VFX ve duruma bağlı müzik sistemi
- 5 bölge ve 50 bölümden oluşan çevrimdışı oynanabilir kampanya
- Misafir oyun, tek Unity Player Account bağlantısı ve çevrimdışı hak talebi kuyruğu
- Cloud Code tarafından doğrulanan bölüm sonucu, ödül ve ilerleme modeli

## Teknik temel

- Unity: `6000.4.1f1`
- iOS minimum sürüm: 15.0
- Android minimum sürüm: API 29; ARM64 + IL2CPP
- Yön: açılış/menü/kampanya/brifing portre; savaş/pause/sonuç yalnız Landscape Left ve Landscape Right
- Bundle ID: `com.emreaydin.defenderchronicles`
- Giriş: Unity Input System
- Test: Unity Test Framework 1.6.0
- UI metni: prefab tabanlı UGUI + TextMeshPro; Türkçe/İngilizce locale varlıkları ve çalışma zamanı kataloğu
- Canlı oyun: Unity Authentication + Player Accounts + Cloud Code C# modülü + Protected Cloud Save
- Production sunumu: doğrulanan `DefenderPresentationCatalog`; eksik varlık/lisans build’i durdurur

Ana akış [GameSessionController.cs](/Users/emreaydin/DefenderChronicles/Assets/Scripts/Core/GameSessionController.cs), savaş aktörleri [GreyboxCombatants.cs](/Users/emreaydin/DefenderChronicles/Assets/Scripts/Core/GreyboxCombatants.cs) içindedir. Sunum varlıkları ve doğrulama [DefenderPresentationCatalog.cs](/Users/emreaydin/DefenderChronicles/Assets/Scripts/Presentation/DefenderPresentationCatalog.cs) üzerinden yönetilir.

## Testler

EditMode:

```sh
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode \
  -testResults "$PWD/Builds/editmode-results.xml" -logFile "$PWD/Builds/editmode-tests.log"
```

PlayMode için `-testPlatform PlayMode` kullanılır.

Doğrulanan sonuçların güncel denetim özeti: [UNITY_AUDIT_REMEDIATION_2026-08-27.md](/Users/emreaydin/DefenderChronicles/docs/UNITY_AUDIT_REMEDIATION_2026-08-27.md).

Önceki fiziksel iOS kabul sonuçları (16 Ağustos 2026):

- EditMode: 9/9 geçti
- PlayMode: 2/2 geçti; 8 sabit/seçilebilir slot, sabit Knight, ekonomi, pause/devam ve production sunumu kontrol edildi
- iOS Development Simulator build: geçti
- iOS Xcode Debug build: geçti
- Normal Simulator açılışı: yakalanmış error/exception yok
- Final iOS yapısında iki ayrı hızlandırılmış tam bölüm: boss zaferiyle geçti
- Ayrı iOS kabul koşusu: yenilgi ve temiz restart geçti

Simulator yalnız işlevsel doğrulama içindir. FPS, bellek ve termal performans kapısı gerçek iPhone’da ölçülmelidir.

## Açık Simulator’a kurma

Simulator’da bir iPhone açıkken:

```sh
./tools/build_and_run_ios_simulator.sh
```

Bu komut Unity’den Xcode projesi üretir, Development yapı alır, açık Simulator’a kurar ve oyunu normal hazırlık ekranında açar. Ayrı Release Xcode çıktısı Unity menüsündeki `Defender Chronicles > Build iOS Simulator Release Xcode Project` komutuyla üretilebilir.

## Fiziksel iPhone'a kurma

iPhone bağlı, kilidi açık ve Developer Mode etkin durumdayken:

```sh
./tools/build_and_run_ios_device.sh
```

Akış cihazı ve geçerli Apple Development takımını otomatik bulur, imzalı Development yapısını kurar ve açar. Development yapısı FPS, frame süresi, Unity belleği, düşük bellek uyarısı ve iOS termal durumunu `[DC_DEVICE_METRICS]` kayıtlarında raporlar. Ham çıktılar `Builds/` altında tutulur.

Fiziksel cihaz kabul durumu: [IOS_DEVICE_ACCEPTANCE_2026-08-16.md](/Users/emreaydin/DefenderChronicles/docs/IOS_DEVICE_ACCEPTANCE_2026-08-16.md).

## Android yapı

Unity içinden `Defender Chronicles > Build Android Development APK` geliştirme APK’sı,
`Defender Chronicles > Build Android Release AAB` ise mağaza AAB çıktısı üretir. Release imzası için yerel/CI keystore yapılandırması ayrıca sağlanmalıdır.

## Varlık kökeni

Blender kaynakları, üretim betikleri, font lisansları ve kullanılan üretim kaynakları [ASSET_PROVENANCE.md](/Users/emreaydin/DefenderChronicles/docs/ASSET_PROVENANCE.md) içinde kayıtlıdır.

## Kapsam dışı

Yeni kuleler, diğer kahramanlar, ekipman, PvP, reklam, IAP, Vivox ve multiplayer bu teslimin kapsamında değildir. Kesin performans/termal kapısı fiziksel iPhone ve Android cihaz profil ölçümüne bırakılmıştır.

Güncel ve gerçek durum sınıflandırması: [DEFENDER_CHRONICLES_MASTER_TODO.md](/Users/emreaydin/DefenderChronicles/docs/DEFENDER_CHRONICLES_MASTER_TODO.md).
