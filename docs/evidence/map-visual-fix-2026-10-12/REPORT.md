# Harita görsel düzeltmeleri — 12 Ekim 2026

Üç kod düzeltmesi ve son doğrulama tamamlandı: **EditMode 152/152**, **PlayMode 117/117**, başarılı iOS simülatör derlemesi ve beş bölümün yeni savaş görüntüsü. 1/10/43'te raporlanan ağaç–slot örtüşmeleri kalktı; yan yollar ana yolla aynı kaplamaya geçti ve T ağızları açıldı. Slotlar 1,58 son hedefiyle yol kenarına daha sıkı oturuyor; düzensiz taşlar arasında bazı çok ince yerel çim çizgileri ve üst slotları örten HUD aşağıda ayrıca kayıtlıdır. Commit/push yapılmadı.

## Uygulanan kurallar

- Environment ağaç/çam/kaya/çalı renderer'ının dünya XZ ayak izi herhangi bir slot merkezine **1,1 unit** veya rota merkez çizgisine **1,2 unit** yaklaşırsa `renderer.enabled=false` olur. Büyük yaprak ayak izleri ve prop içinden geçen uzun rota segmentleri de yakalanır. Biome propları aynı kurala tabidir. Zemin/sırt/backdrop bu dekor filtresinden hariçtir; önceki taban gizleme davranışı değiştirilmedi. GameObject adları ve aktif durumları korunur.
- Ana ve yan yollar aynı organik mesh, kaplama/kenar/aşınma malzemesi, genişlik ve yükseklik üretimini paylaşır; kaplama normalleri yukarı sabittir. Yalnız dalın `OwnedBranchSegments` öneki çizilir. T ağzında gelen taraftaki ana kenar taşları ve ana kaplamaya giren dal taşları atlanır; karşı ana kenar korunur. Ana mesh adları ve dalın `Road Surface` / `Road Edge` adları korunur; mesh'lerde collider yoktur.
- Önce **1,62** hedefi denendi ve test/derleme/görüntüler tamamlandı; ince aralıklar için son hedef ve üretici alt sınırı **1,58** oldu. Alternatif adaylar **1,62 / 1,68**, üst sınır **1,95**. Dünya koordinatı çıkarma yuvarlaması için üretici alt sınırında yalnız **0,0001 unit** tolerans var; mevcut testin **1,55** alt sınırı gevşetilmedi. Bölüm 10'un `ExistingOutposts` ve `OutpostSlotPositions` sekiz sabit slotu önceki görev başlangıcına göre **0,14 unit** yola yaklaştırıldı. Slot aralığı, girişten 2 ve kaleden 3 unit açıklık mantığı korunur.
- Görev öncesindeki `BuildBranchRoute`, `OwnedBranchSegments`, `MapCache` ve slot/rota işi korundu. Görev başlangıç kopyasıyla kaynak farkı kontrol edildi; rota noktaları, hasar/altın/dalga değerleri ve UI dosyaları değiştirilmedi.

## Son test ve derleme

Worktree: `/Users/emreaydin/orca/workspaces/AllinDefense/main`; Unity **6000.4.1f1**. Her koşu `-batchmode -runTests -testPlatform EditMode|PlayMode` ile yapıldı; iki son Unity test çıkış kodu da **0**.

| Koşu | Toplam | Geçti | Başarısız | Atlanan | UTC başlangıç / bitiş | Süre |
| --- | ---: | ---: | ---: | ---: | --- | ---: |
| EditMode | 152 | 152 | 0 | 0 | 2026-10-12 08:37:36Z / 2026-10-12 08:37:37Z | 1.155 sn |
| PlayMode | 117 | 117 | 0 | 0 | 2026-10-12 08:39:02Z / 2026-10-12 08:47:34Z | 512.827 sn |

Kanıtlar: [EditMode XML](../../../Builds/mapfix-editmode.xml), [EditMode log](../../../Builds/mapfix-editmode.log), [PlayMode XML](../../../Builds/mapfix-playmode.xml), [PlayMode log](../../../Builds/mapfix-playmode.log).

Yeni EditMode kapsamı: gerçek environment prefab'ında 1/10/14/24/43 için görünür dekorun hiçbir slot ayak izine 1,1 unit içinde kalmaması, taban renderer durumlarının korunması ve dış dekorun görünür kalması; saf ayak izi/rota kesişimi hesabı; yalnız gelen T tarafının açılması; 50 bölümün giriş/kale açıklığı. Mevcut 50 bölüm rota topolojisi, 1,55 slot alt sınırı, 1,95 üst sınırı ve slotlar arası açıklık testleri korunarak geçti.

Yeni PlayMode testi `CampaignSceneryKeepsSlotsClearAndBranchesMatchPrimaryRoad`: beş bölümde environment/biome dekor açıklığı, dal sayısı, ana/dal kaplama ve taş malzemesi eşitliği, aynı kaplama yüksekliği ve yukarı normaller, collider bulunmaması ve ortak ana devamın yeniden çizilmemesi. Başarısız test yok.

Son `./tools/build_and_run_ios_simulator.sh` çıkış kodu **0**; Unity `Exiting batchmode successfully now!`, Xcode **BUILD SUCCEEDED**, kurulum ve launch başarılı. Kayıtlar: [Unity build log](../../../Builds/unity-ios-simulator-build.log), [Xcode build log](../../../Builds/xcodebuild-ios-simulator.log). Lisansın çevrimdışı süresi dolmasından kaynaklanan ara engel Hub yenilemesiyle çözüldü; rapor son 1,58 kodunun tam koşularına dayanır.

## Yakalama

Cihaz: **iPhone 17 Pro**, iOS **26.5**, UDID `81AAFCAB-9741-4819-B9F7-B9DFA47D6F21`. Beş PNG **2622 × 1206**, dalga öncesi savaş ekranıdır. Her bölüm önce terminate edildi, aşağıdaki launch yapıldı, geçici 8 saniyelik mesaj kapansın diye **13 saniye** beklendi ve ham simülatör görüntüsü alındı. Dokunma otomasyonu ve görüntü düzenleme kullanılmadı.

```sh
SIMCTL_CHILD_DC_CAMPAIGN_LEVEL=<n> SIMCTL_CHILD_DC_ABILITY_CAPTURE=1 \
  xcrun simctl launch 81AAFCAB-9741-4819-B9F7-B9DFA47D6F21 com.emreaydin.allindefense
xcrun simctl io 81AAFCAB-9741-4819-B9F7-B9DFA47D6F21 screenshot docs/evidence/map-visual-fix-2026-10-12/level-<n>.png
```

`DC_ABILITY_CAPTURE=1` mevcut development yolu ön yüzü atlatır ve kahraman yeteneğini bir kez tetikler; slot kurmaz, dalga başlatmaz, rota geometrisini değiştirmez.

## Bölüm bazında önce / sonra

| Bölüm | Önce | Sonra ve kalan görünürlük |
| --- | --- | --- |
| 1 | [Önce](../slot-route-sim-2026-10-12/level-1.png): üst orta slot çamlarla örtülüyor; üst sol slot yoldan ayrık. | [Sonra](level-1.png): üst orta ağacı gizlendi, sekiz platform seçiliyor, slotlar daha sıkı kenara yerleşti. Üst orta platform hâlâ kalıcı tehdit önizleme HUD'ı altında; üst sol düzensiz taş kenarında çok ince çim çizgisi seçilebiliyor. Tek rota. |
| 10 | [Önce](../slot-route-sim-2026-10-12/level-10.png): slot 1 yuvarlak ağaçla, 4 ve 8 çamlarla örtülüyor; slot 2 yoldan ayrık. | [Sonra](level-10.png): 1/4/8 ağaç örtüşmeleri kalktı, sekiz sabit platform yola 0,14 unit yaklaştı; slot 2 kenara daha sıkı oturuyor. Slot 4 HUD altında; kesintili taş kenarında bazı çok ince yerel çizgiler kalabilir. Tek rota. |
| 14 | [Önce](../slot-route-sim-2026-10-12/level-14.png): dal parlak dikdörtgen küp, T ağzında ana taş kenar sürüyor. | [Sonra](level-14.png): dal ana yolun aynı kırmızı organik kaplaması, yüksekliği ve taşlarıyla çiziliyor; ilk dikey ana kola gelen T ağzı açık, karşı ana kenar korunuyor. Ortak uzun devam yeniden çizilmiyor. Slotlar açık; üst slotlarda HUD örtülmesi ve giriş 2 okunda sabit kapı örtüşmesi kalıyor. |
| 24 | [Önce](../slot-route-sim-2026-10-12/level-24.png): açık renk dikdörtgen dal, farklı genişlik/yükseklik ve kapalı taş ağzı. | [Sonra](level-24.png): dal ana yolun aynı mavi kaplaması ve taşlarıyla organik; T ağzındaki çapraz taş dizisi kalktı, karşı kenar korundu. Slotlar yola yaklaştı, alt orta platformun aralığı azaldı; yolun üstüne oturan platform görülmedi. |
| 43 | [Önce](../slot-route-sim-2026-10-12/level-43.png): alt sol slot ve alt giriş 3 çamlar arasında, iki dal düz küp ve T ağızları taşlı. | [Sonra](level-43.png): alt sol platform ve alt giriş 3 tamamen açıldı; iki dal aynı altın renk organik yüzey/taş/aşınma üretimini kullanıyor. Sol T ağzı açık, alt dal ana yatay kola açık bağlanıyor; üst bağlantının bir kısmı HUD altında. Ortak uzun devamda çift çizim yok. |

## Kalan sınırlar ve teslim durumu

- Düzensiz ve yer yer kesintili taş kenarı, bazı platformların yakınında çok ince çim çizgileri bırakıyor. 1,58 son kalibrasyonu önceki ayrıklığı azaltıyor; her platformun tüm çevresi için kesintisiz fiziksel temas garantisi verilmez. Platform merkezleri yol üstüne gelmiyor ve mevcut 1,55 açıklık testi 50 bölümde geçiyor.
- Kalıcı tehdit önizleme HUD'ı bazı üst slotları ve 43'ün üst T bağlantısının bir kısmını örter. Sabit kapı modeli 14'te giriş 2 okunun bir kısmını örter. Bu görüntülerde girişler kamera tarafından kesilmiyor; HUD/kapı görünürlüğü ayrı kapsamdır.
- `ProjectSettings/ProjectSettings.asset` ve tüm Addressables content-state dosyaları görev başlangıç kopyasına geri alındı ve byte karşılaştırmasıyla aynı oldukları doğrulandı.
- Başlangıçta silinmiş `link.xml` / `.meta` son Unity derlemesinde yeniden üretildi. **Yeni koordinatör 11:49 mesajında bu dosyalara dokunmamamı ve temizliği kendisinin yapacağını bildirdi; son derleme dosyaları bu talimata göre koordinatöre bırakıldı.** Bu iki dosya için kaynak temizliği/başlangıçtaki silinmiş durumun korunması koordinatörün kalan işidir. Görev kaynaklarında whitespace kontrolü temizdir; genel `git diff --check` yalnız Unity'nin ürettiği `link.xml.meta` boşluklarını bildirir.

Kaynak teslimleri: `GameSessionController.cs`, `CampaignLevelFactory.cs`, `DefenderBattlefieldDressing.cs`, `CoreRulesTests.cs`, `GreyboxSceneTests.cs`, `docs/MAP_OUTPOST_SYSTEM.md`; kanıt teslimi bu rapor ve beş `level-<n>.png`. Commit, push ve harici kanala gönderim yapılmadı.
