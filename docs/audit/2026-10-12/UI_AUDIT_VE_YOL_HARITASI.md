# ALLIN: Defense — UI audit'i ve yol haritası (2026-10-12)

Eski audit/TODO belgeleri temel alınmadı. Kaynaklar: bugünkü kod, bugün simülatörden alınan 11 ekran görüntüsü
([screens-before](screens-before/)) ve birbirini görmeden çalışan iki bağımsız UI denetimi
([Claude](ui-claude.md), [Codex](ui-codex.md)). Oyun envanteri: [snapshot.md](snapshot.md).

## Özet

1. **Tasarım sistemi var ama hiçbir ekran kullanmıyor.** Faz 1'de üretilen 9 sprite'ın (yuvarlak köşe, halka, altın gradient) çalışma zamanında 0 kullanıcısı var. Ekranlar hâlâ eski kesik köşeli çerçeveyle çiziliyor (doğrulandı: `grep` → 0).
2. **Görünür hataların çoğu küçük işler.** 7 metin taşmasının 2 kök sebebi var. Türkçe büyük harf hatası var ("YEŞIL"). Son dalgaya boss olmasa da BOSS deniyor (`DefenderHudController.cs:708,733`). Yenilgi ekranında "Sonraki Bölüm" görünüyor. Bunların hepsi ~4 saatte düzelir.
3. **Mimari borç:** menüler mutlak koordinatlarla runtime C# içinde kuruluyor. Ortak bileşen yok, metinler 5 ayrı yerelleştirme mekanizmasına dağılmış, testler ~70-86 nesne adına bağlı. Önce bileşen kiti kurulmazsa yeni ekranlarda aynı taşmalar tekrar çıkar.
4. **Dokunma hedefleri 44 pt'nin altında.** 112 canvas birimi iPhone 17 Pro'da ≈41,7 pt, 375 pt genişlikli telefonda ≈38,9 pt ediyor (`MobileTouchTarget.cs:10`).
5. **Toplam iş ~75-110 saat.** İlk görünür büyük fark (hızlı düzeltmeler + altyapı + yeni savaş HUD'u) ~30-40 saatte çıkar.

## Kararlar (Emre, 2026-10-12)

- **K1 Yön:** menü dikey, savaş yatay. Bugünkü düzen korunuyor; adım 5'teki "+4-10 sa K1=A" eki uygulanmayacak.
- **K2 Seçili kule:** kulenin etrafında halka menü. İnşa ile aynı `RadialMenu` bileşeni kullanılacak, istatistikler sol alttaki bilgi kartında gösterilecek.
- **K3 İnşa:** yuva → ikon (önizleme) → aynı ikona tekrar dokunma (onay). Toplam 3 dokunuş.

## Karar seçenekleri (kayıt için)

| # | Karar | Seçenekler | Önerim |
|---|---|---|---|
| K1 | Ekran yönü | **A)** Her yer yatay. Döndürme kalkar; Kingdom Rush, BTD6 ve Arknights böyle. Maliyet: +4-10 sa. **B)** Menü dikey, savaş yatay (bugünkü hâli, mockup B buna göre). | **A.** Her bölümde telefonu 2 kez çevirmek en büyük sürtünme. Menüler zaten baştan yazılacağı için ek maliyet düşük. Codex B'yi önerdi: tek elle menü kullanımı ve onaylı mockup B'nin korunması. |
| K2 | Seçili kule arayüzü | **A)** Kulenin etrafında halka menü (Kingdom Rush gibi). İnşa ile aynı bileşen, harita kapanmaz. **B)** Mockup A'daki sağ kart. | **A.** Tek `RadialMenu` bileşeni hem inşa hem yükseltme/satış/uzmanlaşma için kullanılır. İstatistikler sol alttaki bilgi kartında gösterilir. |
| K3 | İnşa dokunuşu | **A)** Yuva → ikon (menzil ve bilgi önizlemesi) → aynı ikona tekrar dokun, kurulur (3 dokunuş, KR). **B)** Yuva → ikon, hemen kurulur (2 dokunuş). | **A.** Küçük ekranda yanlış kule kurmayı engeller. Bugünkü akış da 3 dokunuş, yalnızca görünümü değişir. |

Ek ürün kararı: Valor/heroXP kazanılıyor ama harcanacak bir yer yok. UI'da harcanabilir para gibi gösterilmeyecek, döngüsü kararlaştırılınca eklenecek.

## Hızlı düzeltmeler (her biri ≤1 sa, toplam ~4 sa)

| # | İş | Dosyalar | Kabul |
|---|---|---|---|
| H1 | Brifing HEDEF ve sonuç metin kutuları: üstten hizalama, içeriğe göre yükseklik, hedef metni ≤40 karakter | `AppFlowController.Screens.cs:105-146`, `AppFlowController.cs:357`, `AppFlowController.ClosedTest.cs:51-72` | Kabul modunda taşma 0 (TR/EN) |
| H2 | BOSS etiketi son dalga sırasından değil gerçek `bossWave` verisinden gelsin | `DefenderHudController.cs:708,733`, `GameSessionController.Battle.cs` | 1-9. bölümlerin son dalgası normal görünür, 10. bölümde boss |
| H3 | Dil: Türkçe büyük harf yardımcısı ("YEŞİL"), bölge adı dil değişince yenilensin, ayarlarda seçili dil vurgulu açılsın, slider dolgusu | `Screens.cs:200,337,375`, `AppFlowController.cs:616,619,658,868` | TR/EN ayar ve harita görüntüleri doğru |
| H4 | Sonuç ekranı: yenilgide "Sonraki" gizlensin; menü ve zaferdeki "Kaydedildi · Eşitleniyor" teknik metni kalksın; sonuçta HUD gizlensin | `AppFlowController.cs:591-667` | Yenilgi ve zafer görüntülerinde geçersiz düğme ve teknik metin yok |
| H5 | Ana menüdeki üst siyah şerit gradient'e dönsün; haritada tek bölgeyken "1" sekmesi ve "> 1 <" süsü kalksın | `AppFlowController.cs:246-251,541`, `Screens.cs:60-72` | Menü ve harita görüntüleri |

## Yol haritası (sıralı; Unity kilidi nedeniyle kod işleri tek tek)

| Adım | İş | Süre | Kabul kapısı |
|---|---|---|---|
| 0 | K1-K3 kararları | 5 dk | Bu belgeye işlenir |
| 1 | Hızlı düzeltmeler H1-H5 | ~4 sa | Kabul modu yeşil, önce/sonra görüntüler |
| 2 | **Altyapı:** `UiMetrics` (pt↔canvas dönüşümü, 44 pt alt sınır, ana CTA 52-56 pt, yetenek 68-74 pt), `UiId` test kimliği (nesne adı yerine), yerelleştirmenin tek sisteme toplanması (213 satır içi TR/EN çifti → sabit anahtar), kabul modunun aktif dalga/boss/seçili kule/pause/uzmanlaşma ekranlarını da çekmesi | ~10-12 sa | Görüntüler piksel olarak değişmez; testler yeşil; dokunma testi ≥44 pt |
| 3 | **Bileşen kiti `UiKit` + `UiMotion`:** Button (altın/cam/tehlike), IconButton, StatPill, Chip, Card, Switch, Slider, SegmentedControl, ProgressSegments, RadialCooldown, RadialMenu, Modal, BottomSheet, StarRow. Builder'a 12 sistem ikonu eklenir (pause, hız, ayar, harita, kilit, yıldız, ok…). Hem menü hem HUD aynı kiti kullanır; yerleşim mutlak koordinatla değil layout ile yapılır | ~12-16 sa | Bileşen başına EditMode testi (5 durum, ≥44 pt, kontrast); bileşen vitrin görüntüsü |
| 4 | **Savaş HUD'u** (mockup A/C + K2/K3): üst pill'ler, dalga segmentleri, boss can çubuğu (%66/%33 çentikli), ikonlu "sıradaki" çipi, radyal inşa/yükseltme/uzmanlaşma, 74 pt yetenek dairesi + radyal cooldown, hedefe bağlı öğretici oku, 4 eylemli pause | ~20-26 sa | PlayMode HUD testleri; 16:9, 19,5:9 ve iPad'de rota, 8 yuva ve kale görünür; HUD ekranın ≤%18'ini kaplar |
| 5 | **Menüler:** ana menü, harita (madalyon düğüm + 3 yıldız, kilit ikonu), brifing paneli (düşman ikon çipleri), gruplu ayarlar, hesap, sonuç (sırayla düşen yıldızlar + ödül sayacı), yeni düşman tanıtım kartı | ~17-25 sa (+4-10 K1=A) | Taşma 0; FrontendExperience testleri yeşil |
| 6 | **Savaş alanı görsel uyumu:** ışık ve post-process menü paletine (ay mavisi + meşale), zemin doku/ton çeşitliliği, mavi disk yerine taş kaide + altın halka yuva, kenarlarda siyah yerine sis, renk kuralı (düşman sıcak, oyuncu soğuk/altın, zemin soluk) | ~8-14 sa | Önce/sonra görüntü; fiziksel cihazda FPS |
| 7 | **Cila:** ekran giriş/çıkış geçişleri, altın sayacı tick'i, yetersiz altın sallanması, dalga bandı, hafif haptik (iOS), UI sesleri; azaltılmış hareket desteği her yerde | ~6-12 sa | Azaltılmış hareket testi; cihazda elle kontrol |

**İlk görünür büyük fark:** adım 1-4 sonunda, ~45-55 sa. **Tamamı:** ~75-110 sa.

## İki denetçinin ayrıldığı yerler

- **Yön:** Claude her yeri yatay yapmayı, Codex MVP'de bugünkü düzeni korumayı önerdi. Bu K1 kararı.
- **Seçili kule:** Claude halka menüyü, Codex mockup A'daki sağ kartı (ekranın en çok %30'u) önerdi. Bu K2 kararı.
- **Kit mimarisi:** Claude kodla kurulan, layout tabanlı kiti; Codex builder'ın ürettiği prefab'ları ve tipli view referanslarını önerdi. Sentez: kit kodda yazılır, menü onu çalışma zamanında, HUD builder'ı ise prefab üretirken çağırır. Böylece tek kaynak olur, yerleşim iki farklı yerde tutulmaz.
- **Süre:** Claude 56-68 sa, Codex 71-103 sa tahmin etti. Bu belge ikisinin aralığını kullanıyor.

## UI dışı notlar (sonraya)

- **Yayın engelleri:** gizlilik/koşullar URL'leri boş, uygulama içi hesap silme yok, imzalama ayarları boş, crash raporlama/analitik yok.
- **Ad değişikliği sonrası:** Unity kapalıyken temiz derleme ve tüm testler koşulmalı. Builds/'daki son test XML'leri 8 Eylül tarihli. `ProjectSettings.asset:990` alanında `projectName` hâlâ eski adı taşıyor (UGS bulut proje adı).
- **Kod sağlığı:** `GameSessionController.cs` 3014 satır; kullanılmayan 14 prototip dosyası var (Heroes/Units/Enemies/WaveManager).
- **İçerik:** normal akışta 10 bölüm açık, katalogda 50 bölüm var. 3 kule × 2 uzmanlaşma, 6 düşman tipi, 1 boss.
