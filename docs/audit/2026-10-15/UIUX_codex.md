ALLIN: Defense v1.1 için oyuncu akışlarını güncel kod ve mevcut görüntüler üzerinden inceledim; doğrulanmış P0 bulmadım, 9 P1 ve 4 P2 bulgu kaydettim.
Öncelik, savaş kararlarını okunur ve açıklayıcı yapmak, duraklatmadan yanlışlıkla ilerleme kaybını önlemek ve menünün kayıt/devam durumunu doğru anlatmasıdır.
Yalnız bu rapor yazıldı; Unity, simülatör, build veya test çalıştırılmadı ve fiziksel cihaz kabulü aşağıdaki altı iş paketinin son kapısı olarak bırakıldı.

# Kapsam ve kanıt sınırı

- İncelenen HEAD: `593a7c6`; tarih: 15 Ekim 2026. Başlangıçta çalışma ağacında başka değişiklikler vardı; bunlara dokunulmadı.
- P0: temel akışı engelleyen doğrulanmış sorun; P1: v1.1 kalite hedefinde ele alınmalı; P2: cila veya erişim kapsamını iyileştirme. P0 bulunmaması cihaz kabulünün geçtiği anlamına gelmez.
- S: dar kapsamlı davranış/metin; M: bir ekran/akış ve durumları; L: birden çok durum, erişim yöntemi veya cihaz doğrulaması.
- Görüntüler `ls -t` ile tarihlendirildi. `Builds/FrontendExperience/iOS/*.png` 13 Ekim tarihli, bazıları **0/30 yıldız ve tek bölge** gösteriyor; bunlar güncel kampanya kapsamının kanıtı değildir. `Builds/actors-v3/after-*.png` 14 Ekim, `Builds/region-text/*.png` 14 Ekim, `Builds/a11y/settings.png` ve `Builds/relearn/*.png` 15 Ekim 10:34–10:38; ayrıca 15 Ekim 10:36 tarihli `Builds/step6-after-*.png` örnekleri açıldı.
- Baktığım görüntüler: iOS ana menü TR, harita EN, brifing TR, savaş, uzmanlaşma, inşa önizleme, pause, yenilgi EN, welcome, hesap EN; relearn ayarlar TR ve düşman kartı EN; region-text final TR ve bölge sonucu EN; actors-v3 bölüm 43/720p; step6 hazırlık 1/720p ve boss 10/1560×720. Üç tasarım mockup'ı ve `tokens.css` de incelendi.
- Güncel **SE ve Pro Max üzerinde bütün akış**, gerçek Dynamic Island kesimi, VoiceOver sesi/odak sırası, titreşim ve ses kaydı için görüntü veya cihaz gözlemi yok. Statik çıkarım, gözlemlenmiş piksel hatası olarak sunulmadı; renk kontrastı ölçülmedi.
- 12/14 Ekim denetimleri yalnız bağlam olarak okundu. Geçmişteki “10 bölüm sınırı”, “valor görünür”, “kartlar yeniden açılamıyor”, “menü düğümlerinde erişilebilirlik yok” iddiaları tekrar açık sayılmadı.

# P0 — 0 bulgu

Bu salt okunur incelemede yeni, doğrulanmış yayın engeli saptanmadı.

# P1 — 9 bulgu

## P1-01 — Ana eylemin gösterdiği bölüm ile açtığı kayıtlı savaş farklı olabilir · M

**Oyuncu:** 8. bölüme ilerlemişken 3. bölümü tekrar oynayıp çıkarsa menü kartı sıradaki bitmemiş bölümü anlatırken BAŞLAT eski 3. bölüm checkpoint'ini açar; dalga başından devam sınırı da yazmaz.
**Neden:** Kısa oturumdaki en önemli vaat, kaldığı yere güvenle dönebilmektir; doğru checkpoint seçilse bile kartın yanlış hedefi göstermesi kaydın karıştığı izlenimi verir.
**Kanıt:** `Assets/Scripts/UI/AppFlowController.cs:429` geçerli checkpoint'i önceliklendirir; aynı dosya `:624` kartı yalnız `NextUnfinishedLevel()` ile doldurur; `Assets/Scripts/UI/AppFlowController.Screens.cs:131` eylemi daima `MenuStart` ile oluşturur. `Assets/Scripts/Core/GameSessionController.cs:920` kayıt noktasının dalga başı olduğunu gösterir. Güncel tekrar-savaşı menü görüntüsü yok.
**En küçük çözüm:** Kart ve düğme aynı devam-hedefi hesabını kullansın; geçerli kayıtta “Bölüm 3 · Dalga 2 başından devam” ve DEVAM ET, kayıtsız durumda BAŞLAT gösterilsin. Harita üzerinden farklı bölüm seçme korunmalı. **Paket:** İP1.

## P1-02 — Kayıt hatası ve kurtarma durumu açıklamasız kalıyor · M

**Oyuncu:** Yazma hatasında profil yanında yalnız çarpı, zaferde ise kaydedildi işaretinin yokluğunu görür; kurtarılmayı bekleyen okunamayan kayıt için profil normal onay işareti bile gösterebilir.
**Neden:** Kazanılan yıldızların kalıcı olup olmadığını ve ne yapacağını anlayamaz; kritik bilgi dekoratif simgeye dönüşür.
**Kanıt:** `Assets/Scripts/UI/AppFlowController.cs:621` yalnız `HasWriteError`/`Failed` ile simge seçer; `:681` zaferde işareti gizler. `Assets/Scripts/UI/AppFlowController.ClosedTest.cs:32` açıklayıcı `SaveStatus()` tanımlar, fakat `Assets/Scripts` taramasında çağrısı yoktur. `Assets/Scripts/Core/CampaignProgressStore.cs:484` okunamayan kaydı `RequiresReview` yapar. Hata durumu görüntüsü yok.
**En küçük çözüm:** Hata/kurtarma varsa profil ve sonuç ekranında kısa kalıcı durum + uygun yeniden dene/bilgi eylemi; sağlıklı kayıtta sade onay. Yerel kayıt hatası ile bağlantı beklemeyi ayır, teknik exception metni gösterme. **Paket:** İP1.

## P1-03 — Ertelenen hesap girişi açılışın ve ilk zaferin aktif vaadi olmaya devam ediyor · M

**Oyuncu:** İlk ekranda Apple/Google ile devam, hesap ekranında cihazlar arası taşıma ve ilk zaferde hesap bağlama çağrısı görür.
**Neden:** Verilen ürün kararı “giriş sonraya” iken ilk oturum gereksiz hesap seçimi ve henüz sürüm kapsamına alınmayan bir güvenceyle açılır. Bu bulgu auth servisinin çalışmadığı iddiası değildir.
**Kanıt:** `Assets/Scripts/UI/AppFlowController.Screens.cs:351` Apple görünürlüğünü platforma bağlar, `:354` Google düğmesini oluşturur; `:407`–`:412` hesapta üç giriş eylemi vardır. `Assets/Scripts/UI/AppFlowController.cs:700` ilk zaferde bağlama önerisini açar; `Assets/Scripts/UI/FrontendCopy.cs:94` cihazlar arası ilerleme sözü verir. Görsel karşılığı: `Builds/FrontendExperience/iOS/welcome.png`, `Builds/FrontendExperience/iOS/account-English.png` (13 Ekim; davranış güncel kodla doğrulandı).
**En küçük çözüm:** v1.1 guest sürümünde sağlayıcı düğmelerini/zafer çağrısını tek sürüm yetenek bayrağıyla gizle; hesap yüzeyini “bu cihazdaki ilerleme”, yasal bağlantılar ve mevcut silme işlemiyle tutarlı yap. Giriş entegrasyonu ekleme. **Paket:** İP2, ilk zafer çağrısının tüketicisi İP1.

## P1-04 — Karar metinleri 9–12 pt civarında; uzun savaş ipuçları kesiliyor · M

**Oyuncu:** Kule rolü, hasar/menzil/hız ve geri alınamayan dal seçimi açıklaması küçük sol-alt karta sıkışır; yeni mekanik mesajının devamını okuyamaz.
**Neden:** Büyük ve rahat dokunulan ikonlar, karar vermek için gereken okunabilir açıklamanın yerini tutmaz. Özellikle kısa oturum ve küçük telefonda yanlış kule seçimini artırır.
**Kanıt:** `Assets/Scripts/Presentation/DefenderHudController.Radial.cs:177`–`:181` rol/hint 9, istatistik 10, başlık 12 boyutundadır; `:193` ellipsis. `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs:112` kartı 198×80 yapıp logical-point ölçeğine taşır; `:235` mesajın genişliğini `min(330, width-340)` ile sınırlar. `Assets/Scripts/Presentation/DefenderHudController.BattleChrome.cs:31` tek satır/ellipsis ve 11–13 boyut kullanır. `Builds/actors-v3/after-ch43-wave-1280x720.png` içinde “Oklar aktif saldırı hatlarını ...” kesilmesi görülür; yeni `Builds/step6-after-chapter10-boss-1560x720.png` de küçük kule kartını gösterir.
**En küçük çözüm:** Halka menüyü koruyarak bilgi kartına içerik yüksekliği ve en az 14 pt hedef gövde ölçüsü ver; önemli uyarılar iki satır olsun veya dokunarak tam açıklama açılsın. Dal değiştirme bedeli ve rehber eylemi ellipsis ile kaybolmasın. **Paket:** İP3.

## P1-05 — Uzmanlaşmayı seçerken dalın asıl işlevi gizleniyor · S

**Oyuncu:** Keskin Nişancı önizlemesinde hasar/hız farkını görür, fakat destek düşmanlarını önce hedeflediğini ve uzun menzil avantajını göremez; açıklama satın aldıktan sonra görünür.
**Neden:** Satıp yeniden kurmayı gerektiren seçim, satın alma öncesi yeterli bilgi olmadan yapılır.
**Kanıt:** `Assets/Scripts/Presentation/DefenderHudController.Radial.cs:384`–`:385` tüm dal önizlemelerinde `selectedRoleText`i gizler; `:400` açıklamayı o gizli metne yazar; `:422`–`:427` Keskin Nişancı için yalnız genel saldırı farkını üretir. `Assets/Scripts/UI/FrontendCopy.cs:210` doğru açıklama zaten vardır. `Builds/FrontendExperience/iOS/specialization.png` aynı karşılaştırma düzenini gösterir (13 Ekim).
**En küçük çözüm:** Her dalda isim + ayırt edici işlev + ilgili sayısal farklar önizlemede birlikte görünsün; Keskin Nişancı için menzil ve hedef önceliği, Kalkan için birlik dayanıklılığı gösterilsin. **Paket:** İP3.

## P1-06 — Sıradaki dalganın içeriği ve erken çağırma sayacı HUD'a ulaşmıyor · M

**Oyuncu:** “×50” gibi bir toplam görür; hangi türlerin geleceğini ve kendiliğinden başlamaya kaç saniye kaldığını göremez. Erken çağırma düğmesi sıradan SONRAKİ DALGA gibi görünür.
**Neden:** TD oyuncusunun hazırlık/yükseltme ve risk zamanlaması kararı bilgi kaybeder.
**Kanıt:** `Assets/Scripts/Core/GameSessionController.cs:434`–`:446` erken çağırma saniyesini üretir, fakat `Assets/Scripts/Presentation/DefenderHudController.cs:289` `SetNextWaveLabel` boştur; `:278` `SetPreview` de boştur. `Assets/Scripts/Presentation/DefenderHudController.Frame.cs:115` yalnız toplamı ve `:121` sayaçsız etiketi yazar. Otomatik başlangıç `Assets/Scripts/Core/GameSessionController.cs:419`, toplamın anlamı `Assets/Scripts/Core/GameSessionController.Battle.cs:19` ile doğrulanır. Görsel: `Builds/step6-after-chapter1-prep-1280x720.png`.
**En küçük çözüm:** Mevcut üretilen önizlemeyi ve geri sayımı göster; çipe dokununca tür/sayı/giriş özeti açılsın. `EarlyWaveReward => 0` (`Assets/Scripts/Core/GameSessionController.Battle.cs:12`) nedeniyle altın bonusu vaat etme; bonus eklemek ayrı denge kararıdır. **Paket:** İP4.

## P1-07 — Duraklatmadan yeniden başlatma tek dokunuşla checkpoint'i siliyor · S

**Oyuncu:** DEVAM ET'in hemen altındaki YENİDEN BAŞLAT'a yanlış dokunursa o savaş baştan açılır; geri alma/onay yoktur. Haritaya dönüşte ise onay vardır.
**Neden:** Kısa mobil oturumda birkaç dakikalık savaş emeği yanlış dokunuşla kaybolur; tehlike hiyerarşisi ters kurulmuştur.
**Kanıt:** `Assets/Scripts/Presentation/DefenderHudController.Frame.cs:67` restart callback'ini doğrudan bağlar; `:66`–`:69` haritaya onay uygular. `Assets/Scripts/Core/GameSessionController.cs:1118` checkpoint'i temizler. Görsel: `Builds/FrontendExperience/iOS/pause.png`.
**En küçük çözüm:** “Bu savaşın ilerlemesi silinecek; bölüm yıldızların korunur” açıklamalı Yeniden başlat / Oyuna dön onayı ekle; iptal varsayılan güvenli eylem olsun. **Paket:** İP4.

## P1-08 — Savaş sırasında hareket/titreşim ayarlarına erişilemiyor · M

**Oyuncu:** Sarsıntıdan rahatsız olduğunda pause içinde yalnız müzik/efekt aç-kapat bulur; azaltılmış hareket, sarsıntı, titreşim ve dil için savaştan menüye çıkması gerekir.
**Neden:** Rahatsızlığın yaşandığı yerde çözüm yok; menüde bulunan erişilebilirlik seçeneği savaş anında kullanışsızlaşır.
**Kanıt:** `Assets/Editor/DefenderPresentationAssetBuilder.cs:1548`–`:1555` pause içeriği; `Assets/Scripts/Presentation/DefenderHudController.Frame.cs:70`–`:79` yalnız iki ses anahtarını bağlar. Tam seçenekler `Assets/Scripts/UI/AppFlowController.Screens.cs:532`–`:545` içinde menüye aittir. Görsel: `Builds/FrontendExperience/iOS/pause.png` ve `Builds/relearn/settings-reset-Turkish.png`.
**En küçük çözüm:** Pause içinde AYARLAR aç; yatay güvenli alanda kaydırılabilir aynı tercihleri sun ve kapatınca savaş duraklatılmış kalsın. Ses düzeylerini sıfırlamadan koru. **Paket:** İP4.

## P1-09 — VoiceOver menüden savaşa geçince temel eylemleri ve bilgi kartını kaybediyor · L

**Oyuncu:** Menü ve ayar düğümleri mevcut olsa da savaşın duraklat/hız/dalga eylemlerini bulması ve düşman kartının açıklamasını dinlemesi sağlanmamış.
**Neden:** Kısmen çalışan erişim akışı savaş girişinde kopar; yeni düşman modalinde yalnız “Anladım” eylemini duymak, içeriği öğrenmek değildir.
**Kanıt:** `Assets/Editor/DefenderPresentationAssetBuilder.cs:1475`–`:1479` hız/pause'u boş etiketli `IconButton`, `:1509` dalgayı boş metinli düğme oluşturur. `Assets/Scripts/UI/Kit/UiAccessibleButton.cs:49` boş etiketi erişilebilir saymaz; `Assets/Scripts/Presentation/DefenderHudController.cs:384` görsel dalga metnini günceller ama erişilebilir kopyayı bağlamaz; `DefenderHudController.Frame.cs:40` yalnız pause **iç** kontrollerini etiketler. `Assets/Scripts/UI/Kit/UiKit.cs:48`–`:60` metinleri erişilebilir düğüm yapmaz; `Assets/Scripts/UI/AppFlowController.Screens.cs:698`–`:703` düşman açıklamasını bu metinlerle kurar. Bu statik kapsam bulgusudur; gerçek VoiceOver oturumu yok.
**En küçük çözüm:** Önce görünür savaş eylemlerine yerelleşmiş ad/değer, kart açıklamalarına okunabilir metin düğümleri ve modal girişinde uygun odak ver. Eylemsiz bilgiye Button rolü verme; altın/can/dalga güncellemelerini sürekli konuşan spam'e dönüştürme. Savaş alanındaki dünya yuvalarının tam VoiceOver oynanabilirliği ayrı kapsam olarak açıkça belirtilmeli. **Paket:** İP4 eylem bağları; İP5 ortak metin/odak altyapısı.

# P2 — 4 bulgu

## P2-01 — Kahramanın dönüş süresi, yetenek bekleme süresiyle aynı görünüyor · S

**Oyuncu:** Kahraman öldüğünde aynı yetenek simgesinin üzerinde saniye sayacı görür; yeniden doğuşu mu, yeteneği mi beklediğini anlayamaz.
**Neden:** Kahramanı tekrar kullanabileceği anı yanlış yorumlar.
**Kanıt:** `Assets/Scripts/Presentation/DefenderHudController.Frame.cs:126` ölümde `RespawnRemaining` değerini aynı cooldown'a yollar; `Assets/Scripts/UI/Kit/UiRadialCooldown.cs:26` yalnız saniye yazar. Ölüm durumunun görüntüsü yok.
**En küçük çözüm:** “Geri dönüyor · 8 sn” durumu ve sönük kahraman işareti; canlı yetenek cooldown'ından farklı erişilebilir değer. **Paket:** İP4.

## P2-02 — Yıldız hedefi açıklanmıyor; 150/150 sonunda olmayan eksiklere yönlendiriliyor · M

**Oyuncu:** Zaferde 1–3 yıldız görür ama üçüncü yıldız için neyi iyileştireceğini öğrenmez; bütün yıldızları toplayınca final hâlâ EKSİK YILDIZLARA DÖN der.
**Neden:** Tekrar oynama hedefi belirsizdir; tamamlanmış koleksiyon için boş durum yanlış anlatılır.
**Kanıt:** `Assets/Scripts/UI/AppFlowController.cs:648` yıldızları kalan can oranına (%75/%40) bağlar; `:673` sonuç yalnız yıldız sayısı yazar. `Assets/Scripts/UI/AppFlowController.CampaignCompletion.cs:111` final CTA'sını koşulsuz eksik yıldız yapar; `:65` en düşük yıldızlı bölüme gider. `Assets/Scripts/Core/CampaignProgressStore.cs:220` tam koleksiyonda da bir bölüm döndürür. `Builds/region-text/final-Turkish.png` 148/150 için doğru hâli gösterir; 150/150 görüntüsü yok.
**En küçük çözüm:** Sonuçta “3 yıldız: en az X can” ve mevcut en iyi sonucu belirt; 150/150'de tamamlanma kutlaması ve BÖLÜM SEÇ CTA'sı kullan. **Paket:** İP1 sonuç açıklaması; İP2 tam koleksiyon durumu.

## P2-03 — Sistem azaltılmış hareket tercihi ilk açılışa taşınmıyor · M

**Oyuncu:** iOS'ta azaltılmış hareket açık olsa da oyun kendi ayarı değiştirilene kadar normal hareket ve varsayılan sarsıntıyla açılır.
**Neden:** Kullanıcının daha önce verdiği erişim tercihini tekrar kurması gerekir.
**Kanıt:** `Assets/Scripts/UI/FrontendPreferences.cs:44` yalnız PlayerPrefs + `false` varsayılanını okur; `:38` sarsıntı varsayılanı true. `Assets/Scripts/UI/AppFlowController.cs:323` açılış animasyonu bu tercih düzenini kullanır. `Assets/Scripts`/`Assets/Plugins` taramasında sistem reduce-motion köprüsü bulunmadı. Cihaz gözlemi yok.
**En küçük çözüm:** Oyun içi açık tercih yoksa iOS hareket tercihini başlangıç değeri olarak kullan; açık kullanıcı tercihini koru ve sistem değişimini dinle. Mevcut azaltılmış hareket davranışını yeniden yazma. **Paket:** İP5.

## P2-04 — İngilizce sayı biçimi cihaz kültürüne bağlı kalıyor · S

**Oyuncu:** İngilizce seçiliyken süre ve saldırı hızında “~2,5 min”, “3,6/s” görebilir.
**Neden:** Dil seçimi sayılara uygulanmaz; arayüzün yerelleştirme tutarlılığı zayıflar.
**Kanıt:** `Assets/Scripts/UI/FrontendCopy.cs:10` `string.Format` için dil kültürü vermiyor; `:376` süre formatı ve `:229` saldırı hızında ondalık formatı var. `Builds/FrontendExperience/iOS/map-English.png` ve `specialization.png` virgüllü İngilizce değerleri gösterir; kod yolu güncelde sürüyor.
**En küçük çözüm:** LocalizedCopy.Format seçilen oyun diline göre sayı kültürü kullansın; TR virgül, EN nokta. Literal sayıları ve teknik kimlikleri etkileme. **Paket:** İP2.

# Akış kapsamı ve korunacaklar

| Akış / boyut | İnceleme sonucu |
|---|---|
| Açılış → ana menü | Boot marka geçişi mevcut (`AppFlowController.cs:304`); hesap kapsamı P1-03, devam/kayıt P1-01/02. Güncel boot cihaz görüntüsü yok. |
| 5 bölge / kilit / yıldız | `AppFlowController.ClosedTest.cs:12` beş bölge; `Screens.cs:150` sekmeler; `CampaignCompletion.cs:24` önceki boss yıldızıyla kilit ve `:33` açık koşul metni. Yeni kilitli-bölge tam ekran görüntüsü yok; eski tek-bölge görüntüsüyle hata çıkarılmadı. |
| Brifing / öğrenme hedefi | `AppFlowController.cs:494` hedef; `:496` düşman toplamları; `Screens.cs:681` tekrar açılabilen kart. Düşman çipleri yatay kaydırılıyor; çok tür/SE taşma kabulü henüz yok. |
| Savaş HUD / inşa / halka | Yuva → ikon önizleme → altın KUR korunmalı (`DefenderHudController.Radial.cs:293`, `:340`); yetersiz altın miktarı ve ses/titreşim geri bildirimi mevcut (`:302`, `:334`). P1-04/05/06. |
| Yetenek / boss / hız / pause | Yetenek menzil önizlemesi mevcut (`DefenderHudController.cs:255`), 44 pt pause/hız ve 64 pt yetenek düzeni var (`DefenderHudResponsiveLayout.cs:231`, `DefenderHudController.Frame.cs:58`); P1-07/08/09, P2-01. Bilinen boss işleri aşağıda yalnız pakete aktarıldı. |
| Zafer / yenilgi / bölge finali | Valor yerine yıldız, yenilgide sızan tür ve tavsiye, gerçek bölge/final kartı var (`AppFlowController.cs:669`, `:673`, `CampaignCompletion.cs:80`). P2-02; yenilgi görüntüsündeki yönlendirme anlaşılır. |
| İlk savaş / yeni düşman | 6 adım, atla ve yardım (`DefenderHudController.Tutorial.cs:60`); ayardan rehberi sıfırlama (`Screens.cs:535`), kartla duraklatıp geri devam (`:661`, `:707`) mevcut. P1-04/09. |
| Hesap | Yerel ilerleme açıklaması/yasal bağlantı/silme yüzeyi var (`Screens.cs:403`, `:417`, `:419`); v1.1 için giriş vaadini azaltma P1-03. Canlı auth/silme servisi denenmedi. |
| Safe area / hedef boyutu | Menü safe root mevcut (`AppFlowController.cs:287`); HUD ve halka safe-area hesabı mevcut (`DefenderHudResponsiveLayout.cs:96`, `DefenderHudController.Radial.cs:506`); `UiMetrics.cs:9` 44 pt ve `MobileTouchTarget.cs:59` genişletme var. Bunlar fiziksel SE/Pro Max'te çakışma/erişim kanıtı yerine geçmez. |
| Yükleme / hata | Yükleme perdesi (`FrontendLoadingOverlay.cs:21`), döndürme zaman aşımı (`FrontendOrientationWait.cs:7`) ve yeniden dene (`AppFlowController.cs:828`) mevcut. Hazırlık başarısızlığında retry var (`:768`); ekrana dönme/iptal alternatifi yok, ancak kilitlenme bu incelemede yeniden üretilmedi. |
| Ses / titreşim / animasyon / stil | Haptik tercihi ve native köprü (`UiHaptics.cs:28`), azaltılmış hareket destekleyen geçişler mevcut; işitilmedi/hissedilmedi. UiTokens paleti ve kit yüzeyleri yaygın; özgün çizgisel simgeler ile detaylı kule ikonlarını topluca yeniden tasarlamayı gerektiren doğrulanmış sorun çıkmadı. |

# En fazla altı bağımsız iş paketi

**İlk üç öneri: İP4 → İP3 → İP1.** Bunlar sırasıyla yanlışlıkla savaş kaybı/zamanlama, satın alma kararının okunurluğu ve oturum devam güvenini düzeltir. Dosya sahiplikleri aşağıda ayrıdır; bir worker başkasının dosyasına düzeltme yapmamalı. Ortak yeni kopyalar için her paket kendine ait `LocalizedCopy` yardımcı dosyası açabilir; `FrontendCopy.cs` yalnız İP2'ye aittir. Aynı bulgunun iki yüzeye dokunması dosya sahipliği paylaşımı değildir; son kabulde birlikte doğrulanır.

| Paket | Kapsam / büyüklük | Tek sahibi olduğu üretim dosyaları | Kabul kriteri |
|---|---|---|---|
| **İP1 — Devam, kayıt ve sonuç güveni** | P1-01/02; P1-03 ilk zafer çağrısını sürüm kapsamından çıkarma; P2-02 yıldız eşiği. **M**. Bilinen menü müziği ürün kararı da bu pakete bağlı bekleyen alt iştir. | `Assets/Scripts/UI/AppFlowController.cs`, `AppFlowController.ClosedTest.cs`; gerekirse yeni `FrontendSessionCopy.cs`, `FrontendMenuMusic.cs`. | 8 açık/3 checkpoint senaryosunda kart ve DEVAM ET aynı bölüm/dalgayı gösterir; geçersiz kayıtta başlat açıkça anlatılır; yerel hata/kurtarma ve bağlantı bekleme farklı metinlerdir; yıldız eşiği başlangıç canına göre doğrudur; guest ilk zaferinde auth çağrısı yok. Menü müziği onaylanırsa ses tercihi ve savaş geçişi çift müziksiz çalışır, karar yoksa bu alt iş bekler. |
| **İP2 — Menü, hesap ve kampanya metinleri** | P1-03 ekranlar; P2-02 tam koleksiyon; P2-04. **M**. | `Assets/Scripts/UI/AppFlowController.Screens.cs`, `AppFlowController.CampaignCompletion.cs`, `FrontendCopy.cs`, `GameLocalization.cs`; gerekirse yeni `FrontendReleaseCapabilities.cs`. | Guest welcome/hesap/ayarlar auth ve bulut vaadi sunmaz, yasal/silme erişimi kalır; 148/150 eksik yıldıza, 150/150 bölüm seçimine gider; TR/EN ondalık biçimleri seçilen dile uyar; tüm beş bölgenin uzun başlıkları ve kartları SE genişliğinde kayıpsızdır. |
| **İP3 — Halka bilgi kartı ve savaş metni** | P1-04/05. **M**. | `Assets/Scripts/Presentation/DefenderHudController.Radial.cs`, `DefenderHudController.BattleChrome.cs`, `DefenderHudResponsiveLayout.cs`, `DefenderHudController.Tutorial.cs`; yeni `HudDecisionCopy.cs`. | Üç kule/altı dalda satın alma öncesi rol + fark + bedel okunur; Keskin Nişancı hedef önceliği görünür; 14 pt hedef gövde ve iki satırlı uyarı, TR/EN ellipsis'siz önemli açıklama; halka/CTA safe area'dan çıkmaz, yuva→ikon→KUR üç dokunuşu ve 44 pt alt sınırı korunur. |
| **İP4 — Dalga, pause ve kahraman durumları** | P1-06/07/08; P1-09 savaş eylem etiketleri; P2-01; **önceden bilinen** boss adı/taç. **L**. | `Assets/Scripts/Presentation/DefenderHudController.cs`, `DefenderHudController.Frame.cs`, `Assets/Editor/DefenderPresentationAssetBuilder.cs`, `Assets/Scripts/UI/Kit/UiRadialCooldown.cs`, `Assets/Scripts/Core/GameSessionController.Battle.cs`; yeni `HudPauseSettings.cs`, `HudStateCopy.cs`; yalnız koordinatörün uygun gördüğü üretim aşamasında `Assets/Art/GeneratedPresentation/DefenderHud.prefab`. | Ara dalga sayacı 8→0, erken çağrı sonrası anında doğru durum; tür/sayı/giriş önizlemesi; bonus yoksa bonus vaadi yok; restart iptali checkpoint'i korur/onay bir kez restart eder; pause ayarları savaşı sürdürmez; ölüm “geri dönüyor” durumudur; VoiceOver pause/hız/dalga/yetenek adı ve değerini okur. Bilinen `.claude/ui-specs/boss-name-hud.txt` uygulanarak 30/50 adları ve taç rozeti düzeltilir. |
| **İP5 — Bilgi erişimi ve sistem hareket tercihi** | P1-09 ortak bilgi/odak; P2-03. **L**. | `Assets/Scripts/UI/Kit/UiAccessibleButton.cs`, `UiKit.cs`, `UiOverlay.cs`, `Assets/Scripts/UI/FrontendPreferences.cs`; yeni `UiAccessibleText.cs`, `UiSystemAccessibility.cs`, `Assets/Plugins/iOS/AllinSystemAccessibility.mm`. | VoiceOver düşman başlık/özellik/taktik metnini ve sonucu eylem gibi sunmadan okuyabilir; modal açıkken arka yüzeye kaçmaz, kapatılınca uygun odağa döner; ekrandan kırpılan kontrole odaklanınca görünür kılınır; OS reduce-motion ilk açılışta etkili ve oyun içi açık tercih kalıcıdır. Menü kontrollerinin mevcut düğümleri korunur. |
| **İP6 — Telefon kabulü ve kanıt matrisi** | Statik denetimde kalan cihaz/ölçüm boşlukları. **M**. Üretim düzeltmesi yapmaz. | Yalnız yeni `Assets/Tests/PlayMode/UiUxV11AcceptanceTests.cs` ve `docs/audit/2026-10-15/DEVICE_ACCEPTANCE.md`; kanıt çıktıları `Builds/uiux-v11/`. | Koordinatör Unity kilidini bıraktığında SE ve Pro Max logical boyutlarında TR/EN tüm akışlar, iki yatay yön/safe area, her yuvada halka, ayarlar kaydırması, 5 bölge kilit/açık, 1/2/3 yıldız ve 150/150, ölüm/geri dönüş, VoiceOver ve azaltılmış hareket matrisi doldurulur. Her sonuç ekran görüntüsü/test veya fiziksel gözlem kimliği taşır; ses/titreşim fiziksel cihazda ayrıca kaydedilir; başarısızlık ilgili dosya sahibine döner. Bu rapor sırasında bu iş çalıştırılmadı. |

**Bilinen açıkların muhasebesi:** Boss adının “Bölge Lordu” kalması ve boss rozetinin taç olmaması yeniden keşif/bulgu sayısına eklenmedi, yalnız İP4'e taşındı. Menü müziği de yeni bulgu sayılmadı; mevcut ürün kararı bekleyen P1-6 olarak İP1'de tutuldu. Ücretsiz/reklamsız kapsam, gizli valor, menü dikey/savaş yatay ve halka menü kararları değiştirilmedi.

**Sahiplik kapısı:** Tablodaki üretim dosyaları paketler arasında tekrarlanmıyor; İP6 üretim koduna sahip değil. Her paketin test ekleri gerekirse ayrı dosya adlarıyla kendi worker'ına verilmeli; ortak builder/prefab üretimi yalnız İP4 tarafından, Unity kullanımını koordinatörle planlayarak yapılmalı. Bu rapor bir uygulama veya cihaz kabul tutanağı değildir.
