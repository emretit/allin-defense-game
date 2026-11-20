- [ ] P1 — İçerik/rota sözleşmesini ve sürüm sınırlarını tanımla (2 kişi-gün).
- [ ] P2 — Bölüm bazlı yol desenleri ve sekiz kule yuvasını üret (4 kişi-gün).
- [ ] P3 — 50 bölümün sayı, karışım, giriş ve zamanlamasını işle (4 kişi-gün).
- [ ] P4 — Yol çizimi, savaş önizlemesi, kadraj ve checkpoint uyumunu tamamla (3 kişi-gün).
- [ ] P5 — Yerel/sunucu yıldız kapısı ve eski kayıt haklarını uygula (3 kişi-gün).
- [ ] P6 — Harita, sonuç ve devam akışını TR/EN güncelle (3 kişi-gün).
- [ ] P7 — İçerik, kayıt, bot, cihaz ve okunurluk kabul kapılarını çalıştır (4 kişi-gün).

# İlerleme tasarımı — bağımsız Codex incelemesi

Tarih: 17 Ekim 2026. Kapsam: `.claude/ui-specs/progression/design.txt` uygulanarak hazırlanmış salt okunur tasarım raporu.
Yalnız bu rapor yazıldı; kod/asset değişikliği, Unity, test, simülatör ve commit yok. Diğer ajanın raporu okunmadı.
Kanıtlar kaynak kodu ve JSON okumasıdır; aşağıdaki hedef eğri oynanmış veya dengesi doğrulanmış değildir.

## 1. Bugünkü durum ve teşhis

### Hangi veri gerçekten çalışıyor?

- `Assets/Scripts/Data/CampaignCatalog.cs:45`: 5 × 10 = 50 bölüm; adlar `:49`, boss işareti her onuncu bölümde `:146`.
- Katalogdaki dalga/altın/süre formülleri son değer değildir: `CampaignCatalog.cs:156` JSON'dan dalga sayısı, başlangıç altını ve hedef süreyi yeniden alır. Başlangıç canı katalogdan 20→12 azalır (`:141`).
- `Assets/Scripts/Data/CampaignContent.cs:31` doğrudan `Resources/Campaign/region-XX` yükler; `:100` JSON dalgalarını kopyalar. `FirstRegionCampaign.cs:18` de aynı kaynağın uyumluluk katmanıdır.
- `CampaignLevelFactory.cs:100` bu dalgaları kullanır; dosyadaki `BuildProgressiveWaves` (`:391`) ve `BuildExistingBossWaves` (`:440`) çalışan içerik sanılmamalı: `Create` bunları çağırmıyor.
- Sağlık katalogdaki `difficulty` alanından gelmez; `Assets/Scripts/Core/GameSessionController.cs:2763` JSON sağlık/hız/hasar çarpanlarını uygular. Bir yağmacının temel canı 75, kuşatmanın 620 (`:2752`).
- `Assets/Scripts/Data/TowerProgression.cs:29` tüm mevcut kuleleri ve uzmanlaşma erişimini bölüm 1'e bağlar. Yeni kule kilidi, yıldız harcaması veya kalıcı geliştirme ağacı önerilmiyor; Valor görünür yapılmıyor.

### Bugünkü sayılar

Aşağıdaki her dizi bölge içindeki 1→10 sırasındadır. Düşman toplamı JSON `groups.count` toplamıdır; boss fazlarında eklenen takviyeler hariçtir. Salt veri toplaması yapıldı, oyun simülasyonu yapılmadı.

| Bölge | Dalga sayıları | Planlı düşman toplamları | Sağlık çarpanı |
|---|---|---|---|
| Yeşil Nöbet | 3,3,4,5,6,6,6,6,6,8 | 204,228,340,403,542,549,570,583,599,824 | 1,1,1,1.04,1.08,1.12,1.16,1.20,1.24,1.28 |
| Kül Geçidi | 6,6,6,6,6,6,6,6,6,9 | 588,618,648,669,631,703,742,766,730,1188 | 1.20,1.23,1.26,1.29,1.31,1.32,1.34,1.36,1.35,1.27 |
| Buz Sınırı | 6,6,6,6,6,6,6,6,6,9 | 910,954,1000,1034,988,1065,1095,1130,1083,1593 | 1.5995,1.6159,1.6248,1.6715,1.6692,1.6965,1.7430,1.7853,1.7924,1.3637 |
| Batık Krallık | 6,6,6,6,6,6,6,6,6,9 | 995,1016,1058,1064,1028,1102,1133,1142,1166,2070 | 1.9744,2.0775,2.1612,2.1999,2.2317,2.3178,2.3862,2.5140,2.5689,1.4060 |
| Son Hisar | 6,6,6,6,6,6,7,7,7,8 | 1310,1763,1776,1406,1218,1365,2080,2204,1039,2495 | 2.1333,1.80,1.80,2.3119,2.5235,2.3997,1.89,1.89,3.78,1.89 |

Kaynak başlangıçları: `Assets/Resources/Campaign/region-01.json:5`, `region-02.json:5`, `region-03.json:5`, `region-04.json:5`, `region-05.json:5`; aynı dosyaların final kayıtları sırasıyla `:1473`, `:3309`, `:3442`, `:3400`, `:4313`.

- İlk üç bölüm yalnız yağmacı. Öncü 4, yenilenen 6, şifacı 7, kuşatma 8, boss 10'da görünür (`region-01.json:270`, `:549`, `:738`, `:962`, `:1473`). Mevcut altı tür yeterli.
- B1-9 karışımı 496/54/24/15/10; B1-10 karışımı 656/85/41/26/15 + 1 boss. Sıra: yağmacı/öncü/yenilenen/şifacı/kuşatma.
- B5-48'de 2204 düşmanın 2164'ü yağmacı: %98.2. B5-49'da 1039'un 1022'si yağmacı: %98.4; toplam düşman %52.9 düşerken sağlık çarpanı iki katına çıkar (`region-05.json:3240`, `:3871`). Oyuncunun gördüğü kalabalık, kodun toplam sağlık eğrisiyle aynı şey değildir.
- B14 ikinci yolun ilk örneği: 669 düşmanın yalnız 39'u ikinci yolda (%5.8). B43 üçüncü yolun ilk örneği: 1776'nın 52'si üçüncü yolda (%2.9). Tanıtım için makul, fakat sonrasında güçlü bir görsel büyüme gerekir (`region-02.json:852`, `region-05.json:852`).
- B50'de dağılım 2183/235/77: üçüncü yol planlı ordunun yalnız %3.1'ini taşır. Üç girişin varlığı üç cephe hissini tek başına sağlamıyor (`region-05.json:4313`).
- B1 süre hedefleri 120–180,120–180,180–240,210–270, ardından beş kez 240–360 ve final 360–480 saniye. B2–5 normal 240–420, boss 420–540 saniye. Eski “tüm normaller 4–7, bosslar 7–9 dk” belgesinin B1 istisnaları kodda gerçekten var (`CampaignDifficultyCurveTests.cs:55`). Bunlar hedef; ölçülmüş süre iddiası değildir.
- `GameSessionController.Battle.cs:124` süreyi yalnız dalga sırasında artırır. Menü/hazırlık ve dalgalar arası bekleme dahil gerçek oturum süresi farklıdır.

### Neden amaçsız hissedebilir?

Bu bölüm oyuncu testi sonucu değil, kaynaklardan çıkarımdır: artış çoğunlukla daha uzun aynı sürü ve görünmeyen can çarpanında; rota değişimi geç, bölge hedefi ise görünür bir başarı şartına bağlı değil.
Yıldızlar kaydediliyor ama `CampaignProgressStore.cs:475` sonraki bölümü doğrudan açıyor; `AppFlowController.CampaignCompletion.cs:24` bölge için yalnız önceki bossun kazanılmasını arıyor.
Boss sonu kartı da yıldız eksiği yerine sonraki bölgeyi gösteriyor (`AppFlowController.CampaignCompletion.cs:100`). İlk 13 bölüm tek yol, 14–42 çift yol, 43–50 üç yol (`CampaignLevelFactory.cs:107`).
Mevcut öğrenme hedeflerini ve taktik haritayı yok saymamak gerekir: brifing dalga/süre/tür adetlerini zaten gösteriyor (`AppFlowController.cs:482`); eksik olan bunları sonraki bölge hedefiyle ve somut rota değişimiyle ilişkilendirmek.

## 2. Yıldız kapısı: öneri 20/30, her bölgede aynı

Yeni oyuncuda sonraki bölge = önceki bölgenin 10 bölümünü tamamla + bossu yen + o bölgede en az 20/30 yıldız. Kapılar B11/B21/B31/B41'de; B50 sonrası yeni kapı yok.
Kümülatif eşik önermiyorum: ilk bölgeden fazla yıldız biriktirerek yeni bölgenin dersini atlamayı önler ve “bu bölgedeki hedefim” anlaşılır kalır. Yıldızlar harcanmaz.
20, bölüm başına ortalama iki yıldızdır; on bölümün hepsini üç yıldız yapmayı istemez. Boss 1 yıldızsa diğer dokuzda toplam 19 yeterli; bossun üç yıldız olması şart değil.
Mevcut yıldız hesabı korunur: kalan can ≥%75 → 3, ≥%40 → 2, pozitif → 1 (`Assets/Scripts/Combat/BattleRules.cs:13`). B1'de 15/20 can üç yıldız, 8/20 iki yıldızdır.

Harita üstünde “Yeşil Nöbet: 16/20 ★ · Kül Geçidi'ne 4 yıldız kaldı” ve ayrı “Bölge lordunu yen” durumu gösterilsin. Boss tamamlandıysa durum işaretli kalsın; eksik yıldız yüzünden kazanılmış boss kaybedilmiş görünmesin.
En fazla üç tamamlanmış, üç yıldızın altındaki bölüm önerilsin; önce iki yıldızlı ve üç yıldız can sınırına en yakın olanlar, sonra diğerleri. Sıralama mevcut `bestLives/startingLives` oranından yapılabilir; “kolay” veya garantili başarı iddiası yapılmasın.
Örnek kart: “Dar Patika · 2/3 ★ · 3 yıldız için en az 15 can”; düğme “Tekrar oyna”. Eşik için 1→2 iyileştirmesi de işe yarar; liste bunu dışlamasın.
Sonuçta kazanılan yıldız ile rekor artışı ayrılmalı: “Bu deneme 2 ★ · Rekorun 3 ★ · Bölge hedefi 16/20”. Aynı sonucu tekrarlamak kapı toplamını artırmaz (`CampaignProgressStore.cs:472`).
Eşik altındaki boss sonucunda ana eylem “Yıldızları tamamla”, ikincil eylem “Harita”; eşik doluysa “Kül Geçidi'ne geç”. Boss öncesi 20 yıldız toplansa da boss şartı kalır.
Eski bölümü iyileştiren her sonuçtan sonra kapı yeniden hesaplanmalı; bossu ikinci kez oynamak gerekmemeli. “Devam et” kapı altında önerilen eksik yıldıza, kapı açılınca sıradaki bitmemiş bölüme gitmeli.
`NextUnfinishedLevel` bugün tüm açık aralıkta en düşük yıldızlı bölüme dönebiliyor (`CampaignProgressStore.cs:212`); kapıya katkı yapacak bölgeyle sınırlandırılmış öneri gerekir. Tamamlanmış 150/150 kayıt için “Kampanya tamamlandı” gösterilsin, eksik yıldız varmış gibi davranılmasın.

### Eski kayıt ve sunucu uyumu

- Yerel kayıt sürümü 5 (`CampaignProgressStore.cs:74`), istemci kuralı `campaign-rules-v2` (`CampaignProgressionContracts.cs:71`). Öneri: yeni şema 6 ve yeni kapı kuralı v3; içerik sürümleri bunlardan ayrıdır.
- Geçişte `unlockedLevel`, tamamlanmış bölümler ve önceki boss kaydından erişilmiş bölgeleri türeten kalıcı `grandfatheredRegions` hakkı saklansın. Örneğin `unlockedLevel=24` ise bölgeler 1–3 açık kalır; B25–30 sıralı ilerler, B31 için yeni kapı uygulanır.
- Önceden B11'e erişmiş 12 yıldızlı oyuncu tekrar kilitlenmez. Korunan bölge rozeti “Açık”; yıldız hedefi isteğe bağlı tamamlama göstergesidir. Geçiş aynı kayıt için tekrarlandığında aynı sonucu vermeli.
- Yalnız UI kilidi değiştirmek yetmez: kayıt `ApplyCompletion`, başlatma/devam/checkpoint erişimi ve sunucu aynı kapı kararını kullanmalı. Sunucu bugün `claim.Level > state.UnlockedLevel` kontrolü ve koşulsuz `+1` yapıyor (`CampaignProgressionCCM/CampaignProgression/CampaignProgressionModule.cs:211`, `:224`).
- Sunucu yanıtı istemcinin `unlockedLevel` değerini doğrudan değiştiriyor (`CampaignProgressStore.cs:309`); eski hak sunucuda da taşınmazsa senkronizasyon bölgeyi kapatabilir. `MergeFrom`/misafir aktarımı/çoklu cihaz birleştirmesinde bölge hakkı korunmalı; profiller arasında kendiliğinden kopyalanmamalı.
- v2 bekleyen talepleri v3 sunucu geçişinde önce eski sıralı kuralla işle, doğan erişim haklarını sabitle, ardından v3 uygula; aksi halde çevrimdışı kazanılmış B11 talebi geriye dönük reddedilebilir. Sunucunun bugün yalnız v1→v2 istisnası var (`CampaignProgressionModule.cs:200`). Bu uyumluluk ayrı test şartıdır.
- Yerel eski erişim sunucu incelemesi beklerken oynanabilir kalmalı; bu hak doğrulanmadan sunucuda sınırsız yeni erişime çevrilmemeli. Mevcut `migrationNeedsReview` davranışıyla birlikte tasarlanmalı (`CampaignProgressStore.cs:294`).

## 3. Önerilen 50 bölüm eğrisi

Hedef: her 2–3 bölümde oyuncunun kararını değiştiren yeni durum. Aşağıdaki değerler ilk dengeleme girdisidir; mevcut JSON'un yerine doğrudan güvenli kabul edilemez. Yeni düşman tipi yok.
Sayı, sağlık çarpanını katlayarak değil, görünen paket büyüklüğü/giriş oranı ve karışımla artacak. Bölge içi küçük nefes bölümleri bilinçlidir; yeni geometri dersinde aynı anda sağlık artışı uygulanmayacak.
Süre önerisi: mevcut B1-1..4 kısa eğitim aralıklarını koru; B1-5..9 ve diğer normaller 4–7 dk, tüm bosslar 7–9 dk. B10'un 6–8'den 7–9'a geçişi açık tasarım değişikliğidir; onay konusu aşağıda.

**Karışım anahtarı:** yüzdeler sırasıyla Yağmacı/Öncü/Yenilenen/Şifacı/Kuşatma; sayıya çevirirken özel türleri aşağı yuvarla, kalanı yağmacıya ver. Boss satırında toplamın içinde 1 boss ayır, yüzdeleri kalanlara uygula.

| Kod | Yüzdeler | Görünür amaç |
|---|---|---|
| A | 100/0/0/0/0 | Saf sürü, yol ve hedefleme öğrenme |
| B | 94/6/0/0/0 | Sürünün önünde zırhlı grup |
| C | 90/7/3/0/0 | Yenilenenleri birlikte durdurma |
| D | 89/7/2/2/0 | Şifacı eşliğinde konvoy |
| E | 88/7/2/2/1 | Az sayıdaki kuşatmayı fark etme |
| F | 84/11/2/2/1 | Zırhlı öncü ağırlığı |
| G | 90/5/3/1/1 | Yağmacı akını + geciken yenilenen |
| H | 88/7/1/3/1 | Şifacıyı farklı hatta izleme |
| I | 87/8/1/1/3 | Kuşatma yönüne hazırlık |

Özel tiplerin aynı anda sahnede olma adedi ayrıca sınırlanacak; yüzdeler bölüm toplamıdır. Şifacıları tüm dalgalara eşit yaymak yerine anlamlı konvoylarda, kuşatmaları belirli pencerelerde sun.
Yol kodları: S tek kıvrımlı; U tek uzun dönüşlü; Y erken birleşen çift; A2 dalga/paket sırayla gelen çift; L2 geç birleşen çift; X2 tek fiziksel girişten ayrılıp birleşen iki rota; T3 üç girişin sırayla açılması; F3 üç girişin kademeli eşzamanlı baskısı. Ayrıntılar bölüm 4'te.

| Bölüm | Yol/giriş varyasyonu | Dalga | Planlı toplam | Karışım | Oyuncunun fark edeceği yenilik |
|---|---|---:|---:|---|---|
| 1 | S, sol | 3 | 204 | A | Tek girişte ilk savunmamı kuruyorum. |
| 2 | U, sol | 3 | 230 | A | Aynı sürüye ikinci virajda yeniden vurabiliyorum. |
| 3 | Y, 2. giriş yalnız son dalga | 4 | 340 | A | Yeni giriş küçük ve önceden haber verilen bir akın getiriyor. |
| 4 | Y, iki giriş 80/20 | 5 | 400 | B | Ön sıradaki zırhlılara farklı kuleyle cevap veriyorum. |
| 5 | A2, girişler sırayla | 6 | 480 | B | Savunmamı iki giriş arasında kaydırıyorum. |
| 6 | A2, geciken yenilenen | 6 | 540 | C | Sürüden sonra gelen dayanıklı gruba gücümü saklıyorum. |
| 7 | L2, şifacı tek kolda | 6 | 580 | D | İki kol geç birleştiği için şifacıyı erken seçiyorum. |
| 8 | L2, kuşatma diğer kolda | 6 | 620 | E | Kuşatma için ayrı bir savunma noktası kuruyorum. |
| 9 | Y, erken birleşme; nefes | 6 | 600 | E | Öğrendiğim beş tipi tanıdık boğazda karşılıyorum. |
| 10 | A2, boss ana girişte | 8 | 850 | E+boss | Bölge lordu iki giriş dersini sınava dönüştürüyor. |
| 11 | Y, sol+üst | 6 | 620 | F | Yeni bölgede zırhlı gruplar daha görünür oluyor. |
| 12 | A2, konvoy giriş değiştirir | 6 | 680 | F | Şifacılı konvoyu her dalgada başka yönde karşılıyorum. |
| 13 | X2, aynı kapı iki kol | 6 | 740 | C | Tek kapıdan çıkanlar yol ayrımında ikiye bölünüyor. |
| 14 | X2, kollar 60/40 | 6 | 780 | D | Şifacılı kolu sürü kolundan ayırıyorum. |
| 15 | Y, nefes | 6 | 750 | F | Azalan baskıda zırhlılara karşı kurulumumu iyileştiriyorum. |
| 16 | L2, sol+alt | 7 | 840 | F | Konvoylar daha geç birleşiyor. |
| 17 | L2, kuşatma yan kolda | 7 | 900 | I | Kuşatmayı son boğaza bırakmadan durduruyorum. |
| 18 | A2, iki paket örtüşür | 7 | 960 | H | İkinci giriş açılırken ilk konvoy hâlâ yolda oluyor. |
| 19 | Y, nefes | 7 | 920 | E | Kalabalık azalıyor, son savunmamı prova ediyorum. |
| 20 | L2, boss+yan refakat | 9 | 1250 | F+boss | Lord ana koldayken yan refakati ayrı tutuyorum. |
| 21 | Y, iki farklı yol uzunluğu | 6 | 850 | G | İki girişin kaleye varış zamanı farklılaşıyor. |
| 22 | A2, hızlı önce | 6 | 900 | G | Önce yağmacıları, sonra ağır grubu karşılıyorum. |
| 23 | X2, kısa kol gecikmeli | 6 | 980 | C | Farklı uzunluktaki kollar aynı anda boğaza ulaşıyor. |
| 24 | X2, kısa kolda yağmacı | 7 | 1030 | G | Kısa kolun baskısı karar süremi daraltıyor. |
| 25 | Y, nefes | 7 | 1000 | E | Tek birleşme noktasında zamanlamamı düzeltiyorum. |
| 26 | L2, zırhlı önce | 7 | 1100 | F | Zırhlılar yolu tutarken yağmacılar arkadan yetişiyor. |
| 27 | L2, gelişler eşlenir | 7 | 1160 | H | Şifacı ile koruduğu grup birleşmede buluşuyor. |
| 28 | A2, paket arası kısalır | 7 | 1220 | G | İki yöndeki kısa arayı doğru kullanıyorum. |
| 29 | Y, nefes | 7 | 1180 | E | Boss öncesi bildiğim zamanlamayı yeniden uyguluyorum. |
| 30 | X2, boss uzun kolda | 9 | 1500 | G+boss | Lord yaklaşırken kısa kolu gözden kaçırmıyorum. |
| 31 | A2, sol+üst | 7 | 1000 | H | Yeni kıyıda baskı yönü her pakette değişiyor. |
| 32 | L2, şifacı ayrı kolda | 7 | 1060 | H | Şifacıyı konvoyla birleşmeden yakalıyorum. |
| 33 | T3, üçüncü giriş küçük | 7 | 1140 | E | Üçüncü kapı ilk kez kısa bir keşif akını açıyor. |
| 34 | T3, 1→2→3 | 7 | 1200 | I | Kuşatmanın hangi kapıda olduğunu takip ediyorum. |
| 35 | A2, üçüncü giriş yok; nefes | 7 | 1160 | E | İki cepheye dönüp savunmamı toparlıyorum. |
| 36 | F3, üçüncü giriş gecikmeli | 8 | 1270 | H | Üçüncü cephe açılmadan yerleşimimi hazırlıyorum. |
| 37 | F3, dalgada özel tip değişir | 8 | 1330 | C/H dönüşümlü | Bir dalgada yenilenen, sonrakinde şifacı grubuna hazırlanıyorum. |
| 38 | X2, aynı kapıdan kuşatma kolu | 8 | 1400 | I | Çok giriş yerine yol ayrımıyla sınanıyorum. |
| 39 | T3, ayrık paketler; nefes | 8 | 1350 | E | Üç kapıyı örtüşmeyen akınlarla prova ediyorum. |
| 40 | F3, boss ana hatta | 9 | 1750 | H+boss | Lord ve iki yan cepheyi birlikte yönetiyorum. |
| 41 | L2, iki kol | 7 | 1200 | F | Son bölgede zırhlı baskıyı tanıdık düzende çözüyorum. |
| 42 | T3, 1→3→2 | 7 | 1280 | G | Bildiğim üç girişin geliş sırası değişiyor. |
| 43 | F3, 50/30/20 | 8 | 1380 | H | Üçüncü kol artık küçük bir gösteri olmaktan çıkıyor. |
| 44 | F3, kuşatma yönü değişir | 8 | 1450 | I | Ağır hedefi her dalgada başka cephede görüyorum. |
| 45 | Y, nefes | 8 | 1400 | E | Son saldırılar öncesi tek boğazda toparlanıyorum. |
| 46 | X2, iki kol eşzamanlı varır | 8 | 1510 | F | Aynı kapının iki kolunda hedef önceliği seçiyorum. |
| 47 | T3, şifacı en son | 8 | 1590 | H | Şifacı gelmeden önce öncü konvoyu dağıtıyorum. |
| 48 | F3, 40/30/30 | 8 | 1660 | I | Üç cephenin de ciddi savunma istediğini görüyorum. |
| 49 | A2, nefes ve final provası | 8 | 1600 | E | Sayı azalırken bildiğim kararları son kez deniyorum. |
| 50 | F3, boss 1; takviye 2/3 | 9 | 1950 | E+boss | Bütün yol ve hedef derslerini son lordda birleştiriyorum. |

**Bölge farkları ve dalga üretim kuralları:**
- B37'de tek dalgalar C, çift dalgalar H temelini kullanır; C dalgalarında şifacı zaten yoktur, H dalgalarındaki %1 yenilenen payı yağmacıya aktarılır. Böylece iki özel tip aynı dalgada birleşmez; bölüm toplam karışımı dalga adetleriyle ağırlıklandırılır.
- B1 temel tipleri tanıtır; B2 zırhlı konvoy/ayrılma; B3 varış zamanlaması; B4 üçüncü cephe; B5 bütün derslerin daha dengeli giriş paylarıyla birleşmesi. Yeni desen tanıtımı B3/B13/B33'tedir; mevcut 14/43 eşikleri bilinçli değişir.
- Normal çift yolda başlangıç dağılımı 60/40, üçlüde 50/30/20; özel satırlar bunu değiştirir. B3 ve B33 giriş tanıtımında yeni yol bölüm toplamının yaklaşık %10'unu, yalnız son dalgalarda taşır; takip eden iki bölümde pay artar.
- A2/T3 paketleri yeni yol dersinde örtüşmez; ustalık satırlarında gecikmelerle örtüşür. `startDelay` dalga başlangıcına göredir; kuyruk bitişi `startDelay+(count-1)*interval` ile hesaplanmalı.
- Başlangıç dalga payları artan ağırlıkla dağıtılır (1.0→1.3, normalize edilerek); boss satırlarında son dalgaya tek boss + bölümün en fazla %15'i normal düşman ayrılır. Diğerleri önceki dalgalarda artan ağırlıkla dağılır. Böylece final tek dev sürü duvarı olmaz.
- Tablo **planlı** toplamı verir; gerçekleşen toplam = tablo + tetiklenen boss takviyeleri. `GameSessionController.Battle.cs:176` takviye sayısını rota sayısıyla da çarpabiliyor. P3 her boss için faz başına/adet üst sınırını ayrı kayıt edip toplam ve ≤60 kapısını buna göre doğrulamalı; bu rapor takviyeleri sıfır saymıyor.
- Çoklu yol eklemek toplam düşmanı otomatik katlamaz; aynı bütçe kollara bölünür. 20/30 kapısı karşısında iki aktif botun bölge yıldızı da ölçülür; tek tek zafer almak kapıyı geçmeye yetmeyebilir.
- Sağlık çarpanı, başlangıç altını ve öldürme ödülleri birlikte ayarlanır. Bugünkü B5 altını 1000→2100 (`region-05.json:5`, `:2644`); sayı azaltırken bunu aynen tutmak aşırı kolaylık yaratabilir. İlk prototip mevcut ekonomiyle ölçülür, sonra paket başına gelir ve toplam gelen sağlık raporuyla ayarlanır; rastgele can şişirme yok.

## 4. Yol desteği, tek yol varsayımları ve desen kataloğu

### Bugün çalışan destek

`RuntimeCampaignLevel` hem `path` hem `routes` taşır (`CampaignLevelFactory.cs:8`); `path`, ilk rotanın kopyasıdır (`:96`). `BuildRoutes` beş `RegionOnePaths` şeklinden seçer, küçük kaydırmalar ve çift numaralı bölgelerde `MirrorPath` uygular (`:185`, `:650`). Bu, 50 ayrı topoloji değildir.
İkinci/üçüncü rota soldan, yer yoksa üst/alt kenardan girer; ana rotaya tek dik T kavşağıyla bağlanır ve aynı son yolu/kale ucunu paylaşır (`:221`, `:308`). Merkez çizgileri arası 2.4 birim, giriş arası 3.5 birim, kavşak-köşe açıklığı 1.4 birim (`:210`).
Spawn grubu `routeId` dalga zamanlayıcısından düşmanın kendi yoluna gider (`GameSessionController.Battle.cs:91`, `GameSessionController.cs:946`). Bir dalgada farklı girişler, farklı tipler ve gecikmeli paketler zaten mümkün. Çift yol için yeni hareket sistemi gerekmez.
Kışla yakın yolu arıyor (`GameSessionController.cs:2430`); rota bazlı ilerleme/kaleye kalan mesafe yardımcıları da var (`:2714`). Taktik harita tüm rotaları çizer ve sınırlarını hesaba katar (`FrontendTacticalMapPreview.cs:53`, `:85`).

### Gerçek sınırlamalar

- `entranceCount` bugün rota sayısıyla eş anlamlı (`GameSessionController.cs:75`); X2 gibi **bir fiziksel giriş, iki rota** durumu ayrı modellenmiyor. `routeId` korunmalı, ayrıca `entranceId` ve giriş→rota listesi eklenmeli. Doğrulama rota sayısına bakmalı; bugünkü `CampaignContent.cs:91` giriş sayısıyla sınırlar.
- Yol üretimi ayrılıp tekrar birleşme ve birleştikten sonra yeniden ayrılmayı üretmiyor. Tam rota nokta dizileri hareketi taşıyabilir; ancak ortak segment çizimi değişmeden güvenli değil.
- `OwnedBranchSegments` ilk ortak segmentte dalın çizimini bitirir (`CampaignLevelFactory.cs:375`). X2'nin ortak başlangıcı sonrası ayrılan kısmı görünmeyebilir. Dünya çizimi bunu kullanıyor (`GameSessionController.cs:543`); tüm yolların yönsüz ortak segmentlerini bir kez çizen çözüm gerekir.
- Sekiz yuva yalnız ana yol boyunca aday seçer; diğer yollar sadece açıklık ve küçük kapsama puanına katılır (`CampaignLevelFactory.cs:481`, `:513`, `:529`). Yan yolun bağımsız savunulabileceği garanti edilmez. `SlotRouteClearance` 1.58–1.95 ve kale/giriş boşlukları korunmalı (`:472`).
- B10 sabit tek yol/sekiz eski yuva istisnasıdır (`CampaignLevelFactory.cs:170`, `:187`). Önerilen çift yollu B10 için bu istisna da kaldırılmalı veya yeni harita sürümüyle ayrılmalı.
- Guardian/kale referansı ana yol sonuna bağlı (`CampaignLevelFactory.cs:621`, `GameSessionController.cs:1012`). Tüm yeni rotalar aynı kaleye bitsin; çok kale bu işin kapsamında değil.
- Dalga başlangıcı sunum işareti `path[0]` kullanır (`GameSessionController.cs:930`). Giriş okları bütün yollar için hazırlanır (`:644`), dalga başında tümü kapanır (`:918`); dalga bazlı gerçek aktif giriş uyarısı sayılmaz.
- “Sıradaki” şeridi hazır (`GameSessionController.cs:444`), fakat B11+ önizlemesi en yüksek enum tipinin genel adına indirgenir; giriş/adet/zaman göstermez (`CampaignContent.cs:100`). Aktif HUD şeridi yalnız hazırlık/ara sırasında görünür ve üst alan genişliğinin %40'ıyla sınırlanır (`DefenderHudController.WaveForecast.cs:82`); üç giriş kartı bu alana ölçmeden eklenmemeli. Mevcut oklar var diye bu ihtiyaç karşılanmış kabul edilmemeli.
- Kamera safe-area destekler, fakat rotalardan dinamik sınır almaz; 29.6 dünya birimi hedef genişlik ve sabit asgari yükseklik kullanır (`AdaptiveBattlefieldCamera.cs:12`, `:72`). Üst/alt giriş ve etiketlerin HUD altında kalması cihazda doğrulanmalı.

### Desenler ve üretim

Tüm desenlerde tek kale ve mevcut dünya zarfı korunur; rastgele sınırsız arama yerine bölüm planındaki desen/ayna/giriş kenarı/merge fraction kullanılır. Aynalama yeni desen diye sayılmaz.

| Desen | Üretim yaklaşımı | Sekiz outpostSlots dağılımı | Yatay iPhone riski | Önceden gösterim |
|---|---|---|---|---|
| S — tek kıvrım | Mevcut beş patikadan az dönüşlü şablon; tek tam rota | Yol başı/orta/sonda 2/4/2 | Düşük; aynı çizgiye yapışan etiket | “Giriş 1 · Yağmacı ×N” ve 1 numaralı ok |
| U — uzun dönüş | Sabit U şablonu; paralel parçalar arası ≥2.4 | İç dönüşte 4, dış uçlarda 2+2 | Paralel düşmanlar birbirine karışabilir | U çizgisi kısa süre parlar; tek giriş etiketi |
| Y — erken çift birleşme | Mevcut branch aramasında ana yolun %30–40'ında hedef birleşme | Her girişte 2, ortak kolda 4 | Kavşakta kule/etiket yığılması | “1 + 2 → ortak hat”, iki ayrı tip/adet kartı |
| A2 — sırayla çift | Y geometrisi + giriş bazlı startDelay; sol/üst veya sol/alt | 2+2 giriş, 4 ortak | Üst giriş HUD'a yaklaşabilir | “Önce 1; sonra 2, +12 sn”; yalnız sıradaki giriş parlar |
| L2 — geç çift birleşme | Ana yolun %65–75'inde birleşen, daha sade iki rota; arama başaramazsa onaylı sabit şablon | Her bağımsız kolda 3, ortak kolda 2 | Küçük ekranda arada yuva için yer kalmayabilir | “1 ve 2 · geç birleşme”; iki yol farklı çizgi deseni |
| X2 — tek giriş, ayrıl/birleş | Ortak başlangıç + iki ayrı nokta listesi + ortak son; routeId 0/1 aynı entranceId | Ayrım öncesi 2, her kolda 2, birleşim sonrası 2 | Üst üste çizim, tek kapıya iki numara basma | Tek kapı rozeti, kol A/B; “1→A %60 / B %40” |
| T3 — üç giriş sırayla | Üç sade tam rota; mümkünse sol/üst/alt; ana hatta iki ayrı birleşme | Her girişte 2, son ortak hatta 2 | Üst/alt etiketler HUD/çentik altında kalabilir | “1→2→3”, aktif sıranın süre ve adetleri |
| F3 — üç cephe | T3 geometrisi; 8–12 sn aralıklarla örtüşen paketler, farklı birleşme konumları | 2+2+2 bağımsız, 2 ortak | En yüksek yoğunluk; bütün yolu küçültmek dokunmayı bozar | “1 şimdi · 2 +8 sn · 3 +16 sn”, tipi gösteren sabit rozet |

Bu dağılımlar yerleşim hedefidir; yalnız slot sayısını sağlamak yeterli değil. Her bağımsız kolda erişilebilir en az iki yuva ve yükseltilmemiş menzille ölçülen kapsama olmalı; yola taşma ve kuleler arası mesafe test edilmeli. Sığmayan desen yayımlanmaz, sessizce tek yola düşmez.
X2'de hareket sırasında rastgele yön seçimi yok: düşman spawn anında grubun tam rotasına atanır. Böylece yeniden oynama ve checkpoint deterministik kalır.
“Sıradaki” en fazla üç giriş kartı göstersin: giriş numarası/kol harfi, baskın tip, adet ve gecikme. Renk tek başına anlam taşımasın; sayı/ok/desen eşleşsin. İlk tanıtımda otomatik dalgadan önce mevcut hazırlık ekranında bu bilgi okunabilsin.
Boss takviye penceresi önizlemeye dahil olsun; tetiklenen fazda ilgili giriş tekrar işaretlensin. Harita brifingindeki yol ile savaş çizgisi aynı veri modelini kullansın.

## 5. Uygulama iş paketleri ve bağımlılıklar

Aşağıdaki sahiplik **önerilen gelecekteki uygulama** içindir; bu incelemede hiçbir paket uygulanmadı. Bir dosyanın tek yazarı vardır; başka paketin bulduğu düzeltme dosya sahibine gider. Tüm test dosyalarının yazarı yalnız P7'dir.

| Paket | Hedef ve yalnız bu paketin yazacağı dosyalar | Kabul/test kapısı | Süre |
|---|---|---|---:|
| P1 | Şema/route–entrance ayrımı ve içerik sürümü sözleşmesi: `Assets/Scripts/Data/CampaignContent.cs`, `CampaignCatalog.cs`, `FirstRegionCampaign.cs`, `GreyboxDefinitions.cs`; yeni saf `CampaignLayoutPlan.cs` | 50 bölüm şeması; geçersiz routeId/entranceId reddi; catalog→content→factory döngüsel başlatma olmaması; JSON ve önizleme aynı kaynaktan | 2 gün |
| P2 | S/U/Y/A2/L2/X2/T3/F3 ve yuvalar: yalnız `Assets/Scripts/Data/CampaignLevelFactory.cs` | Deterministik 50 harita, tek ortak kale, tüm rotalar geçerli; açıklık/yuva/kapsama; X2 giriş≠rota; B10 istisnası çözülmüş | 4 gün |
| P3 | Sayı/karışım/zamanlama/ekonomi: yalnız `Assets/Resources/Campaign/region-01.json`…`region-05.json`, `boss-profiles.json` | Tablonun toplamları; özel tip tanıtımı; boss takviye üst sınırı; 4–7/7–9 ve eğitim istisnaları; test sürüm sabitleri için P1/P7'ye bildirim | 4 gün |
| P4 | Runtime giriş modeli, ortak yol çizimi, savaş önizlemesi ve checkpoint: `Assets/Scripts/Core/GameSessionController*.cs`, `Assets/Scripts/Presentation/AdaptiveBattlefieldCamera.cs`, `DefenderBattlefieldDressing.cs`, `DefenderHudController*.cs`, `DefenderHudResponsiveLayout.cs` | Aynı rota geometri/çizim/ok; aktif giriş zamanlaması; güvenli alan; içerik değişince eski dalga checkpoint'i kontrollü yeniden başlatma | 3 gün |
| P5 | Kapı ve göç: `CampaignProgressStore.cs`, `CampaignProgressionContracts.cs`, yeni saf kapı kuralı; `CampaignProgressionCCM/CampaignProgression/CampaignProgressionModule.cs`, `.csproj`, `Assets/Resources/LiveGame/campaign-rules-v3.json` | İstemci/sunucu 19/20/21 yıldız ve boss durumlarında aynı karar; eski bölge korunur; v2 bekleyen talep/misafir/çoklu cihaz/idempotent tekrar; yeni kural yayın sırası | 3 gün |
| P6 | Harita/sonuç/devam ve TR/EN: `Assets/Scripts/UI/AppFlowController*.cs`, `FrontendTacticalMapPreview.cs`, `FrontendCopy.cs`, `UI/Copy/FrontendCopy.Progression.cs` (yeni) | Kapı altı önerileri; bossu tekrar istemeden açılma; eski açık bölge; 150/150 tamamlanma; TR/EN metin sığması, gerçek başlangıç yetkisiyle UI tutarlılığı | 3 gün |
| P7 | Yalnız `Assets/Tests/**`, `CampaignProgressionCCM/CampaignProgression.Tests/**` ve ayrı kabul raporları | EditMode içerik/geometri/göç, PlayMode checkpoint/akış, 50×4 RegionBalance ve cihaz SetPass/kadraj; sonuçları P1–P6 sahiplerine geri gönder | 4 gün |

P1 plan verisini tanımlar; P2 geometriyi ve P3 içeriği onu tüketerek geliştirir. P4, P1/P2 çıktılarına; P6, P4 önizleme ve P5 kapı API'sine bağlıdır. P5 kendi kayıt/sunucu hattında ilerleyebilir. P7 sözleşme testlerini erkenden yazar, son ölçümü bütünleşme sonrası yapar.
P4 satırındaki kısaltılmış renderer/HUD dosyaları `Assets/Scripts/Presentation/` altındadır; `DefenderBattlefieldDressing.cs:54` ve `:148` de ortak segment varsayımını kullanır. P6 yalnız frontend'e yazar. P3'ün içerik sürümü isteği P1'in sabit değişikliğini tetikler, dosyayı kendisi değiştirmez.
Tahmin: **7 paket, toplam 23 kişi-gün**, beklenen aralık 19–27. İki uygulayıcı ve bir test/denge sorumlusuyla bağımlılıklar dahil yaklaşık 12–15 iş günü; cihaz erişimi ve ciddi yeniden dengeleme bu takvimi uzatabilir. Bu rapor paralel iş başlatmıyor.
Önerilen teslim sırası: önce 1–10 dikey kesit (iki giriş + kapı); sonra X2 içeren B2/B3; sonra T3/F3 içeren B4/B5; en son bütün kampanya ve kayıt uyumu kapısı. Sunucu v3 uyumluluğu istemciden önce hazır olmalı; bu rapor yayın/deploy yapmaz.

## 6. Riskler ve yayıma çıkış kapıları

1. **En büyük risk: sayı/karışım/rota/ekonomi birlikte değişince denge.** Eski bot başarısı yeni eğriyi onaylamaz. `Assets/Tests/PlayMode/CampaignBalanceTests.cs:60` her bölümde iki aktif, üç saniye gecikmeli ve pasif okçu planı çalıştırır; aktiflerde can/süre, yavaşta ≥1 yıldız, bosslarda pasif yenilgi bekler (`:76`). 20/30 kapısı için ayrıca her aktif stratejinin bölge toplamı ≥20 olmalı; yavaş oyuncunun seçili tekrarlarla kapıyı geçebildiği insan testinde görülmeli.
2. **İkinci büyük risk: eski erişimin senkronizasyonda kaybolması.** Yalnız v5→v6 yerel göç yetmez; sunucu şeması, bekleyen eski talepler ve misafir aktarımı aynı kuralla ilerlemeli. B11/B21/B31/B41 önceden açıkken düşük yıldızlı kayıt, eski checkpoint ve farklı cihazdan eski profil dönüşü temel senaryolardır.
3. `CampaignDifficultyCurveTests.cs:26` nefes bölümlerinin toplam sağlık ilişkisini, `:71` ve `:147` mevcut 14/43 giriş tanıtımını sabitler. Bunlar yeni tasarımla bilinçli değişecek; testleri sadece yeşile çevirmek yerine yeni desen/tanıtım sözleşmesini doğrula. Nefes bölümünde hem toplam sağlık hem en yoğun pencere azalsın; 49'daki sağlık ikiye katlama tekrarlanmasın.
4. ≤60 düşman bugün spawn sırasında zorlanan sınır değil, test kabulüdür: zamanlayıcı zamanı gelen hepsini çıkarır (`GameSessionController.Battle.cs:126`), spawn yalnız tepe adedini kaydeder (`GameSessionController.cs:962`). Her girişin ayrı ayrı 60 olması yanlış; bütün sahne + boss takviyesi birlikte ≤60. İlk tasarım hedefi normalde ≤45, bossta ≤55 tepe; 60 sert kabul kapısı. Süre uzatma/başlatmayı sessizce geciktirme yerine planlı paketleri ayarla.
5. SetPass için mutlak bir bütçe bu incelemede doğrulanmadı. Mevcut `Assets/Tests/PlayMode/BattlefieldLookTests.cs:51` biyom karşılaştırmasında +3 sınırı tanımlar ve `:215` kontrol eder. Yeni yollar/oklar mevcut ortak materyalleri kullanmalı; aynı cihaz/sahneyle önce-sonra ölçüm, bu +3 kapısı ve cihaz kare süresi raporu istenmeli. Bot testi çizim performansının yerine geçmez.
6. Checkpoint yalnız dalga başını, kule yuva indekslerini ve kahraman konumunu tutar (`GameSessionController.Checkpoints.cs:27`, `:49`). Yeni haritada aynı slot indeksi farklı yere gider. Yol/yuva değişse bile bölge `contentVersion` yükseltilmeli; mevcut sürüm eşitliği kontrolü (`:96`) bu şekilde eski savaşın devamını engeller, kampanya yıldızlarını silmez (`:67`). Salt Factory değişikliğinde sürüm unutulması özellikle risklidir.
7. X2 için rota sayısı≠giriş sayısı ayrımı API, çizim, ok, validator ve boss `reinforcementRoute=all` davranışında birlikte ele alınmalı. “all” fiziksel giriş mi rota mı sorusu P1'de açık tanımlansın: öneri fiziksel giriş başına toplam bütçe, kollar arasında paylaşım; aksi halde tek kapı iki kez takviye üretebilir.
8. iPhone yatayda 16:9 ve geniş çentikli ekran, güvenli alanın iki yönü, metin ölçeği, üç giriş ve sekiz yuva birlikte görülmeli. Ekrana sığdırmak için bütün sahneyi küçültüp dokunma hedeflerini bozmak kabul edilmez; gerekirse daha sade rota şablonu seçilmeli.
9. TR/EN tüm kapı, dalga, giriş/kol ve güncelleme metinleri aynı parametreli kopya kaynağından çıkmalı. Örnek EN: “4 more stars to Ash Pass”; “Entrance 2 · Raiders ×24 · +12s”. Bölge adları mevcut yerelleştirmeden gelmeli; sayı formatı ve uzun İngilizce butonlar ayrıca kontrol edilmeli. Valor/XP yeni ilerleme arayüzüne sızmamalı.

## 7. Emre'ye kararlar (en fazla 5)

1. **Kapı:** öneri her önceki bölgede 20/30 + boss; kümülatif toplam yerine bu yerel hedefi seçelim mi? Eski açılmış bölgelerin korunması zorunlu kabul edildi.
2. **Yol tanıtımı:** öneri çift giriş B3, yol ayrımı B13, üçüncü giriş B33; böylece görünür yenilik daha erken gelir. Mevcut 14/43 eşiklerini korumak erken oyun tekrarını daha az azaltır.
3. **Süre:** öneri B1-1..4 kısa eğitim istisnalarını korumak, B10 dahil tüm bossları 7–9 dk yapmak; diğer normaller 4–7 dk. Bu karar, eski genel belgeyle kod arasındaki farkı açıkça kapatır.
4. **Topoloji kapsamı:** öneri X2 ayrıl/birleş dahil sekiz desen, tek kale, savaş sırasında değişmeyen yollar; dinamik makas/çok kale sonraya kalsın. Böylece yeni düşman türü gerekmiyor.
5. **Eğri bütçesi:** öneri daha dengeli giriş payları ve daha görünür özel tiplerle B5'i yeniden ayarlamak; 1950 planlı final düşmanı ilk ölçüm hedefi, kesin yayın sayısı değil. 23 kişi-günlük tam kapsam yerine önce B1 dikey kesiti kabul kapısından geçirelim.

Teslim durumu: tasarım ve kaynak incelemesi tamamlandı; önerilen uygulama, test, cihaz ölçümü ve kararların onayı gelecekteki işlerdir.
