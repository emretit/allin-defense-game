# İlk dalga düğmesi P0 — 2026-10-15

Durum: düzeltildi, test kapıları yeşil; commit/push/build yapılmadı.

## Kanıtlanan kök neden

`Assets/Scripts/UI/AppFlowController.cs:785` normal `LaunchCampaignLevel` akışında savaş açılınca frontend canvas'ını kapatıyor. `Assets/Scripts/UI/AppFlowController.Screens.cs:673` ilk kez görülen düşman için savaşı duraklatıyor; `BuildEnemyCard` kartı aynı kapalı canvas altında oluşturuyor (düzeltme sonrası satır 693). `GameSessionController.cs:919` interceptor nedeniyle dönüyor; kart görünmediğinden oyuncu onaylayamıyor ve savaş Paused durumunda kalıyor. Bu iki kod yolu bildirilen `593a7c6` sürümünde de mevcut.

Rehber ve dalga düğmesi raycast'i engellemiyor: bilinen düşman durumunda düzeltme öncesinde de düğmenin ekrandaki konumundan tıklama dalgayı başlattı. Eski `FrontendExperienceTests.NewEnemyAppearsOnceAndRestoresPhaseAndSpeed` testi gerçek savaş girişini kullanmayıp `ShowScreen(Battle)` çağırdığı için canvas'ın kapatılmasını kapsamıyordu.

## Düzeltme ve regresyon

- `Assets/Scripts/UI/AppFlowController.Screens.cs:692`: savaş kartı oluşturulmadan frontend canvas etkinleştiriliyor.
- Aynı dosya `:713`: kapanış animasyonu tamamlanınca, hâlâ savaş ekranındaysa ve sıradaki kart açılmamışsa canvas tekrar kapatılıyor; mevcut onay → pause kaldırma → StartNextWave akışı korunuyor.
- `Assets/Tests/PlayMode/FirstWaveButtonTests.cs:58`: bölüm 1 rehber açık / bölüm 7 rehbersiz × düşman biliniyor / ilk kez görülüyor dört senaryo.
- Test normal `LaunchCampaignLevel` yolunu kullanıyor, iki kule kuruyor, açık rehber hedefini doğruluyor ve dalga/onay/duraklat düğmelerini `EventSystem.RaycastAll` + en üst hit'in handler kontrolü + `ExecuteEvents.pointerClickHandler` üzerinden tıklıyor; StartNextWave veya onClick doğrudan çağrılmıyor.
- İlk kez görülen düşman senaryoları düzeltmeden önce görünmez modal assertion'ında KIRMIZI, sonra YEŞİL; bilinen düşmanlı senaryolar önce ve sonra YEŞİL.
- Kart kapandıktan sonra Phase == Wave, oyun hızı geri yüklenmiş ve HUD duraklat düğmesi yeniden ulaşılabilir.

## Test sonuçları

| Koşu | Sonuç | Rapor |
| --- | --- | --- |
| Düzeltme öncesi FirstWaveButtonTests | 2 geçti / 2 beklenen hata | Builds/first-wave-red.xml |
| Tam EditMode, -nographics | 283/283 | Builds/first-wave-edit.xml |
| FirstWaveButtonTests + ClosedTestAcceptanceTests + frontend'in iki ilgili testi, -nographics | 13/13 | Builds/first-wave-green.xml |
| Tam FrontendExperienceTests, -nographics | 20/20 | Builds/first-wave-frontend.xml |
| FirstWaveButtonTests + tam RelearnExperienceTests, tek grafikli koşu | 6/6 | Builds/first-wave-visual.xml |

Toplam 33 farklı PlayMode testi yeşil (tekrarlı doğrulamalarla 39 başarılı test çalıştırması). Başsız Unity 640×480 kullanıyor; tek grafikli koşuda dört yeni senaryo 844×390 yatay ekranda geçti. Brifing kartları ve ayarlardan rehberi tekrar açma davranışı mevcut RelearnExperienceTests ile doğrulandı. Makine raporu: `Builds/FirstWave/report.json`, status `passed`.

Brifing regresyon ekran görüntüleri: `Builds/relearn/briefing-card-Turkish.png`, `Builds/relearn/briefing-card-English.png`; ilk dalga için ayrı ekran görüntüsü üretilmedi. Tüm Unity koşuları ortak unity-lock ve caffeinate ile çalıştı; link.xml/meta ve üretilen dinamik font yan etkileri geri alındı, git diff --check temiz. Fiziksel iPhone / çentik safe-area / TestFlight yeniden doğrulaması yapılmadı; simülatör koordinatöre bırakıldı. Paralel UIUX raporlarına ve mevcut başka değişikliklere dokunulmadı.
