# ALLIN: Defense — 50 bölüm / 5 bölge v1 içerik ve uygulama planı

> Hazırlayan: Claude worker (task_4776a3e58872) · 14 Ekim 2026 · `main` `2a4c27f` · salt okuma, statik inceleme.
> Unity/test/simülatör koşulmadı. Sayısal düşman/can toplamları kodun formülleri elle yeniden hesaplanarak bulundu (yöntem §2.3).
> AppFlowController*.cs, FrontendCopy.cs ve GameSessionController.Checkpoints.cs satırları `git show HEAD:` sürümünden alındı.

## 1. Özet

1. Bölge 2-5 için iskelet hazır: 50 bölümün adı, dalga sayısı, başlangıç altını/canı, harita üreticisi (1-3 giriş), sunucu kural dosyası ve bölge sekmeli harita ekranı kodda var; ama 11-50'nin dalgaları bölge 1'in elle ayarlanmış "kalabalık sürü" dengesinden kopuk, eski bir formülden geliyor (11. bölüm 10. bölümün ~1/14'ü kadar toplam düşman canı taşıyor) ve görsel olarak beş bölge aynı orman gibi görünüyor.
2. Eksikler: elle ayarlı bölge verisi (40 bölüm), bölgeye özgü kimlik (biyom görünümü, boss varyantı, öğretici hedef metinleri), oynanabilir sınırın 10'a sabitlendiği 9 nokta, 11+ bölümlerde hiç çalışmayan checkpoint, bölge sonu/final ekranı, 41 eksik İngilizce isim ve 11-50'yi oynayan denge testi.
3. Toplam iş: tek geliştirici + ajanlarla **~28-32 iş günü (≈6 hafta)**; buna bölge başına 2-3 kişilik gerçek oyuncu testi için takvim süresi eklenir. İlk teslim "bölge 2 dikey dilimi" altyapı dahil **~10 iş günü**.

## 2. Envanter (kanıtlı)

### 2.1 Kampanya verisi — ne var

| Konu | Durum | Kanıt |
|---|---|---|
| Bölge/bölüm sayısı | 5 × 10 = 50 sabit | `Assets/Scripts/Data/CampaignCatalog.cs:45-47` |
| Bölge adları + alt başlık | Kül Geçidi "Zırh ve kuşatma", Buz Sınırı "Hız ve don etkileri", Batık Krallık "Çoklu baskı hatları", Son Hisar "Ustalık sınavı" | `CampaignCatalog.cs:49-58` |
| 50 bölüm adı (TR) | Var; 50. bölümün adı "Lordrush" (TR/EN karışık) | `CampaignCatalog.cs:72-79` |
| Dalga sayısı, altın, can, süre, zorluk | Formülle: dalga 4→12, altın 215→155, can 18→12 | `CampaignCatalog.cs:119-129` |
| Bölge 1 istisnası | 1-10 elle ayarlı JSON'dan dalga/altın/süre alıyor | `CampaignCatalog.cs:137-144`, `Assets/Scripts/Data/FirstRegionCampaign.cs:17-18` |
| Kural dosyası | `campaign-rules-v2.json` 50 bölümü içeriyor (dalga/altın/can/ödül/pathPreset) | `Assets/Resources/LiveGame/campaign-rules-v2.json:2-4` |
| Dosya-katalog eşitliği | EditMode testi her alanı katalogla karşılaştırıyor | `Assets/Tests/EditMode/AuditRemediationTests.cs:44-67` |
| Sunucu (Cloud Code) | Aynı JSON'u gömüyor, tam 50 bölüm bekliyor; yalnız `id`, `startingLives`, `valorReward`, `heroXpReward` kullanıyor | `CampaignProgressionCCM/CampaignProgression/CampaignProgression.csproj:16`, `CampaignProgressionModule.cs:105-110`, `:206-224` |

**Sonuç:** Bölüm 11-50'nin `startingLives` değeri değişirse sunucu yıldız hesabı değişir → `campaign-rules-v3` + Cloud Code dağıtımı gerekir (`CampaignProgressionModule.cs:200-202` yalnız v1→v2 geçişini kabul ediyor). Dalga/altın gibi diğer alanlar sunucuyu etkilemez.

### 2.2 Dalgalar ve düşman karışımı (11-50)

- Üretici: `CampaignLevelFactory.cs:394-441` `BuildProgressiveWaves`. Her bölümde 4. dalgadan itibaren aynı sırayla Öncü → Yenilenen → Şifacı → Kuşatma ekleniyor (`:411-422`); 11. bölümden itibaren **beş normal tipin hepsi her bölümde** var. Yeni ders/tanıtım yok.
- 10'dan sonra tüm gecikme/aralıklar yarıya iniyor (`CampaignLevelFactory.cs:107-108`).
- Düşman güçlendirmesi bölüm numarasına göre doğrusal: can ×1→×2,7, hız ×1→×1,16 (`Assets/Scripts/Core/GameSessionController.cs:2785-2795`); bölge 1 ise JSON'daki `healthMultiplier` ve `killRewards` kullanıyor (`:2778-2783`).
- Grup verisinde giriş seçimi (`routeId`) var (`Assets/Scripts/Data/GreyboxDefinitions.cs:30`) ve doğumda kullanılıyor (`GameSessionController.cs:981-983`); fakat elle ayarlı JSON okuyucu 4 parametreli kurucuyu çağırıp `routeId`'yi düşürüyor (`FirstRegionCampaign.cs:33-35`). Yani bugünkü JSON biçimi çoklu girişi taşıyamaz.

### 2.3 Zorluk kopukluğu (en önemli bulgu)

Yöntem: `FirstRegionCampaign.json` grupları ve `BuildProgressiveWaves` formülü Python ile toplandı; toplam can = adet × taban can (`GameSessionController.cs:2765-2771`) × bölüm can çarpanı. Hız, ödül, giriş sayısı ve zamanlama hesaba katılmadı; kaba bir karşılaştırmadır.

| Bölüm | Dalga | Toplam düşman | Toplam can (≈) | Not |
|---|---|---|---|---|
| 9 (elle) | 6 | 599 | 85.000 | |
| 10 (elle, boss) | 8 | 824 | 131.000 | |
| **11 (üretici)** | 4 | **52** | **9.200** | 10'un ~1/14'ü |
| 14 | 5 | 120 | 19.600 | ilk 2 giriş |
| 20 (boss) | 9 | 348 | 64.000 | bölge 1'in 9. bölümünden düşük |
| 30 (boss) | 10 | 436 | 95.000 | |
| 40 (boss) | 11 | 540 | 137.000 | 10. bölüm seviyesi |
| 50 (final) | 12 | 651 | 192.000 | |

Bölge 1, 2a4c27f ile "çok ama zayıf yağmacı" (dalga başına 50-88 yağmacı, düşük ödül: `FirstRegionCampaign.json` 1. bölüm) stiline geçti; 11-50 bu geçişten hiç etkilenmedi. **11-50 "sabitleri değiştirip açılabilir" durumda değil; yeniden yazılmalı.**

### 2.4 Harita ve rota üretimi

- 5 temel yol şekli (`CampaignLevelFactory.cs:45`), bölüm numarasından seçilen desen + küçük kaydırma (`:191-200`), çift numaralı bölgelerde ayna (`:201`).
- Giriş sayısı: 1-13 tek, 14-42 iki, 43-50 üç (`CampaignLevelFactory.cs:174`). Yan yol arama algoritması ve yuva yerleşimi deterministik (`:226-314`, `:484-505`).
- 50 haritanın tümü EditMode'da yapısal olarak sınanıyor: benzersiz/ilerleyen giriş (`Assets/Tests/EditMode/CoreRulesTests.cs:359`), yuva-yol mesafesi (`:453`, `:470`), yan yol tek birleşim (`:495`).
- Eksik: harita seçimi veriden kontrol edilemiyor (desen numarası formülde gömülü). Elle ayarlı bölge için bölüm başına `mapPattern`/`mirror` alanı gerekir.

### 2.5 Biyom ve görsel çeşitlilik

- `Biome()` 5 renk paleti tanımlıyor (zemin/vurgu/sis/ışık) (`CampaignLevelFactory.cs:124-154`) ve sahneye bölge numarası gidiyor (`GameSessionController.cs:76`, `:522`, `:636`).
- Ama etkisi çok zayıf: ortam ışığına %8, sise %18 karışıyor (`Assets/Scripts/Presentation/DefenderSceneLook.cs:61`, `:67`). Zemin rengi sabit (`DefenderBattlefieldDressing.cs:175`), ağaç/kaya/çalı paleti sabit (`:389-392`), biyom yalnız rastgele tohumu değiştiriyor (`:259`).
- Biyoma özel süs kodu (`BuildBiomeDressing`, `GameSessionController.cs:704`) **hiç çağrılmıyor** (ölü kod). Dünya kökünün adı her bölgede "Greenwatch Valley" (`:594`) ve testler bu adı arıyor (`Assets/Tests/PlayMode/GreyboxSceneTests.cs:549`).
- Fırsat: ağaç/kaya/çalı tek ortak malzemede vertex rengiyle çiziliyor (`DefenderBattlefieldDressing.cs:395`), test bunu "2 grove mesh" olarak kilitliyor (`Assets/Tests/PlayMode/BattlefieldLookTests.cs:211`). **Biyom başına renk/şekil değişimi ek SetPass maliyeti olmadan yapılabilir.**

### 2.6 Düşman, boss, kule, kahraman

- 5 normal tip + 1 boss (`GreyboxDefinitions.cs:13-21`); sunumda 8 aktör türü (`Assets/Scripts/Presentation/DefenderPresentationCatalog.cs:11-21`); modeller Blender'dan üretilmiş FBX (`Assets/Art/Models/Generated/Boss.fbx` vb.). Yeni tip = enum + istatistik + model + animasyon + ikon + kart + tavsiye metni.
- Boss tek tip: %66/%33 safha (`Assets/Scripts/Core/GreyboxCombatants.cs:182-192`), 2. safhada her zaman 6 yağmacı ve hep 1. girişten takviye (`GameSessionController.Battle.cs:171-183`). Bölgeye göre farklılaşma yok.
- Kule hedefleme 10. bölüme özel kural taşıyor (`GameSessionController.cs:2588`).
- Mevcut uzmanlaşmalar bölge kimliklerine "cevap" olabilir: Hızlı Atış/Keskin Nişancı, Kalkan/Saldırı Muhafızı, Patlama/**Don** (`Assets/Scripts/Data/TowerProgression.cs:8`, `:32-37`). Tümü 8. bölümde açılmış oluyor (`:29-31`).

### 2.7 Müzik

- 6 durum parçası (hazırlık/savaş/yoğun/boss/zafer/yenilgi) (`DefenderPresentationCatalog.cs:49-57`), tüm bölgelerde aynı; WAV kaynak toplam ~8 MB, Vorbis q0.55 akış (`Assets/Audio/Presentation/Music/Combat.wav.meta`).

### 2.8 Kayıt, ilerleme, checkpoint sınırları

| Sınır | Değer | Kanıt |
|---|---|---|
| Kilit açma | 50'ye kadar ilerliyor (10'u bitiren oyuncuda zaten `unlockedLevel = 11`) | `Assets/Scripts/Core/CampaignProgressStore.cs:461-462` |
| Kayıt sürümü | `CurrentVersion = 5`, 1-50 aralığına kırpma | `CampaignProgressStore.cs:74`, `:399-405` |
| "Devam et" hedefi | `NextUnfinishedLevel(int available = 10)` varsayılanı 10 | `CampaignProgressStore.cs:212-216` |
| Oynanabilir sınır | `MaxPlayableLevel = min(FirstRegionCampaign.LevelCount, unlocked)` | `AppFlowController.ClosedTest.cs:14` (HEAD) |
| Sonuçta sonraki bölüm | 10'a kırpılıyor | `AppFlowController.cs:631` (HEAD) |
| Yeniden yükleme | 10'a kırpılıyor | `AppFlowController.cs:922` (HEAD) |
| Profil yıldızı | `level <= 10` toplamı, "/30" sabit | `AppFlowController.cs:590` (HEAD), `FrontendCopy.cs:253` (HEAD) |
| Öğrenme hedefi | 1-9 özel, 10+ hep "Lord" metni | `AppFlowController.ClosedTest.cs:49-61` (HEAD) |
| **Checkpoint** | `level <= 10` ve tek global sürüm `first-region-v2` şartı → **11+ bölümlerde checkpoint her açılışta siliniyor** | `GameSessionController.Checkpoints.cs:29`, `:92-94` (HEAD) |
| Kabul otomasyonu | `unlockedCampaignLevel` 10 sabit | `AppFlowController.Acceptance.cs:121`, `:316` |

### 2.9 Harita ekranı çoklu bölge desteği

Hazır: bölge sekmeleri, yatay sayfalayıcı (`FrontendCampaignPager`), bölge başına dikey kaydırma ve tembel (ilk görünüşte) inşa var; hepsi `VisibleRegionCount` ile açılıp kapanıyor (`AppFlowController.Screens.cs:147-176`, `:227-279`, HEAD). `VisibleRegionCount = 1` (`AppFlowController.ClosedTest.cs:12`, HEAD). Eksik: kilitli bölge sayfası görünümü (ilk bölüm kilitliyken sayfa ne gösterecek), bölge tamamlandı rozeti/özeti.

### 2.10 Çeviri

İngilizce sözlükte yalnız bölge 1'in adı, alt başlığı ve 10 bölüm adı var (`Assets/Scripts/UI/GameLocalization.cs:24-35`); eksik çeviri sessizce Türkçe kalıyor (`:138-141`). Eksik: 4 bölge adı + 4 alt başlık + 40 bölüm adı = **48 anahtar**, artı yeni bölge hedef/ekran metinleri. Kural dosyasında `campaign.level.NN.name` anahtarları tanımlı ama kullanılmıyor (`campaign-rules-v2.json:11-12`).

### 2.11 Testlerin 50 bölümü kapsaması ve süreleri

| Test | Kapsam | Süre (son koşu) |
|---|---|---|
| EditMode yapı testleri (katalog, harita, yuva, yan yol) | **1-50** (`CoreRulesTests.cs:104-121`, `:342-497`) | 240 test 4,2 sn (`Builds/balance/final-edit.xml`) |
| `FirstRegionBalanceTests` (bot gerçek kaynakla oynar) | yalnız 1-10 (`FirstRegionBalanceTests.cs:19-20`) | 3 test 42,6 sn (`Builds/balance/stage2-play.xml`) |
| `FirstRegionLearningCurveTests` | yalnız 1-10 | 13 test 49,8 sn (`Builds/balance/learning-play.xml`) |
| `FirstRegionDifficultyCurveTests` (veri kuralları) | yalnız 1-10 (`FirstRegionDifficultyCurveTests.cs:11-24`, `:54`) | EditMode içinde |
| Görsel/sahne testleri | 1, 10, 14, 24, 43 (`BattlefieldLookTests.cs:197`, `GreyboxSceneTests.cs:539`) | |
| Performans | 14 ve 10 (`AuditLayer2PerfTests.cs:272`, `:404`) | |

Denge testi botu yalnız "kazanır mı, hedef sürede mi, can ≥8 mi" bakıyor (`FirstRegionBalanceTests.cs:50-57`); "zayıf strateji kaybeder" gibi alt sınır yok. **11-50 bugün hiçbir oynanış testinde koşmuyor.**

## 3. Bölge kimlikleri

İlke: yeni kule, yeni kahraman, kalıcı gelişim yok. Kimlik dört kaldıraçla kurulur: (1) düşman karışımı ve dalga ritmi, (2) giriş sayısı ve rota şekli, (3) bölge boss'unun takviye/safha davranışı (veriyle), (4) biyom paleti + süs şekli. Mevcut adlar korunur (kodda zaten var, çeviri dışında iş yok). Her bölge mevcut bir uzmanlaşmayı "doğru cevap" yapar ama tek cevap yapmaz.

| | Bölge 2 · Kül Geçidi (11-20) | Bölge 3 · Buz Sınırı (21-30) | Bölge 4 · Batık Krallık (31-40) | Bölge 5 · Son Hisar (41-50) |
|---|---|---|---|---|
| Biyom / renk | Kül grisi zemin, kor turuncu vurgu, ölü ağaç + bazalt kaya | Soluk mavi-beyaz zemin, buz mavisi vurgu, çam + kristal kaya | Koyu deniz yeşili zemin, turkuaz vurgu, kırık sütun + yosunlu taş | Koyu kırmızı-kahve zemin, altın vurgu, sur kalıntısı + sancak |
| Ana baskı | Zırh: Öncü ağırlıklı, kuşatma konvoyu (Kuşatma + Öncü eskort) | Hız: kalabalık hızlı Yağmacı sürüsü (+%10-20 hız, veriyle) | Destek: Şifacı + Yenilenen, iki koldan dönüşümlü dalga | Hepsi karışık, üç kol, uzun dalgalar |
| Yeni oynanış unsuru | **İkinci giriş 13'te** yumuşak tanıtım (yalnız yağmacı), 14'ten itibaren karışık | Uzun yılan rotalar (yavaşlatmanın değeri artar); Don uzmanlaşması öne çıkar | **Dalga ritmi**: iki giriş sırayla gecikmeli baskı; yan yolda kışla tutma | **Üçüncü giriş 43'te**; son bölümler tüm tipleri birlikte |
| Öne çıkan cevap | Büyü/Patlama, Keskin Nişancı | Okçu/Hızlı Atış, Büyü/Don | Keskin Nişancı (şifacı önceliği), Kalkan Muhafızı | Ustalık: dengeli kurulum |
| Boss | **Geçit Efendisi**: 2. safhada Öncü takviyesi, kuleyi durdurma süresi uzun | **Buz Hükümdarı**: 3. safhada daha hızlı + Yağmacı sürüsü | **Derinliklerin Lordu**: takviye ikinci girişten + 2 Şifacı | **Son Lord** (öneri; "Lordrush" yerine): takviye her girişten, 3 safha, en uzun dalga |
| Zorluk hedefi | 11 ≈ 8-9. bölüm baskısı (boss sonrası nefes), 20 ≈ 10. bölümün ~1,3 katı | Bölgeye giriş 20'nin ~%80'i, 30 ≈ 20'nin ~1,3 katı | Aynı desen | 50 ≈ 40'ın ~1,4 katı; 3 yıldız yalnız iyi oyunla |
| Hedef süre | 4-7 dk | 5-8 dk | 6-9 dk | 7-10 dk (`CampaignCatalog.cs:123` hedefiyle uyumlu) |

**Yeni düşman tipi önermiyorum.** Gerekçe: her tip yeni model + animasyon + sunum kaydı + ikon + ilk karşılaşma kartı + tavsiye + denge demek; tahmini **L (3-5 gün/tip)**. Bölge farkını boss varyantı ve dalga ritmi daha ucuza sağlıyor. Oyuncu testinde "bölgeler aynı" geri bildirimi gelirse ilk aday: **"Elit" işareti** — mevcut tipe veriyle can/zırh çarpanı + altın kenar rengi (yeni model yok), **M (~1 gün)**.

## 4. Akışlar

1. **Bölge sonu ekranı (10, 20, 30, 40):** boss zaferinden sonra normal sonuç ekranının ardından tek kart: "Kül Geçidi tamamlandı · 24/30 yıldız · Sıradaki: Buz Sınırı". Düğmeler: "Sonraki bölgeye geç" (haritayı yeni bölge sayfasında aç) ve "Eksik yıldızlar". Mevcut sonuç ekranı ve pager yeniden kullanılır; yeni ekran türü gerekmez.
2. **Kampanya finali (50):** aynı kartın final sürümü: toplam yıldız / 150, bölge başına yıldız satırı, "Krallık kurtuldu" metni, kısa jenerik. Sonrasında "Devam et" en düşük yıldızlı bölüme yönlendirir (bugün 10'a dönüyor: `CampaignProgressStore.cs:212-216`).
3. **Görünür son bölgenin sonu:** kapalı testte bölgeler aşamalı açılırsa, görünür son bölgenin boss'unda "bölüm N açıldı" mesajı bastırılır, "Yeni bölge yakında" gösterilir. Bu, `OYUN_EKSIKLERI.md` P0-1 ile aynı iş; şu an başka worker'ın üzerinde çalıştığı dosyalarda. **Bu planın akış işleri o iş bittikten sonra başlamalı.**
4. **Sonraki bölgenin kilidi:** mevcut kural yeterli (10 kazanılınca 11 açılır). Ek kural önermiyorum (ör. yıldız eşiği) — kalıcı gelişim olmadığı için yıldız kapısı oyuncuyu tekrar oynamaya zorlar, kazanç sağlamaz.
5. **30 → 150 yıldız:** tek kaynak `PlayableLevelCount = VisibleRegionCount × 10`; profil "/30" ve `FrontendCopy.ProfileStars` buna bağlanır. Ana menüdeki bölge ilerleme şeridi zaten bölgeye göre sayıyor (`AppFlowController.cs:602-604`, HEAD).
6. **Kayıt göçü — öneri: bölge başına içerik sürümü.**
   - Checkpoint `ruleVersion` alanı bölgenin sürümünü taşır (`region-01-v2`, `region-02-v1`…). Bölge 3'ün dengesini değiştirmek bölge 2'deki yarım savaşı silmez.
   - `first-region-v2` → `region-01-v2` eşlemesi tek satırlık uyum kuralı olur; mevcut bölge 1 checkpoint'leri korunur.
   - `CampaignProgressStore.CurrentVersion` değişmez (şema aynı); 11-50'deki eski yıldızlar korunur (kayıt zaten 1-50'yi tutuyor).
   - Sunucu: 11-50'nin `id`, `startingLives`, `valorReward`, `heroXpReward` değerleri **dondurulur** → Cloud Code dağıtımı gerekmez. `AuditRemediationTests` yalnız bu dört alanı + dalga sayısını karşılaştıracak biçimde daraltılır; diğer alanlar istemciye ait veri olur.

## 5. Veri ve denge yöntemi

**Öneri: bölge başına elle ayarlı JSON, üretici yalnız harita için.**

- Bölge 1'in yöntemi (elle JSON + bot testi + eğri testi, `Builds/balance/report.json`) işe yaradı; aynı biçim 4 dosyada tekrar edilir: `Assets/Resources/Campaign/region-02.json` … `region-05.json`.
- JSON biçimi genişler: grup başına `routeId`; bölüm başına `mapPattern`, `mirror`, `speedMultiplier`, `bossReinforcement` (tip, adet, giriş). Bölge 1 dosyası aynı okuyucuya taşınır.
- İlk taslak elle değil, **Python taslak üretici** ile: `Builds/balance/apply_curve.py` örneğindeki gibi, hedef toplam can eğrisinden (§3) dalga gruplarını öneren bir betik; sonra elle ayar. Bu, 40 bölümü sıfırdan yazmaktan ~2 kat hızlı.
- Formül üreticisi (`BuildProgressiveWaves`) bölge verisi geldikçe devreden çıkar; son durumda silinir (11-50 için tek kaynak JSON olur).

**Otomatik denge testleri:**

| Test | İçerik | Tahmini süre |
|---|---|---|
| `CampaignBalanceTests` [TestCase(bölge)] | Bot 2-3 stratejiyle 10 bölümü oynar; hedef süre penceresi, can ≥ eşik, eşzamanlı düşman ≤60 | ~45-60 sn/bölge (bölge 1'den: 42,6 sn); 50 bölüm ~4-5 dk |
| Alt sınır testi (yeni) | "Yalnız okçu, yükseltmesiz" botu bölge boss'unu **kaybetmeli** | ~10 sn/bölge |
| `CampaignDifficultyCurveTests` (EditMode) | Ardışık bölümlerde desen farklı, toplam can eğrisi monoton (boss sonrası nefes hariç), son düşmana süre yeter | <1 sn |

- Bölge başına ayrı `TestCase` olduğu için iş sırasında yalnız ilgili bölge koşulur; tam koşu yalnız aşama sonunda (CI süresi ~5 dk artar).
- Rapor: bölge başına `Builds/balance/region-0N.json` (bölge 1 rapor biçimi).

**Gerçek oyuncu testi noktaları:**

1. Bölge 2 dikey dilimi bitince: 2-3 kişi, 11-20 (iki giriş tanıtımı anlaşılıyor mu, 13-14'te takılma).
2. Bölge 3 + 4 bitince: 2-3 kişi, 21-40 (bölgeler farklı hissettiriyor mu — "elit" kararı burada).
3. Bölge 5 + final: 1-50'yi uzun süre oynayan 1-2 kişi (yorulma, zorluk duvarı, toplam süre ~5-7 saat).

## 6. Uygulama aşamaları

Büyüklük: **S** ≤2 saat · **M** ≤1 gün · **L** >1 gün. Dosya sahipliği her satırda; aynı dosya aynı anda tek worker'da.

### Aşama 0 — Altyapı (~4 iş günü, 0a/0c/0d paralel)

| İş | Kapsam | Dosyalar (sahip) | Büyüklük |
|---|---|---|---|
| 0a İçerik modeli | Bölge JSON okuyucu (`CampaignContent`), `routeId`/harita/hız/boss takviyesi alanları, katalog + ölçekleme + 10. bölüm hedefleme özel durumunun veriye taşınması, bölge başına checkpoint sürümü | `FirstRegionCampaign.cs` (→ `CampaignContent.cs`), `CampaignCatalog.cs`, `CampaignLevelFactory.cs` (Biome hariç), `GameSessionController.cs` (`GetScaledStats`, `:2588`), `GameSessionController.Battle.cs`, `GameSessionController.Checkpoints.cs` | L (1,5-2 gün) |
| 0b Akış ve sınır | Tek `PlayableLevelCount`; 9 sabit noktanın ona bağlanması (§2.8); bölge sonu + final kartı; bölge hedef metinleri; 150 yıldız | `AppFlowController*.cs`, `FrontendCopy.cs`, `CampaignProgressStore.cs` | L (1-1,5 gün) — **diğer worker'ın P0-1 işi bitince**; 0a'nın Checkpoints dosyası ile çakışmaması için 0a'dan sonra |
| 0c Denge koşumu | `CampaignBalanceTests`, alt sınır testi, `CampaignDifficultyCurveTests`, rapor yazımı | `Assets/Tests/PlayMode/CampaignBalanceTests.cs` (yeni), `Assets/Tests/EditMode/CampaignDifficultyCurveTests.cs` (yeni) | M |
| 0d Biyom görünüm profili | Biyom başına zemin/ağaç/kaya/yosun paleti + süs şekli seçimi, `SceneLook` karışım oranları, dünya kök adı; `BuildBiomeDressing` ölü kodunun silinmesi (0a ile koordineli) | `Assets/Scripts/Presentation/BiomeLook.cs` (yeni), `DefenderBattlefieldDressing.cs`, `DefenderSceneLook.cs` | M |

Kabul kapısı: EditMode + PlayMode tam yeşil; bölge 1 denge raporu değişmemiş (aynı süre/can, ±%5); `env2` ölçümünde SetPass artışı ≤%10; 11. bölümde checkpoint kaydedilip geri yükleniyor.

### Aşama 1 — Bölge 2 dikey dilimi (~6 iş günü)

| İş | Kapsam | Dosyalar | Büyüklük |
|---|---|---|---|
| 1a Veri | `region-02.json` 10 bölüm (taslak betik + elle ayar), 13'te ikinci giriş tanıtımı, harita seçimi | `Assets/Resources/Campaign/region-02.json` | L |
| 1b Boss | Geçit Efendisi takviye/safha verisi | `region-02.json` (1a ile aynı worker) | S |
| 1c Metin | 10 bölüm + bölge adı/alt başlık EN, 10 öğrenme hedefi | `GameLocalization.cs`, `FrontendCopy.cs` | S |
| 1d Biyom 2 | Kül paleti + ölü ağaç/bazalt şekli | `BiomeLook.cs` | M |
| 1e Denge döngüsü | Koşum → ayar, 3-4 tur | `region-02.json`, rapor | L |
| 1f Açılış | `VisibleRegionCount = 2`, kilitli/açık bölge sayfası görünümü | `AppFlowController.ClosedTest.cs`, `AppFlowController.Screens.cs` | S-M |

Paralel: 1c, 1d, 1a aynı anda (farklı dosyalar); 1e, 1a'dan sonra; 1f en son. Kabul kapısı: `CampaignBalanceTests(2)` yeşil; alt sınır testi boss'u kaybettiriyor; simülatörde 10 → 11 → 20 → bölge sonu kartı uçtan uca; 2-3 kişilik oyuncu testi notları.

### Aşama 2-4 — Bölge 3, 4, 5

| Aşama | İçerik | Büyüklük |
|---|---|---|
| 2 Bölge 3 | `region-03.json`, Buz Hükümdarı, biyom 3, metin, denge | ~4 gün (L) |
| 3 Bölge 4 | `region-04.json`, dalga ritmi, Derinliklerin Lordu, biyom 4, metin, denge | ~4 gün (L) |
| 4 Bölge 5 + final | `region-05.json`, üç giriş, Son Lord, biyom 5, final kartı, metin, denge | ~6 gün (L) |

Paralel: bölge 3 ve 4 verisi farklı JSON dosyalarında olduğu için iki worker aynı anda yazabilir; `BiomeLook.cs` ve `GameLocalization.cs` ortak olduğundan bu iki dosya tek worker'da sıralı. Kabul kapısı her bölgede aşama 1 ile aynı.

### Aşama 5 — Kapanış (~3 iş günü)

Tam 50 bölüm denge koşusu, eski formül üreticisinin silinmesi, performans ölçümü (bölge 5 / 3 giriş / boss), çeviri taşma kontrolü (EN), uzun oyuncu testi. **M-L.**

**Toplam:** 4 + 6 + 4 + 4 + 6 + 3 = **27 iş günü**, ara düzeltmelerle **28-32 iş günü**.

## 7. Riskler

| Risk | Neden | Önlem |
|---|---|---|
| **Performans** | Son ölçüm başarısız: SetPass 1280×960 savaşta 102 (%17 artış, limit %10), boss 113-115 (`Builds/env2-metrics.json`). Bölge 5'te üç giriş + daha kalabalık dalga çizim sayısını artırır (10. bölüm boss: 1.220 draw call). | Biyom farkı yalnız vertex rengi/mesh ile (ek malzeme yok); eşzamanlı düşman ≤60 testi her bölgede; perf testine 43 ve 50 eklenmeli. |
| Build boyutu | Kod/veri ile ihmal edilebilir. Bölge başına müzik eklenirse her parça sıkıştırılmış ~0,3-0,5 MB (tahmin). | Yeni müzik isteğe bağlı; ilk tercih mevcut 6 parça. |
| Test süresi | Tam denge koşusu ~4-5 dk PlayMode ekler. | Bölge başına `TestCase`; tam koşu yalnız aşama sonlarında. |
| Çeviri | 48 eksik isim + yeni metinler; eksik çeviri sessizce Türkçe kalıyor (`GameLocalization.cs:138-141`). | "Katalogdaki her isim EN sözlükte var" EditMode testi (S). |
| Kayıt uyumluluğu | (a) 11+ checkpoint bugün çalışmıyor; (b) eski kapalı test sürümlerinde 11-50'yi oynamış oyuncuların yıldızları kalır; (c) `startingLives` değişirse sunucu kural sürümü + dağıtım. | (a) aşama 0a; (b) koru (zararsız); (c) can değerlerini dondur. |
| Kapsam kayması | "Bölgeler aynı" geri bildirimi yeni düşman/mekanik isteğine dönüşebilir. | Önce elit işareti (M), yeni tip ancak oyuncu testi kanıtıyla. |
| Dosya çakışması | 0b ve 1f, şu an başka worker'ın değiştirdiği `AppFlowController*.cs` / `FrontendCopy.cs` dosyalarında. | O iş birleşmeden 0b başlamaz. |

## 8. Emre'nin karar soruları

1. **Bölgelerin açılma sırası (kapalı test ve yayın):**
   A) Kapalı testte bölge bölge aç (2 → 3 → 4 → 5), mağazaya 50 bölüm tek sürümde çıksın — **önerim**: her bölge oyuncu testinden geçer, v1 kararı korunur.
   B) Hepsi bitince birlikte aç. Daha az ara sürüm, geç geri bildirim.
   C) v1.0 = 20 bölüm, kalanlar güncellemeyle. Kararı değiştirir; yalnız takvim baskısı olursa.

2. **11-50'nin dalga stili:**
   A) Bölge 1'in "kalabalık zayıf sürü" stilini sürdür — **önerim**: oyuncunun öğrendiği ritim bozulmaz, denge yöntemi hazır.
   B) "Az ama güçlü" (bugünkü üretici stili). Daha az çizim yükü, ama 10 → 11 geçişinde his değişir.

3. **Yeni düşman tipi:**
   A) Yok; kimlik = karışım + rota + boss varyantı + biyom — **önerim** (0 ek gün).
   B) Gerekirse yalnız "elit" işareti (M, ~1 gün, yeni model yok).
   C) Bölge başına 1 yeni tip (4 × L, ~15-20 gün).

4. **Bölgeye özgü çevre mekaniği** (ör. buzlu yolda düşman hızlanır, kül fırtınası menzili düşürür):
   A) v1'de yok — **önerim**: her biri yeni kural + görsel + öğretici + denge.
   B) Yalnız bir tane (Buz Sınırı'nda buzlu yol bölgesi, M-L ~2 gün).
   C) Her bölgeye bir (~8-10 gün).

5. **Zorluk hedefi:**
   A) Bölümü geçmek ortalama oyuncu için kolay, 3 yıldız iyi oyun ister; boss'lar "sınav" ama duvar değil — **önerim**.
   B) Bölge boss'ları belirgin duvar (birkaç deneme). Kalıcı gelişim olmadığı için oyuncuyu bırakma riski yüksek.

6. **Final ve isim:**
   A) 50 sonrası kısa final kartı + jenerik, final boss adı "Son Lord" (Lordrush yerine) — **önerim**.
   B) Hikâye metinli final sahnesi (M-L ek).
   C) Final sonrası zor mod (v1 dışı, L).
