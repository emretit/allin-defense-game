# İlerleme Tasarımı — ALLIN: Defense (Claude, 2026-10-17)

Salt-okur tasarım raporu. Tüm sayılar koddan ve `Assets/Resources/Campaign/region-0X.json`'dan çıkarıldı (eski belgelerden değil).

## Uygulama iş paketleri (checklist)

**Faz A — paralel başlar**
- [ ] WP1 Yıldız kapısı mantığı + kayıt v6 (~2.5 sa)
- [ ] WP3 Yol düzeni tablosu + üretici parametreleri (~4 sa)

**Faz B — Faz A'dan sonra**
- [ ] WP2 Yıldız kapısı arayüzü: harita + sonuç ekranı (~3 sa, WP1'e bağlı)
- [ ] WP4 Çatal (ayrılma) deseni — Emre onaylarsa (~5 sa, WP3'e bağlı, aynı dosya → sıralı)
- [ ] WP6 Dalga önizlemesi + dalga bazında giriş okları (~3.5 sa, WP3'e bağlı)

**Faz C — en son**
- [ ] WP5 Dalga verisi: giriş kullanımı + tip karışımı, 5 bölge JSON, bot dengesi (~8 sa, WP3 (+WP4) sonrası)
- [ ] WP7 Bölüm hedef metinleri TR/EN (~2 sa, WP2 + WP5 sonrası)
- [ ] Koordinatör: tüm EditMode + CampaignBalanceTests + tek simülatör koşusu (~1 sa)

Toplam: WP4 dahil ~29 sa, WP4'süz ~24 sa (7 paket).

---

## 1. Teşhis — bugün bölümden bölüme ne değişiyor

### 1.1 Sayılarla bugünkü eğri

| Ölçü | Bugün | Kaynak |
|---|---|---|
| Giriş sayısı | 1–13: **1** · 14–42: **2** (29 bölüm üst üste aynı) · 43–50: **3** | `CampaignLevelFactory.cs:107` |
| Yol şekli | 5 elle çizilmiş "S-yılan" şablon; her bölgede her şablon tam **2 kez**; uzunluklar 42.0–49.5 (fark %18) | `CampaignLevelFactory.cs:45-81`, `:188` |
| Yol farkı | küçük kaydırma (z ±0.54, x ±0.55) + çift numaralı bölgelerde ayna | `:190-198` |
| Kol yol | her zaman tek T-kavşakla ana yola **~%42**'de katılır (3. yol ~%60) | `:226` |
| Kule yuvası | her bölümde **8**, ana yol boyunca eşit aralıklı | `:483-490` |
| Dalga sayısı | R1: 3,3,4,5,6,6,6,6,6,8 · R2–R4: 9 bölüm **6**, boss 9 · R5: 6×6, 7×3, boss 8 | JSON |
| Toplam düşman | R1 204→824 · R2 588→1188 · R3 910→1593 · R4 995→2070 · R5 1310→2495 (R5 içinde zikzak: 1763, 1776, 1406, 1218, 1365, 2080, 2204, **1039**, 2495) | JSON |
| Yağmacı dışı pay | R1 %0→20 · R2 %12→22 · R3 %13→16 · R4 **%11–12** sabit · R5 **%2–8** | JSON |
| HP çarpanı | bölge içinde +0.3; bölge başında düşüş/sıçrama (L10 1.28→L11 1.20; L39 2.57→L41 2.13; L49 tek başına 3.78) | JSON `healthMultiplier` |
| Başlangıç altını | 230 → 2100 (9×), ama yuva sayısı hep 8 | JSON `startingGold` |
| Yıldız | yalnız can oranı: 3★ ≥%75, 2★ ≥%40, yoksa 1★ | `Combat/BattleRules.cs:13-15` |
| Yıldızın işlevi | yalnız gizli valor (+5/yıldız) ve sayaç; hiçbir şeyi açmaz | `CampaignProgressStore.cs:469` |
| Bölge kilidi | önceki bölgenin 10. bölümünde ≥1★ yeter | `AppFlowController.CampaignCompletion.cs:24-25` |

### 1.2 Neden "amaçsız" hissettiriyor

1. **Yıldızın karşılığı yok.** 1★ ile de bir sonraki bölüm açılıyor (`CampaignProgressStore.cs:475`); valor gizli. Tekrar oynamak için hiçbir sebep yok.
2. **Çeşitlilik ilerledikçe azalıyor.** Yağmacı dışı düşman payı R2'de %21'le zirve yapıp R5'te %2'ye düşüyor. Son bölge, düşman karışımı açısından oyunun en tekdüze bölgesi. Şifacı/Yenilenen R5 bölümlerinde 1–3 adet; oyuncu fark etmiyor.
3. **Harita 29 bölüm boyunca aynı.** 14'ten 42'ye kadar hep "2 giriş, ~%42'de T-kavşak". Şablonlar birbirine çok benziyor (hepsi soldan sağa S, kale sağ kenarda).
4. **Ritim düz.** 37 bölüm tam 6 dalga. Sayı artışı oyuncuya "aynı sürü, biraz daha kalabalık" olarak geliyor; altın da orantılı arttığı için güç hissi değişmiyor (8 yuva doluyor, gerisi yükseltme).
5. **Yenilikler metinde kalıyor.** Bölüm hedefleri (`AppFlowController.ClosedTest.cs:75-125`) "yan yolu izle", "yollar sırayla" diyor ama harita ve önizleme bunu göstermiyor: bölge 2–5 önizlemesi yalnız en güçlü tipin adı (`CampaignContent.cs:101-103`), giriş okları ilk dalga başlarken kapanıyor (`GameSessionController.cs:918`).

### 1.3 Çoklu yol bugün ne kadar destekleniyor

**Çalışan kısım**
- Veri: her spawn grubu `routeId` taşıyor; JSON doğrulaması `routeId < EntranceCount` (`CampaignContent.cs:91`).
- Spawn: rota seçimi + güvenli sınır (`GameSessionController.cs:946-950`), zamanlayıcı rota bilgisini koruyor (`GameSessionController.Battle.cs:90-104`).
- Boss takviyeleri `first / rotate / all` (`GameSessionController.Battle.cs:177-190`, `boss-profiles.json`).
- Yol süslemesi kol yolun kendi parçasını çiziyor (`DefenderBattlefieldDressing.cs:51-57`, `:140-150`).
- Kampanya ekranındaki taktik önizleme tüm yolları ve giriş numaralarını çiziyor (`FrontendTacticalMapPreview.cs:53-67`).
- Testler: rota sayısı = giriş sayısı, tüm yollar aynı kalede biter, kol yol bir kez katılır, yuvalar her yoldan uzak (`Tests/EditMode/CoreRulesTests.cs:378-381`, `:482`, `:507`).

**Tek yol / sabit varsayımlar (değişmesi gereken yerler)**
1. Giriş sayısı bölüm numarasından formül, veri değil: `CampaignLevelFactory.cs:107`.
2. Bölüm 10 zorla tek yol + elle yuvalar + elle muhafız konumu: `:170`, `:180-181`, `:187`.
3. 8 yuva yalnız **ana yol** uzunluğu boyunca dağıtılıyor; kol yolun kendi parçası yuvayı ancak beraberlik bozucuyla alıyor: `:483-490`, `:529`. Geç birleşen kol yolda yan cephede 0–1 yuva kalabilir.
4. Muhafız/kale konumu ana yolun sonundan: `:181`, `:621-629`.
5. Kol yol yalnız ana yola, tek T-kavşakla katılır; kol yolun kol yola katılması ya da aynı girişten çatal yok: `:223-311`, giriş aralığı ≥3.5 şartı `:342`, "sahip olunan baş kısım" varsayımı `OwnedBranchSegments` `:375-381`.
6. Kol yolun hedef birleşme noktası sabit: `targetFraction = 0.42 + 0.18*(i-1)` `:226`; kenar girişine sabit ceza 0.6 `:293`, `:301`.
7. Sunum olayları ana yola bağlı (`path[0]`, `path[^1]`): `GameSessionController.cs:930`, `:1419`, `:1700`.
8. Giriş okları dalga bazında değil; hepsi hazırlıkta açık, ilk dalgada kapanıyor: `GameSessionController.cs:639-680`, `:918`. L43 metni "Oklar aktif saldırı hatlarını gösterir" diyor (`:348`) ama böyle bir davranış yok.
9. Muhafız (kışla birimi) çoklu yolda tek rotaya kilitli: `GreyboxCombatants.cs:838-845`.

Ayrıca: `BuildProgressiveWaves` ve `BuildExistingBossWaves` (`CampaignLevelFactory.cs:391-467`) hiçbir yerden çağrılmıyor; ölü kod.

---

## 2. Yıldız kapısı

### 2.1 Eşik önerisi: **bölge başına 20/30 yıldız + bölge boss'u yenilmiş**

- Kural: Bölge N+1 açılır ⇔ bölge N'nin 10. bölümü ≥1★ **ve** bölge N'den toplam ≥20★.
- Gerekçe:
  - 20 = bölüm başına ortalama 2★ = "canlarının en az %40'ıyla bitir". Boss'un "duvar değil sınav" ilkesiyle uyumlu: her bölümü 2★ bitiren oyuncu hiç 3★ almadan geçer.
  - Boss 1★ ile geçilse bile 9 bölümden 19★ gerekir → zayıf geçilen boss, önceki bölümlerden 1–2 tekrarla telafi edilir.
  - Bölge başına eşik mesajı yerel ve okunur: "Kül Geçidi'ne 4 yıldız kaldı" + aynı bölgedeki bölümler. Kümülatif eşik, R1'deki kolay 3★'ları sonraki bölgelere taşıyıp kapıyı anlamsızlaştırır.
  - Sabit sayı (her bölgede 20) tek kez öğrenilir.
- Doğrulama kapısı: `CampaignBalanceBot` orta planı her bölgede ≥20★ toplamalı. Toplayamıyorsa eşik o bölge için `bot_medyanı − 2`'ye iner (ör. 18), ama 18'in altına inmez.

### 2.2 Eşik altındaki oyuncu ne görür

**Sonuç ekranı (her zafer)** — bugün "Bölüm N açıldı" çipi var (`AppFlowController.cs:676-680`):
- Bölge içi her zaferde ek çip: `Bölge yıldızı 14/20 ★` (hedef her bölümde görünür, "amaç" bu).
- Boss zaferi + eşik altında: başlık `Yeşil Muhafız yenildi!`, alt satır `Kül Geçidi'ne 4 yıldız kaldı`, altında en fazla 3 öneri:
  `Bölüm 3 · 1★ → +2`, `Bölüm 7 · 2★ → +1` (sıra: kazanılabilir yıldız çok → az, sonra bölüm no). Buton: mevcut `EKSİK YILDIZLARA DÖN` metni (`FrontendCopy.cs:112`).
- Eşik tam o zaferle aşıldıysa: `Kül Geçidi açıldı!` (yeni bölge vurgusu).

**Harita**
- Kilitli bölge kartı (`AppFlowController.CampaignCompletion.cs:31-36`): bugünkü `Önce bölüm {0}'i tamamla` (`FrontendCopy.cs:113`) yerine iki satır: `Yeşil Nöbet'ten 16/20 ★` + ilerleme çubuğu; boss yenilmemişse `Önce Yeşil Muhafız'ı yen`.
- Kilit kartında `EKSİK YILDIZLARA DÖN` → bölgenin en düşük yıldızlı bölümünü seçer (bugünkü `LowestStarLevel` `CampaignProgressStore.cs:219-222` bölgeyle sınırlanarak).
- Bölüm brifinginde 3★ şartı somut: `3★: en fazla 5 can kaybet` (`startingLives − ceil(0.75 × startingLives)`). Oyuncu bugün yıldızın nasıl hesaplandığını bilmiyor.

### 2.3 Geriye uyum (açılmış bölge kilitlenmez)

- İstemci kaydına `highestOpenedRegion` alanı, kayıt sürümü **5 → 6** (`CampaignProgressStore.cs:74`, göç deseni `:395-414`).
- Göç: `highestOpenedRegion` = eski kurala göre açık olan en yüksek bölge (önceki bölgenin 10. bölümü ≥1★). Bu oyuncular için hiçbir şey kilitlenmez.
- Yeni kural yalnız `highestOpenedRegion`'ı yükseltir, asla düşürmez. Bulut birleştirmesinde `max` (bugünkü `unlockedLevel` birleştirmesi gibi, `:324`).
- Yeni cihazda bulut geri yüklemesi (alan yoksa): bölgede ≥1★'lı bölüm varsa o bölge açık sayılır.
- Sunucu (`CampaignProgressionCCM/.../CampaignProgressionModule.cs:211`, `:224`) doğrusal `UnlockedLevel` ile kalır; kapı yalnız istemci arayüzünde. Gerekçe: rekabetçi ekonomi yok, valor gizli; Cloud Code dağıtımı gerekmez.
- Dikkat: `unlockedLevel` boss sonrası 11 olur. Kapı kapalıyken "Devam" ve otomatik ilerleme 11'e gitmemeli: `NextUnfinishedLevel` (`CampaignProgressStore.cs:212-217`), `MaxPlayableLevel` (`AppFlowController.ClosedTest.cs:31`), `campaignDestinationLevel` akışı kapıyı kontrol etmeli. Seviye düğmeleri zaten `IsRegionUnlocked` ile AND'leniyor (`AppFlowController.cs:557`).

---

## 3. Bölüm eğrisi (50 bölüm)

Kısaltmalar (desenler §4'te): **T** tek · **T-K/T-U** kısa/uzun tek · **Ç-E** çift erken birleşme · **Ç-G** çift geç birleşme · **Ç-K** çift, kenardan (üst/alt) giriş · **A** çatal (tek giriş ikiye ayrılır) · **Ü3** üç yol.
Giriş kullanımı (veri): *eş* eşzamanlı · *dön* dönüşümlü · *sız* sızma (yan yoldan küçük hızlı grup) · *uyu-N* 2./3. kapı N. dalgada açılır.
Dalga sayısı bugünküyle aynı (denge riski düşük kalsın); değişim yol + karışım + toplamda. Toplam "bugün → hedef", hedef ±%10 bot ile oturtulur.

### Bölge farkları

| Bölge | İmza | Yağmacı dışı pay bugün → hedef | Yol repertuvarı |
|---|---|---|---|
| R1 Yeşil Nöbet | temeller, her bölüm 1 yeni tip/kule | %0→20 → aynı | T, T-K, T-U, ayna, ilk Ç-E (L9) |
| R2 Kül Geçidi | Öncü + Kuşatma | %12–22 → %18–24 | Ç-E, Ç-G, Ç-K, *uyu* |
| R3 Buz Sınırı | hız (×1.10–1.18) + Yenilenen | %13–16 → %16–20 | Ç-K alt, A, *uyu*, nefes T-U |
| R4 Batık Krallık | Şifacı + dönüşümlü baskı | %11–12 → %20–24 | *dön*, A, ilk Ü3 (küçük grup) |
| R5 Son Hisar | hepsi, ustalık | %2–8 → %22–28 | Ü3 *eş/dön/uyu*, A+Ç |

R5'te karışım artınca adet düşer (zırhlılar yağmacıdan çok daha dayanıklı); bu yüzden R5 hedef toplamları bugünden düşük ama bölge içinde **tekdüze artan**.

### Tablo

| # | Ad | Yol | Dalga | Toplam | Öne çıkan | Bu bölümün yeniliği |
|---|---|---|---|---|---|---|
| 1 | İlk Nöbet | T | 3 | 204 | Yağmacı | İlk savunma: tek yol, okçu kulesi. |
| 2 | Dar Patika | T | 3 | 228 | Yağmacı | Kışla: yolu askerlerle kes. |
| 3 | Çifte Baskı | T-K | 4 | 340 | Yağmacı | Kısa yol: düşman kaleye daha çabuk varır, büyü kulesi. |
| 4 | Eski Köprü | T | 5 | 403 | Öncü | İlk zırhlı öncü. |
| 5 | Sessiz Koruluk | T-U | 6 | 542 | Öncü | Uzun kıvrım: kule rollerini dağıt. |
| 6 | Kırık Yol | T | 6 | 549 | Yenilenen | İlk yenilenen düşman. |
| 7 | Gece Akını | T (ayna) | 6 | 570 | Şifacı | İlk şifacı; yol ters yönde kıvrılıyor. |
| 8 | Taş Çember | T | 6 | 583 | Kuşatma | İlk kuşatma birimi. |
| 9 | Kapı Önü | Ç-E *sız* | 6 | 599→610 | karma | İlk yan kapı: 8–12 yağmacı ikinci girişten, ana yola erken katılır. |
| 10 | Yeşil Muhafız | T (sabit) | 8 | 824 | Boss | Bölge lordu. |
| 11 | Kızıl Toprak | T | 6 | 588→590 | Öncü | Kül biyomu, öncü kalabalığı. |
| 12 | Maden Yolu | Ç-E *sız* | 6 | 618→620 | Şifacı | Yan kapı dönüyor; şifacı öncüyü koruyor. |
| 13 | Ağır Adımlar | T-U | 6 | 648→650 | Öncü+Kuşatma | Uzun yolda ağır birlik. |
| 14 | İki Cephe | Ç-G *eş* | 6 | 669→670 | Öncü | Gerçek ikinci cephe: kollar kaleye yakın birleşir. |
| 15 | Kül Yağmuru | Ç-E *dön* | 6 | 631→640 | Yağmacı | Dalgalar sırayla 1. ve 2. girişten. |
| 16 | Demir Konvoy | Ç-G | 6 | 703→700 | Şifacı+Öncü | Konvoy yalnız yan yoldan gelir. |
| 17 | Yarık Geçit | Ç-K (üst) | 6 | 742→740 | Yağmacı | Yan yol yukarıdan iner, çok kısa. |
| 18 | Kuşatma Kampı | Ç-G *uyu-3* | 6 | 766→770 | Kuşatma | İkinci kapı 3. dalgada açılır, kuşatma oradan. |
| 19 | Kor Ateşi | Ç-G *eş* | 6 | 730→790 | karma | İki yolu birlikte tut (boss provası). |
| 20 | Geçit Efendisi | Ç-G | 9 | 1188 | Boss | Takviyeler 1. girişten (`first`). |
| 21 | İlk Ayaz | Ç-E | 6 | 910 | hızlı paket | Buz: düşmanlar %10 hızlı. |
| 22 | Donmuş Dere | Ç-K (alt) | 6 | 954 | Yağmacı | Alt kenardan giriş — yeni yön. |
| 23 | Beyaz Sis | A (WP4 yoksa Ç-G) | 6 | 1000 | Yenilenen | Tek giriş ikiye ayrılıyor. |
| 24 | Keskin Rüzgar | Ç-G *eş* | 6 | 1034 | Öncü | Kahraman iki yol arasında koşar. |
| 25 | Buz Köprüsü | T-U | 6 | 988 | Yenilenen | Nefes: tek uzun yol, yenilenen yoğun. |
| 26 | Kurtuluş Yolu | Ç-G *uyu-2* | 6 | 1065 | gecikmeli paket | Uyuyan kapı erken açılır. |
| 27 | Soğuk Pusu | A *dön* | 6 | 1095 | karma | Çatalın kolları dalga dalga değişir. |
| 28 | Kristal Kapı | Ç-K (üst) | 6 | 1130 | Şifacı | Şifacılar ilk dalgadan. |
| 29 | Uzun Gece | Ç-G *eş* | 6 | 1083→1140 | karma | Boss provası. |
| 30 | Buz Hükümdarı | Ç-G | 9 | 1593 | Boss | Hızlı boss + yenilenen takviye. |
| 31 | Kayıp Kıyı | Ç-E *dön* | 6 | 995 | Şifacı | Sessiz yol değişimi. |
| 32 | Batık Yol | Ç-E | 6 | 1016 | Şifacı+Yenilenen | Birleşme noktasını tek kaleyle kapsa. |
| 33 | Gelgit | Ç-G *dön* | 6 | 1058 | karma | Her dalga diğer kapı: bütçeyi böl. |
| 34 | İkiz Liman | Ç-K (üst) *eş* | 6 | 1064 | Kuşatma | Kuşatma yan yoldan. |
| 35 | Derin Sular | T-U | 6 | 1028 | Öncü | Nefes: sessiz yükseltme bölümü. |
| 36 | Korsan Baskını | A *sız* | 6 | 1102 | Öncü+Şifacı | Çatal + yan koldan baskın. |
| 37 | Yıkık Saray | Ü3 (3. yol küçük) | 6 | 1133 | Yenilenen+Şifacı | İlk üçüncü giriş: küçük grup. |
| 38 | Boğaz Savunması | Ç-G *eş* | 6 | 1142 | Öncü | İki koldan aynı anda yüklenme. |
| 39 | Son Fener | Ü3 *uyu-4* | 6 | 1166 | karma | Üçüncü kapı geç açılır. |
| 40 | Derinliklerin Lordu | Ç-G | 9 | 2070 | Boss | Takviyeler girişler arasında döner (`rotate`). |
| 41 | Dönüş Yolu | Ç-G | 6 | 1310→1250 | tüm tipler | Tüm dersler, gerçek karışım. |
| 42 | Kara Ordu | Ç-G + 3. yol görünür/kapalı | 6 | 1763→1300 | Öncü | Üçüncü yol haritada, henüz kullanılmıyor. |
| 43 | Üç Koldan | Ü3 *eş* | 6 | 1776→1350 | karma | Üç giriş tam güçte. |
| 44 | Son Erzak | Ü3 *dön* | 6 | 1406→1400 | karma | Girişler sırayla. |
| 45 | Kırılan Hat | T-U | 6 | 1218→1300 | Kuşatma | Nefes: tek uzun yol, ağır kuşatma. |
| 46 | Şafak Öncesi | Ü3 *uyu-3* | 6 | 1365→1450 | karma | Üçüncü yol geç açılır. |
| 47 | Büyük Kuşatma | A + Ç-K | 7 | 2080→1500 | Kuşatma | Çatal + kenar kapı: kahramanı kuşatmaya yönelt. |
| 48 | İç Kale | Ü3 (geç birleşme) | 7 | 2204→1550 | Öncü+Şifacı | Uzun koridorlar, kaleye yakın birleşme. |
| 49 | Son Kapı | Ü3 *eş* | 7 | 1039→1600 | karma | Final provası (HP 3.78 → ~2.2, adet artar). |
| 50 | Son Lord | Ü3 | 8 | 2495→2000 | Boss | Takviyeler üç girişten (`all`). |

Yenilik sıklığı: her bölgede yol deseni en az 4 kez değişir; en uzun "aynı desen" serisi 2 bölüm.

---

## 4. Yol varyasyonları kataloğu

Ortak sınırlar (kod): dünya x −2…24, z 0.6…10.9; kale hep sağ kenar (x≈24); yollar arası ≥2.4 (`RouteSeparation` `:210`); kamera sabit ortografik 8.9, odak (11, 5.5) (`GameSessionController.cs:495-496`).

| Desen | Tanım | Kodda üretim | Yuva etkisi | iPhone yatay risk | Önceden gösterim |
|---|---|---|---|---|---|
| **T** tek | Bugünkü S-yılan | Şablon + kaydırma (`:188-198`), tabloda sabit şablon no | 8 yuva ana yolda (bugünkü) | düşük | — |
| **T-K / T-U** kısa/uzun | 3 kıvrımlı ~34 birim / 5 kıvrımlı ~56 birim | `RegionOnePaths`'e 2 yeni şablon; tablo seçer | Kısa: yuvalar sıkışır (aralık 2.6→~1.9, `:475`); uzun: daha çok viraj kapsaması | Uzun şablon üst/alt kenara yaklaşır → HUD altında kalabilir | Brifingde "Kısa yol: düşman çabuk gelir" |
| **Ç-E** erken birleşme | Kol yol ana yola %20–30'da katılır | `targetFraction` parametre (`:226`); `0.18` alt sınır (`:258`) | Kol yolun özel kısmı kısa → yuva gerekmez | düşük | Giriş 2 oku + "Sıradaki" şeridinde ② |
| **Ç-G** geç birleşme | %60–75'te katılır, iki uzun ayrı cephe | Aynı parametre; üst sınır 0.8 | **8 yuvadan 2–3'ü kol yolun özel kısmına ayrılmalı** (bugün yalnız ana yol dağıtılıyor `:490`) | Orta: iki uzun koridor ekranı doldurur | ② oku + şeritte adet/rota |
| **Ç-K** kenar girişi | Kol yol üst ya da alt kenardan iner | Kenar tercihi parametresi (bugün sabit ceza 0.6 `:293`, `:301`) | Değişmez | **Yüksek**: üst giriş (z 10.9) üst HUD, alt giriş (z 0.6) radyal menü/alt çubuk altında kalabilir | Kenarda okun HUD güvenli alanı içine kaydırılması |
| ***uyu-N*** uyuyan kapı | Yol baştan çizili, N. dalgaya kadar spawn yok | **Yalnız veri** (`routeId` kullanımı) | Değişmez | düşük | Kapalı kapı ikonu + "Dalga 3'te açılır"; N−1. aradan sonra ok yanıp söner |
| ***dön*** dönüşümlü | Her dalga farklı giriş | Yalnız veri (L15 bugün yapıyor) | Değişmez | düşük | Aradaki şeritte hangi giriş: "Sıradaki: ② Yağmacı ×50" |
| ***sız*** sızma | Yan yoldan küçük hızlı grup | Yalnız veri | Değişmez | düşük | ② yanında küçük "×10" |
| **A** çatal | Tek giriş ~%30'da ikiye ayrılır, ~%75'te birleşir | Yeni `BuildSplitRoute`; `IsClearBranch` giriş aralığı şartı (`:342`) çatal için atlanır; `OwnedBranchSegments` (`:375`) "orta parça" döndürmeli; süsleme `DefenderBattlefieldDressing.cs:51-57,140-150` | 2 yuva iki kolun arasına (iki kolu birden görür) | Orta: iki paralel kol + 2.4 aralık dikeyde yer ister | Tek ok "1/2"; şeritte kol harfi (A/B) |
| **Ü3** üç yol | Üç giriş, iki kol farklı noktalarda katılır | Bugünkü kod (L43+); tabloya taşınır | 8 yuva: 5 ana + 3 kollar | Orta-yüksek: üç giriş solda/üst/alt sıkışır | ①②③ okları, şeritte rota numaraları |
| **ayna** | z ekseninde ters | Bugünkü `MirrorPath` (`:650-654`); bölge çift/tek yerine tablo bayrağı | Değişmez | düşük | — |

**Önceden gösterme (WP6)**
1. "Sıradaki" şeridi (`GameSessionController.cs:444-458`): tüm bölgelerde grupları rota numarasıyla listele: `Sıradaki: ① Yağmacı ×50 • ② Öncü ×6`. Bugün yalnız R1 adetleri gösteriyor, R2–5 tek tip adı (`CampaignContent.cs:101-103`).
2. Giriş okları dalga bazında: hazırlık/arada yalnız sıradaki dalgada spawn olacak girişler yanar; dalga sırasında bekleyen spawn'ı olan giriş açık kalır. Bugün `:918`'de hepsi kapanıyor.
3. Uyuyan kapı: kapalı kapı ikonu + "Dalga N'de açılır" (TR/EN).
4. Brifing: taktik önizleme (`FrontendTacticalMapPreview`) zaten yolları çiziyor; desen etiketi ekle: "Çift yol · geç birleşme".

---

## 5. Uygulama planı

Kural: iki paket aynı dosyaya yazmaz; aynı dosyaya dokunan paketler sıralıdır. Hiçbir pakette commit/push/deploy/migration/DB yazma yok.

**WP1 — Yıldız kapısı mantığı (~2.5 sa)**
- Hedef: `CampaignProgressStore.IsRegionOpen(r)` 20★ + boss kuralını ve `highestOpenedRegion` geriye uyumunu uygular; v5 kayıt v6'ya kayıpsız göçer.
- Dosyalar: `Assets/Scripts/Core/CampaignProgressStore.cs`, yeni `Assets/Scripts/Data/CampaignRegionGate.cs` (eşik sabitleri), yeni `Assets/Tests/EditMode/CampaignRegionGateTests.cs`.
- Kapı: EditMode testleri (göç: eski açık bölge açık kalır; 19★ kapalı, 20★ açık; birleştirmede max; `NextUnfinishedLevel` kapalı bölgeye gitmez).

**WP2 — Yıldız kapısı arayüzü (~3 sa, WP1'den sonra)**
- Hedef: harita kilit kartı `16/20 ★` + eksik yıldız butonu; sonuç ekranında bölge yıldız çipi ve boss sonrası öneri listesi.
- Dosyalar: `Assets/Scripts/UI/AppFlowController.CampaignCompletion.cs`, `Assets/Scripts/UI/AppFlowController.cs`, `Assets/Scripts/UI/FrontendCopy.cs`, `Assets/Tests/PlayMode/CampaignFlowTests.cs`.
- Kapı: EditMode derleme + `CampaignLocalizationTests`; PlayMode yalnız Emre onayıyla.

**WP3 — Yol düzeni tablosu + üretici parametreleri (~4 sa)**
- Hedef: 50 bölümün yol deseni veri tablosundan gelir; tablo varsayılanları bugünkü haritaları **birebir** üretir, yalnız işaretli bölümler değişir.
- Dosyalar: yeni `Assets/Scripts/Data/CampaignLayoutTable.cs`, `Assets/Scripts/Data/CampaignLevelFactory.cs` (`EntranceCount` tablodan; şablon, ayna, `mergeFraction`, kenar tercihi parametreleri; 2 yeni şablon; geç birleşmede kol yola yuva payı; ölü kod silme), `Assets/Tests/EditMode/CoreRulesTests.cs` (rota testleri), yeni `Assets/Tests/EditMode/CampaignLayoutSnapshotTests.cs` (değişmeyen bölümlerin rota/yuva özeti eşit).
- Kapı: EditMode tümü; 50 haritanın hepsi üretilebiliyor (bugün kol yol bulunamazsa `:204` hata atıyor).

**WP4 — Çatal deseni (opsiyonel, ~5 sa, WP3'ten sonra; aynı dosya)**
- Hedef: tek girişten ayrılıp birleşen kol; süsleme ve muhafız erişimi doğru.
- Dosyalar: `CampaignLevelFactory.cs` (WP3 bittikten sonra devralır), `Assets/Scripts/Presentation/DefenderBattlefieldDressing.cs`, `Assets/Scripts/Core/GreyboxCombatants.cs` (`CanReach` `:838-845`), `CoreRulesTests.cs`.
- Kapı: EditMode rota/yuva/süsleme testleri.

**WP6 — Önizleme + dalga bazında giriş okları (~3.5 sa, WP3'ten sonra)**
- Hedef: "Sıradaki" şeridinde rota numaraları; ok yalnız aktif girişte; uyuyan kapı ikonu.
- Dosyalar: `Assets/Scripts/Core/GameSessionController.cs` (`CreateEntranceIndicators`, `RefreshThreatPreviewForPhase`, `:918`), `Assets/Scripts/Data/CampaignContent.cs` (`CreateWaves` önizleme metni), yeni `Assets/Tests/EditMode/WavePreviewTests.cs`.
- Kapı: EditMode; önizleme TR/EN metin testleri.

**WP5 — Dalga verisi (~8 sa, WP3 (+WP4) sonrası)**
- Hedef: §3 tablosundaki giriş kullanımı, tip karışımı ve toplamlar; her bölge `contentVersion` artırılır.
- Dosyalar: `Assets/Resources/Campaign/region-01..05.json`, `boss-profiles.json` (gerekirse).
- Kapı: `CampaignContentTests`, `CampaignDifficultyCurveTests`, `FirstRegionDifficultyCurveTests`, `CampaignBalanceTests` (bot: tüm planlarda zafer hedefi, peak ≤60, orta plan bölge başına ≥20★).

**WP7 — Bölüm hedef metinleri (~2 sa, WP2 + WP5 sonrası; FrontendCopy WP2'den devralınır)**
- Hedef: değişen bölümlerin hedef metni yeni desenle uyumlu (ör. L9 "İlk yan kapı", L37 "Üçüncü giriş", L45 "Tek uzun yol").
- Dosyalar: `Assets/Scripts/UI/AppFlowController.ClosedTest.cs` (`:75-125`), `Assets/Scripts/UI/FrontendCopy.cs`, `GameSessionController.cs` L14/L43 mesajları (`:337-349`) → WP6 bittikten sonra.
- Kapı: `CampaignLocalizationTests`.

**Sıra**: WP1 ∥ WP3 → WP2 ∥ WP4 → WP6 ∥ WP5 → WP7 → koordinatör tam koşu + tek simülatör.
(WP6 ve WP4 farklı dosyalarda; WP5 haritalar donduktan sonra başlar, yoksa bot dengesi iki kez yapılır.)

---

## 6. Riskler

1. **Denge (en büyük risk).** Yuvalar ana yoldan türetiliyor (`:480-501`); bir bölümün yolu değişince 8 yuvanın hepsi kayar, bot sonuçları değişir. Önlem: WP3 varsayılanları bugünkü haritaları birebir üretir + anlık görüntü testi; yalnız tabloda işaretli ~25 bölüm değişir. R5'te karışım %2→%25 olunca HP/adet yeniden ayarlanmalı; bot 1 tur ≈ dakikalar, ~5–8 tur beklenir.
2. **Checkpoint (kayıt).** Checkpoint kuleleri **yuva indeksiyle** geri kurar (`GameSessionController.Checkpoints.cs:78-81`) ve yalnız `contentVersion`'ı kontrol eder (`:54`, `:97`). Harita değişip `contentVersion` artmazsa yarım kalmış savaş kuleleri yanlış yerlere koyar. Önlem: haritası değişen her bölgenin JSON sürümü WP5'te artırılır; daha sağlamı checkpoint'e harita özeti (`mapHash`) eklemek (WP3 kapsamına alınabilir).
3. **Kayıt v6 göçü.** `highestOpenedRegion` yanlış hesaplanırsa açık bölge kilitlenir. Önlem: göç yalnız yükseltir; test eski v5 dosyalarıyla.
4. **Performans.** ≤60 eşzamanlı düşman bot testinde (`CampaignBalanceTests.cs:76`, `FirstRegionBalanceTests.cs:23`); üç girişte eşzamanlı spawn tepeyi artırır → aralıklar rota başına ayarlanmalı. Çatal ve ek kol yollar yol süsleme parçası ekler; SetPass sınırı bölge başına +3 (commit d448e53) aşılabilir — WP4 sonrası `AuditLayer2PerfTests` koşulmalı.
5. **Okunurluk (iPhone yatay).** Üst/alt kenar girişleri HUD ile çakışır; kenar girişli bölümler (17, 22, 28, 34, 47) için simülatörde tek kontrol.
6. **Çeviri TR/EN.** Yeni metinler (~15 satır: kapı, çip, öneri, uyuyan kapı, desen etiketleri, değişen hedefler) `LocalizedCopy` ile; `CampaignLocalizationTests` kapsamalı. `{0}/30` sabit metni (`FrontendCopy.cs:111`) eşikle karışmamalı.

---

## 7. Emre'ye sorulacak kararlar

1. **Eşik**: bölge başına 20/30 + boss (önerilen) · bölge başına 18 · kümülatif (20/40/60/80)?
2. **Çatal deseni (WP4, ~5 sa)** yapılsın mı? Önerilen: **evet**, WP5'ten önce; hayırsa A satırları Ç-G olur.
3. **Üçüncü giriş R4'e (L37, küçük grup) çekilsin mi?** Önerilen: **evet**; L43 "Üç Koldan" tam güçte kalır.
4. **R5 karışımı**: yağmacı dışı pay %2→%25, adet buna göre düşer (önerilen) · adetler korunur, karışım az artar?
5. **İlk yan kapı L9'da** (önerilen) mı, bugünkü gibi L14'te mi?

Ayrıca: `BuildProgressiveWaves`/`BuildExistingBossWaves` ölü kodu (`CampaignLevelFactory.cs:391-467`) WP3'te silinebilir.
