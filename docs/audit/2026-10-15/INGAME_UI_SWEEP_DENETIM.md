# Oyun içi UI taraması — X1/X2/Y/W denetimi · 2026-10-15

Kapsam: `git diff refs/snapshots/pre-sweep refs/snapshots/post-xyw` (34 dosya). Salt okuma; Unity/test koşulmadı, hepsi kodla hesap.
Satır numaraları `post-xyw` anlık görüntüsüne göre.

**Özet:** 1 P1 · 5 P2. Çökme ya da değişmez ihlali (halka yeniden dizilmesi, zıplama, kahraman yeri, 1x↔2x, kilitsiz kuleler) bulunmadı.
En önemli eksik: bilgi kartı standart iPhone'larda neredeyse her zaman sol-altta kalıyor; "seçimin karşı tarafı" kuralı uygulanmıyor.

## P1

### P1-1 · Bilgi kartı telefonda seçimin karşı tarafına geçmiyor (B P1-3/P1-4 yarım)
- **Yer:** `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs:160-190` (`ChooseInformationCardCorner`), `:141-142` (`CardCornerRect`).
- **Ne yanlış:** Köşeler iki turda deneniyor: önce 8 pt boşlukla, sonra 1 pt ile. İlk turda bir köşe uyarsa ikinci tura hiç geçilmiyor.
  Sağ-alt köşe (corner 1) 8 pt boşlukla "Sıradaki" şeridi engeline çarpıyor: kart üstü 278 pt + 8 = 286 pt, şerit engeli
  `height-96` = 273 pt (750×369) / 279 pt (667×375). Üst köşeler (2/3/5/6) aynı şeride ya da kenara çarpıyor. Köşe 4 (kahramanın solu) 960 pt'den
  dar ekranda toast yuvasına çarpıyor. Sonuç: standart iPhone'larda ilk turda yalnızca **sol-alt** (corner 0) uyuyor. Kart ancak seçilen yuvanın
  dokunma alanı sol-alt kartın dikdörtgenine girerse sağa geçiyor. Spec'teki kural "seçim solda → kart sağ-altta".
- **Senaryo:** iPhone 13 (güvenli alan 750×369 pt), sol-üst bölgedeki yuva (150, 300) pt. Dokunma alanı 122–178 × 272–328, sol-alt kart
  8–268 × 80–228 ile kesişmiyor. Bu yüzden corner 0 seçiliyor ve kart yuvanın hemen altında, aynı tarafta açılıyor. Halka bu kartı engel saydığı için
  alt yarısını kullanamıyor ("halkaya o köşede yer bırakmıyor" bulgusu sürüyor). Pro Max'te (814×409) şerit daha yukarıda kaldığı için sorun yok.
- **Test neden yakalamıyor:** `BattleLayoutStabilityTests.InformationCardUsesClearStableCorner…` (:155) yalnızca "kart yuvayı örtmüyor" diye bakıyor;
  kartın hangi tarafta açıldığını ölçmüyor. Sol-alt test noktası (150,110) zaten kartın içine düştüğü için sağa geçiyor.
- **En küçük düzeltme:** İki turu birleştir. Önce hedefin karşı yarısındaki köşeleri dene (8 pt, olmazsa 1 pt boşlukla), sonra aynı yarıdakileri.
  Ya da puanı `uzaklık` yerine `(karşı taraf mı, boşluk, uzaklık)` sırasıyla karşılaştır. Teste sol-üst bir yuva için
  `card.center.x > safe.center.x` iddiası ekle.

## P2

### P2-1 · Gizli toast yuvası halka tarafından ayrılmıyor; sonradan çıkan toast halka düğmesinin üstüne biniyor
- **Yer:** `DefenderHudController.Radial.cs:679` → `DefenderHudResponsiveLayout.cs:279` (`AppendFrameScreenRects` aktif olmayanı atlıyor).
  Toast artık sabit sol-alt yuvada: `ResponsiveLayout.cs:327-341` (8–368 × 8–72 pt).
- **Ne yanlış:** Kart köşesi seçilirken toast yuvası görünmese de ayrılıyor (`:159-163`). Halka ise yalnızca o an **görünen** `MessageChrome`'u engel sayıyor.
- **Senaryo:** Dalga sırasında sol-alt yuva (150,110) pt seçilir, halkanın alt düğmesi ~y 35 pt'ye yerleşir. Ardından "Kaleye sızma: …" toast'ı
  çıkar (`GameSessionController.cs:2519`). Halka kural gereği yeniden dizilmediği için toast hapı o düğmenin üstüne çizilir. Dokunuş geçer
  (`blocksRaycasts=false`) ama düğme ve fiyatı okunmaz.
- **Düzeltme:** `PositionBattleRadial` engellerine toast'ın dinlenme dikdörtgenini her zaman ekle. Örneğin ResponsiveLayout'a kart rezervindeki gibi bir
  `ToastScreenRect()` ekle ve `radialFrameRects.Add(...)` ile ver.

### P2-2 · Karşılanamayan seçeneğe ikinci dokunuşta "fiyat sallanması" görünmüyor
- **Yer:** `DefenderHudController.Radial.cs:363` (önizlenen seçeneğin fiyat çipi gizleniyor) + `:426-428` (`RejectRadial` tam o çipi sallıyor).
- **Senaryo:** 60 altınla Büyücü (100) önizlenir, çip gizlenir. Aynı düğmeye tekrar dokunulur, `UiRadialMenu.Choose` → `Rejected` → `RejectRadial("Mage")` çalışır.
  Bu da gizli çipte sallanma başlatır. Kullanıcı yalnızca ses ve titreşim alır, görsel ret geri bildirimi kaybolur.
- **Düzeltme:** `RejectRadial` içinde çip aktif değilse `radialAction` (ya da düğmenin kendisi) sallansın.

### P2-3 · Öğretici açıkken her karede bellek ayrılıyor (GC)
- **Yer:** `DefenderHudController.Tutorial.cs:121` (`new[] {...}`) ve `:150-157` (`TutorialBounds` her çağrıda `new Vector3[4]`).
  Çağıran `LateUpdate` (`DefenderHudController.Sales.cs:60`) → her kare.
- **Senaryo:** İlk bölümde öğretici balonu görünürken her kare 5 dizi ayrılıyor (~0,4 KB/kare, 60 fps'te ~24 KB/sn). Bu düzenli GC tetikler.
  Halka tarafında aynı iş için önbellekli `radialCorners` zaten var (`Radial.cs:42`).
- **Düzeltme:** `TutorialBounds`'u statik yapma, `radialCorners`'ı kullan. Üç hedefi dizi yerine ardışık üç çağrıyla işle.

### P2-4 · Kart yükseklik testi her zaman geçiyor (kart taşmasını yakalayamaz)
- **Yer:** `Assets/Tests/PlayMode/BattleInfoCardReadabilityTests.cs:303`. Neden: `DefenderHudResponsiveLayout.cs:209` yüksekliği 120–148'e kırpıyor.
- **Ne yanlış:** `bounds.height <= ReservedInformationCardHeight (148)` iddiası panel dikdörtgenini ölçüyor. Panel zaten en fazla 148 olabildiği için
  test hiçbir zaman kırmızıya dönmez. İçerik yüksekliği (`LayoutCardContent`, `Radial.cs:228-253`) 148'i aşarsa yazı kartın dışına taşar, test yine yeşil kalır.
- **Senaryo:** Bugünkü en uzun içerik ~132 pt; sorun yok. Ama 2 satırlık bir uzmanlık açıklaması + Kalkan Birliği detayı (2 satır) + ipucu =
  22+34+1+40+34+14+4 = 149 pt eder. Kart 148'de kalır, son satır dışarıda çizilir, test geçer.
- **Düzeltme:** `selectionCardHeight`'ı (yansıma ile) ya da `Hint` metninin alt kenarını panel alt kenarıyla karşılaştır.

### P2-5 · Testler ekran boyutunu başsız modda sessizce atlıyor / yazının kendisini ölçmüyor
- **Yer:** `BattleUiPolishTests.cs:131` ve `BattleRadialInteractionTests.cs:434`. Grafiksiz koşuda `SetSize` atlanıyor ama test sürüyor.
  `BottomChromeAndKeepStayInsideSafeAreaAtAllAcceptanceRatios` 4 oranı vaat edip aynı 1280×720'yi 4 kez ölçüyor.
  `PreviewingChoicesKeepsTheRingShapeItOpenedWith` 844×390 yerine varsayılan boyutta koşuyor. Sonuç yeşil görünse de telefon oranı doğrulanmamış oluyor.
  Aynı aileden: `BattleLayoutStabilityTests.cs:189` yuvaları 1280×720 kamerayla izdüşürüp 844×390'a ölçekliyor. Bu, telefondaki gerçek kamera
  kadrajını yansıtmıyor.
- **Yer 2:** `BattleRadialInteractionTests.cs:542-556` `SellCaptionStaysInside…` testi metnin değil çipin dikdörtgenini (sabit 60×18) ölçüyor.
  Bu yüzden köşe kontrolü her zaman geçer. Ayrıca yalnızca 1. seviye Okçu'yu (2 haneli satış değeri) deniyor. Uzmanlaşmış kulede değer 3 haneye çıkıyor
  (yatırım ≥ taban + yükseltme + 160–220, iade %60). "SELL +2xx" 13 pt'de ~60 pt ediyor ve 60 pt çipin sınırında; otomatik boyut kapalı, taşarsa test görmez.
- **Düzeltme:** Grafiksiz modda `Assert.Ignore("… grafik cihazı gerekir")` kullan (`BattleHudFrameTests.cs:266`'daki gibi) ya da BattleLayoutStabilityTests'teki
  `UiMetrics` önbellek yöntemine geç. Satış testinde `caption.GetRenderedValues(true).x <= chip.rect.width` iddiasını kullan ve uzmanlaşmış kule durumu ekle.

## Temiz bulunanlar (paket başına)
- **X1:** Restart onayı pt ölçeğine girdi (`ResponsiveLayout.cs:347`), başlık 20/metin 14, Close gizli. Üst barın compact kararı karttan bağımsız (`:304`).
  Toast sabit yuvada, giriş ofseti mutlak (`:355-362`). Kart rezervi sabit ve raycast kapalı. Kahraman etiketi 120 pt ve ortalı. Preview çipi çıkarıldı. DALGA etiketi 100 pt.
- **X2:** `UiPointSize.IgnoreOwnScale` ile basılan düğme halkayı büyütmüyor. Eylem düğmesi seçim boyunca sabit, 8 pt arama adımı var. Ayak izi hesabı erken çıkıştan sonra yapılıyor.
  Kart yalnız büyüyor, seçimde sıfırlanıyor. Fiyat tekrarı kalktı. Menzil önizlemesi aynı karede değişiyor (`RefreshRadialSelectionPreview`). Rol rengi sıfırlanıyor.
  Sabitler tek yerde. `radialRingDirty` değişmezi korunuyor.
- **Y:** `IntroModalOpen` pause menüsünü bastırıyor ve yalnız kendi pause'unu geri açıyor (hız `OnPausePressed` ile geri geliyor). Popup canvas'ı HUD altında,
  Paused'da kapalı. Dalga bandı şerit yuvasında, 16 pt ve a11y etiketli. NextWave uzmanlaşmada görünür ama basılamaz. Kahraman sayacı kısa metinli, 10–12 autosize.
  Düşman kartı metinleri 32 birim ve Overflow.
- **W:** Muhafız ve kahraman can barı yalnızca hasarda görünüyor, ölüm/doğuşta gizleniyor (bütün `health` yazım yolları `Refresh` çağırıyor; iyileşme yolu yok).
  Kalkan ikonu barın çocuğu ve barla birlikte gizleniyor. Yuva rengi yalnız değişince yazılıyor.

Kapsam dışı not: dost birlik/kahraman başına ayrı world-space Canvas ve her birinde `WorldSpaceBillboard` var. Spec bunu kabul etti; düşman başına Canvas perf notu hâlâ açık.
