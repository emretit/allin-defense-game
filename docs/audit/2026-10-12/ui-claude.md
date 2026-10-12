# ALLIN: Defense — UI/UX denetimi ve yeniden tasarım planı (Claude, 2026-10-12)

## Özet (5 satır)
1. Tasarım sistemi (Faz 1) kâğıt üstünde hazır ama hiçbir ekran onu kullanmıyor: 9 yeni sprite'ın (yuvarlak köşe, halka, altın gradient) çalışma zamanında **0** tüketicisi var; ekranlar hâlâ eski kesik köşeli `buttonSprite` ile çiziliyor.
2. Görünen kalite sorunlarının çoğu 1 saatlik işler: 7 metin taşmasının yalnız **2 kök sebebi** var, harita bölge adı yanlış büyük harfle ("YEŞIL") ve dil değişince güncellenmiyor, ayarlarda Türkçe seçiliyken dil düğmesi seçili görünmüyor, ana menünün üstünde düz siyah bir şerit var.
3. En büyük UX kararı: menü portre / savaş yatay ayrımı her bölümde telefonu 2 kez döndürtüyor. Referans TD oyunlarının hepsi tek yön kullanıyor. Önerim **her yerde yatay**; harita görseli zaten yatay (1672×941).
4. Savaş HUD'u için onaylı A/C mockup'ları doğru yön. Tek değişiklik önerim: seçili kule kartı yerine Kingdom Rush tarzı **kule çevresi halka menü**. Böylece inşa ve yükseltme aynı bileşeni kullanır ve harita kapanmaz.
5. Yol haritası: önce 5 hızlı kazanım (~4 sa), sonra bileşen kiti → HUD → menüler → savaş alanı → cila. Toplam **~52–64 saat** (fazlar aşağıda, kabul kapılarıyla).

> Kanıt notu: Ekran görüntülerindeki altın düğmelerde metin açık renk. Kod ise koyu `GoldText` seçiyor (`AppFlowController.cs:1208`, `RelativeLuminance(Gold)≈0,54 > 0,4`). Yani görüntüler Faz 1 öncesi bir build'den olabilir. **Doğrulanamadı.** Bu yüzden renk/kontrast bulgularını koddan, yerleşim bulgularını görüntüden çıkardım.

---

## 1. Ekran ekran eleştiri

Kısaltmalar: **İ** = iyi, **K** = kötü, **R** = referans oyunlar, **Ö** = öneri. "pt" = iOS nokta (iPhone 17 Pro'da 1 pt = 3 px).

### 1.1 Boot / yükleme (görüntü yok; koddan)
- **İ:** Logo 0,54 sn'de 0,92→1 ölçekle açılıyor, azaltılmış hareket destekleniyor (`AppFlowController.cs:305-330`). Yön perdesinde yeniden deneme düğmesi var (`:700-723`).
- **K:** Yön perdesindeki "SAVAŞ ALANI HAZIRLANIYOR / CİHAZ YATAYA ALINIYOR" metni (`GameLocalization.cs`) sürtünmeyi oyuncuya açıkça gösteriyor. `FrontendLoadingOverlay` başka bir ölçek kullanıyor (`matchWidthOrHeight=0.5`, `FrontendLoadingOverlay.cs:35`); ana canvas ise `0` kullanıyor (`AppFlowController.cs:238`). Bu yüzden logo boyutu geçişte zıplayabilir (doğrulanamadı).
- **R:** Kingdom Rush ve BTD6'da logo, ilerleme çubuğu ve tek ipucu satırı bulunur, başka bir şey yoktur.
- **Ö:** Logo + ince altın ilerleme çubuğu + dönen "ipucu" satırı (oynanış ipuçlarından). Yön değişimi kalkarsa (bkz. §2) perde metni de kalkar.

### 1.2 Hoş geldin (yeni oyuncu; görüntü yok)
- **İ:** Yeni misafir oyuncu tek dokunuşla 1. bölüme giriyor (`AppFlowController.cs:161`, `Screens.cs:261-262`). Sürtünme yok, bu iyi.
- **K:** "Hesap bağla" düğmesi kurulup gizleniyor (`Screens.cs:266`). Ölü kod.
- **Ö:** Aynen koru. Ölü düğmeyi sil.

### 1.3 Ana menü (`main-menu.png`)
- **İ:** Boyalı kale illüstrasyonu güçlü. Tek kart ve altın CTA hiyerarşisi doğru yönde. Bölüm bilgisi (3 dalga · ~2,5 dk · 230 altın) faydalı.
- **K:**
  1. Üstteki %18'lik alanda düz %72 koyu dikdörtgen var. İllüstrasyonla arasında sert bir dikiş oluşuyor (görüntüde y≈360). Kaynak: `UiTokens.TopShade` düz renk + `AppFlowController.cs:249-250`. Gradient sprite'ı (`VerticalGradient`) zaten üretilmiş ama kullanılmıyor.
  2. Profil çipinde teknik metin var: "Kaydedildi · Eşitleme bekliyor" (`AppFlowController.cs:607`). Oyuncuya değer katmıyor.
  3. "AYAR" kısaltması ikon yerine kesik metin (`FrontendCopy.cs:43`).
  4. İki büyük CTA var. İlk savunmada ikisi de haritayı açıyor (`ContinueCampaign → IsFirstDefence → OpenCampaign`, `AppFlowController.cs:410-417`). Aynı işi yapan iki düğme oyuncunun kafasını karıştırır.
  5. Kartta yıldız grafiği ve bölge ilerlemesi yok. "0/3 YILDIZ" düz metin.
  6. Hoş geldin görseli 853×1844 px ve 1206 px genişliğe 1,41× büyütülüyor. Bu yüzden hafif bulanık (`Resources/Presentation/Frontend/GreenwatchWelcome.png`).
- **R:** Kingdom Rush ve BTD6 ana menüsünde tek büyük "Oyna" ve köşelerde ikon düğmeler (ayar, başarımlar) bulunur. Clash Royale'de alt sekme çubuğu ve ortada tek savaş düğmesi var. Teknik durum hiçbirinde görünmez; yalnız hata olduğunda ikon çıkar.
- **Ö:** Mockup B'nin içeriği doğru. Yön değişirse yatay yerleşim:
```
┌──────────────────────────────────────────────────────────────────┐
│ (◉ Misafir Muhafız ★ 0/30 ✓)                              (⚙)   │
│ ┌──────────────────┐  BÖLGE 1 · YEŞİL NÖBET · BÖLÜM 1    ☆ ☆ ☆  │
│ │  boyalı kale     │  İLK NÖBET                                  │
│ │  (portre görsel, │  Yağmacılar vadiye iniyor. Yolu tut.        │
│ │   ~1:1 ölçek)    │  (🌊 3 dalga) (⏱ ~2,5 dk) (● 230)           │
│ │  + ALLIN logo    │  ▓░░░░░░░░░  Bölge 0/10                     │
│ │                  │  [ ▶  SAVUNMAYA BAŞLA              ]  altın │
│ └──────────────────┘  [ 🗺 HARİTA ]   [ 🛡 KAHRAMAN ]     cam    │
└──────────────────────────────────────────────────────────────────┘
```
Portre görsel yatay ekranda sol panele yaklaşık 1:1 sığar ve büyütme bulanıklığı biter. Senkron durumu yalnız hata olduğunda küçük bir ikonla gösterilir.

### 1.4 Krallık haritası + brifing (`map-Turkish.png`, `map-English.png`, `returned-map.png`)
- **İ:** Boyalı savaş masası arka planı güzel. Mini rota önizlemesi (`FrontendTacticalMapPreview`) gerçekten faydalı. Bölüm detay satırı net.
- **K:**
  1. Bölge sekmesi tam genişlikte, altın renkli ve üstünde yalnız "1" yazıyor (`Screens.cs:67-72`). Ana CTA gibi görünüyor ama bir işe yaramıyor (tek bölge var).
  2. Bölge adı `ToUpperInvariant()` ile büyütülüyor, Türkçede "YEŞIL" çıkıyor (görüntüde noktasız I). Kaynak `Screens.cs:200`. Ayrıca ad yalnız kurulumda yazılıyor. Bu yüzden İngilizceye geçince "YEŞİL NÖBET" kalıyor (`map-English.png`), yeniden kurulunca "GREENWATCH" görünüyor (`returned-map.png`). Aynı `ToUpperInvariant` hatası `AppFlowController.cs:616/619/658` satırlarında da var.
  3. Seçili düğüm "> 1 <" ASCII süsü (`AppFlowController.cs:541`). Kilitli düğümler "× KİLİTLİ" tekrarı; yıldızlar "1/3 YILDIZ" metni (`:546-550`).
  4. Brifing kartı ekranın alt %30'unu kaplıyor ve son düğümleri kesiyor. HEDEF metni CTA'nın içine taşıyor (TR ve EN). EN'de "Raider" ile "OBJECTIVE" üst üste biniyor.
  5. Üstte ~150 pt boş koyu bant var. Geri düğmesi 140×140 birim ama içindeki "‹" glifi minik.
- **R:** Kingdom Rush'ta harita tam ekrandır. Düğüm bayrak/madalyondur ve altında 3 yıldız durur. Bölüme dokununca ortada bir brifing modalı açılır (düşman ikonları, zorluk modları, Başla). Arknights'ta bölüm düğmesi kod + yıldız taşır, detay sağ panelde açılır.
- **Ö:** Harita yatay ve tam ekran olsun. Düğümler madalyon şeklinde (FilledCircle + CooldownRing kenar), altlarında 3 yıldız sprite'ı. Kilitliler gri ve kilit ikonlu, etiket yok. Brifing sağda sabit panel (yatayda) ya da alttan açılan sayfa (portrede). Düşmanlar metin değil **ikon çipi** olsun. Hedef tek satır, en fazla 40 karakter. Bölge sekmesi 2. bölge gelene kadar gizli kalsın.
```
┌──────────────────────────────────────────────────────────────────┐
│ (‹)  KRALLIK HARİTASI                                 ★ 0/30     │
│ ┌──────────── yatay savaş masası ───────────────┐┌── 1. İLK NÖBET ┐│
│ │  (①)★★☆ ── (②) ── (③🔒)                        ││ 3 dalga·2,5 dk  ││
│ │                 ╲                             ││ [mini rota]     ││
│ │  (⑥🔒) ── (⑤🔒) ── (④🔒)                        ││ Düşman: 🗡 🛡    ││
│ │     ╲                                         ││ Hedef: Okçu kur ││
│ │  (⑦🔒) ── (⑧🔒) ── (⑨🔒) ── (👑⑩)                ││[ ▶ BAŞLAT ]     ││
│ └───────────────────────────────────────────────┘└─────────────────┘│
└──────────────────────────────────────────────────────────────────┘
```

### 1.5 Ayarlar (`settings-Turkish.png`, `settings-English.png`)
- **İ:** Azaltılmış hareket, kamera sarsıntısı ve titreşim seçenekleri var; erişilebilirlik için iyi. Satırlar büyük ve dokunması kolay.
- **K:**
  1. Türkçe seçiliyken iki dil düğmesi de mavi; seçili olan görünmüyor. Sebep: vurgu yalnız `HandleLanguageChanged`'de uygulanıyor, kurulumda (`Screens.cs:337-340`) ve `OpenSettings`'te (`AppFlowController.cs:868-879`) uygulanmıyor.
  2. Slider'da dolgu (fillRect) yok. %100 ses boş siyah çizgi gibi görünüyor. Tutamak sağ kenarda paneli taşıyor (`Screens.cs:375-386`).
  3. Toggle kare kutu; mobil beklentisi anahtar (switch).
  4. "KAHRAMANIM" (isim girişi) ayarların içinde, yeri yanlış. Kart ile "TAMAM" arasında ~330 pt boşluk var.
  5. Açılış animasyonu yok. Ekranı `alpha=0` ile gizleyip modalı anında gösteriyor (`:875-876`).
- **R:** BTD6 ve Clash Royale'de ayarlar gruplu kartlardır (Ses / Oyun / Hesap), yanlarında switch'ler ve segment kontrol bulunur. Dil ayrı bir satırda seçilir.
- **Ö:** Gruplu kartlar: **Ses** (2 slider, dolgulu ve ikonlu), **Oynanış** (3 switch), **Dil** (segment: TR | EN), **Hesap** (tek satır: "Misafir · Hesabı bağla ›"). Kapatma sağ üstte ✕ ve scrim'e dokununca. "TAMAM" kalksın.

### 1.6 Hesap (`account-Turkish.png`, `account-English.png`)
- **K:** Başlık "İlerlemeni koru" ama ana CTA "Misafir olarak başla". Ayarlardan gelen oyuncu zaten misafir; bu düğme ona bir şey yapmıyor gibi görünür. Kartın ortasında ~260 pt boşluk var. Gizlilik ve Koşullar tam boy düğme olarak çok yer kaplıyor. `OpenLegalUrl` hata metni yalnız Türkçe (`AppFlowController.cs:898-905`, `Screens.cs:301,304`).
- **R:** AFK Journey ve Clash Royale'de hesap ekranı mevcut durumu (bağlı / misafir) ve tek bağlama düğmesini gösterir. Yasal bağlantılar altta küçük metin linkleridir.
- **Ö:** Üstte durum satırı ("Misafir · ilerleme bu cihazda"). Tek altın CTA "Hesabını bağla". Altta 2 metin linki. Misafir düğmesi yalnız ilk kurulum akışında görünsün.

### 1.7 Savaş HUD — hazırlık (`battle.png`; mockup C ile karşılaştırma)
- **İ:** Kaynak sayıları büyük ve okunur. Hız ve duraklat sağ üstte. Seçili yuva altın halkayla işaretli. Tehdit önizlemesi var.
- **K:**
  1. Üst bar 4 ayrı kutu ve renkli alt çizgilerden oluşuyor. "STATUS PREPARING" kutusu ~380 pt genişliğinde ve bilgi değeri düşük.
  2. "Raider ×74" önizleme şeridi ve "Tap the gold ring…" ipucu rotanın tam üstüne biniyor.
  3. Sol altta "Warrior READY" metin kutusu yetenek düğmesi olarak duruyor; portre ve dairesel cooldown yok. Yanında hazırlıkta anlamsız bir "CANCEL" düğmesi var.
  4. "START FIRST WAVE" koyu zeminli ve neredeyse ikincil düğme gibi görünüyor. Ekrandaki asıl CTA bu olmalı.
  5. Savaş alanı sol ve sağda siyah boşlukla bitiyor (x<140, x>1860).
- **R:** Kingdom Rush: sol üstte can ve altın, ortada dalga, sağ üstte duraklat; yeni dalga yolun girişinde nabız atan bir "çağır" ikonudur. BTD6: sağda kule paleti, sağ altta büyük oynat/hız düğmesi.
- **Ö:** Mockup C'yi aynen uygula. Ek olarak: (a) "Sıradaki" çipi metin yerine düşman ikonu + sayı göstersin; (b) öğretici ipucu orta şerit yerine hedef yuvanın üstünde nabız atan ok ve kısa etiket olsun (§1.12); (c) Cancel kalksın, yuva dışına dokunmak seçimi kapatsın; (d) dalga çağırma için yolun girişine dünya içi bir ikon da eklensin (KR alışkanlığı).

### 1.8 Savaş HUD — dalga ve boss (görüntü yok; koddan)
- **İ:** Faz rozeti BOSS/uyarı durumlarını ayırıyor (`DefenderHudController.cs:702-760`). "Yaklaşan boss" önizlemesi var (`:329-345`).
- **K:** Boss can çubuğu yok (`grep -i 'boss.*health|BossBar'` → 0 sonuç). Oysa 10. bölüm hedefi "%66/%33 fazları · kırmızı uyarıyı kes" diyor (`AppFlowController.ClosedTest.cs:62`). Oyuncu bu fazları göremiyor. Yetenek cooldown'u dikey dolgu çubuğu (`:922-930`), radyal değil. Kahraman metni "Warrior 100% · 12 s" satır içi.
- **R:** Kingdom Rush ve Arknights'ta boss girişinde isim bandı ve üstte faz çentikli büyük can çubuğu vardır.
- **Ö:** Boss gelince üst ortadaki dalga paneli boss çubuğuna dönüşsün (isim + %66/%33 çentikleri + kırmızı "uyarı" yanıp sönmesi). Yetenek 74 pt dairesel düğme, `CooldownRing` dolgusu ve HAZIR rozetiyle (mockup A).

### 1.9 İnşa akışı (görüntüde yalnız ipucu; koddan)
- **K:** İnşa seçenekleri seçili panelin altına 86 birim yüksekliğinde 3 metin düğmesi olarak yerleşiyor (`DefenderHudController.cs:647-685`). Dokunuş noktasından uzak kalıyor; göz yuvadan panele, sonra yuvaya gidip geliyor.
- **R:** Kingdom Rush'ta yuvaya dokununca yuvanın etrafında 4 ikonlu halka açılır; her ikonda fiyat yazar, yetersiz altında ikon gri olur. İkinci dokunuş onaydır ve ikonun üstünde ✓ çıkar. BTD6'da sürükle-bırak var.
- **Ö:** Mockup C'deki radyal menü. Önerilen kural: **tek dokunuş önizleme (menzil halkası + bilgi kartı), ikinci dokunuş inşa**. Yanlış kule kurma hatasını keser.

### 1.10 Seçili kule paneli (mockup A)
- **K (bugün):** Panel istatistikleri `<size=70%><color=#A6B4B8>` biçimlendirilmiş tek metin bloğunda veriyor (`DefenderHudController.cs:694-695, 880-885`). Bu, renk tokenı dışında satır içi bir renk.
- **K (mockup A):** Sağ kart ekran genişliğinin ~%26'sını ve rotanın sağ kısmını kapatıyor.
- **Ö (karar değişikliği):** Kartı **kule çevresi halkaya** taşı: üstte Yükselt (fiyat çipi), altta Sat (+iade), seviye 3'te iki uzmanlaşma ikonu. İstatistikler sol alttaki aynı bilgi kartında (C'deki "Okçu Karakolu" kartı) gösterilsin. Gerekçe: inşa ve yükseltme tek `RadialMenu` bileşenini paylaşır, harita kapanmaz, başparmak yolu kısalır. Maliyet: A'daki kartla aynı (~3 sa), çünkü radyal menü zaten inşa için yazılacak.

### 1.11 Uzmanlaşma modalı (koddan)
- **K:** İki adet 440×260 düğme ve "GERİ • Dal değiştirmek için satıp yeniden kur" gibi uzun bir geri etiketi var (`DefenderHudController.Specializations.cs:62-83`). Karşılaştırma görsel değil, metin.
- **R:** Kingdom Rush'ta 4. seviyede iki kule portresi yan yana çıkar; dokununca kısa açıklama görünür, fiyat ikonun altındadır.
- **Ö:** Halka menüde iki büyük ikon. Seçince alt bilgi kartında "+%40 hasar / alan yavaşlatma" gibi iki farkı ve fiyatı göster. "Geri alınamaz" notu tek satır, küçük ve soluk.

### 1.12 Pause (koddan)
- **K:** 920×440 kart, yalnız 2 düğme (Devam / Ana menü), düğmeler 80 birim yüksek (`DefenderHudController.cs:1449-1452`). Yeniden başlat ve ses ayarı yok. Ana menüye dönüşte onay sorulmuyor (doğrulanamadı).
- **R:** Kingdom Rush pause: Devam, Yeniden başlat, Ayarlar (müzik/efekt), Çıkış. Çıkışta onay istenir.
- **Ö:** Devam (altın) · Yeniden başlat · Ses/müzik ikon toggle'ları · Haritaya dön (onaylı).

### 1.13 Zafer / yenilgi (`victory.png`, `defeat.png`)
- **İ:** Yenilgide en çok sızan düşman ve bir tavsiye gösteriliyor (`AppFlowController.cs:663-664`). Bu çok iyi bir öğretici dokunuş.
- **K:**
  1. Yıldızlar metin ("0 / 3 STARS"). Animasyon yalnız metni değiştiriyor (`:1310-1324`). Zafer görüntüsündeki 0/3, büyük ihtimalle animasyonun ilk karesinde alınmış (doğrulanamadı).
  2. Zaferde "Saved · Syncing" teknik metni var. Ödül (altın, yıldız, açılan bölüm) gösterilmiyor.
  3. Yenilgide "NEXT LEVEL ›" pasif ama görünür (`:666-667`).
  4. 3 eşit boy düğme var; "RETURN TO MAP" 2 satıra kırılıyor.
  5. Arkada HUD hâlâ görünüyor ("PREPARING", "START FIRST WAVE").
  6. Sonuç ekranı AppFlow canvas'ında (yatay, `match=0`); HUD ise `match=0.5` (`GameSessionController.cs:821-823`). İki farklı ölçek aynı sahnede kullanılıyor.
- **R:** Kingdom Rush'ta 3 büyük yıldız sırayla "pat" diye düşer, ardından Devam / Yeniden. BTD6'da ödül sayaçları yukarı doğru sayar.
- **Ö:**
```
            ┌────────────────────────────────────────┐
            │              ★   ★   ☆                  │  ← sırayla ölçek 0→1.2→1 + ses
            │            Z A F E R                     │
            │        İlk Nöbet tamamlandı              │
            │   (● +120)  (★ 2/3)  (🔓 Bölüm 2)       │  ← sayaç tick
            │  [↻]   [  ▶ SONRAKİ BÖLÜM  ]   [🗺]     │  ← tek altın CTA, ikinciller ikon
            └────────────────────────────────────────┘
```
Yenilgide yıldız satırı yerine "Dalga 1 · Yağmacı" + tavsiye kartı (kule ikonu), ana CTA "Yeniden dene". Sonraki bölüm düğmesi gizli. Sonuç ekranında HUD gizlenir.

### 1.14 Öğretici ipuçları (`battle.png`; `GameSessionController.Tutorial.cs:26-31`)
- **İ:** Adımlar mantıklı: okçu kur → ikinci okçu → dalga → yetenek → yükselt.
- **K:** Hepsi rotanın üstünde duran tek bir metin şeridinde. Metinler uzun ("Kahraman: Sarsıcı Darbe yakındaki düşmanları sersemletir · Basılı tutarak alanı gör.").
- **R:** Kingdom Rush ve BTD6 öğreticisinde hedefin üstünde parmak/ok animasyonu, en fazla 6 kelimelik balon ve gerektiğinde diğer alanlarda karartma vardır.
- **Ö:** Dünya-uzayına bağlı işaretçi (yuvanın veya düğmenin üstünde nabız atan altın ok) + ≤6 kelimelik balon + "1/5" adım sayacı. Yetenek adımında düğmenin etrafında halka nabzı.

---

## 2. Akış ve bilgi mimarisi

**İlk açılıştan ilk zafere dokunuş sayısı** (koddan tahmin; cihazda sayılmadı):
Boot (otomatik ~0,7 sn) → Hoş geldin "OYUNA BAŞLA" (1) → yön perdesi + yükleme → yuva (2) → Okçu (3) → yuva (4) → Okçu (5) → İlk dalga (6) → yetenek (7) → yuva + okçu (8-9) → kule seç + yükselt (10-11) → zafer. Toplam **~11 dokunuş, ~3 dk**. Bu, KR seviyesinde ve iyi.

**Sürtünme noktaları (öncelik sırasıyla):**
1. **Yön değişimi:** Her bölümde portre→yatay→portre geçişi oluyor (`AppFlowController.OrientationForScreen`, `:98-103`; `MobileOrientationController.cs:43-57`; perde ve bekleme `:700-846`). Oyuncu telefonu iki kez çeviriyor. Ayrıca iki canvas ölçeği ve ayrı perde kodu bakım yükü yaratıyor.
2. İlk savunmada ana menüdeki iki CTA aynı yere gidiyor (§1.3).
3. Hesap ekranında misafir/bağla mantığı karışık (§1.6).
4. Sonuç ekranında "Haritaya dön" eşit ağırlıkta; döngü "Sonraki"ye yönlenmiyor.

**Yön kararı:**

| Seçenek | Artı | Eksi | Maliyet |
|---|---|---|---|
| **A. Her yerde yatay (önerim)** | Döndürme yok. KR, BTD6, Arknights ve Legends of KR böyle çalışıyor. Harita görseli zaten yatay (1672×941). Portre kale görseli yatayda sol panele 1:1 sığıyor. Perde/çift ölçek kodu silinir (~150 satır). `FrontendResponsiveLayout.UseColumns` sütun düzeni zaten var (`Screens.cs:128-146`) | Onaylı mockup B portre; yeniden çizim gerekir. Tek elle menü kullanımı zorlaşır | Faz 4'e +4–6 sa. Menüler zaten yeniden kurulacağı için marjinal maliyet düşük |
| B. Portre menü + yatay savaş (bugün) | Mockup B hazır | Bölüm başına 2 döndürme ve bekleme; iki ölçek | 0 |
| C. Her yerde portre | Tek el | Savaş alanı ve kamera baştan tasarlanır | 30+ sa, önermem |

Karar kullanıcının (mockup B onaylıydı). Ancak en yüksek UX kazancı A'da.

**Eksik ekranlar ve MVP kararı:**
- **Yeni düşman/kule tanıtım kartı** (ilk karşılaşmada 1 kartlık pop-up, KR tarzı): **MVP için gerekli.** Bölüm 4+ zırh/şifacı/rejenerasyon mekanikleri şu an yalnız brifing metninde anlatılıyor (`ClosedTest.cs:55-61`). ~4 sa.
- **Ansiklopedi** (kule/düşman listesi): MVP dışı. Kartlar sonradan buraya toplanabilir.
- **Kahraman ekranı:** Bugün yalnız isim girişi var ve ayarlarda duruyor. Meta para (yıldız/Valor) harcanamıyorsa MVP için gerekmez. İsim girişi profil çipine taşınsın. Valor/heroXP durumunu envanter raporuyla doğrulayın (bu raporun kapsamı dışında).
- **Bölüm yıldız koşulları** ("3 yıldız = ≥%75 kale canı", `AppFlowController.cs:640`): brifingde tek satırla gösterilmeli. Oyuncu bugün kuralı bilmiyor.

---

## 3. Görsel dil

**Mevcut kararlar yeterli mi?** Kısmen.
- *Temiz Fantastik + UGUI + kodla şekiller:* HUD ve menü için **yeterli**. Mockup A/C'deki cam pill'ler, halkalar ve gradient'ler 9-slice ile üretilebilir; blur gerekmiyor.
- *Yetersiz kalan iki yer:* (1) **İkonlar.** Sistem ikonları (pause, hız, ayar, harita, kalkan, kilit, yıldız) yok. Bugün "‹", "×", "AYAR" metinleri kullanılıyor. Kodla üretilebilir: builder'a 12 basit vektör şekli (yıldız, kilit, ok, dişli, harita, pause, hız) eklenir, ~3 sa. (2) **Savaş alanı.** Düz renk low-poly zemin menü illüstrasyonunun yanında ucuz görünüyor.

**Kalite uçurumu nasıl kapanır** (yeni AI görseli olmadan, maliyet sırasıyla):
1. Işık ve post-process'i menü paletine bağla: ay mavisi ana ışık, sıcak meşale vurguları, hafif vinyet, bloom ~0,3. `DefenderSceneLook.cs` bu altyapıya zaten sahip (Bloom/Vignette/ColorAdjustments, `:16-21`). ~2 sa.
2. Zemin: tek düz renk yerine savaş masası görselinden (`GreenwatchWarTable.png`) örneklenmiş, düşük kontrastlı bir doku ya da vertex-color gürültüsü. Yol kenarı taşlarını seyrelt (görüntüde yolun her iki yanında sık taş dizisi var ve görsel gürültü yaratıyor). ~4 sa.
3. Yuva: parlak mavi disk yerine taş kaide + altın halka (plan Faz 5). Mavi diskler bugün sahnenin en dikkat çeken öğesi; oysa en çok düşmanların dikkat çekmesi gerekir.
4. Sahne kenarları: siyah pillarbox (x<140) yerine sis veya kenar dekoru.
5. Renk hiyerarşisi kuralı: **düşmanlar sıcak (kırmızı/turuncu), oyuncu öğeleri soğuk (mavi/altın), zemin desatüre.** Bugün kristal kayalar da mavi; oyuncu öğeleriyle karışıyor.

**Tipografi:** Cinzel başlıklarda iyi. Gövdede Noto Bold var, SemiBold yok (Faz 1 notu). Öneriler: sayılar için tabular rakam ve +%10 boyut; etiketler 20–24 birimde büyük harf ve +%8 harf aralığı; gövdede asla tüm büyük harf kullanılmasın. `ToUpperInvariant` yerine `ToUpper(new CultureInfo("tr-TR"))` ya da dile göre kültür seçilsin.

**Hareket (motion):** Bugün yalnız düğme basma ölçeği, menü reveal ve pause fade var. Eksikler: ekran çıkışı, sayı tick'i (altın), yıldız pop'u, "yetersiz altın" sallanması, dalga başlangıç bandı. Hepsi `UiTokens` süreleriyle ve azaltılmış hareket desteğiyle yazılmalı.

**Ses ve haptik:** Haptik yalnız `Handheld.Vibrate()` (`AppFlowController.cs:1340`). iOS'ta bu uzun ve sert bir titreşim (~400 ms); modern oyunlar hafif "tık" (UIImpactFeedbackGenerator light/medium) kullanır. Küçük bir iOS native eklentisi gerekir (~2 sa). UI sesleri: tık, inşa, yükselt, yetersiz altın (düşük "bonk"), yıldız ×3 (artan perde).

---

## 4. Teknik zemin

| Ölçüm | Değer | Kanıt |
|---|---|---|
| Menü kurulum yöntemi | %100 runtime C#, mutlak koordinatlarla (`SetRect/SetAnchors` + sabit sayılar) | `AppFlowController.cs` 1425 satır + `.Screens.cs` 409 satır |
| HUD kurulum yöntemi | Builder prefab'ı + çalışma zamanında yol-adıyla `Find("BottomBar/Ability")` ve `Ensure*Chrome` yamaları | `DefenderHudController.cs:494-531`, 1727 satır |
| Yeniden kullanılabilir bileşen | Yok. Yalnız fabrika metotları (`CreateButton/CreatePanel/CreateText`), menü ve HUD'da ayrı ayrı | `AppFlowController.cs:1180-1286`, `DefenderHudController.cs:1505,1637,1673` |
| Faz 1 sprite'larının çalışma zamanı kullanımı | **0** | `grep -rn 'UiDesignSystemAssets\|Rounded14\|Pill64\|CooldownRing\|GoldGradient' Assets/Scripts` (tanım dosyası hariç) → boş |
| `UiTokens.` kullanımı | HUD 64, Theme 30, AppFlow 20+10+1, diğer 10 | `grep -c 'UiTokens\.'` |
| Satır içi `new Color(` (UI+Presentation, UiTokens hariç) | **48**: VFX 23, SceneLook 10, Dressing 10, diğer 5; menü/HUD'da 1 (`Specializations.cs`) | `grep -rnE 'new Color(32)?\('` |
| Metin içi renk etiketi | `<color=#A6B4B8>` | `DefenderHudController.cs:695` |
| `FontStyles.Bold` | **36** (HUD 16, AppFlow 18, Core 1, Map 1). Artık gerçek Bold yüze gidiyor | `grep -rn 'FontStyles.Bold'` |
| Testlerin nesne adı bağımlılığı | **118 çağrı, 86 benzersiz ad** (`GreyboxSceneTests.cs` 82 çağrı) | `grep -rhoE '(name == \|Find\(\|Find<..>\(\|GetButtonInteractable\()"..."' Assets/Tests` |
| Yerelleştirme kaynakları | **5 ayrı mekanizma**: `FrontendCopy` (32 çift), satır içi `GameLocalization.Text(tr,en)` (**213**), Türkçe anahtarlı sözlük (95), `UiCopyCatalog` (enum + TR asset + EN switch), kullanılmayan Unity Localization tablosu (`Assets/Localization/Defender UI Shared Data.asset`) | `grep -ro 'GameLocalization.Text(' \| wc -l` |
| Oyun metni konumu | Bölüm hedefleri ve tavsiyeler `AppFlowController.ClosedTest.cs` içinde | `:51-72` |
| Canvas ölçekleri | Menü 1080×1920 `match=0`; yatayda 1920×1080 `match=0`; HUD 1920×1080 `match=0.5`; yükleme 1080×1920 `match=0.5` | `AppFlowController.cs:235-238,843`, `GameSessionController.cs:821-823`, `FrontendLoadingOverlay.cs:34-35` |
| Safe area | Menüde `SafeAreaFitter`, HUD'da `DefenderHudResponsiveLayout` (rail'ler kenara uzar, içerik güvenli alanda) | `AppFlowController.cs:276-279`, `DefenderHudResponsiveLayout.cs:6-9` |
| Dokunma alanı | `MobileTouchTarget` min 112 birim; yorum 0,398 pt/birim varsayıyor (430 pt genişlik). iPhone 17 Pro portrede 402/1080 = 0,372 → **41,7 pt < 44**. Ayrıca her düğmede `LateUpdate` her kare çalışıyor | `MobileTouchTarget.cs:9-10,23` |
| Tween kütüphanesi | Yok. Animasyonlar elle yazılmış coroutine'ler | `grep DOTween\|PrimeTween` → 0 |
| Haptik | Yalnız `Handheld.Vibrate` | `AppFlowController.cs:1340` |

**report.json'daki 7 taşmanın kaynağı: 2 kök sebep.**
1. **Brifing "HEDEF" kutusu (5 kayıt: map TR/EN, settings TR/EN, returned-map).** Ayar ekranı haritanın üstünde açıldığı için aynı metin tekrar sayılıyor. Kutu 112 birim yüksek (`Screens.cs:145-146`), 32 pt sabit boyut, otomatik boyut yok. Metin 3 satır ("HEDEF\n" + ~50 karakterlik hedef, `ClosedTest.cs:53`). Ayrıca TMP'de `TextAlignmentOptions.Left` **dikeyde ortalı** (MidlineLeft). Bu yüzden taşma hem yukarıya (EN'de "Raider" ile çakışma) hem aşağıya (CTA içine) gidiyor.
2. **Sonuç "Reward" kutusu (2 kayıt: zafer/yenilgi).** 52 birim yüksek, 25 pt, 2–3 satır metin (`AppFlowController.cs:357-359, 659-664`).
- **Düzeltme:** üst hizalama (`TopLeft`), otomatik boyut 22–32, `overflowMode=Ellipsis`, hedef metinlerini ≤40 karaktere kısalt. Ayrıca kabul testi `isTextOverflowing` kontrolünü yalnız aktif ekrana uygulasın; settings kayıtları çift sayım.

**Mimari önerisi (hızlı ve güvenli yeni UI):**
1. **Kod-öncelikli bileşen kiti kalsın, prefab'a geçilmesin.** Gerekçe: menü zaten koddan kuruluyor, testler kodla üretilen adlara bağlı, Unity Editor kilidi worker'ların prefab düzenlemesini zorlaştırıyor. Ama mutlak koordinat yerine **düzen (layout) tabanlı** kurulsun: `UiKit.Button(parent, UiButtonStyle.Gold, copy, icon)` gibi bileşenler `LayoutElement` ve `Vertical/HorizontalLayoutGroup` ile yerleşsin. Böylece taşma ve en-boy sorunlarının çoğu kendiliğinden biter.
2. **Tek `UiKit` hem menüyü hem HUD'u beslesin.** `CreateButton` (`AppFlowController.cs:1180`) ile `EnsureActionButton` (`DefenderHudController.cs:1505`) birleşsin. HUD'daki `Ensure*Chrome` yamaları kaldırılsın; HUD prefab'ı builder'da kit ile yeniden üretilsin (Faz 0'ın `RegenerateHudPrefab` yolu hazır).
3. **Test kimliği:** nesne adı yerine küçük bir `UiTestId` bileşeni (string id). Mevcut 86 ad, id olarak aynen taşınır; hiyerarşi değişse de testler kırılmaz.
4. **Yerelleştirme:** satır içi 213 çift tek seferde `FrontendCopy` tarzı sabit kimliklere taşınsın (dinamik metinler `string.Format` şablonu olarak). Türkçe anahtarlı sözlük ve kullanılmayan Unity Localization tablosu silinsin. Büyük harf dönüşümü tek yardımcıda, dile göre kültürle yapılsın.
5. **Animasyon altyapısı:** harici kütüphane yerine ~150 satırlık `UiMotion` (fade/scale/slide/punch/count, `unscaledTime`, `UiTokens` süreleri, azaltılmış hareket desteği). PrimeTween (MIT, sıfır GC) da makul bir alternatif, ama paket eklemek kullanıcı kararı.
6. **Tek ölçek:** HUD ve menü aynı `CanvasScaler` (1920×1080, match 0,5 yatay) kullansın. `MobileTouchTarget` sabiti ekran boyutundan hesaplansın, `LateUpdate` yerine yalnız boyut değişince çalışsın.

---

## 5. Uygulama yol haritası

### 5.0 Hızlı kazanımlar (her biri ≤1 sa, toplam ~4 sa)
| # | İş | Dosya | Süre | Kapı |
|---|---|---|---|---|
| H1 | Brifing ve sonuç metin kutuları: `TopLeft` hizalama + autosize 22–32 + ellipsis; hedef metinlerini kısalt | `AppFlowController.Screens.cs:105-110,143-146`, `AppFlowController.cs:357-359`, `ClosedTest.cs:51-72` | 45 dk | Kabul modu: overflow **0**; map/victory/defeat TR+EN görüntüsü |
| H2 | Türkçe büyük harf + bölge adının dil değişiminde yenilenmesi (`LocalizedTmpText` ile) | `Screens.cs:200`, `AppFlowController.cs:616,619,658` | 30 dk | EditMode: "Yeşil"→"YEŞİL"; PlayMode: dil değişince başlık değişir |
| H3 | Dil düğmesi vurgusunu ayarlar açılırken uygula; slider'a dolgu ekle, tutamağı içeri al | `AppFlowController.cs:868-879`, `Screens.cs:365-387` | 30 dk | settings-Turkish görüntüsünde TÜRKÇE altın; slider dolu |
| H4 | Üst/alt düz gölge şeritlerini `VerticalGradient` sprite'ına çevir (dikiş kalkar) | `AppFlowController.cs:246-251` | 20 dk | main-menu görüntüsünde y≈360 dikişi yok |
| H5 | Sonuç ekranı: yenilgide "Sonraki" gizli, teknik "Kaydedildi · Eşitleniyor" metni zafer/menüden kalksın, sonuçta HUD gizlensin; haritada tek bölgede sekme gizli, "> 1 <" yerine yalnız halka | `AppFlowController.cs:607,659-667`, `:541`, `Screens.cs:60-72` | 60 dk | FrontendExperienceTests (`Next Level` testi güncellenir) + görüntüler |

### 5.1 Fazlar
| Faz | İş | Ana dosyalar | Efor | Kabul kapısı |
|---|---|---|---|---|
| 0 | Yön kararı (A yatay / B portre) kullanıcıdan alınır | — | 0,5 sa | Karar kaydı |
| 2 | **UiKit + UiMotion + UiTestId:** Button(altın/cam/tehlike/ikon), StatPill, Chip, Card, Switch, Slider, Segmented, ProgressSegments, RadialCooldown, **RadialMenu**, Modal, BottomSheet, StarRow. 12 vektör ikon builder'a eklenir | yeni `Assets/Scripts/UI/Kit/*`, `UiDesignSystemAssetBuilder.cs`, `UiTokens.cs` | 10–12 sa | Bileşen başına EditMode testi (5 durum, ≥44 pt, kontrast); ikon atlas testi |
| 2b | Yerelleştirme birleştirme: 213 satır içi çift → sabit kimlik; sözlük ve Unity Loc tablosu silinir; TR büyük harf yardımcısı | `GameLocalization.cs`, `FrontendCopy.cs`, `UiCopyCatalog.cs`, `ClosedTest.cs`, HUD/Session dosyaları | 5–6 sa | TR/EN tüm ekran görüntüleri değişmedi (piksel diff ≈0); test: sözlükte Türkçe anahtar kalmadı |
| 3 | **Savaş HUD'u** (mockup A/C + halka menü): üst pill'ler, dalga paneli + boss çubuğu, sıradaki çipi (ikon), radyal inşa ve yükseltme, uzmanlaşma, yetenek dairesi, öğretici işaretçisi, pause (4 eylem). `Ensure*Chrome` kaldırılır, prefab kit ile yeniden üretilir | `DefenderHudController*.cs`, `DefenderHudResponsiveLayout.cs`, `DefenderPresentationAssetBuilder.cs`, `GameSessionController.Tutorial.cs` | 14–16 sa | PlayMode HUD testleri (id ile); 16:9, 19.5:9 ve iPad görüntüleri; rota/8 yuva/kale görünür; HUD'un kapladığı alan ≤%18 |
| 4 | **Menüler** (seçilen yönde): ana menü, harita (madalyon düğüm + yıldız), brifing paneli, ayarlar (gruplu), hesap, sonuç (yıldız pop + ödül tick), yeni düşman kartı. Yatay seçilirse yön perdesi ve çift ölçek kodu silinir | `AppFlowController*.cs`, `FrontendResponsiveLayout.cs`, `MobileOrientationController.cs` | 12–14 sa (+4–6 yatay) | Overflow 0; FrontendExperienceTests yeşil; menü→savaş geçişinde döndürme yok (A) |
| 5 | **Savaş alanı görünümü:** ışık/post paleti, zemin dokusu, taş kaide yuvalar, kenar sisi, renk hiyerarşisi | `DefenderSceneLook.cs`, `DefenderBattlefieldDressing.cs`, `StylizedPresentationFactory.cs` | 8–10 sa | Önce/sonra görüntüsü; fiziksel cihazda ≥60 FPS (iPhone 12 sınıfı) |
| 6 | **Cila:** ekran geçişleri, sayı tick'i, yetersiz altın sallanması, dalga bandı, iOS hafif haptik eklentisi, UI ses seti | `UiMotion`, yeni `Plugins/iOS/Haptics.mm`, `DefenderAudioController.cs` | 6–8 sa | Azaltılmış hareket testi; cihazda haptik elle kontrol |

**Toplam:** hızlı kazanımlar ~4 sa + fazlar ~52–64 sa (yatay seçilirse +4–6) → **yaklaşık 7–9 iş günü.**

**Sıralama mantığı:** H1–H5 bugünkü görüntüyü hemen düzeltir. Faz 2 olmadan Faz 3–4 yine mutlak koordinatlı tek seferlik kodla yazılır ve aynı taşma sorunları geri gelir. Faz 2b, metin değiştiren her işten önce gelmeli. Faz 5 HUD'dan sonra yapılmalı, çünkü HUD kamera kadrajını değiştiriyor.

**Riskler:** (1) 86 test adı: `UiTestId` geçişi Faz 2'de ve aynı commit'te yapılmalı. (2) Editor kilidi: builder ve test koşuları kullanıcının Unity'si kapalıyken ya da worktree'de yapılmalı (Faz 0/1 notlarındaki `link.xml` tuzağına dikkat). (3) Kapsam: ansiklopedi ve kahraman ekranı MVP dışında kalmalı.
