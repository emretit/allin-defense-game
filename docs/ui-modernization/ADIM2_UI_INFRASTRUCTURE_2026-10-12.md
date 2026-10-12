# Adım 2 — UI altyapısı

Çalışma ağacı: `/Users/emreaydin/AllinDefense-ui`, `ui-modernization`. Adım 1 tabanı `21cd9be` (EditMode 166, PlayMode 120). Değişiklikler commit edilmedi.

## Ölçüler ve dokunma

`UiMetrics` mantıksal ekran ölçüsünü iOS'ta UIKit `UIScreen.bounds`, Android'de `DisplayMetrics` yoğunluk ve piksel ölçüsüyle alır; Editör'de ekran pikselleri mantıksal birimdir. DPI tek kaynak değildir. CanvasScaler'ın referans çözünürlüğü, match değeri ve geometrik/logaritmik ölçek hesabı için saf yardımcılar bulunur; Expand/Shrink desteklenir. Minimum 44 pt, ana CTA 52 pt, yetenek 72 pt sabitleri sunulur.

`MobileTouchTarget` görünür RectTransform'u değiştirmez; şeffaf raycast çocuğunu boyut/ölçek/ekran dönüşümü değiştiğinde yeniden boyutlandırır. LateUpdate kaldırıldı. Canvas render bildirimi ölçek animasyonlarını izler; aynı değerlerde hit rect'e yazmaz. Komşu minimum hedef testi mantıksal 60 pt merkez aralığında 16 pt boşluğu doğrular. PlayMode ayrıca gerçek Button hedeflerini farklı CanvasScaler referansları ve %75 görsel basılma ölçeğiyle ölçer; komşu hit alanlarının kesişmediğini ve görsel boyutun değişmediğini doğrular. Sayısal yuvarlama için runtime hedef 44,01 pt olarak korunur.

## Kimlikler

`UiId` serialized string kimlik taşır. UI oluşturma yardımcıları ve HUD kurulumu UI RectTransform'larına mevcut isimle kimlik verir; kimlik nesne yeniden adlandırılınca değişmez. Test yardımcısı `UiQuery.FindById(root,id)` etkin olmayan/lazy ekranlarda da kimlik bulabilir ve scoped yolları destekler. GreyboxSceneTests, FrontendExperienceTests, AuditLayer2UxTests, HeroNameFlowTests ve AuditAudioSettingsTests UI sorguları taşındı; dünya, ışık, ses ve model adları korunur.

## Yerelleştirme

Tek UI kopya mekanizması mevcut `FrontendCopy` olarak seçildi: önceki menü bağlama davranışı korunur, kimlikler metin içeriğinden bağımsız C# alanlarıdır ve HUD da aynı çiftleri kullanır. `UiCopyCatalog` mevcut serialized asset/API için bir adapter olarak kalır, metinleri `FrontendCopy` üzerinden seçer. Dinamik çiftler `LocalizedCopy.Format(params object[])` şablonlarına dönüştürüldü; sayı formatı önceki interpolation ile aynı geçerli kültürü kullanır. Büyük harf için Adım 1 `GameLocalization.Uppercase` yardımcısı tek yerdedir.

Başlangıç `rg -n 'GameLocalization.Text\(' Assets/Scripts/UI Assets/Scripts/Presentation` çıktısı 108 satırdı (UI 45, Presentation 63; aynı satırda birden fazla çağrı var). Katalog Value getter'ı dışındaki toplam 113 literal TR/EN çağrısı taşındı. Önceki kaynakla katalog şablonları karşılaştırıldı: 113/113 birebir eşdeğer, eksik 0 (`Builds/FrontendExperience/adim2-copy-equivalence.json`). Ek olarak dil ternary/switch seçimi yapan briefing, devam detayları, giriş sayısı ve tehdit seviyeleri de aynı kataloğa taşındı. Sonrasında literal çift çağrısı 0; katalog Value getter'ının iki değişkenli çağrısı mekanizmanın tek dil seçme noktasıdır.

Türkçe anahtarlı `GameLocalization.English` sözlüğü korunur: `CampaignCatalog.cs` localizedName, Core wave preview, ShowMessage ve legacy HUD `HasTranslation`/`LocalizedLegacyText` hâlâ kullanıyor. Core/Data metinleri bu adımın kapsamı dışındadır; UI SetLoginStatus dışarıdan gelen eski Core durumlarıyla uyumlu kalır. Tüm kullanıcılar taşınmadığı için sözlüğü kaldırmak kampanya/HUD çevirilerini bozardı.

Unity Localization tabloları silinmedi. GUID taraması Assets, ProjectSettings ve Packages altında yapıldı; `1cc0a7aefeb3d44e08290385c99711ca` Türkçe ve `bed70df4d15eb45318b95fd6e5c2ea04` İngilizce tabloları Addressables Localization-String-Tables gruplarında, `4bf2ea280ed884788a91299e4aed0d11` Shared Data Localization-Assets-Shared grubunda referanslıdır. Koleksiyon GUID'i `7e039c3167dd74ce4a19c903c6299b0c` ve iç tablo/shared-data bağlantıları da kaydedildi; silme şartı sağlanmıyor.

## Kabul akışı

Native kabulün mevcut `UnityGameServicesController.Configure -> RestoreSession -> CloudCode` başlangıcı yüzünden yerel fixture'a bulut ilerlemesi karıştığı bulundu. Koordinatörün açık kararı (`msg_4ed1b95a71d9`) doğrultusunda debug+DC_FRONTEND_ACCEPTANCE koşulunda Configure erken döner; normal gövde aynen korunur. Ağsız PlayMode regresyonu acceptance modunda hiçbir progress/flow bağlanmadığını, env=0 yolunda mevcut bağlamanın hâlâ çalıştığını gösterir; SDK'ya varmadan eksik test progress'inde durur. Build ilk launch'ı da `SIMCTL_CHILD_DC_FRONTEND_ACCEPTANCE=1 zsh tools/build_and_run_ios_simulator.sh` ile yapılır.

**Açık operasyonel risk:** koruma eklenmeden önce verilen standart simulator başlangıcı mevcut otomatik UGS restore/sync yolunu çalıştırdı. Önceki kabul savaşlarının test-misafir hesabına claim göndermiş olması mümkün; backend okuması, yazması veya düzeltmesi yapılmadı. Koordinatöre `msg_734e288f2d43` ile bildirildi. Bundan sonraki native kabul çevrimi ağsızdır.

Debug+DC_FRONTEND_ACCEPTANCE koşulu altında her koşu için benzersiz geçici save kökü kullanılır; aynı kök scene reload aşamaları boyunca korunur. Böylece .active/.profiles/backup yan kayıtlar önceki misafiri geri yüklemez, hiçbir oyuncu kaydı silinmez. Başlangıçta sıfır yıldız ve unlockedLevel=1 kontrolü vardır. Boot/welcome, savaşta inşa menüsü, seçili kule, uzmanlaşma, 3 sn aktif dalga, pause, bölüm 10 boss dalgası ve yıldız animasyonu sonrası gerçek üç yıldız zaferi eklenir. Altın/unlock fixture yalnız bu kabul coroutine'lerinde uygulanır; savaş deterministik AdvanceBattle üzerinden normal spawn/hasar/kale kurallarıyla ilerletilir.

Taşma taraması aktif canvases'in görünür TextMeshProUGUI metinlerini kapsar (3D dünya numaraları kapsam dışıdır); disabled/alpha=0/cull edilen, ekran dışında veya RectMask2D dışında kalan metinler ve ForceMeshUpdate sonrasında görünür glyph üretmeyen etiketler hariçtir. Hatalar ekran + tam hiyerarşi yolu + dil ile tekilleştirilir.

İlk genişletilmiş kabul taraması daha önce test edilmeyen eski HUD taşmalarını ortaya çıkardı. Koordinatörün açık kararı (Orca `msg_802d540094dd`, ayrıca ask yanıtı) uyarınca HUD görünümü bu adımda düzeltilmez; bilinen Barracks/Mage ellipsis ve seçili kule Description taşmaları `knownIssues` içinde ayrı raporlanır ve status'u düşürmez. Yalnız yedi mevcut İngilizce ekran+yol+dil anahtarı allowlist'tedir; yeni bir yol, ekran veya dildeki her taşma `errors` alanına düşer. Adım 4 bu listeyi boşaltmalıdır. Boss reload fixture'ı izole save'de unlockedLevel=10 kurulumu yapar; yıldız sonucu değiştirilmez. Zaferde AppFlow'un gizlediği HUD için sonuç animasyonunun inactive coroutine başlatması engellendi; ayrı PlayMode regresyonu eklendi.

## Kapılar

- EditMode: 174/174 geçti (166 baseline + 8 yeni).
- PlayMode: **124/124 tam suite geçti** (120 baseline + 4 yeni; Builds/adim2-play.xml). Son üç test sorgusu yalnız kimliğe çevrildikten sonra ilgili HeroNameFlowTests + AuditAudioSettingsTests ayrıca **16/16** geçti (Builds/adim2-query-play.xml). Hedefli ilk regresyon koşusu da 4/4 geçti.
- **Simülatör kabulü: passed**, **errors=[]**, **knownIssues=6**, **19 PNG** (`Builds/FrontendExperience/iOS/report.json`). Son çevrim local-guest sahibi ve yerelde 1 pending claim ile kaldı; yeni misafir başlangıç kontrolü geçti, unlockedLevel=10 yalnız boss fixture sonucu. Build ilk açılışı ve kabul koşusu SIMCTL_CHILD_DC_FRONTEND_ACCEPTANCE=1 ile yapıldı.
- Görsel karşılaştırma: Adım 1 ekran kopyaları `/tmp/allin-ui-step1-captures` altında korundu; sonuçlar `Builds/FrontendExperience/adim2-image-comparison.json` ve `adim2-opaque-ui-comparison.json`. Ana menü ve TR/EN hesap ekranlarının opak parlak UI yazı pikselleri birebir aynı (20.705 / 15.074 / 15.385 pikselde değişim 0). Ayarlarda 3/5 pikselde en fazla 1 kanal birimi, haritada 81/115 pikselde en fazla 2 kanal birimi fark var. Tüm bitmapler birebir eşit değildir: main-menu ortalama mutlak kanal farkı 0,00423/255, hesaplar yaklaşık 0,004/255; mevcut ambient/renderer zaman farkı olarak yorumlandı. Savaş/yenilgi/zafer ve dönen harita farklı kabul senaryoları, gerçek 3 yıldız sonucu ve bölüm 10 unlock fixture'ı taşır; bu görüntüler veri bakımından baseline ile eşit değildir. Kaynakta görsel font, renk ve yerleşim değişikliği yoktur.
- `git diff --check`: temiz.


### Son knownIssues (EN)

- `battle-build-menu`: SelectedPanel/BuildChoices/Barracks/Label — GUARD BARRACKS / Level 2 ellipsis.
- `battle-build-menu`: SelectedPanel/BuildChoices/Mage/Label — MAGE OUTPOST / Level 3 ellipsis.
- `battle-selected-tower`, `specialization`, `battle-active-wave`, `pause`: SelectedPanel/Description — üç satırlı DAMAGE / RANGE / RATE mevcut kutudan taşar.

Tam kök yol `AllinDefenseSession/Game HUD/Defender HUD/…`; bu altı kayıt raporda ekran+yol+dil ile tutulur. Adım 4 bunları gidermelidir.

Build'in oluşturduğu ProjectSettings ürün adı/bundle ID/simulator SDK değişiklikleri ile addressables_content_state.bin geri alındı; link.xml ve meta her batch sonrası geri alındı. HUD builder/prefab değiştirilmedi, prefab GUID'i korundu. Commit/push/stash/reset/rebase/branch işlemi yapılmadı.

Görsel kapı kararı: koordinatör Orca ask yanıtında "Succeeded raporla" dedi; kapının amacını yerleşim/font/renk değişmemesi olarak teyit etti. Mevcut ambient/zaman gürültüsü ve istenen senaryo/gerçek sonuç farkları yukarıdaki ölçümlerle açıkça raporlandı.
