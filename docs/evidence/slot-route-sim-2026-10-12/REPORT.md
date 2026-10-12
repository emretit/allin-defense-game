# Slot ve dal rota doğrulaması — 12 Ekim 2026

Doğrulama tamamlandı: PlayMode **116/116 geçti**, iOS simülatör derlemesi başarılı ve istenen beş seviyenin savaş ekranı kaydedildi. Görsel kabul bütünüyle temiz değil: ağaçların örttüğü slotlar, bazı slotlarla yol arasında kalan dar boşluklar ve T birleşimlerinde kaplama/kenar dikişleri var. Kaynak kod, denge değerleri ve mevcut slot/rota değişiklikleri değiştirilmedi; commit atılmadı.

## Test ve derleme

- Worktree: `/Users/emreaydin/orca/workspaces/DefenderChronicles/main`.
- Test öncesinde süreç listesinde bu projeyi açık tutan başka Unity Editor yoktu. Sonradan görülen diğer Editor `ui-faz0` worktree'sindeydi.
- Unity: `6000.4.1f1`; istenen `-batchmode -runTests -testPlatform PlayMode` komutu çalıştırıldı.
- Sonuç: toplam **116**, geçti **116**, başarısız **0**, atlanan **0**; Unity çıkış kodu **0**.
- XML UTC başlangıç/bitiş: `2026-10-12 06:44:00Z` / `06:52:33Z`; süre `512.6747877` saniye.
- Başarısız test adı veya hata mesajı yok. Koordinatörün ILPP runner uyarısı alındı; koşu tamamlandığı için tekrar gerekmedi.
- Kanıt: `Builds/routes-playmode.xml`, `Builds/routes-playmode.log`.
- `./tools/build_and_run_ios_simulator.sh` başarılı; Unity batch çıkışı başarılı, Xcode `BUILD SUCCEEDED`, uygulama kurulup açıldı.
- Derleme kayıtları: `Builds/unity-ios-simulator-build.log`, `Builds/xcodebuild-ios-simulator.log`; üretilen projeler `Builds/iOS-Simulator/` ve `Builds/XcodeDerivedData/` altında.

## Yakalama yöntemi

Cihaz: iPhone 17 Pro, UDID `81AAFCAB-9741-4819-B9F7-B9DFA47D6F21`, açık iOS 26.5 simülatörü. PNG'ler 2622 × 1206 piksel, yatay savaş ekranı, ilk dalga öncesi durumundadır.

`GameBootstrap.Awake` içindeki `DC_CAMPAIGN_LEVEL` okuması yalnızca `ShouldShowFrontend()` false olduğunda uygulanıyor. Seviye değişkeni tek başına ön yüzü atlamıyor; mevcut `DC_ABILITY_CAPTURE=1` development otomasyon yolu bunu sağlıyor. Bu yol kahraman yeteneğini bir kez tetikliyor; slot kurmuyor, dalga başlatmıyor ve yol geometrisini değiştirmiyor. Dokunma otomasyonu kullanılmadı.

Her seviye öncesinde uygulama sonlandırıldı ve şu biçimde açıldı:

```sh
SIMCTL_CHILD_DC_CAMPAIGN_LEVEL=43 SIMCTL_CHILD_DC_ABILITY_CAPTURE=1 \
  xcrun simctl launch 81AAFCAB-9741-4819-B9F7-B9DFA47D6F21 com.emreaydin.defenderchronicles
xcrun simctl io 81AAFCAB-9741-4819-B9F7-B9DFA47D6F21 screenshot docs/evidence/slot-route-sim-2026-10-12/level-43.png
```

Seviye 14 görüntüsü geçici yeni tehdit mesajı kapandıktan sonra tekrar alındı. Kalıcı tehdit önizleme paneli bazı üst slotları karartıyor; aşağıdaki görünürlük değerlendirmesi bunu ayrıca belirtiyor. Konum yüzdeleri görüntünün sol üst köşesinden yaklaşık slot/işaret merkezi konumudur.

## Seviye bazında görsel inceleme

| Seviye / kanıt | Slot ve dekor | Dal yollar / girişler |
| --- | --- | --- |
| [1](level-1.png) | Slotlar yol yakınında; üst orta slot (%39, %23) ağaç kümesiyle örtüşüyor ve önizleme panelinin arkasında kalıyor. Üst soldaki slot (%21, %32) ile yol kenarı arasında görünür dar çim boşluğu var. Belirgin slot–kale/kapı çakışması görülmedi. | Tek rota; dal T kontrolü uygulanmıyor. Giriş 1 kadraj içinde, kapı yanında küçük numarası kısmen kapı tarafından örtülüyor. |
| [10](level-10.png) | Sabit slot 1 alt solda (%23, %69) yuvarlak ağaçla; slot 4 üst orta solda (%41, %27) çam kümesiyle; slot 8 üst sağda (%79, %22) çam kümesiyle örtüşüyor. Slot 4 ayrıca önizleme paneli altında. Slot 2 (%23, %48) ile ilk dikey yolun sol kenarı arasında dar çim boşluğu var. Belirgin slot–kale/kapı çakışması yok. | Tek rota; dal T kontrolü uygulanmıyor. Giriş 1 ekran içinde. |
| [14](level-14.png) | Sekiz slot seçiliyor; belirgin ağaç/kaya/kale/kapı çakışması görülmedi. Üst sağdaki iç slot (%64, %26) önizleme panelinin altına giriyor. Sol iç slot (%38, %44) gibi bazı noktalarda yol ile slot arasında dar çim boşluğu var. | Giriş 2 soldan yatay gelip ilk dikey ana kola tek T ile bağlanıyor (%32, %53). Uzun ortak rota yeniden çizilmiş görünmüyor; ancak birleşimde ana yolun taş kenarı devam ediyor ve düz dikdörtgen dal kaplaması organik ana kaplamadan ayrılıyor. Girişler 1/2 kamera tarafından kesilmiyor; giriş 2 okunun bir kısmı sabit kapı modeliyle örtüşüyor. |
| [24](level-24.png) | Sekiz slot görünür; belirgin ağaç/kaya/kale/kapı çakışması görülmedi. Alt orta soldaki slot (%42, %59) ile yol kenarı arasında dar boşluk var. | Giriş 2 sol orta kenardan ilk dikey kola tek T ile bağlanıyor (%24, %39). Ortak devam yolu boyunca çift çizim görülmüyor; T ağzında taş kenar ve farklı renk/genişlikte dikdörtgen kaplama dikişi var. Girişler 1/2 kadraj içinde. |
| [43](level-43.png) | Alt soldaki slot (%32, %67) çam kümesinin büyük ölçüde arkasında; mavi yüzeyi yapraklar arasında kısmen görünüyor. Üst orta sağ slot (%61, %26) önizleme paneli altında. Belirgin slot–kale/kapı çakışması görülmedi; diğer slotlar yol kenarına yakın, bazı yerlerde küçük çim aralıkları var. | Dal 2 sol üstten gelip orta sol dikey kola (%40, %36), dal 3 alt kenardan gelip üst orta yatay ana kola (%49, %28) birer T ile bağlanıyor. Uzun ortak devam bölümünde çift yol çizimi görülmüyor. Her iki birleşimde düz dal kaplaması ve ana yol kenar taşları temiz birleşim görünümünü bozuyor; üst birleşim ayrıca önizleme paneli altında. Alt giriş 3 (%48, %67) oku ve numarası ekran içinde, kamera tarafından kesilmiyor; çam kümesi okun bir kısmını örtüyor. |

**Yol kenarına temas:** Slot merkezlerinin rota merkez çizgisine yakınlığı iyileşmiş ve slotlar yol kenarı boyunca duruyor; ekran görüntüleri bütün slotların görsel yol kenarına fiziksel olarak değdiğini doğrulamıyor. Yukarıda konumu verilen örneklerde dar çim aralıkları görülebiliyor. Bu nedenle “tüm slotlar yol kenarına değiyor” ölçütü tam geçmedi.

**Giriş kadrajı:** İncelenen beş seviyede kamera tarafından kesilen giriş işareti bulunmadı. Seviye 43 alt kenar girişini kapsıyor; bu seçili görüntülerde iç alandan üst kenara çıkan ayrı bir giriş örneği yok, üst kenar girişlerinin tamamı için genellenmiş onay verilmiyor. Ağaç/kapı/HUD örtülmesi kamera kırpılmasından ayrı bir okunurluk sorunu.

## Nedenler ve kalan iş

- Slot seçimi `CampaignLevelFactory.TryPickSlot` içinde yol mesafesini, önceki slot aralığını ve rota uçlarını kontrol ediyor; dekor modeli konumlarını kontrol etmiyor. `BuildStylizedWorldIncrementally` sabit environment prefab'ını kuruyor ve ardından yeni slotları yerleştiriyor. Bu, 1/10/43 seviyelerindeki ağaç–slot örtüşmesini açıklıyor.
- Ana yol `DefenderBattlefieldDressing.CreateRoadSurface` ile organik mesh ve taşlarla çiziliyor; ek dal segmentleri `GameSessionController.CreateRoadSegment` ile farklı genişlik/yükseklik ve kaplamadaki küplerden oluşuyor. `OwnedBranchSegments` ortak devam bölümünün tekrar çizilmesini önlüyor; fakat birleşim ağzındaki ana kenar taşlarını kaldırmıyor ve kaplamaları birleştirmiyor. Bu yüzden rota topolojisi T olsa da görsel birleşim tam temiz değil.
- Yol merkez çizgisinden 1.65–1.95 uzaklık ölçütü, organik yolun dalgalı kenarı ve gerçek slot mesh'inin dış sınırıyla birebir temas garantisi vermiyor.
- Testler başarılı olduğu için düzeltme yapılmadı. Kalan iş, ağaçlardan arındırılmış slot görünümü, gerçek görsel kenarla temas ve T birleşim kaplamasının/taşlarının temizlenmesi; denge ayarı gerekmiyor.

Derlemenin değiştirdiği `ProjectSettings/ProjectSettings.asset` ve iOS Addressables content-state dosyası başlangıç durumuna geri alındı; başlangıçta silinmiş olan `link.xml` / `.meta` silinmiş durumda bırakıldı. Görev öncesinde mevcut olan C# / EditMode test / doküman değişiklikleri korundu.

Yeni kalıcı teslimler: bu rapor ve `level-1.png`, `level-10.png`, `level-14.png`, `level-24.png`, `level-43.png`; ayrıca yukarıda belirtilen test ve derleme kayıtları.
