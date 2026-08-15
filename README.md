# Defender Chronicles

Bağımsız, mobil öncelikli 3B savunma oyunu gri kutu dikey dilimi.

Bu proje Age of Arena veya FutbolQuiz’den kod, model, ses, font, ikon ya da UI dosyası kullanmaz. İlk teslimin dünyası Unity primitive’leriyle oluşturulur.

## Mevcut oynanabilir kapsam

- XZ zemin üzerinde özgün, sabit izometrik 3B harita
- Görünür rota, düşman kapısı, kale, 8 karakol slotu ve 4 komuta noktası
- 200 başlangıç altını ve 20 kale canı
- Okçu (75), Kışla (100) ve Büyü (125) karakolları
- Tek seviye yükseltme ve toplam yatırımın %60’ı satış iadesi
- Kışladan çıkan, düşmanı durduran ve yeniden doğan iki birlik
- Hareket, saldırı, bayılma, 10 saniyede dönüş ve Kalkan Darbesi olan Knight
- Hızlı, tank, rejenerasyon, destek, kuşatma ve üç safhalı boss düşmanları
- 6 normal dalga + 1 boss dalgası
- Hazırlık, savaş, 8 saniyelik mola, pause, zafer, yenilgi ve restart durumları
- Safe-area içinde yatay mobil HUD

## Teknik temel

- Unity: `6000.4.1f1`
- iOS minimum sürüm: 15.0
- Yön: Auto Rotation; yalnız Landscape Left ve Landscape Right
- Bundle ID: `com.emreaydin.defenderchronicles`
- Giriş: Unity Input System
- Test: Unity Test Framework 1.6.0
- UI metni: çalışma zamanında TMP’ye bağımlı değildir; Unity’nin yerleşik fontu kullanılır

Ana akış [GameSessionController.cs](/Users/emreaydin/DefenderChronicles/Assets/Scripts/Core/GameSessionController.cs), savaş aktörleri [GreyboxCombatants.cs](/Users/emreaydin/DefenderChronicles/Assets/Scripts/Core/GreyboxCombatants.cs) içindedir. `GameBootstrap` yalnız oturumu başlatır.

## Testler

EditMode:

```sh
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode \
  -testResults "$PWD/Builds/editmode-results.xml" -logFile "$PWD/Builds/editmode-tests.log"
```

PlayMode için `-testPlatform PlayMode` kullanılır.

Doğrulanan sonuçlar (15 Ağustos 2026):

- EditMode: 7/7 geçti
- PlayMode: 1/1 geçti; sahne, içerik, ekonomi ve pause/devam kontrol edildi
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

## Kapsam dışı

Catapult, diğer kahramanlar, ekipman, hesap, bulut kayıt, PvP, reklam, IAP, final modeller, animasyon, ses ve müzik bu gri kutu teslimine dahil değildir.

Güncel ve gerçek durum sınıflandırması: [DEFENDER_CHRONICLES_MASTER_TODO.md](/Users/emreaydin/DefenderChronicles/docs/DEFENDER_CHRONICLES_MASTER_TODO.md).
