# Adım 1 — H1–H5 sonuç raporu

Çalışma yeri: `/Users/emreaydin/AllinDefense-ui`; değişiklikler commit edilmedi.

## Değişiklikler

- H1: `Assets/Scripts/UI/AppFlowController.Screens.cs:108` hedef alanı TopLeft ve 26–32 otomatik boyut; `:129` brifing düzeni CTA'dan ayrıldı. `Assets/Scripts/UI/AppFlowController.cs:358` sonuç açıklaması TopLeft, 980×100; CTA daha aşağıda. `Assets/Scripts/UI/FrontendCopy.cs:65` on hedef çiftinin en uzun metni 41 karakter. `Assets/Scripts/UI/AppFlowController.Acceptance.cs:145` CanvasGroup alpha=0 metinleri taşma denetiminden çıkarır; ayarlar arkasındaki gizli harita artık ikinci defa sayılmaz.
- H2: `Assets/Scripts/Presentation/DefenderHudController.cs:285`, `:703`, `:733` üst bar ve faz rozetinin boss sinyali session.NextWaveHasBoss üzerinden gerçek bossWave verisini okur; son dalga indeksi tek başına boss değildir.
- H3: `Assets/Scripts/UI/GameLocalization.cs:132` ortak tr-TR/en-US büyütme yardımcı metodu; oyuncuya görünen invariant çağrılar değişti. `Assets/Scripts/UI/AppFlowController.cs:1382` mevcut bölge başlığını dil değişiminde yeniler; `:880` ayarlar açılırken dil vurgusunu günceller. `Assets/Scripts/UI/AppFlowController.Screens.cs:379` fillRect ve 30 birim iç boşluklu handle alanı.
- H4: `Assets/Scripts/UI/AppFlowController.cs:671` geçersiz sonraki bölüm gizlenir ve iki kalan düğme ortalanır; `:677` sonuç açılınca HUD gizlenir. `Assets/Scripts/Core/GameSessionController.cs:259` HUD görünürlüğü metodu. `Assets/Scripts/UI/AppFlowController.ClosedTest.cs:15` normal kayıt/eşitleme metni boş; yalnız hata veya bağlı hesabın bekleyen bağlantısı kısa durum gösterir.
- H5: `Assets/Scripts/Presentation/FrontendPresentationTheme.cs:37`, `Assets/Resources/Presentation/FrontendPresentationTheme.asset:37` tema tasarım varlığına bağlı; `Assets/Editor/UiDesignSystemAssetBuilder.cs:21` Unity üzerinden referansı kaydeden yöntem. `Assets/Scripts/UI/AppFlowController.cs:255` Top Shade mevcut VerticalGradient sprite'ını kullanır; `:542` ASCII seçim süsü kalktı. `Assets/Scripts/UI/AppFlowController.Screens.cs:73` tek bölge sekmesi gizli.
- Testler: `Assets/Tests/EditMode/CoreRulesTests.cs:1520` Türkçe/İngilizce kültür ve normal son dalga ayrımı; `Assets/Tests/PlayMode/FrontendExperienceTests.cs:221` dil/ayar yenilenmesi, `:246` bölüm 1 ve 10 gerçek dalga verisi, `:267` fillRect ve gizlenen sonraki bölüm/yenilgi beklentileri.

## Kapılar

| Kapı | Sonuç | Kanıt |
| --- | --- | --- |
| Baseline EditMode | 165/165, 0 başarısız | /tmp/allin-ui/baseline-edit.xml |
| Baseline PlayMode | 118/118, 0 başarısız | /tmp/allin-ui/baseline-play.xml |
| EditMode tam suite | 166/166, 0 başarısız | Builds/step1-edit.xml; Builds/step1-edit.log |
| PlayMode tam suite | 120/120, 0 başarısız | Builds/step1-play.xml; Builds/step1-play.log |
| Tema referansı kaydı | başarılı | Builds/step1-theme.log |
| iOS Simulator build | başarılı | Builds/unity-ios-simulator-build.log; Builds/xcodebuild-ios-simulator.log |
| Simülatör kabul | status=passed, errors=[] | Builds/FrontendExperience/iOS/report.json |
| git diff --check | temiz | Son kontrolde çıkış kodu 0 |

## Görsel kanıt

`Builds/FrontendExperience/iOS/` içindeki `main-menu.png`, `map-Turkish.png`, `map-English.png`, `settings-Turkish.png`, `victory.png`, `defeat.png` tek tek açılıp incelendi.
Hedef ve sonuç açıklamaları sığıyor; Türkçe İ doğru, bölge başlığı English'e geçiyor, tek bölge sekmesi/ASCII süsü yok, dil vurgusu/dolgu görünür, yenilgide iki düğme var, arka PREPARING/START FIRST WAVE HUD'ı görünmüyor.

## Kalan sınırlar

Açık H1–H5 engeli yok; bu kanıt iPhone 17 Pro simülatöründen, fiziksel cihaz ölçümü değil.
Mevcut kabul modu zaferi sentetik faz değişimiyle tetikler ve görüntüde 0/3 yıldız bulunur; gerçek oynanmış bölüm sonucu veya boss savaş görüntüsü bu adımın kanıtına dahil değildir.
HUD builder/prefab değişmedi; prefab GUID'i korundu. Unity'nin build sırasında değiştirdiği ProjectSettings ve addressables_content_state.bin geri alındı; her batch sonrasında link.xml ve meta geri yüklendi.
