# Oyun içi UI taraması — ALAN A (savaş çerçevesi) · 2026-10-15

**Özet.** Wave 1'in getirdiği yeniden başlatma onayı, puan (pt) ölçeğine çevrilmediği için iPhone'da ~143 pt genişlikte ve **8 pt yazıyla** açılıyor (P0). Halka hatasının "akrabası" olan 3 zıplama bulundu: Pro Max'te bilgi kartı açılınca üst bar yeniden diziliyor, mesaj balonu (toast) seçime ve kart boyuna göre 54 pt'ye kadar yer değiştiriyor, toast giriş animasyonu yarıda yeniden dizilirse 6 pt kayık kalıyor. Kompaktlıkta en büyük kazanç: can bilgisinin iki kez gösterilmesi (üst-sol "20" + kale plakası "KALE 20/20") ve telefonlarda ekranın tam ortasında yüzen 378 pt'lik toast; toplam 1 P0 · 8 P1 · 7 P2, 16 bulgunun 16'sı YENİ (6'sı plandaki bir maddenin yan etkisi/akrabası olarak işaretli).

Yöntem: kod okundu (HEAD 0a69123 + commit'siz wave 1 ağacı). Unity/simülatör koşulmadı. Görsel kanıt yalnız `Builds/HudFrame/preparation-{844x390,800x450}.png` (15 Eki 12:38, editör; güvenli alan boşluğu yok, "Sıradaki" şeridi görünüyor → wave 1 sonrası). Diğer her ölçü **kodla hesap**.

Ölçek: savaş HUD canvas'ı 1920×1080, match 0.5 (`GameSessionController.cs:809-811`). 1 canvas birimi = SE 0.347 pt · iPhone 13 0.398 pt · Pro Max 0.440 pt · iPad 0.616 pt. `DefenderHudResponsiveLayout` üst/alt barı, toast'u, pause ve harita onayını `localScale = 1/points` ile **pt'ye** çeviriyor; bu listede olmayan her öğe canvas biriminde kalıyor (P0-1 ve P1-7'nin kök sebebi).

Güvenli alan varsayımı (yatay): SE 667×375 (boşluk yok) · iPhone 13 844×390 → 750×369 (sol/sağ 47, alt 21) · Pro Max 932×430 → 814×409 (59/59, alt 21) · iPad 1024×768.

---

## Kompaktlık tablosu — iPhone 13 (844×390 pt, güvenli 750×369) · kodla hesap

| Öğe | Boyut (pt) | Ekran payı | Ne zaman | Not |
|---|---|---|---|---|
| Üst bar bandı | 56 yükseklik | yüksekliğin %14'ü | hep | bar arka plansız; öğeler 44 pt, band 56 (yalnız geniş moddaki 52 pt dalga paneli için) |
| Üst bar öğeleri (Can 76 + Altın 124 + Dalga 200 + Hız 76 + Duraklat 44) | 520×44 | alanın %7'si, güvenli genişliğin %69'u | hep | `ResponsiveLayout.cs:234-255` |
| "Sıradaki" şeridi | ≤300×32 (tipik ~180×32) | %1.7 | hazırlık + ara | üst HUD'u ortada ekranın **%25'ine (96 pt)** kadar indiriyor |
| Kale plakası | 146×48 | %2.1 | hep | üst-sol can hapıyla aynı bilgi (P1-4) |
| Alt-sağ küme (NextWave 208×54 + yetenek 64×88 + kahraman adı 80×18) | çerçeve 288×122 | öğeler %5.6, çerçeve %10.7 | NextWave yalnız hazırlık/ara | dalga sırasında 208 pt boş kalıyor, yetenek sağdan 224 pt içeride |
| Toast (mesaj) | 378×≥44 | ≥%5.1 | ~2–5 sn | alttan **138 pt yukarıda, yatay ortada** (P1-3) |
| Toplam kalıcı (hazırlık) | — | ~%16.4 | | dalga sırasında ~%11.2 |

---

## P0

### P0-1 · Yeniden başlatma onayı minicik: 8 pt yazı, 143 pt kart, fazladan "İptal" — YENİ (#9 düzeltmesinin yan etkisi)
- **Ne:** Duraklat → YENİDEN BAŞLAT'a basınca açılan onay penceresi, arkadaki duraklat kartının (360 pt) ~%40'ı genişlikte. "İlerleme silinecek" uyarısı ve düğme yazıları okunamayacak kadar küçük. Altta üçüncü bir düğme ("İptal") var; "Oyuna dön" ile aynı işi yapıyor.
- **Kanıt:** `DefenderHudController.WaveForecast.cs:51-58` modal HUD köküne canvas biriminde kuruluyor; `DefenderHudResponsiveLayout.cs:282-286` pt'ye çevirme döngüsü yalnız `"PauseOverlay", "MapConfirmation"` içeriyor. Kart genişliği `UiKit.cs:304` `AbilityPt*5 = 360` birim → 143 pt (iPhone 13), 125 pt (SE). Uyarı `TypeCaption 20` (`WaveForecast.cs:53`) → **8.0 pt** (SE 6.9 pt); düğme etiketleri de `TypeCaption 20` (`UiKit.cs:146`) → 8 pt; başlık `TypeSubheading 36` → 14 pt. Kapat düğmesi `UiKit.cs:306` her modalda oluşuyor; pause/harita onayında gizleniyor (`DefenderPresentationAssetBuilder.cs:1547,1563`), burada gizlenmiyor. Düğme yükseklikleri `UiPointSize` sayesinde 52/44 pt kalıyor, yani kart dar ve uzun.
- **Durum:** tüm cihazlar; en kötü SE.
- **En küçük düzeltme:** `ResponsiveLayout.cs:282` dizisine `"RestartConfirmation"` ekle (modal `InitializeFrame` içinde, `responsiveLayout.Initialize()`'dan önce kuruluyor; `DefenderHudController.cs:230,254`). Sonra pause ile aynı tipografi: uyarı ve düğmeler 14 pt, başlık 20 pt (`DefenderPresentationAssetBuilder.cs:1557-1558` gibi). `RestartConfirmation.Card/RestartConfirmation.Close` → `SetActive(false)`. Test: kart genişliği ≥ 300 pt, uyarı ≥ 13 pt.
- **Boyut:** S

---

## P1

### P1-2 · Pro Max'te yuvaya basınca üst bar yeniden diziliyor (halka hatasının akrabası) — YENİ
- **Ne:** Bilgi kartı açılınca Altın hapı 24 pt daralıyor, dalga paneli 220×52'den 200×44'e iniyor ve 10 pt sağa kayıyor, "DALGA" etiketi ve ilerleme çubuğu küçülüyor; kart kapanınca hepsi geri büyüyor. Her yuva seçiminde üst bar "nefes alıyor".
- **Kanıt:** `DefenderHudResponsiveLayout.cs:233` `compact = width < 760 || selectedPanel.gameObject.activeSelf && width < 900`; bağımlı ölçüler `:238,:240-253`. Tetik: `DefenderHudController.Radial.cs:531-532` (`SetInformationCardVisible` → `RefreshLayout`).
- **Durum:** güvenli genişliği 760–900 pt arası olan cihazlar: Pro Max/Plus (932 → 814), 16 Pro Max (956 → 832); ayrıca editördeki 844×390 yakalamaları (boşluk yok → 844). SE (667) ve iPhone 13 (750) hep sıkışık modda, iPad (1024) hep geniş modda → zıplama yok.
- **En küçük düzeltme:** `compact = width < 900` (seçime bağlılığı sil). Pro Max'te de kalıcı sıkışık düzen kullanılır, üst bar 8 pt daha kısa olur. Test: seçim aç/kapa öncesi ve sonrası `TopBar/*` rect'leri eşit.
- **Boyut:** S

### P1-3 · Toast telefonlarda ekranın ortasında yüzüyor; seçim/kart boyu değişince zıplıyor — YENİ
- **Ne:** (a) Seçim yokken toast 378 pt genişlikte (iPhone 13'te genişliğin %50'si, SE'de %57'si), alttan 138 pt yukarıda, tam ortada; 5 sn boyunca savaş alanının göbeğini kapatıyor. (b) Mesaj açıkken yuvaya basınca toast ortadan sağa ışınlanıyor, SE'de genişliği 378 → 295 pt'ye düşüp yeniden kırılıyor. (c) Bilgi kartının boyu 126 ↔ 180 pt arasında değiştikçe (kule/boş yuva/seçenek önizleme) toast dikeyde 150 ↔ 204 pt arasında **54 pt** zıplıyor.
- **Kanıt:** `DefenderHudResponsiveLayout.cs:258-277`: genişlik `Min(378, width - (selection ? 372 : 32))`; konum `selection ? SafeMaxX : SafeCenterX`; alt kenar `selection ? Max(heroBottom+18, edge + informationCardHeight) + 2*edge : (width+chromeWidth)/2 > width-heroRight-80-edge ? heroBottom+18+2*edge : 2*edge`. Telefonlarda ikinci koşul hep doğru (iPhone 13: 564 > 438) → 138 pt. Kart boyu tetiği `Radial.cs:242` → `SetInformationCardHeight` → `RefreshLayout`.
- **Durum:** SE, iPhone 13, Pro Max (iPad'de alt-ortada 16 pt, sorunsuz). Mesaj örnekleri: "Kale muhafızı..." (5 sn, `GameSessionController.cs:1053`), "Yetersiz altın".
- **En küçük düzeltme:** Toast'a seçimden ve kart boyundan bağımsız tek yuva ver. Öneri: güvenli alanın alt-sol köşesi, `x = SafeMinX + 8`, `bottom = 8`, genişlik `Min(360, width - 288 - 24)` (kahraman kümesinin solu; SE'de 355 pt). Seçim açıkken kartın üstünde **sabit** `8 + ReservedInformationCardHeight(200) + 16` kullan, canlı `informationCardHeight` kullanma.
- **Boyut:** M

### P1-4 · Can iki kez gösteriliyor: üst-sol "20" + kale plakası "KALE 20/20" — YENİ
- **Ne:** Aynı bilgi iki ayrı öğede duruyor: üst-sol can hapı (76×44) ve kalenin üstündeki plaka (146×48, çubuklu). Toplam ~7.000 pt² kalıcı alan tekrar ediyor. Kritik canda ikisi de ayrı ayrı "punch" animasyonu yapıyor.
- **Kanıt:** `DefenderHudController.Frame.cs:96-101` (hap), `DefenderKeepPlate.cs:34,67-79` (plaka, aynı `Lives <= 5` eşiği). Görsel: `Builds/HudFrame/preparation-844x390.png` sol üstte "20", sağda "KALE 20/20".
- **Durum:** tüm cihazlar, tüm fazlar.
- **En küçük düzeltme:** Önerim: üst bardaki can hapını kaldır, Altın'ı `x=6`'ya al. Üst sırada 84 pt yer açılır; SE'de dalga paneli ortaya rahat oturur. Plaka kaleyle bağlam kurduğu ve hasar popup'ları ona bağlı olduğu (`DefenderRewardPopupPool.cs:190-196`) için kalsın. Alternatif: plakayı kaldır, hapı tut. Ürün kararı → Emre. `TopBar/Lives`'a bağlı testler güncellenmeli.
- **Boyut:** M

### P1-5 · Savaşta yeni düşman kartı açılınca arkasında Duraklat menüsü de açılıyor — YENİ
- **Ne:** İlk kez görülen düşman kartı oyunu `TogglePause` ile duraklatıyor. HUD bunu normal duraklatma sanıp "DURAKLATILDI" kartını ve karartmasını açıyor. Pause kartı ~346 pt, düşman kartı iPhone 13'te ~229 pt yüksekliğinde → pause kartının başlığı ve alt satırları düşman kartının üstünden ve altından taşar, çift karartma oluşur. "Anladım"dan sonra pause tekrar kapanır.
- **Kanıt:** `AppFlowController.Screens.cs:672-673` (`enemyOwnsPause` → `session.TogglePause()`); `DefenderHudController.Frame.cs:159-168` `paused = Phase == Paused && !SpecializationModalOpen` → `pauseModal.Show()`. HUD yenilemesi duraklamada da çalışıyor (`GameSessionController.cs:421-427`, unscaled 0.1 sn). Düşman kartı ölçüsü: `Screens.cs:696-704` (1000 birim genişlik, yaklaşık 574 birim yükseklik → 229 pt).
- **Durum:** her bölgede ilk yeni düşman dalgası; tüm cihazlar. Kodla hesap (`new-enemy` yakalaması yanlış ekranı çekiyor, plan #35).
- **En küçük düzeltme:** Oturuma `SpecializationModalOpen` benzeri bir bayrak ekle (ör. `IntroModalOpen`; `ShowNewEnemy` açar, `AcknowledgeEnemy` kapatır). `Frame.cs:159`'daki koşula `&& !session.IntroModalOpen` ekle. Test: kart açıkken `PauseOverlay` etkin değil.
- **Boyut:** S

### P1-6 · Altın/hasar popup'ları Duraklat ve Yeniden Başlat pencerelerinin üstünde donup kalıyor — YENİ (#29 akrabası)
- **Ne:** Dalga sırasında duraklatınca, o anda havada olan "+2", "-1" yazıları (24 taneye kadar) pause kartının ve karartmanın **üstünde** görünür kalır. Zaman durduğu için kaybolmazlar.
- **Kanıt:** `DefenderRewardPopupPool.cs:64-67` ayrı bir ScreenSpaceOverlay canvas, `sortingOrder = 5`; savaş HUD canvas'ı sıralama vermiyor (= 0, `GameSessionController.cs:806-811`). Ömür `Time.deltaTime` ile ilerliyor (`:168`), `timeScale=0`'da donuyor.
- **Durum:** dalga sırasında duraklatma, uzmanlaşma seçimi (savaş durur) ve yeni düşman kartı (HUD'un üstünde değil; AppFlow canvas'ı 500) dışındaki tüm HUD modalları.
- **En küçük düzeltme:** `LateUpdate`'te `canvas.enabled = Time.timeScale > 0`. Ya da popup canvas'ını `sortingOrder = -1` yapıp HUD'un altına al; ikincisi popup'ları üst barın da altına iter, ki bu #29 için de iyi.
- **Boyut:** S

### P1-7 · "DALGA 2/5" başlangıç bandı dalga panelinin üstüne biniyor; boyutu cihaza göre değişiyor — YENİ
- **Ne:** Her dalga başında 1.1 sn görünen bant, üst bardaki dalga panelinin tam ortasını kapatıyor ve panelde zaten yazan "DALGA 2/5"i tekrar ediyor. iPhone 13'te 175×19 pt kutu ve 11 pt yazı; iPad'de 271×30 pt. Güvenli alanı da hesaba katmıyor.
- **Kanıt:** `DefenderHudController.Motion.cs:23-30`: kök canvas biriminde `anchoredPosition (0,-64)`, `sizeDelta 440×48`, `TypeBody 28`, `localScale` yok. iPhone 13'te ekran üstünden 25–44 pt; dalga paneli 6–50 pt (`ResponsiveLayout.cs:239-243`).
- **Durum:** her dalga başlangıcı; tüm cihazlar.
- **En küçük düzeltme:** Bandı "Sıradaki" şeridinin yuvasına taşı. O şerit dalga sırasında zaten gizli (`WaveForecast.cs:73-74`), yani `TopBar` altında aynı yer ve aynı pt ölçeği kullanılabilir. Yazı 16 pt. Ya da bandı tamamen kaldır; panel zaten güncelleniyor.
- **Boyut:** S

### P1-8 · Kahraman etiketi 80 pt: "Geri dönüyor · 12 sn" ve uzun kahraman adları kesiliyor — YENİ (#10 düzeltmesinin yan etkisi)
- **Ne:** Kahraman ölünce yeteneğin üstünde "Geri dönü…" görünür, süre okunmaz. Oyuncunun verdiği 20 karakterlik ad da ~12. karakterde "…" ile kesilir.
- **Kanıt:** `DefenderHudResponsiveLayout.cs:280` `Hero` 80×18 pt; `DefenderPresentationAssetBuilder.cs:1507` 12 pt, `UiKit.Text` varsayılanı NoWrap + Ellipsis (`UiKit.cs:55-56`). Metinler: `FrontendCopy.Wave.cs:8` TR 20 karakter ≈ 130 pt, EN "Returning · 12 sec" ≈ 115 pt. Ad sınırı 20 karakter (`FrontendCopy.cs:82`).
- **Durum:** tüm cihazlar, kahraman ölüyken ya da uzun adda.
- **En küçük düzeltme:** Ölüyken etikete yalnız "{0} sn" yaz (dönüş anlamını gri halka zaten veriyor, `UiRadialCooldown.SetReturning`). Etiketi 120 pt yap, ortasını yetenek düğmesine hizala (`x = -heroRight + edge - 20`). Ad için autosize 10–12.
- **Boyut:** S

### P1-9 · Öğretici "Okçu ikonuna dokun" balonu "Sıradaki" şeridinin üstüne biniyor — YENİ
- **Ne:** Öğreticinin 1/6, 2/6 ve 5/6. adımlarında halka açıkken balon üst-ortaya taşınıyor. Üst-ortada hazırlık fazında "Sıradaki: Yağmacı ×50" şeridi duruyor; ikisi ~13–17 pt üst üste biniyor. Balon bu sırada adım değişiminde hedefin yanından üst-ortaya zıplıyor ve boyu 300×64'ten 217×29'a iniyor.
- **Kanıt:** `DefenderHudController.Tutorial.cs:120-124`: `sizeDelta 217×29`, alt kenarı `max.y - 112 pt` → ekran üstünden 83–112 pt (dekor 79–108). Şerit: `WaveForecast.cs:28-31`, TopBar altı + 8 → 64–96 pt; ikisi de yatayda ortalı. Normal moddaki kıskaç `:117-118` (`max.y - 116`) de balonu 52–81 pt'ye, yani dalga paneli ve şeridin bandına itebiliyor.
- **Durum:** Bölüm 1 öğreticisi (ilk izlenim); tüm telefonlar. Kodla hesap.
- **En küçük düzeltme:** `:124`'te `112` → `96 + 8 + 29 = 133` pt (şeridin altı). `:118` üst kıskacını da `max.y - (104 + 64)` yap.
- **Boyut:** S

---

## P2

### P2-10 · Duraklat kartı SE'de 375 pt'nin 346'sını kullanıyor; WP5'in ekleyeceği satırlar sığmaz — YENİ (#13 riski)
- **Ne:** Duraklat kartının yüksekliği: 16 dolgu + 30 başlık + 8 + [8 + 52 + 8 + 44 + 8 + 44 + 8 + 44 + 8 + 44 + 8] + 16 = 346 pt (dış dolguyla 362). SE'de yalnız 13 pt boşluk kalıyor. Plan #13/WP5 hareket/sarsıntı/titreşim satırlarını ekleyince (3 × 52) kart ekrandan taşar.
- **Kanıt:** `DefenderPresentationAssetBuilder.cs:1541-1558`, `UiKit.cs:103-110` (Card padding 16, spacing 8), `UiKit.cs:300-301` (dış dolgu 8).
- **Durum:** SE (iPhone 13'te 390'ın 362'si).
- **En küçük düzeltme:** WP5 spec'ine ekle: ayar satırlarını 2 sütunlu ızgaraya al (müzik | efekt / hareket | sarsıntı) ya da kart 480 pt genişlikte yatay iki panel. Kabul ölçütü SE'de `card.rect.height ≤ 375 - 16`.
- **Boyut:** S (spec notu)

### P2-11 · Üst bar 44 pt'lik öğeler için 56 pt band; geniş moddaki şişkinlikler — YENİ
- **Ne:** Kompaktlık için ölçülebilir küçültmeler. Altın hapı 124/148 pt ama içinde 3–4 haneli sayı var. Hız hapı 76 pt (ikon + "2x"). Faz çipi "HAZIRLIK/SAVAŞTA" 92 pt, oysa faz zaten NextWave görünürlüğü ve şeritten anlaşılıyor. Dalga paneli geniş modda 52 pt, diğer öğeler 44 pt; bu yüzden band 56 pt.
- **Kanıt:** `DefenderHudResponsiveLayout.cs:50,234-255`; `DefenderPresentationAssetBuilder.cs:1439-1459,1475-1478`.
- **Durum:** tüm cihazlar.
- **En küçük düzeltme:** Altın 96, Hız 60, dalga paneli her modda 44 yüksekliğinde (band 50 → `TopContentHeight = 50`), faz çipini kaldır → dalga paneli 200'den ~120 pt'ye iner. Toplam üst sırada ~150 pt yatay ve 6 pt dikey kazanç. P1-4 ile birlikte üst sıra 520'den ~290 pt'ye iner. Not: bilgi kartı ve halka testleri `TopContentHeight`'a bağlı (`ResponsiveLayout.cs:151-160`).
- **Boyut:** M

### P2-12 · Toast giriş animasyonu sırasında yeniden dizilirse 6 pt'ye kadar kayık kalıyor — YENİ
- **Ne:** Mesaj belirirken (0.18 sn) seçim açılır ya da kart boyu değişirse toast son konumuna değil, birkaç pt yukarısına oturur ve bir sonraki yeniden dizilmeye kadar öyle kalır.
- **Kanıt:** `DefenderHudController.BattleChrome.cs:51-59` konumu `localPosition += offset - messageOffset` ile göreli değiştiriyor. `LayoutFrame` (`ResponsiveLayout.cs:273-278`) konumu sıfırlıyor ama `messageOffset` sıfırlanmıyor → kalıcı fark = o anki ofset (≤ 6 pt).
- **Durum:** P1-3 (b/c) tetikleri ile aynı anda gelen mesaj, ör. "Yetersiz altın" + kart değişimi.
- **En küçük düzeltme:** Ofseti mutlak uygula: `LayoutFrame` sonunda `messageOffset = 0` (ya da `ShowMessageChrome` içinde taban konumu sakla ve `base + offset` ata).
- **Boyut:** S

### P2-13 · 11 pt altı yazılar: altın artışı 10 pt, yeni düşman kartı SE'de 9.7 pt — YENİ
- **Kanıt ve durum:**
  - `Gold.Delta` autosize 10–12 pt (`DefenderPresentationAssetBuilder.cs:1452-1454`).
  - Yeni düşman kartında özellik ve taktik metni 28 birim (`AppFlowController.Screens.cs:700-701`): SE'de 9.7 pt, iPhone 13'te 11.1 pt. Taktik metni 120 birimlik sabit kutuda Ellipsis'le kesilebilir (`:702-703`).
  - Dalga bandı 11 pt (P1-7).
- **En küçük düzeltme:** Delta min 11. Düşman kartı metni 32 birim (SE 11.1 pt), taktik kutusu `minHeight` yerine tercih edilen yükseklik + Overflow.
- **Boyut:** S

### P2-14 · Ölü/yarım kalmış çerçeve öğeleri ve UiTokens dışı sabitler — YENİ (#6 kalıntısı)
- **Ne:** "Preview" çipi (eski "×50") her karede gizleniyor ama hâlâ yerleştiriliyor ve halka engel listesinde duruyor. HUD'un kendi `ResultPanel`'i hiç görünmüyor, çünkü sonuçta HUD kapanıyor. Çerçevede birçok sabit ölçü UiTokens dışında.
- **Kanıt:**
  - Preview: `DefenderHudResponsiveLayout.cs:190,256`; `WaveForecast.cs:25,72`; `DefenderHudController.cs:318`.
  - ResultPanel: `DefenderHudController.cs:333-362`, `:932-960`; `AppFlowController.cs:693` `SetBattleHudVisible(false)`. `GameSessionController.cs:1037-1041,2545-2549` eski `resultPanel` literal'leri ("YENİLGİ\nGREENWATCH DÜŞTÜ", plan #28).
  - Sabitler: `DefenderKeepPlate.cs:34-47` (146×48, 11/16 pt), `Tutorial.cs:29-41,109,123` (205/217/300, 12/11 pt), `Motion.cs:24-25`, `ResponsiveLayout.cs:259-270,280` (378/372/48/18/80).
- **En küçük düzeltme:** Preview'i `framePaths` ve `LayoutFrame`'den çıkar. HUD `ResultPanel`/`ShowResult` ölü kodunu sil (WP5 builder regen ile). Sabitleri `UiTokens.Space*`/`Type*`'a bağla.
- **Boyut:** M

### P2-15 · 10+ dalgalı bölümlerde "DALGA 10/12" sıkışık modda kesilebilir; 12 segment nokta gibi görünüyor — YENİ
- **Ne:** Sıkışık modda (SE, iPhone 13) dalga etiketi 86 pt. "DALGA 10/12" 14 pt kalın ≈ 89 pt → "DALGA 10/1…" riski (sınırda, kodla hesap). 12 dalgalı bölümde segmentler 180 pt'ye 8 pt boşlukla bölünüyor → her biri ~7.7×8 pt nokta.
- **Kanıt:** `DefenderHudResponsiveLayout.cs:244,248`; dalga sayısı en fazla 12 (`CampaignCatalog.cs:138-139`); segment boyu `UiKit.cs:252` (tercih edilen 32, aralık 8).
- **En küçük düzeltme:** Etiket genişliği 100 pt'ye sabitlenip faz çipi kaldırılır (P2-11). 8'den fazla dalgada segment aralığı 2 pt.
- **Boyut:** S

### P2-16 · Öğretici 3/6 ve 4/6 balonu kahraman adını ve yetenek düğmesinin üstünü kapatıyor — YENİ
- **Ne:** "İlk dalgayı başlat" ve "Yetenekle sersemlet" adımlarında balon hedefin 20 pt üstüne konuyor ve sağ kenara kıskaçlanıyor. 217 pt'lik balon dekoru sağdan ~89–306 pt, alttan ~90–119 pt aralığına düşüyor. Burası kahraman adı (alttan 104–122) ve yetenek düğmesinin (8–96) üst kısmı. 4/6'da işaret edilen yetenek düğmesinin kendisi balonun altında kalabilir.
- **Kanıt:** `DefenderHudController.Tutorial.cs:110-118` (`+45/+20 pt` ofset, `±150` kıskaç), `:33` (dekor ofseti); alt-sağ küme `ResponsiveLayout.cs:266-281`. Kodla hesap.
- **En küçük düzeltme:** UI hedeflerinde (adım 2/3) balonu hedef rect'in **üst kenarının** 12 pt üstüne koy: `local.y + hedefYükseklik/2 + 12`. Yetenekte bu kahraman etiketinin de üstü (≥ 130 pt) olmalı.
- **Boyut:** S

---

## Plandaki maddelerle ilişki (yeniden yazılmadı)
- PLANDA #4 (SE üst bar / önizleme çakışması): Preview artık hiç görünmüyor; SE'de dalga paneli 233–433 pt, hız 535 pt → çakışma kalmadı (kodla hesap). Kalan iş yalnız P2-14 temizliği.
- PLANDA #1 (mesaj/ipucu ellipsis): toast artık Overflow + çok satır (`BattleChrome.cs:31-32`). Kesilme yok ama yükseklik büyüyünce P1-3'teki orta-ekran sorunu ağırlaşıyor.
- PLANDA #29 (popup sütunu): P1-6 ayrı bir sorun (sıralama ve donma).
- PLANDA #10 → P1-8 · #9 → P0-1 · #13 → P2-10 · #6 → P2-14: plan maddesinin düzeltmesinden doğan yeni kusurlar.

## Doğrulananlar (sorun yok)
- NextWave gizlenince yetenek ve kahraman yer değiştirmiyor (sabit `heroRight`, `ResponsiveLayout.cs:266-281`).
- Boss barı açılınca dalga panelinin içi değişiyor ama panel boyutu ve komşuları sabit (`Frame.cs:173-197`; panelde layout grubu yok, `Builder.cs:1457`).
- "Sıradaki" şeridi `ignoreLayout`; görünüp gizlenmesi başka öğeyi itmiyor (`WaveForecast.cs:27`).
- Duraklat ve harita onayı pt ölçeğinde, iki düğme de ≥ 44 pt. Hız ile duraklat arası 6 pt, hit alanları çakışmıyor (`MobileTouchTarget` yalnız 44'e tamamlıyor).

ALAN DIŞI: Bilgi kartı sol kenardan sabit 96 pt içeride (`ResponsiveLayout.cs:114`); sol 96 pt'lik şerit boş kalıyor. Gerekçesi kodda yok (ALAN B kontrol etsin).
ALAN DIŞI: `GameSessionController.cs:2463-2464` hazırlıkta NextWave etiketini her yenilemede `uiCopy.FirstWave` ile eziyor; `Frame.cs:135` ise `FrontendCopy.HudFirstWave` yazıyor. İki farklı metin kaynağı var (WP2/WP6).
