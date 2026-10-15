# Savaş ekranı UI taraması — ALAN B (dünya etkileşimi) · 2026-10-15

**Özet.** Halka şekil düzeltmesi (`radialRingDirty`) yalnız seçim değişince korunuyor; dalga sırasında her düşman vuruşunda kamera sarsıldığı için halka saniyede 10 kez yeniden diziliyor, yani düzeltme savaşta fiilen devre dışı (P1-1). Bilgi kartı ekranın sol-altına sabit; seçilen yuvanın konumuna bakmıyor, 1. bölümün ilk yuvasını örtüyor, altındaki yuvalara dokunmayı engelliyor ve halkaya 260×200 pt'lik görünmez bir yasak alan bırakıyor (P1-3, P1-4). Pro Max'te kartın açılıp kapanması üst barı yeniden diziyor, mesaj kutusu ise karttan bağımsız olması gerekirken kartın boyuna göre zıplıyor (P1-2, P2-1).

**Sayılar:** P0 0 · P1 4 · P2 12 · **YENİ 16** (PLANDA olan yeniden yazılmadı).
**Kanıt sınırı:** Kod okundu. Unity, simülatör ve cihaz koşulmadı. Görüntü olarak yalnız `Builds/HudFrame/preparation-844x390.png` (15 Eki 12:38, safe-area boşluğu yok) ve `Builds/step6-after-chapter1-wave-1560x720.png` (12:19) kullanıldı. İkisi de halka düzeltmesinden önceye ait olabilir, bu yüzden yalnız yuva konumu ve kart görünümü için kullanıldı. "Kodla hesap" yazan ölçüler görüntüyle doğrulanmadı.
**Cihaz güvenli alanları (yatay):** SE 667×375 (boşluk yok) · iPhone 13 844×390 → 750×369 (sol/sağ 47, alt 21) · Pro Max 932×430 → 814×409 (sol/sağ 59, alt 21) · iPad 1024×768.

## Kompaktlık tablosu — 844×390 (329.160 pt²), kodla hesap

| Öğe | Boyut (pt) | Ekran payı | Kaynak |
|---|---|---|---|
| Bilgi kartı (kule seçili, önizleme yok) | 260×120 | %9,5 (yüksekliğin %31'i) | `Radial.cs:242`, `DefenderHudResponsiveLayout.cs:115` |
| Bilgi kartı (inşa önizleme) | 260×~129 | %10,2 | `Radial.cs:218-243` |
| Bilgi kartı (dal önizleme + "Savaş duraklatıldı" 2 satır) | 260×~182 | %14,4 (yüksekliğin %47'si) | `Radial.cs:507-510` |
| Halkaya yasak kart rezervi (kart kapalıyken de) | 260×200 | %15,8 | `Radial.cs:52,:622` |
| Halka, 3 seçenek (60 pt düğme, yarıçap 60) | ~164×120 | %6 | `Radial.cs:65,:126`; `UiRadialLayout.cs:122` |
| KUR/YÜKSELT/SEÇ düğmesi | 164×52 | %2,6 | `Radial.cs:573` |
| **İnşa önizlemesinde toplam** (kart + halka + düğme) | — | **~%19** | |
| SE 667×375'te dal önizleme kartı / rezerv | 260×182 / 260×200 | %18,9 / %20,8 | |

Fiyat aynı anda üç yerde yazıyor: halka çipi "70", eylem düğmesi "KUR · 70", kart ipucu "70 ALTIN · KUR'a dokun" (`Radial.cs:312,:362,:505`). Kart ipucunu eylem düğmesi görünürken kaldırmak kartı 14–28 pt kısaltır (P2-2).

---

## P1

### P1-1 · Dalga sırasında halka saniyede 10 kez yeniden diziliyor; şekil düzeltmesi savaşta çalışmıyor · YENİ · S
- **Oyuncu ne yaşıyor:** Dalga sürerken halka açıkken düşmanlar vuruldukça halka ve KUR/YÜKSELT düğmesi titriyor. Engellere yakın yuvalarda halka tam daire, yarım yay ve yörünge arasında şekil değiştirebiliyor. Bu, sabah düzeltilen hatanın akrabası.
- **Kanıt:** Her düşman vuruşu kamerayı sarsıyor (`GreyboxCombatants.cs:252` → `DefenderPresentationFeedbackController.cs:70-77`; EnemyHit 0,025 birim/0,08 sn, boss 0,16 birim, `PresentationFeedbackContract.cs:49-56`). HUD her 0,1 sn'de bir yenileniyor: `GameSessionController.cs:423-430` → `:2465` → `:2284-2292` (`SetSelected` + `SetSelectionAnchor`) → `PositionBattleRadial` iki kez (`Radial.cs:525`, `DefenderHudController.cs:330`). `Radial.cs:644` `moved = placedCenter != center` tam eşitlikle karşılaştırıyor; 0,01 px kayma bile `:647-655`'te `SetObstacles` + `Place` + yeniden dizilim + `PositionRadialAction` ızgara aramasını tetikliyor. O anda açık olan toast da yeni engel olarak giriyor.
- **Ne zaman:** Tüm cihazlarda, dalga fazında, kamera sarsıntısı açıkken. Sarsıntı varsayılan olarak açık; sistemde azaltılmış hareket açıksa kapalı (`FrontendPreferences.cs:44`).
- **En küçük düzeltme:** `moved` için eşik koy (merkez ≥ 6 pt kaymadıkça yeniden dizme), ya da merkez kayınca halkayı yalnız ötele (`rect.anchoredPosition += delta`, `Place` çağırma). Merkezi sarsıntısız kamera pozundan hesaplamak da bir seçenek.

### P1-2 · Pro Max'te yuvaya/kuleye dokununca üst bar yeniden diziliyor · YENİ · S
- **Oyuncu ne yaşıyor:** Kuleyi seçince ya da önizleme açılınca altın kutusu 148→124 pt daralıyor. Dalga paneli 220×52 → 200×44 oluyor ve 10 pt sağa kayıyor; ilerleme çubuğu 200→180 pt daralıp 4 pt aşağı iniyor. Seçim kapanınca hepsi geri dönüyor.
- **Kanıt:** `DefenderHudResponsiveLayout.cs:233` `compact = width < 760 || selectedPanel.activeSelf && width < 900`; etkisi `:238,:240-243,:248,:253`. Tetik `Radial.cs:528-533` (`SetInformationCardVisible` → `RefreshLayout`).
- **Ne zaman:** Güvenli genişliği 760–900 pt arası olan cihazlarda: Pro Max 814, 13 Pro Max/14 Plus 832. SE (667) ve iPhone 13 (750) hep kompakt, iPad (1024) hiç kompakt değil, bu yüzden onlarda zıplama olmuyor.
- **En küçük düzeltme:** Kompakt kararını karttan ayır: `compact = width < 900`. Bu durumda Pro Max her zaman kompakt düzende açılır.

### P1-3 · Bilgi kartı seçili yuvanın/kulenin üstüne biniyor ve altındaki yuvalara dokunmayı engelliyor · YENİ · M
- **Oyuncu ne yaşıyor:** 1. bölümde öne çıkarılmış ilk yuvaya (844×390 görüntüde ~(272,245) px) dokunup Okçu önizleyince kartın üst kenarı (~y 253) yuvanın altına değiyor. Dal önizlemesinde kart 182 pt'ye çıkınca yuva ve kule tabanı tamamen örtülüyor, menzil çemberinin sol-alt yarısı da kartın altında kalıyor. Kart görünürken altında kalan yuvaya, örneğin (357,255)'teki komşu yuvaya, dokunmak hiçbir şey yapmıyor.
- **Kanıt:** Kart her zaman güvenli alanın sol-alt köşesinde, sol 96 / alt 8 pt'de (`DefenderHudResponsiveLayout.cs:112-115`). `SetContextualSelectionFromScreenPoint` hedef noktayı kaydediyor (`:119-132`) ama `contextualTarget` hiçbir yerde okunmuyor. Kart yüzeyi ışın yakalıyor (`UiKit.cs:106,:121` `raycastTarget = true`), dünya dokunuşu UI üstündeyse iptal ediliyor (`GameSessionController.cs:2205-2208`). Yuva konumu: `Builds/HudFrame/preparation-844x390.png`.
- **Ne zaman:** Bütün telefonlarda, sol-alt çeyrekteki yuvalarda. Oran en kötü SE'de: kart genişliğin %39'u, yüksekliğin %49'u.
- **En küçük düzeltme:** Kartın yanını seçime göre belirle: `contextualTarget.x` güvenli merkezin solundaysa kartı sağ-alta, hero/NextWave sütununun soluna yerleştir. Kart yüzeyinde `raycastTarget = false` yap ya da kart altına düşen dokunuşu dünyaya ilet.

### P1-4 · Halka sol-alttaki 260×200 pt alana hiç giremiyor; o bölgedeki yuvalarda halka yuvadan kopuyor · YENİ · S–M
- **Oyuncu ne yaşıyor:** Kart rezervinin içindeki yuvada, örneğin 1. bölümün ilk yuvasında (taban 145 pt, x 272), seçenekler yuvanın çevresine dizilemiyor; yuvadan uzakta, üstte ya da sağda yarım yay veya yörünge olarak çıkıyor. Kart gizliyken bile aynı durum oluyor.
- **Kanıt:** `Radial.cs:52` `ReservedInformationCardHeight = 200`, `:621-622` her zaman engel listesinde; `DefenderHudResponsiveLayout.cs:219-226`. Kartın gerçek en büyük boyu ~182 pt (kodla hesap: başlık 32 + rol 17 + 6 + karo 52 + detay 33 + 6 + ipucu 28 + 8). SE'de sol sütunda halkaya kalan dikey bant: 375 − 58 (üst pill + pay) − 216 (rezerv + pay) ≈ **101 pt**. 3 seçenekli tam halka ~120–180 pt istiyor, sığmıyor.
- **Ne zaman:** SE'de sol %39'luk sütunun tamamında; 844×390'da sol-alt çeyrekte.
- **En küçük düzeltme:** P1-3 ile birlikte çöz. Kart yuvanın karşı tarafına yerleşirse rezerv yalnız o tarafta uygulanır. Kısa yol: rezervi 200 yerine ölçülen en büyük boy (~185) yap.

## P2

### P2-1 · Kart açılınca/boyu değişince mesaj (toast) yer değiştiriyor · YENİ · S
- **Ne:** Seçim yokken toast alt-ortada ve 32 pt yüksekte. Seçim açılınca sağa geçiyor (`messageAnchor`), yüksekliği kart boyuna bağlanıyor: `max(122, 8+kartBoyu)+16` → 136–206 pt. SE'de genişliği de 378 → 295 pt'ye düşüyor, satır kırılımı değişiyor. Her önizleme değişiminde kart boyu değiştiği için toast da aşağı yukarı kayıyor.
- **Kanıt:** `DefenderHudResponsiveLayout.cs:258-277` (özellikle `:259,:263,:270`); tetik `Radial.cs:242` → `:139-144`.
- **Ne zaman:** Tüm telefonlar. Kart solda (96–356), toast sağda (≥364) olduğu için yatayda çakışmıyorlar; kart boyuna göre yükseltmek gereksiz.
- **Düzeltme:** Seçim varken de yükseklik yalnız hero kuralına bağlı olsun (`heroBottom + 18 + 2·edge`). Kart boyu terimini kaldır.

### P2-2 · Kartın boyu seçeneğe göre 120↔182 pt değişiyor ve üst kenarı zıplıyor; fiyat üç yerde tekrar ediyor · YENİ · S
- **Ne:** Kart alttan sabit ve yukarı büyüyor. Kule seçili (120) → Yükselt önizleme (~129) → dal önizleme (~182) geçişlerinde üst kenar 62 pt'ye kadar zıplıyor. İpucu satırı ("70 ALTIN · KUR'a dokun", "Savaş duraklatıldı · SEÇ'e dokun") eylem düğmesindeki "KUR · 70"yi tekrar ediyor.
- **Kanıt:** `Radial.cs:218-243`, `:505-510`, `:362`; `DefenderHudResponsiveLayout.cs:113` (pivot alt).
- **Düzeltme:** Eylem düğmesi görünürken ipucunda yalnız eksik altın ya da "Savaş duraklatıldı" kalsın. Seçim boyunca kart yüksekliği yalnız büyüsün, küçülmesin; seçim değişince sıfırlansın.

### P2-3 · Halka düğmesine basılınca (Pressed 0,975) halkanın çapı büyüyor · YENİ · S
- **Ne:** Basılan düğme 0,975'e küçülünce `UiPointSize` kendi ölçeğine bölüyor: minWidth 60 → 61,5. Halka çapı en büyük çocuğa göre hesaplandığı için tüm düğmeler 61,5 pt'ye çıkıyor ve yarıçap büyüyor; bırakınca geri dönüyor. Engele 1,5 pt yakın yerleşimlerde `Fits` sonucu değişip şekil sıçrayabilir.
- **Kanıt:** `UiPointSize.cs:12` (her karede `Refresh`), `:18-21` döngü düğmenin kendi `localScale`'ini de sayıyor; `UiVisualState.cs:87`; `UiTokens.cs:75` `PressScale = .975f`; `UiRadialLayout.cs:36-39,:72-73`.
- **Düzeltme:** `UiPointSize.Refresh` döngüsü `transform.parent`'tan başlasın. Kendi ölçeği animasyondur, yerleşim girdisi olmamalı.

### P2-4 · KUR/YÜKSELT/SEÇ düğmesi her seçenekte farklı yere gidiyor; etiketi 1↔2 satır arasında atlıyor · YENİ · M (metin kısmı S)
- **Ne:** Eylem düğmesi önizlenen seçenekten 112 pt dışarıya en yakın boş noktaya yerleşiyor. Okçu → Büyücü geçişinde düğme ekranın başka köşesine atlıyor (`Radial.cs:50` yorumu bunu bilinçli kabul ediyor). Ayrıca altın azaldığında "YÜKSELT · 150" yerine "YÜKSELT · 45 eksik" yazıyor. 14 pt'de ~150 pt metin + altın ikonu 164 pt'lik düğmeye sığmadığı için (kodla hesap) etiket ikinci satıra kırılıyor.
- **Kanıt:** `Radial.cs:568-599` (`:583` `outward * 112`, `:573` 164×52), `:362`, `:82-85` (Normal kırılım + Overflow).
- **Düzeltme:** Düğmeye halkaya göre tek, sabit bir yer ver (halka merkezinin altı/üstü, engel varsa karşı taraf). Eksik metnini kısalt ("KUR · −45"), böylece tek satırda kalır.

### P2-5 · Uzmanlaşma seçerken (savaş duraklı) "Sonraki dalga" düğmesi kayboluyor · YENİ · S
- **Ne:** Hazırlık ya da ara fazında dal önizlemesine dokununca `BeginSpecializationModal` oyunu duraklatıyor; sağ-alttaki 208×54'lük NextWave düğmesi kayboluyor, seçim bitince geri geliyor. Geri sayım halkası da onunla birlikte gidip geliyor.
- **Kanıt:** `DefenderHudController.Frame.cs:120-122` `canCall = session.Phase is Preparing or Intermission` (`UnpausedPhase` değil); `GameSessionController.Modals.cs:12-13`.
- **Düzeltme:** `canCall` için `UnpausedPhase` kullan, duraklıyken yalnız `interactable = false` yap.

### P2-6 · Menzil çemberi önizlemede ≤0,1 sn çift görünüyor, onayda ≤0,1 sn kayboluyor; iki farklı çizgi stili var · YENİ · S
- **Ne:** Önizlemeye dokununca altın `radialRange` hemen çiziliyor, mevcut türe özgü çember (okçu altın, büyücü mor, kışla toplanma çizgisi) ise bir sonraki HUD yenilemesine kadar (≤100 ms) duruyor. Onaylayınca tersi oluyor, kısa bir süre çember hiç görünmüyor. Kalınlık 0,10 ve 0,085, renkler de farklı; aynı bilgi iki ayrı görünümle veriliyor.
- **Kanıt:** `Radial.cs:520-524`, `:430-432`; `GameSessionController.cs:2322-2330`, `:2389`, `:2372`; `CombatRangeVisual.cs:18-19`.
- **Düzeltme:** `PreviewRadial`/`ConfirmRadial` sonunda oturumun `RefreshSelectionPreview(force: true)` çağrısını tetikle (olay ile). Uzun vadede tek görsel kullan.

### P2-7 · "SAT/ONAYLA" etiketi halka çakışma hesabına girmiyor · YENİ · S
- **Ne:** Etiket Sat düğmesinin 20 pt altında (72×20). `Fits` yalnız çap karesine bakıyor ve güvenli alan payı yarım çap kadar. Sat düğmesi alt kenara ya da karta yaslandığında etiket güvenli alanın dışına (home çubuğu) ya da kart/NextWave üstüne taşabiliyor (kodla hesap).
- **Kanıt:** `Radial.cs:164-166`; `UiRadialLayout.cs:108-113,:120`. Eylem düğmesi yerleşimi etiketi hesaba katıyor (`Radial.cs:593` 74×90), halka katmıyor.
- **Düzeltme:** Etiketi düğmenin içine, fiyat çipinin yerine al ("SAT +45"). Ya da `Fits` alt kenarına 20 pt ekle.

### P2-8 · Kalkan ikonu ~7×9 pt; can barı gizliyken tek başına havada duruyor · YENİ · S
- **Ne:** Öncü düşmandaki zırh ikonu 24×30 tuval × 0,012 = 0,29×0,36 dünya birimi. 844×390 görüntüde 1 birim ≈ 24 pt, yani ikon ~7×9 pt ve telefonda seçilemiyor. İkon `healthRoot` altında değil; düşman vurulana kadar bar gizli olduğu için kalkan tek başına süzülüyor.
- **Kanıt:** `ShieldStatusIcon.cs:11-16`; `GreyboxCombatants.cs:296-299` (ölçek 0,012), `:340`, `:399`.
- **Düzeltme:** İkonu `healthRoot`'un çocuğu yap, ya da barla birlikte göster/gizle. Ekranda ≥12 pt olacak şekilde boyutu 40×48 tuval birimine çıkar.

### P2-9 · Muhafızların ve kahramanın dünyada can barı yok · YENİ · M
- **Ne:** Kışla/Kalkan Birliği'nin satış argümanı "Birlik canı 300→420", ama savaşta muhafızın ya da kahramanın ne kadar canı kaldığı hiçbir yerde görünmüyor. Kahraman yüzdesi yalnız eski (artHud yok) yolda yazılıyor.
- **Kanıt:** Can barını yalnız `GreyboxEnemy` oluşturuyor (`GreyboxCombatants.cs:283`). `GreyboxFriendlyUnit` (`:657-820`) ve `GreyboxHero` (`:821-`) çizmiyor. Kahraman %: `GameSessionController.cs:2488` (yalnız eski HUD).
- **Düzeltme:** Düşman barını (110×17) yeniden kullan; mavi/altın dolguyla yalnız hasar alınca göster.

### P2-10 · Kart rol yazısının rengi geçmişe bağlı · YENİ · S
- **Ne:** Bir kez boş yuva seçildiyse rol satırı `TextMuted` kalıyor; hiç seçilmediyse `TextSecondary`. Aynı kule kartı oturuma göre iki farklı tonda görünüyor.
- **Kanıt:** `DefenderHudController.cs:995,:1007` (TextMuted); `Radial.cs:254` (ilk TextSecondary). `ApplySelectedOutpostView` (`DefenderHudController.cs:974-982`) ve `RefreshRadialCard` (`Radial.cs:469-471`) rengi sıfırlamıyor.
- **Düzeltme:** `RefreshRadialCard` içinde `selectedRoleText.color = UiTokens.TextSecondary`.

### P2-11 · Halka ölçüleri satır içi ve birbirine bağlı sabitler · YENİ · S
- **Ne:** 60 pt düğme üç yerde ayrı yazılmış (`Radial.cs:65,:126,:311`). Eylem düğmesi çakışma kutusu 74×90 / 37,53 elle 60 pt düğme + 20 pt etiket varsayıyor (`:593`). 52 (`:79`, `:573`), 164, 112, 48, 22, 18 sabitleri `UiMetrics`/`UiTokens` dışında. Biri değişirse eylem düğmesi halkayla çakışır. (Kart karo sabitleri WAVE1_REVIEW P2-7'de; bu madde halka tarafı.)
- **Düzeltme:** `const float RadialButtonPt = 60, RadialLabelPt = 20`; çakışma kutusunu bunlardan türet; 52 → `UiMetrics.PrimaryCtaPt`.

### P2-12 · Halka her 0,1 sn'de pahalı işler yapıyor; yuvalar her karede renk yazıyor · YENİ · S
- **Ne:** `PositionBattleRadial` her çağrıda erken çıkmadan önce kule/yuva renderer'larını topluyor ve 8 köşe izdüşümü yapıyor (`Radial.cs:536-566`). HUD yenilemesi başına iki kez çağrılıyor. Merkez kayınca (P1-1) eylem düğmesi için 4 pt'lik ızgarada arama yapıyor: 844'te ~12 bin, iPad'de ~38 bin aday × ~20 engel (`:578-596`). Ayrıca her `DefenderSlotVisual` her karede `SetPropertyBlock` çağırıyor (`DefenderSlotVisual.cs:74-86`).
- **Düzeltme:** Ayak izi hesabını `dirty || moved` kontrolünden sonraya al. Izgara adımını 8 pt yap. Yuva rengini yalnız altın/faz/seçim değişince yaz.

---

## PLANDA (yeniden yazılmadı)
- Kart/halka yazı boyutları ve kart genişliği → **#1** (WP1, uygulandı; yukarıdaki kompaktlık bulguları bunun yan etkisi).
- Kilit açıklaması / rozet boyutu → **#3** (rozet 22 pt, kart "N. bölümde açılır" yazıyor; `Radial.cs:180,:511-516`).
- Altın popup'larının halkayı örtmesi → **#29**.
- Yuvaların VoiceOver ile oynanması → **#16** (kapsam dışı).
- SE üst bar sıkışıklığı → **#4**.

## ALAN DIŞI
- ALAN DIŞI: WAVE1_REVIEW P2-10 "WaveForecast tam genişlik" iddiası güncel değil; şerit artık merkez anchor'lı (`DefenderHudController.WaveForecast.cs:27-31`). Engel bandı yalnız şerit genişliği kadar.
- ALAN DIŞI: Her düşman kendi world-space `Canvas`'ını açıyor (`GreyboxCombatants.cs:287-296`); 60 düşmanda 60 canvas oluyor (performans).
