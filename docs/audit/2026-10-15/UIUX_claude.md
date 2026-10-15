# ALLIN: Defense v1.1 — UI/UX denetimi (oyuncu gözüyle) · claude · 2026-10-15

**Özet.** Menü, harita, brifing, ayarlar ve sonuç akışı sağlam bir temel üzerinde: güvenli alan, 44 pt dokunma alanı, haptik ve menü VoiceOver kapsamı (%100, `Builds/a11y/report.json`) kodda var. Mağaza kalitesini en çok savaş ekranı düşürüyor: kule bilgi kartı ve halka menü yazıları 9–12 pt, dalgalar arası 8 saniyelik otomatik başlatma geri sayımı ekranda hiç görünmüyor, giriş ekranının ilk düğmeleri (Apple/Google) cihazda henüz denenmedi. Aşağıda 3 P0, 10 P1 ve 10 P2 bulgu var. Bulgular birbiriyle çakışmayan dosya sahipliğine sahip 6 iş pakete bölündü; paket 5 (metinler) önce koşmalı.

---

## Kanıt sınırı (okumadan önce)

- **Ekran görüntüleri iki farklı tarihten.** `Builds/FrontendExperience/iOS/*.png` 13 Ekim tarihli (1206×2622, iPhone 16 Pro). Bu görüntüler v3 kule/birlik görünümünden ve 5 bölge açılmadan önce alındı. 15 Ekim görüntüleri: `Builds/step6-after-*`, `Builds/flow-0b/*`, `Builds/relearn/*`, `Builds/a11y/settings.png`. 14 Ekim görüntüleri: `Builds/actors-v3/*`, `Builds/towers-v3/*`. Eski görüntüdeki her durumu kodla yeniden doğruladım. Örnek: 13 Ekim zafer ekranındaki "+27" ödülü artık gösterilmiyor, kod yalnız yıldızı yazıyor (`AppFlowController.cs:673`).
- **Olmayan görüntüler:** iPhone SE (667×375 pt), Pro Max (956×440 pt), iPad dikey menü, uzmanlaşma sonrası halka menü (v3), bölge 2–5 harita sayfası, kilitli bölge kartı, yükleme perdesi. `Builds/FrontendExperience/iOS/new-enemy.png` adlı dosya yanlış ekranı içeriyor: yeni düşman kartı yerine duraklat menüsü. Bu ekranlar için yazdığım her şey yalnız koda dayanıyor ve öyle işaretli.
- Unity, simülatör ve iOS build koşulmadı. Font boyutları kodda tanımlı **pt** (nokta) değerlerinden okundu: savaş HUD'u `1/pointsPerUnit` ölçeğiyle mantıksal noktaya çevriliyor (`DefenderHudResponsiveLayout.cs:108-113`, `:210-216`).

---

## P0 — mağazaya çıkmadan önce şart

**P0-1 · Savaşta karar verilen tek yüzeyin yazıları okunmuyor (9–12 pt).**
- **Oyuncu ne yaşıyor:** Kule kurarken, yükseltirken ya da uzmanlaşma seçerken hasar/menzil/hız karşılaştırmasını telefonda kolayca okuyamıyor. Halka menüde "SAT" etiketi ve fiyatlar da çok küçük.
- **Neden önemli:** Kingdom Rush oyuncusu her kararı bu kartla verir. Apple'ın önerdiği alt sınır 11 pt; kartta 9 pt satırlar var.
- **Kanıt:** Kart 198×80 pt (`DefenderHudResponsiveLayout.cs:112-113`). Başlık 12, rozet 10, rol satırı **9**, istatistik satırı 10, ipucu satırı **9** pt (`DefenderHudController.Radial.cs:177-181`). Halka menüde etiket **10** pt (`:276`), fiyat 12 pt (`:280`). Görüntü: `Builds/step6-after-chapter10-boss-1560x720.png` (sol alttaki "MUHAFIZ KIŞLASI" kartı), `Builds/FrontendExperience/iOS/specialization.png`. Hedef tasarımda kart sağda, büyük istatistik kutucuklarıyla duruyor: `docs/ui-modernization/mockups/A-battle-combat.png`.
- **En küçük çözüm:** Kartı yaklaşık 260×120 pt'ye büyüt. Gövde metni en az 13 pt, ikincil metin en az 11 pt olsun. Hasar/Menzil/Hız değerlerini maketteki gibi 3 kutucukta göster ("42→29" büyük rakamla). Halka etiketi ve fiyat en az 12–13 pt olsun.
- **Büyüklük:** M

**P0-2 · Dalgalar arası 8 saniyelik otomatik başlatmanın geri sayımı hiç görünmüyor.**
- **Oyuncu ne yaşıyor:** Dalga bitince kule kurmaya ya da yükseltmeye başlıyor; 8 saniye sonra bir sonraki dalga uyarısız başlıyor. Düğmede yalnız "SONRAKİ DALGA" yazıyor.
- **Neden önemli:** Haksız can kaybı yaşanıyor ve oyuncu "oyun benden habersiz başladı" diye düşünüyor.
- **Kanıt:** Ara verme süresi `GameSessionController.cs:55`'te 8 sn, otomatik başlatma `GameSessionController.Battle.cs:116-117`'de. Geri sayım metni `GameSessionController.cs:444` üzerinden `artHud.SetNextWaveLabel` çağrısına gidiyor, ama bu yöntem **boş** (`DefenderHudController.cs:289` → `{ }`). Üstelik `DefenderHudController.Frame.cs:121` aynı düğmeyi her karede sayaçsız metinle yeniden yazıyor. Erken çağırma ödülü de her zaman 0 (`GameSessionController.Battle.cs:12`), yani "+N" eki hiç çıkmıyor.
- **En küçük çözüm:** `RefreshFrame` içinde, ara verme fazındayken kalan süreyi düğmede göster ("SONRAKİ DALGA · 6"). Düğmenin kenarına `UiRadialCooldown` benzeri dolan bir halka ekle. Son 3 saniyede hafif nabız efekti ver (azaltılmış hareket açıkken nabız olmasın).
- **Büyüklük:** S

**P0-3 · Açılış ekranının ilk iki düğmesi (Apple / Google ile giriş) fiziksel cihazda denenmedi.**
- **Oyuncu ne yaşıyor:** Uygulamayı ilk açtığında en üstte Apple ve Google düğmelerini görüyor. Bunlar çalışmazsa ilk deneyim "Giriş tamamlanamadı" hatası oluyor.
- **Neden önemli:** Apple incelemesi ilk ekrandaki işlevsiz Apple ile Giriş düğmesini reddeder (Kural 2.1). Ayrıca Apple ile girilen hesap silinirken Apple bağlantısının da iptal edilmesi (token revoke) gerekiyor, bu henüz yok.
- **Kanıt:** Düğmeler `AppFlowController.Screens.cs:351-355` (karşılama) ve `:407-409` (hesap). Google düğmesi platform kontrolü olmadan her zaman görünüyor. `TODO.md:11-15`: "Fiziksel iPhone'da Apple/Google ile giriş dene", "Apple token revoke". Görüntü: `Builds/FrontendExperience/iOS/welcome.png`.
- **En küçük çözüm:** İki seçenekten biri.
  - (a) Cihazda doğrula ve revoke işini tamamla.
  - (b) v1.1 için `ExternalSignInEnabled` bayrağı ekle (varsayılan kapalı). Karşılama ekranında yalnız "MİSAFİR OLARAK BAŞLA" kalsın, hesap ekranında harici giriş düğmeleri gizlensin.
  - Hangisinin seçileceği ürün kararı. (b) yaklaşık 1 saatlik iş.
- **Büyüklük:** S (bayrak) / L (tam doğrulama ve revoke)

---

## P1 — v1.1'de olmalı

**P1-1 · Ana menü kartındaki bölüm ile BAŞLAT'ın açtığı bölüm farklı olabiliyor.**
- **Oyuncu ne yaşıyor:** Kartta "Bölüm 8" yazıyor ama BAŞLAT'a basınca yarım kalan Bölüm 3 açılıyor. Hangi dalgadan devam edeceği de yazmıyor.
- **Neden önemli:** Oyuncu yanlış bölüme girdiğini sanıyor; ana akışa güveni azalıyor.
- **Kanıt:** Kart her zaman `NextUnfinishedLevel` gösteriyor (`AppFlowController.cs:623-630`). Devam eylemi ise önce geçerli checkpoint'i açıyor (`AppFlowController.cs:431-433`).
- **En küçük çözüm:** Geçerli checkpoint varsa kart o bölümü göstersin ve "DEVAM · Dalga N" etiketi taşısın.
- **Büyüklük:** S

**P1-2 · Kilitli bölge kartında Türkçe ek hatası (dört bölgede de).**
- **Oyuncu ne yaşıyor:** "Önce bölüm 10'i tamamla", "20'i", "30'i", "40'i" yazısını görüyor. Doğrusu "10'u / 20'yi / 30'u / 40'ı".
- **Neden önemli:** Ana gezinti ekranında her oyuncunun gördüğü bir dil hatası.
- **Kanıt:** Metin `FrontendCopy.cs:113` (`RegionLocked = "Önce bölüm {0}'i tamamla"`). Sayılar `AppFlowController.CampaignCompletion.cs:33-34`'te 10/20/30/40 olarak veriliyor.
- **En küçük çözüm:** Eki tamamen kaldıran bir kalıp kullan: "Önce {0}. bölümü tamamla".
- **Büyüklük:** S

**P1-3 · Öldürme altını yazıları sürü dalgalarında üst üste yığılıyor.**
- **Oyuncu ne yaşıyor:** Darboğazda "+2+2+2+2" diye okunamayan bir sütun oluşuyor ve halka menü düğmelerinin üstünü kapatıyor.
- **Neden önemli:** v1 tasarımında dalgalar kalabalık ve zayıf (bölüm 1'de 50–88 yağmacı). Bu yığılma her bölümde yaşanıyor.
- **Kanıt:** `DefenderRewardPopupPool.cs:11` (24 popup sınırı var, birleştirme ya da kaydırma yok). Görüntüler: `Builds/step6-after-chapter10-boss-1560x720.png`, `Builds/actors-v3/after-ch50-boss-1280x720.png`, `Builds/actors-v3/after-ch01-wave-1280x960.png`.
- **En küçük çözüm:** ~0,3 sn içinde ~1,5 birim yarıçapta oluşan popup'ları tek "+N" yazısında topla. Halka menü açıkken onun ekran alanına popup çizme.
- **Büyüklük:** S

**P1-4 · Kilitli kule ya da uzmanlaşmanın ne zaman açılacağı söylenmiyor.**
- **Oyuncu ne yaşıyor:** Halkada asma kilit ikonlu bir seçenek görüyor (rozet 16 pt). Dokununca hata sesi ve titreşim geliyor ama bir açıklama yok.
- **Neden önemli:** Oyuncu ilerlemeyi göremiyor ve yasağın nedenini anlamıyor.
- **Kanıt:** Kilit hesabı `DefenderHudController.Radial.cs:266-267`, rozet boyutu `:163`, ret davranışı `:326-338` (yalnız titreme). Açılma bölümleri `TowerProgression.cs:29` (6/7/8). `FrontendCopy.AfterLevel` (`FrontendCopy.cs:199`) tanımlı ama hiç kullanılmıyor. Görüntü: `Builds/FrontendExperience/iOS/battle-build-preview.png`.
- **En küçük çözüm:** Kilitli seçeneğe dokununca bilgi kartında "Bölüm 7'de açılır" yazsın. Kilit rozetini en az 20 pt yap.
- **Büyüklük:** S

**P1-5 · Erken dalga çağırmanın oyuncuya bir getirisi yok.**
- **Oyuncu ne yaşıyor:** "SONRAKİ DALGA" düğmesi yalnız hazırlık ve ara verme fazında görünüyor. Erken çağırmak ödül ya da süre kazancı vermiyor.
- **Neden önemli:** Türün temel risk/ödül döngüsü (erken çağır, altın kazan) eksik. Hedef maket bunu öngörüyor (`A-battle-combat.png`: "ERKEN ÇAĞIR 18 sn").
- **Kanıt:** `GameSessionController.Battle.cs:12` (`EarlyWaveReward => 0`), görünürlük kuralı `DefenderHudController.Frame.cs:118-121`.
- **En küçük çözüm:** Kalan ara verme süresiyle orantılı küçük bir altın bonusu ver ve düğmede göster. Bu bir **ürün/denge kararı**; denge testlerini etkiler.
- **Büyüklük:** M

**P1-6 · Sıradaki tehdidin önizlemesi yalnız minik bir "×50" sayısı.**
- **Oyuncu ne yaşıyor:** Sağ üstte ikon ve "×50" görüyor. Hangi düşman tipinin geleceğini bilmiyor.
- **Neden önemli:** Hazırlık kararı (büyücü mü, kışla mı?) bu bilgiye bağlı. Maket "Sıradaki · Hızlı yağmacılar ×12" şeridi öngörüyor.
- **Kanıt:** Önizleme alanı 72×24 pt (`DefenderHudResponsiveLayout.cs:233`), metin `DefenderHudController.Frame.cs:115` (yalnız sayı). Dalga verisinde hazır önizleme metni var: `region-01.json` içindeki `"preview": "Yağmacı ×50"`. Görüntü: `Builds/step6-after-chapter1-prep-1560x720.png`.
- **En küçük çözüm:** Hazırlık ve ara verme fazında üst barın altında "Sıradaki: Öncü ×12 · Şifacı ×4" şeridi göster (düşman ikonuyla). Boss dalgasında kırmızı vurgu kullan.
- **Büyüklük:** M

**P1-7 · Zafer ekranı yıldızın neden 2 olduğunu açıklamıyor.**
- **Oyuncu ne yaşıyor:** 2 yıldız alıyor ama 3 yıldız için ne yapması gerektiğini bilmiyor. Ekranda kaybedilen can ya da süre gibi istatistik de yok.
- **Neden önemli:** Tekrar oynamanın tek hedefi yıldız (150 yıldız). Kural görünmezse oyuncu yıldızları tamamlamaya motive olmaz.
- **Kanıt:** Kural `AppFlowController.cs:644-647`: can oranı ≥%75 → 3★, ≥%40 → 2★. Ekranın içeriği `:654-705`. Görüntü: `Builds/FrontendExperience/iOS/victory-Turkish.png`.
- **En küçük çözüm:** Yıldız sırasının altına tek satır ekle: "Kale 15/20 · 3★ için 15+ can". Yıldızlar sırayla dolarken her birinde ses ve hafif titreşim olsun.
- **Büyüklük:** S

**P1-8 · Bölge sonu ve kampanya finali sade bir metin kartı.**
- **Oyuncu ne yaşıyor:** 10 bölümü ya da 50 bölümlük kampanyayı bitirdiğinde düz bir liste görüyor ("Yeşil Nöbet · 30/30 yıldız" …). Arka plan boş, kutlama ve bölge görseli yok, büyük/küçük harf kullanımı karışık ("Sonraki bölgeye geç" ile "HARİTAYA DÖN" yan yana).
- **Neden önemli:** Oyunun en büyük ödül anları bunlar; şu haliyle mağaza kalitesinin altında kalıyorlar.
- **Kanıt:** `AppFlowController.CampaignCompletion.cs:53-136`, görüntüler `Builds/flow-0b/region-Turkish.png` ve `Builds/flow-0b/final-Turkish.png`.
- **En küçük çözüm:**
  - Bölgenin kapak görselini arka plana koy.
  - Bölge başına yıldız çubuğu ve bölge ikonu ekle.
  - Kısa bir parıltı/konfeti efekti ekle (azaltılmış harekette kapalı).
  - Düğme etiketlerini tek harf düzenine çek.
- **Büyüklük:** M

**P1-9 · Savaş HUD'unun düğmeleri VoiceOver için etiketsiz.**
- **Oyuncu ne yaşıyor:** VoiceOver açıkken duraklat, hız, dalga başlat, yetenek ve "?" düğmeleri adsız okunuyor.
- **Neden önemli:** Menü tarafı %100 kapsandı (`Builds/a11y/report.json`), ama raporda savaş ekranı hiç yok. Savaş alanının tamamını erişilebilir yapmak kapsam dışı; yine de bu 5 temel kontrolün etiketi olmalı.
- **Kanıt:** `DefenderHudController.cs:403-416` bu düğmeleri bağlıyor ama `UiAccessibleButton` eklemiyor. Etiket yalnız duraklat modalında ve halka menüde var (`DefenderHudController.Frame.cs:40-48`, `Radial.cs:79,165`). "?" düğmesinin görünen metni yalnız "?" (`DefenderHudController.Tutorial.cs:44`).
- **En küçük çözüm:** 5 kontrole `UiAccessibleButton` ekle. Etiketler: "Duraklat", "Oyun hızı, 2x", "Sonraki dalga, 6 saniye", "Sarsıcı Darbe, hazır", "Rehberi göster". a11y raporuna `battle` ekranını ekle.
- **Büyüklük:** S

**P1-10 · Cihaz ölçeği ve iPad için görsel kanıt yok.**
- **Oyuncu ne yaşıyor:** Bilinmiyor; görüntü yok. Uygulama iPhone ve iPad'i birlikte hedefliyor (`ProjectSettings/ProjectSettings.asset:12` `targetDevice: 2`), bu yüzden App Store iPad ekran görüntüsü ve iPad incelemesi isteyecek.
- **Neden önemli:**
  - iPhone SE'de üst bar sıkışıyor: altın kutusu x=88…236 pt, dalga paneli ~237…457 pt, aralarında yaklaşık 1 pt boşluk var.
  - Önizleme alanı (451…523 pt) dalga paneliyle ~6 pt üst üste biniyor. Bu yerleşim `DefenderHudResponsiveLayout.cs:217-233`'ten hesaplandı.
  - Hesaplama kesin değil, görüntüyle doğrulanmalı.
- **Kanıt:** `Builds/FrontendExperience/iOS/` yalnız 1206×2622 çözünürlüğü içeriyor. `new-enemy.png` yanlış ekranı yakalamış (duraklat menüsü).
- **En küçük çözüm:** Kabul testindeki görüntü yakalama matrisine SE, 16 Pro, Pro Max ve iPad (dikey menü + yatay savaş) ile TR/EN ekle. `new-enemy` yakalamasını düzelt. Koşuyu koordinatör iş grubunun sonunda tek seferde yapsın.
- **Büyüklük:** M

---

## P2 — cilalama

1. **Ondalık ayırıcı oyun diline değil cihaz diline göre seçiliyor.** 13 Ekim cihaz görüntüsünde "Menzil 4,8", 15 Ekim editör görüntüsünde "0.9 · 2.7/sn" yazıyor. Kanıt: `FrontendCopy.cs:10` (`string.Format` sistem kültürünü kullanıyor), `Radial.cs` içindeki `RadialStats` / `*Differences` metinleri. Çözüm: biçimlendirmeye oyun dilinin kültür bilgisini ver. **S**
2. **Azaltılmış hareket ayarı iOS sistem tercihini okumuyor.** Kanıt: `FrontendPreferences.cs:44` (varsayılan `false`). Çözüm: ilk açılışta `UIAccessibilityIsReduceMotionEnabled` değerini varsayılan yap (yeni küçük bir `.mm` köprüsü). **S**
3. **Kritik anlarda titreşim yok.** Titreşim yalnız kurma, yükseltme, zafer ve hatada var (`DefenderPresentationDirector.cs:51-54`, `AppFlowController.cs:1372`). Can kaybı, boss girişi ve yenilgide titreşim yok. Çözüm: can kaybında `Light`, boss girişinde `Medium`, yenilgide `Error`. **S**
4. **Brifingde düşman çipleri yatay kayıyor ama kaydığı anlaşılmıyor.** Görüntü: `Builds/relearn/briefing-card-Turkish.png`, 5. çip kesik. Kanıt: `AppFlowController.Screens.cs:210-216`. Çözüm: kenarlarda soluklaşma efekti ya da iki satıra sarma. **S**
5. **İngilizce terimler tutarsız.** Aynı kavram için "Level" ve "Chapter" kullanılıyor (`NextLevel` ↔ `UnlockedChapter` / `RegionLocked`). İngiliz/Amerikan yazımı karışık: "Defence" / "SPECIALIZE" / "specialisation". Kanıt: `FrontendCopy.cs`. Çözüm: tek sözlük. **S**
6. **"Greenwatch" adı 5 bölgelik kampanyada her yerde geçiyor.** Açılış alt başlığı "GREENWATCH SEFERİ" (`AppFlowController.cs:311`, `boot.png`), varsayılan oyuncu adı "Greenwatch Muhafızı" (`AppFlowController.cs:618`), HUD zafer metni (`DefenderHudController.cs:351`). Çözüm: kampanya geneli bir ad kullan. **S**
7. **Profil çipindeki kayıt durumu yalnız ✓ ikonu.** Kanıt: `AppFlowController.cs:620-622`, `main-menu-Turkish.png`. Maket "✓ Kayıtlı" metni öngörüyor (`B-menu.png`). Çözüm: kısa bir metin etiketi ekle; kayıt hatasında "Kayıt bekliyor" yaz. **S**
8. **Ayarlar ekranındaki küçük eksikler.**
   - Sürüm numarası ve emeği geçenler bölümü yok (`grep Application.version` → sonuç yok).
   - Dil düğmeleri "TR/EN" kısaltması; "Türkçe / English" olmalı.
   - Kaydırılan içerik altta kesiliyor ama kaydırma göstergesi yok (`Builds/relearn/settings-reset-Turkish.png`).
   - Kapat düğmesi panelin sağ kenarından taşıyor (`settings-Turkish.png`, `account-Turkish.png`).
   - **S**
9. **Duraklat menüsünde yalnız müzik ve efekt açma/kapama var.** Titreşim, kamera sarsıntısı ve dil için menüye dönmek gerekiyor. Kanıt: `DefenderHudController.Frame.cs:43-44`, `pause.png`. Çözüm: "AYARLAR" düğmesiyle ayarlar modalını savaş içinde aç. **M**
10. **Haritada küçük görsel sorunlar.** Kilitli düğümlerde bölüm numarası yok. Aşağı kaydırınca bölge başlığı kayboluyor ve yıldız satırı başlık çubuğunun altında kırpılıyor (`map-last-node-English.png`). Hesap ekranında birbirine benzeyen 3 "bağla" düğmesi var (Apple / Google / HESABINI BAĞLA, `account-Turkish.png`). **S**

**Zaten listede olanlar (yeniden keşfedilmedi, yalnız paketlere yerleştirildi):**
- Boss barı bölüm 30/50'de "Bölge Lordu" yazıyor (`.claude/ui-specs/boss-name-hud.txt`).
- Boss rozeti taç değil, düşman ikonu (`AppFlowController.Screens.cs:641` `EnemyIcon`).
- Menü müziği yok (P1-6, ürün kararı bekliyor).

---

## İş paketleri (en fazla 6, dosya sahipliği çakışmıyor)

> **Sıra:** WP5 (metinler) önce koşar ve diğer paketlerin istediği yeni TR/EN anahtarlarını ekler; diğer paketler `FrontendCopy.cs` ve `GameLocalization.cs` dosyalarına **yazmaz**. WP2'nin boss adı erişimcisi bittikten sonra WP3 brifingdeki boss adını bu erişimciye bağlar. WP1–WP4 kendi aralarında paralel koşabilir. WP6 en son koşar.

### WP1 — Savaş bilgi kartı, halka menü ve savaş geri bildirimi · **M**
- **Kapsam:** P0-1, P1-3, P1-4, P2-1, P2-3.
- **Sahiplik:**
  - `Assets/Scripts/Presentation/DefenderHudController.Radial.cs`
  - `Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs`
  - `Assets/Scripts/Presentation/DefenderRewardPopupPool.cs`
  - `Assets/Scripts/Presentation/DefenderPresentationDirector.cs`
  - Yeni `Assets/Tests/PlayMode/BattleInfoCardReadabilityTests.cs`
- **Kabul kriterleri:**
  - Bilgi kartında ve halkada hiçbir TMP metin 11 pt'nin altında değil (test `UiMetrics.PointsPerCanvasUnit` ile ölçer).
  - Kart en az 240×110 pt ve SE yatay güvenli alanına sığıyor.
  - Kilitli seçenek "Bölüm N'de açılır" metnini gösteriyor (mevcut `AfterLevel` ya da WP5 anahtarı).
  - 0,3 sn içinde ölen 20 düşman en fazla 3 popup üretiyor.
  - TR ve EN dilinde ondalık ayırıcı dile göre doğru.
  - Can kaybında, boss girişinde ve yenilgide titreşim çağrılıyor (sahte köprüyle test).

### WP2 — Dalga akışı, üst bar ve boss adı · **M**
- **Kapsam:** P0-2, P1-5 (ödül miktarı ürün kararına bağlı; karar yoksa yalnız geri sayım), P1-6, P1-9, hız adımları (`GameSessionController.cs:57-58`: 1/2/4/8x yerine 1x↔2x önerisi, ürün onayıyla) ve bilinen boss adı spec'inin HUD kısmı.
- **Sahiplik:**
  - `Assets/Scripts/Presentation/DefenderHudController.cs`
  - `Assets/Scripts/Presentation/DefenderHudController.Frame.cs`
  - `Assets/Scripts/Presentation/DefenderHudController.BattleChrome.cs`
  - `Assets/Scripts/Presentation/DefenderHudController.Tutorial.cs` (yalnız "?" etiketi)
  - `Assets/Scripts/Core/GameSessionController.cs` (yalnız ara verme etiketi, hız dizisi ve `:1015` boss mesajı)
  - `Assets/Scripts/Core/GameSessionController.Battle.cs`
  - `Assets/Scripts/Data/FirstRegionCampaign.cs` (boss adı erişimcisi)
  - Yeni `Assets/Tests/EditMode/BossNameTests.cs` ve `Assets/Tests/PlayMode/BattleWaveFlowTests.cs`
  - "Sıradaki" şeridi `BattleChrome.cs` içinde kendi `RectTransform`'uyla kurulur; `DefenderHudResponsiveLayout.cs` WP1'in dosyası, dokunulmaz.
- **Kabul kriterleri:**
  - Ara verme fazında düğme etiketi her saniye azalan sayıyı gösteriyor (test 8→7).
  - Hazırlık fazında şerit dalganın `preview` metnini gösteriyor.
  - Bölüm 10/20/30/40/50'de boss barı doğru adı gösteriyor (boss-name-hud.txt kapıları).
  - 5 HUD kontrolünün TR/EN VoiceOver etiketi boş değil.

### WP3 — Giriş, hesap, ayarlar, harita ve brifing ekranları · **M**
- **Kapsam:** P0-3 (bayrak yolu ya da Emre'nin seçtiği yol), P2-2, P2-4, P2-8, P2-10, bilinen boss rozeti tacı ve brifing kartındaki boss adı (WP2 erişimcisini çağırır).
- **Sahiplik:**
  - `Assets/Scripts/UI/AppFlowController.Screens.cs`
  - `Assets/Scripts/UI/AppFlowController.AccountDeletion.cs`
  - `Assets/Scripts/UI/FrontendPreferences.cs`
  - Yeni `Assets/Plugins/iOS/UiAccessibility.mm`
  - Taç ikonu gerekiyorsa `Assets/Scripts/UI/Kit/UiDesignSystemAssets.cs`
  - Yeni `Assets/Tests/PlayMode/FrontendScreensPolishTests.cs`
- **Kabul kriterleri:**
  - Bayrak kapalıyken karşılama ekranında harici giriş düğmesi aktif değil.
  - Ayarlarda sürüm metni (`Application.version`) görünüyor.
  - Dil düğmeleri "Türkçe / English" yazıyor.
  - Kaydırılabilir alanlarda görsel ipucu var.
  - Brifingde boss çipinde taç ikonu ve bölgeye özgü ad görünüyor.
  - Azaltılmış hareketin varsayılanı sistem ayarından geliyor; editörde `false`.

### WP4 — Ana menü kartı, sonuç ekranı, bölge sonu ve final · **M**
- **Kapsam:** P1-1, P1-7, P1-8, P2-6 (kod tarafı), P2-7. Menü müziği (bilinen P1-6) ürün kararı gelirse bu pakete eklenir (`AppFlowController.cs:138`).
- **Sahiplik:**
  - `Assets/Scripts/UI/AppFlowController.cs`
  - `Assets/Scripts/UI/AppFlowController.CampaignCompletion.cs`
  - Yeni `Assets/Tests/PlayMode/ResultAndCompletionPolishTests.cs`
- **Kabul kriterleri:**
  - Geçerli checkpoint varken ana menü kartı checkpoint bölümünü ve "Dalga N"i gösteriyor; BAŞLAT aynı bölümü açıyor.
  - Zafer ekranında yıldız kuralını anlatan satır var.
  - Bölge sonu kartında bölge görseli ve yıldız çubuğu var.
  - Final kartı 5 bölgeyi ikonlu satırlarla listeliyor.
  - Düğme etiketleri tek harf düzeninde.
  - Profil çipinde kayıt durumu metni var.

### WP5 — Metinler ve yerelleştirme (önce koşar) · **S**
- **Kapsam:** P1-2, P2-5, P2-6 (metin kısmı), WP1–WP4'ün ihtiyaç duyduğu yeni anahtarlar:
  - kilit nedeni
  - geri sayım etiketi
  - "Sıradaki: {0}" şeridi
  - 5 HUD erişilebilirlik etiketi
  - yıldız kuralı satırı
  - "DEVAM · Dalga {0}"
  - kayıt durumu etiketi
  - sürüm etiketi
- **Sahiplik:**
  - `Assets/Scripts/UI/FrontendCopy.cs`
  - `Assets/Scripts/UI/GameLocalization.cs`
  - `Assets/Tests/EditMode/` altındaki metin/taşma testleri (yeni `CopyConsistencyTests.cs`)
- **Kabul kriterleri:**
  - Kilitli bölge metni 10/20/30/40 için ek hatası içermiyor.
  - İngilizcede "Chapter/Level" için tek terim kullanılıyor.
  - Oyuncunun gördüğü metinlerde "Greenwatch" yalnız bölge 1 bağlamında geçiyor.
  - Mevcut TR/EN taşma testleri yeşil.

### WP6 — Görsel kabul matrisi (en son, koordinatör koşar) · **M**
- **Kapsam:** P1-10.
- **Sahiplik:**
  - `Assets/Scripts/UI/AppFlowController.Acceptance.cs`
  - Görüntü yakalayan PlayMode test dosyası ya da dosyaları (ör. `Assets/Tests/PlayMode/FrontendExperienceTests.cs`; koşan testin dosyası)
  - `Builds/uiux-v1.1/**` çıktısı
- **Kabul kriterleri:**
  - Her ekran için (karşılama, menü, harita, kilitli bölge, brifing, düşman kartı, savaş hazırlık/dalga/boss/halka/uzmanlaşma, duraklat, zafer, yenilgi, bölge sonu, final, ayarlar, hesap) şu boyutlarda TR ve EN PNG var: 667×375, 874×402, 956×440 pt ve iPad 1024×1366.
  - `new-enemy` gerçekten düşman kartını gösteriyor.
  - Kısa bir `report.json` var: üst üste binen HUD kutuları ve 11 pt altındaki metin sayısı 0.

---

**Önerilen ilk üç paket:** WP5 → WP1 (P0-1) ve WP2 (P0-2) paralel. WP3'teki P0-3 için önce Emre'nin "bayrak mı, cihaz doğrulaması mı" kararı gerekiyor.
