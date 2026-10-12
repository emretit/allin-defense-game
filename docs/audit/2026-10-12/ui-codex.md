ALLIN: Defense — bağımsız UI/UX denetimi, 12 Ekim 2026.
Öncelik: ekran süslemekten önce bilgi hiyerarşisini, inşa kararını ve okunabilirliği düzeltmek; HUD ilk sırada.
7 taşma kaydı iki metin alanına dayanıyor; gerçek boss verisinden bağımsız son-dalga etiketi ayrıca düzeltilmeli.
Faz 0–1 temeli kodda var; yeni şekiller henüz ekranlara bağlanmamış, fiziksel dokunma ölçüsü ortaklaştırılmamış.
Öneri: onaylı Temiz Fantastik + uGUI korunarak 71–103 saat uygulama; %20 payla yaklaşık 85–124 saat.

## 1. Kapsam, kanıt ve ilk beş hızlı kazanım

Salt okuma yapıldı; repo, ad değişikliği, Unity Editor ve servisler değiştirilmedi. Yalnız bu rapor yazıldı. Eski audit/TODO belgeleri ve diğer denetçinin raporu okunmadı. Güncel kaynak kod denetlendi; Unity testi, oyun çalıştırma, fiziksel cihaz ve ses denemesi yapılmadı. Aşağıdaki kabul kapıları **gelecek uygulama içindir**, geçtiği iddia edilmiyor.
**Kanıt işaretleri:** G = görüntüde gözlenen; K = kaynakta doğrulanan; Ç = tasarım değerlendirmesi/çıkarım; Ö = öneri. Ekranı bulunmayan davranışlara “Doğrulanamadı” yazıldı. Dosya adları aşağıda verilen dizinlere göredir.
**Kısaltmalar:** UI = `Assets/Scripts/UI/`; P = `Assets/Scripts/Presentation/`; C = `Assets/Scripts/Core/`; E = `Assets/Editor/`; T = `Assets/Tests/`; S = `/tmp/allin-audit-2026-10-12/screens/`.
**Görülen bütün güncel PNG'ler:** `main-menu.png`, `map-Turkish.png`, `map-English.png`, `settings-Turkish.png`, `settings-English.png`, `account-Turkish.png`, `account-English.png`, `battle.png`, `victory.png`, `defeat.png`, `returned-map.png`. Ayrıca onaylı `docs/ui-modernization/mockups/{A-battle-combat,B-menu,C-battle-prep}.png` ayrı ayrı incelendi. Eski logo adı bulgu değildir.
**Başlangıç planı:** `docs/ui-modernization/UI_MODERNIZATION_PLAN_2026-10-12.md`, `FAZ0_FOUNDATION_2026-10-12.md`, `FAZ1_DESIGN_SYSTEM_2026-10-12.md`, `mockups/tokens.css` okundu. Planın eski durum/sayı iddiaları yerine aşağıdaki güncel kod sayımı kullanıldı. Görüntüler ile mevcut kod aynı sunumu her noktada kanıtlamıyor: örneğin fotoğrafta altın CTA metni beyazken güncel `UI/AppFlowController.cs:1209` koyu `GoldText` seçiyor; bunu yeniden yapılacak font/renk işi saymıyorum.

Her hızlı kazanım **tek başına ≤1 saat**; toplam 3 saat, dar değişiklik ve hedefli kontrol varsayımıyla. Tam cihaz matrisi sonraki fazdadır.

| Öncelik | Değişiklik ve görünür sonuç | Dosya/kanıt | Efor | Kabul kapısı |
|---|---|---|---:|---|
| Q1 | Brifing hedefini iki kısa satıra indir; sonuç açıklamasını içerik yüksekliğine göre büyüt, CTA ile araya boşluk koy | UI/AppFlowController.Screens.cs:145; AppFlowController.cs:357; AppFlowController.ClosedTest.cs:53 | 1 sa | TR/EN harita, zafer, yenilgi çekimi; iki alanda TMP taşması ve düğmeyle kesişme 0 |
| Q2 | Son dalga yerine gerçek `bossWave` bilgisinden BOSS etiketi üret | P/DefenderHudController.cs:708,733; C/GameSessionController.Battle.cs:12 | 0,5 sa | Boss olmayan bölümün son dalgası normal; bölüm 10 gerçek boss uyarısı; iki ekran |
| Q3 | Bölge başlığını canlı dil yenilemesine kat; ayarlar açıldığında seçili dili hemen boya | UI/AppFlowController.Screens.cs:199,337; AppFlowController.cs:1363 | 0,5 sa | İlk TR açılışı seçili; map TR→EN başlığı anında Greenwatch; ayarlar/harita çekimi |
| Q4 | Yenilgide ve son bölüm zaferinde geçersiz “Sonraki” düğmesini gizle; kalan iki eylemi yeniden hizala | UI/AppFlowController.cs:670; S/defeat.png | 0,5 sa | Yenilgi/son bölüm sonucu: görünür geçersiz eylem yok, retry/map çalışır; iki çekim |
| Q5 | Profil satırını ad + yıldızla sadeleştir; normal kayıt durumunu küçük simgeye, ayrıntıyı hesap ekranına taşı | UI/AppFlowController.cs:591; S/main-menu.png | 0,5 sa | Uzun ad/TR/EN tek satır sınırı, hata varsa erişilebilir durum açıklaması; menü çekimi |

## 2. Referansları nasıl kullanıyorum?

**Karşılaştırma sınırı:** Referans oyunların güncel istemcileri bu çalışmada oynanmadı. Aşağıdaki ekran karşılaştırmaları bilinen tasarım örüntülerinin uzman yorumudur; güncel piksel konumları veya ölçülmüş başarı oranları değildir. Resmî kaynaklarla özellik kapsamı kontrol edildi, ALLIN yerleşimleri özgün öneridir.
- **KR — Kingdom Rush:** Kule yetenekleri/uzmanlaşma ve kahramanlarla savunma kararını oyun alanına bağlayan yaklaşım örneği. [Ironhide resmî oyun sayfası](https://www.kingdomrush.com/kingdom-rush), [resmî App Store kaydı](https://apps.apple.com/us/app/kingdom-rush-tower-defense-td/id516378985). İnşa ve harita için, özellik sayısını kopyalamadan bağlamsal bilgi ilkesini alın.
- **BTD — Bloons TD 6:** Birbirinden ayrılan geliştirme yolları, kahramanlar, boss karşılaşmaları ve çevrimdışı tek oyuncu akışı resmî ürün tanımında yer alıyor. [Ninja Kiwi'nin Steam sayfası](https://store.steampowered.com/app/960090/Bloons_TD_6/). ALLIN için ders: rol/fiyat/sonuç karşılaştırması ve ayrıntıyı seçime bağlı açmak.
- **CR — Clash Royale:** Kart geliştirme ve savaş kazanımını belirgin görsel nesnelerle anlatan referans. [Supercell resmî sayfası](https://supercell.com/en/games/clashroyale/). Buradan alınacak şey koleksiyon/mağaza yoğunluğu değil, kaynak ve sonuç simgelerinin okunurluğudur.
- AFK Journey'nin boyalı dünya bütünlüğü ve Arknights'ın taktik bilgi katmanları estetik karşılaştırma adaylarıdır; bu denetimde güncel ekranları doğrulanmadığından somut bulgu kaynağı yapılmadı.

## 3. Ekran ekran öneri

Wireframe'ler yerleşim sözleşmesidir; kutular görsel çerçeve zorunluluğu değildir. “İyi/kötü” değerlendirmeleri belirtilen G/K kanıtının Ç yorumudur. Ölçüler fiziksel mobil mantıksal puan (pt); Canvas birimine dönüşüm §5'te.

### 3.1 Boot / yükleme / yön değişimi
**İyi (K):** Tek marka odağı; yüklemeden önce en az bir kare geri bildirim çiziliyor, yeniden deneme yolu var (`UI/AppFlowController.cs:292,700,804`; `FrontendLoadingOverlay.cs:64`).
**Kötü (K→Ç):** Boot yalnız logo ve küçük bölüm yazısı; ayrıca yükleme katmanı ve yön perdesi ayrı yüzeyler. Teknik “cihaz yataya alınıyor” dili oyuncuya iş akışı açıklıyor. Statik logo animasyonu ~0,70 sn; reduced-motion ~0,28 sn (`AppFlowController.cs:305`). Ekran görüntüsü yok, geçişte titreme/süre algısı **Doğrulanamadı**.
**Referans:** CR'nin marka → oynanabilir hedef odağı; BTD'nin çevrimdışı oyun yaklaşımı. Bu ekranda hesap bağlantısını bekleme kapısı yapmayın.
**Yeni (Ö):** Aynı arka plan/arma; tek “Savunma hazırlanıyor”; ancak gerçek ilerleme varsa çubuk, yoksa kısa hareketli gösterge. Hata halinde geri/yeniden dene; rotasyon gereğinde küçük cihaz simgesi, bitince kendiliğinden kalksın.
```text
[             ARMA              ]
[       Savunma hazırlanıyor    ]
[              ...             ]
[ Hata olursa: Geri | Yeniden dene ]
```

### 3.2 İlk karşılama
**İyi (K):** Gerçek ilk yerel misafirde Welcome açılıyor; “Oyuna başla” doğrudan bölüm 1'e gidiyor, hesap butonu gizli (`UI/AppFlowController.cs:161`; `Screens.cs:246`).
**Kötü (K→Ç):** Üç satır hikâye, kampanya etiketi ve misafir dipnotu ilk aksiyonla yarışabilir; kullanıcıya sadece görsel üzerinden ilk açılış denetimi yapılmış gibi davranılamaz: kabul modu misafir girişini zorla yapıyor (`Acceptance.cs:53`). Welcome görüntüsü **Doğrulanamadı**.
**Referans:** KR'nin oyun içinde öğrenme yaklaşımı; CR'nin ilk savaş odağı. Giriş ekranında ansiklopedi sunmayın.
**Yeni (Ö):** Menüyle aynı bileşenler; tek cümle “Kaleyi koru”; “İlk savunmaya başla”; misafir bilgisi küçük, okunabilir alt satır. İsim seçimi zorunlu olmasın.
```text
[ ARMA / kale illüstrasyonu ]
[ Kaleyi koru.             ]
[ İLK SAVUNMAYA BAŞLA      ]
[ İlerleme cihazında saklanır ]
```

### 3.3 Ana menü
**İyi (G):** Kale illüstrasyonu güçlü atmosfer kuruyor; alt ana eylem başparmağa yakın; bölüm süresi/dalga sayısı önceden biliniyor (`S/main-menu.png`).
**Kötü (G→Ç):** Üstte küçük logo ile geniş boşluk, altında iki satırlı teknik profil; “Misafir Muhafız · Misafir” tekrarı; tanıtım sloganı gerçek bölüm adından büyük. Altın ve doygun mavi dev düğmeler dikkati bölüyor. Kesik köşeli kalın çerçeve her öğeye aynı ağırlığı veriyor.
**Referans:** CR'den tek savaş CTA'sının önceliği; KR'den ilerlemenin haritayla ilişkisi. Sosyal/meta menü yoğunluğunu almayın.
**Yeni (Ö):** B-menu yönünü kabul: küçük profil/ayar üstte; orta illüstrasyon; alt bölüm kartı + tek altın CTA + iki sade kısayol. Mockup'taki dev logo telefon kısa olduğunda küçülsün. Kahraman kısayolu salt ad değiştirmeye açılacaksa “Kahraman adı” deyin; boş tam ekran vaat etmeyin.
```text
[ Profil  * 0/30        (ayar) ]
[          ARMA / KALE        ]
[ İlk Nöbet       * * *       ]
[ 3 dalga | ~2,5 dk | 230     ]
[     SAVUNMAYA BAŞLA         ]
[   Harita     | Kahraman adı ]
```

### 3.4 Kampanya haritası
**İyi (G/K):** Düğümler arası yol okunuyor; seçili bölüm belirgin, kilitler görünür; geri dönünce kaydırma konumu saklanıyor (`S/map-Turkish.png`; `UI/AppFlowController.cs:490,1125`).
**Kötü (G):** Tek bölge için dev altın “1”, her düğüm altında “KİLİTLİ”, keskin zikzak çizgiler; illüstrasyonun yollarıyla ilişkisi zayıf. Alt brifing haritanın büyük bölümünü kapatıyor. `map-English.png` başlığı “YEŞİL NÖBET”, `returned-map.png` “GREENWATCH”: canlı dil yenileme tutarsızlığı.
**Referans:** KR'nin bölüm haritası örüntüsü: düğüm durumu/yıldızla ilerleme. CR'deki ödül ilerlemesinden yalnız “sonraki anlamlı hedef” görünürlüğünü alın.
**Yeni (Ö):** Tek bölgede sekme yerine “Yeşil Nöbet · 0/30”; madalyon düğümler, kilit simgesi, kazanılmış yıldızlar; bir sonraki erişilebilir düğümde sınırlı vurgu. Brifing başlangıçta 112–144 pt kompakt; açılınca harita üzerinde yarım panel.
```text
[ <   Yeşil Nöbet        0/30 ]
[       (1)*** -- (2)         ]
[                  -- (kilit)]
[ Bölüm 1: İlk Nöbet      ^   ]
[       SAVUNMAYA BAŞLA       ]
```

### 3.5 Brifing / seçili bölüm
**İyi (G):** Minik rota, dalga/süre/altın/giriş ve düşman bilgisi karar öncesi sunuluyor (`S/map-Turkish.png`, `map-English.png`).
**Kötü (G/K):** Sağdaki dar sütuna başlık + uzun hedef sıkışmış; metin CTA arkasına akıyor. Ayrı tam ekran brifing mevcut akışta kurulmamış; haritanın Selection Bar'ıdır (`UI/AppFlowController.Screens.cs:101,126`; `AppFlowController.cs:1138`). “HEDEF” altında üç talimat aynı anda öğretiliyor.
**Referans:** BTD'deki bilgi katmanlama ve KR'deki harita→bölüm hazırlığı ilkesi. İlk bölüm için ayrıntılı strateji reçetesi şart değil.
**Yeni (Ö):** Kompakt kartta ad/süre/dalga; açılmış panelde rota solda, düşman ikonları sağda; hedef en altta tam genişlikte iki satır. CTA içerikten bağımsız sabit footer. İçerik gerçekten sığmazsa panel içi kaydırma; yazıyı küçülterek saklamayın.
```text
[ İlk Nöbet       3 dalga | 2,5 dk ]
[ ROTA ÖNİZLEME  | Yağmacı (i)     ]
[ Hedef: Kaleyi koru.              ]
[ İlk ipucu: Yol kıvrımına okçu kur.] 
[         SAVUNMAYA BAŞLA          ]
```

### 3.6 Ayarlar
**İyi (G/K):** Müzik/efekt ayrı; titreşim, sarsıntı, azaltılmış hareket var; satırın tamamı toggle (`Screens.cs:389`), içerik kaydırılabilir (`Screens.cs:323`).
**Kötü (G/K):** TR çekiminde iki dil aynı mavi; EN seçildikten sonra vurgu var. Büyük form çerçeveleri ve boş alt alan; slider doluluk izi yok, yalnız siyah ray ve uç tutamacı (`Screens.cs:375`). Kahraman adı oyun tercihlerinin arasında; kare doluluk “açık/kapalı”yı yalnız renkle anlatıyor (`S/settings-*.png`).
**Referans:** CR/BTD ayarlarının ikincil yardımcı işlev olması ilkesi; işlem düğmelerini savaş CTA'sı kadar baskın yapmayın.
**Yeni (Ö):** Dil segmenti, Ses, Erişilebilirlik, Hesap grupları; dolu slider + yüzde; “Açık” etiketli switch. Otomatik kaydet, geri/kapat; geniş altın “Tamam” yerine sade geri. Her satır ≥48 pt, kaydırma belirgin.
```text
[ < AYARLAR     [TR | EN] ]
[ Ses: Müzik ----o 80%    ]
[      Efekt -----o 100%  ]
[ Titreşim     [Açık o]   ]
[ Azaltılmış hareket [o]  ]
[ Hesap ve kayıt       > ]
```

### 3.7 Hesap / ilerlemeyi koruma
**İyi (G):** Misafir devamı ana eylem; açıklama yararı anlatıyor; kapat ve yasal bağlantılar erişilebilir (`S/account-Turkish.png`, `account-English.png`).
**Kötü (G→Ç):** Zaten misafir oyuncuya “Misafir olarak başla” yeniden başlangıç çağrışımı; legal butonlar içerikle aynı görsel boyda; büyük boş alan. İlerleme hesabın bu cihazdaki durumuyla somutlaştırılmıyor.
**Referans:** BTD'nin çevrimdışı oyun ile bulut kaydını ayırması; CR'nin hesap/oyun ayrımı. “Hesap şart” hissi vermeden ilerleme koruması sunun.
**Yeni (Ö):** Duruma göre “Bu cihazda kayıtlı · 12 yıldız”; “Hesabı bağla”; “Oyuna dön”; yasal metinler küçük ama ≥44 pt hit alanlı bağlantı. Bağlantı bekleniyor/hata/iptal/başarı aynı kartta; mevcut ilerleme içe aktarma onayını koru (`UI/AppFlowController.ClosedTest.cs:30`).
```text
[ İLERLEMENİ KORU        X ]
[ Bu cihazda kayıtlı: **12 ]
[       HESABI BAĞLA       ]
[        Oyuna dön        ]
[ Gizlilik   |   Koşullar  ]
```

### 3.8 Savaş HUD — hazırlık
**İyi (G):** Can/altın/dalga ve rota açık; sarı ilk yuva yönlendirmesi var; dalga oyuncu başlatana kadar bekliyor (`S/battle.png`; C/GameSessionController.Tutorial.cs:24).
**Kötü (G→Ç):** Üstte dört büyük kutu ve iki kontrol, ortada geniş tehdit kutusu ve ikinci talimat kutusu; haritanın üst yarısına UI yığılıyor. İkonlar metinden çok küçük; “Warrior READY” oyuncuya yeteneğin ne yaptığını söylemiyor. İptal boş seçimde de görünür (`P/DefenderHudController.cs:369` yalnız etkileşimi kapatıyor).
**Referans:** KR'de bağlamsal yuva kararı; BTD'de tur başlatma odağı. Aynı anda hem envanter hem ders hem durum paneli açmayın.
**Yeni (Ö):** C mockup temeli: 56 pt üst şerit; can/altın sol, dalga/faz orta, hız/pause sağ. İlk ipucu hedef yuvaya bağlı, tehdit tek çip. Sağ alt büyük başlat; yetenek ayrı yuvarlak kontrol. Statü kutusunu kaldır.
```text
[can 20][altın230] [0/3 HAZIRLIK] [1x][||]
[               ilk dalga: Yağmacı (i)  ]
[        YOL / YUVALAR / KALE           ]
[    ^ Bu yuvaya dokun                 ]
[                     (yetenek) [BAŞLAT]]
```

### 3.9 Savaş HUD — aktif dalga / ara
**İyi (K):** Yetenek hazır/cooldown/ölü/paused durumları ayrılmış; can kritik rengi ve erken dalga görünürlüğü var (`P/DefenderHudController.cs:280,852,910`). Görüntü yalnız hazırlığı gösteriyor; aktif dalga okunurluğu **Doğrulanamadı**.
**Kötü (K→Ç):** Canlı sayılar ve durum metinleri aynı büyük çerçeve diliyle güncelleniyor; cooldown dikdörtgen dolgu, sürekli hazır nabzı. Önizleme genişliği güvenli alanın %50'sine kadar; altındaki ikinci mesaj da alan tüketiyor (`P/DefenderHudResponsiveLayout.cs:225`).
**Referans:** BTD'den bilgi ve kontrol bölgesini ayırma; CR'den kaynak sayısını hızlı taranabilir kılma.
**Yeni (Ö):** A mockup, fakat seçili kule yokken sağ panel yok. Dalga segmentleri gerçek sayıya bağlı; aktif tehdit ve gelecek dalga ayrı anlam taşısın. Altın +N 0,4 sn; kritik can yalnız eşik geçişinde; yetenek 68–74 pt halka ve kalan saniye. Erken çağır butonu ödül veriyorsa gerçek ödülü göster, yoksa “bonus” uydurma.
```text
[20][340 +12] [2/3 SAVASTA ===--] [2x][||]
[           oyun alanı                  ]
[                                      ]
[                    [Erken çağır] (8sn)]
```

### 3.10 Boss
**İyi (K):** Yaklaşan gerçek boss için önizleme `NextWaveHasBoss` okuyor (`P/DefenderHudController.cs:329`); tehdit rengi var.
**Kötü (K):** Üst bar ve faz etiketi yalnız son dalga indeksini boss sayıyor (`DefenderHudController.cs:708,733`). Bölüm 1–9'un bütün `bossWave` değerleri false; gerçek boss bölüm 10 sonunda (`Assets/Resources/FirstRegionCampaign.json`). Normal son dalgada yanlış alarm koddan çıkarılıyor; çalıştırılarak **Doğrulanamadı**. Boss savaş görüntüsü yok.
**Referans:** BTD boss karşılaşmalarındaki ayrı tehlike odağı; KR'nin büyük düşman/saldırı okuma ilkesi. Sadece kırmızı bir “BOSS” sözcüğü yeterli değil.
**Yeni (Ö):** Boss gerçekten aktifken orta üstte ad + can + faz çizgileri; gerçek saldırı hazırlığında dünyada menzil/tehlike izi ve kısa “Darbeyle kes” çipi. Görüşü örten tam ekran alarm yerine 0,6 sn kısa giriş; reduced-motion'da sabit simge.
```text
[20][340] [BÖLGE LORDU ====|==|--] [1x][||]
[           [Saldırı hazırlanıyor]       ]
[ YOL: tehlike alanı ////    boss        ]
[                     (SARSICI DARBE)    ]
```

### 3.11 İnşa akışı
**İyi (K):** Fiyat kontrolü, seçim önizlemesi ve yanlış slota inşa önlemi var (`C/GameSessionController.cs:1128`). Menü seçili yuvaya yakın ve sınır içine sıkıştırılıyor (`P/DefenderHudResponsiveLayout.cs:176`).
**Kötü (K→Ç):** Yuva + tür önizlemesi + aynı türe tekrar = **3 dokunuş**; ilk dokunuştan sonra açıklama okunmazsa “çalışmadı” hissi. Mevcut üç yatay buton, radyal mockup uygulanmış değil (`P/DefenderHudController.cs:647`). Önizleme/kurma aynı görsel yüzeyde farklı anlam taşıyor.
**Referans:** KR'nin yuvanın yanında sınırlı tür seçimi; BTD'nin satın alma öncesi fiyat/rol görünürlüğü. Üç temel kule için kapsamlı mağaza gerekmez.
**Yeni (Ö):** Yuva + tür = 2 dokunuş. Türde ikon, fiyat, kısa rol; basılı tutma isteğe bağlı bilgi/menzil, bırakma satın alma üretmesin. Radyal menü kenarda yarım yaya dönüşsün, yetersiz altında “25 eksik”; hata sadece kırmızı renkle anlatılmasın. Güvenli sınır/diğer hit hedefleri çakışırsa sabit üçlü alt panel kullanılsın; maliyet +2–3 sa faza dahil.
```text
[      Okçu75     Kışla100      ]
[             (YUVA)   Büyü125  ]
[ Okçu: tek hedef / uzun menzil ]
[ boş alana dokun = kapat       ]
```

### 3.12 Seçili kule paneli
**İyi (K):** Rol, istatistik, satış iadesi ve eksik altın sunuluyor; satış iki aşamalı 3 sn onayla korunuyor (`P/DefenderHudController.Specializations.cs:14`; `.Sales.cs:26`).
**Kötü (K→Ç):** Önce genel spec, sonra specialization kodu metinleri tekrar yazıyor; anlatım tek kaynaktan gelmiyor (`DefenderHudController.cs:377`). Bağlamsal 380×340 panelde ad/rol/üç stat/iki işlem/hint yoğun; gerçek seçili kule PNG'si yok, taşma **Doğrulanamadı**.
**Referans:** BTD'nin geliştirme kararını seçilen kuleye bağlayan yaklaşımı; KR'nin kule rolü/uzmanlaşma ayrımı.
**Yeni (Ö):** A mockup'taki sağ kart, ancak güvenli genişliğin en çok %28–30'u; seçili yuvayı veya kale yaklaşımını kapatıyorsa karşı tarafa geçsin. İkon + rol; üç sayı; yükseltmede önce→sonra farkı; altın CTA, ayrı tehlike satış. Satış onayı aynı düğmede korunsun, panel açılırken oyun hızını gizlice değiştirmeyin.
```text
[ KULE / menzil    | Okçu  Lv2     X ]
[                 | Tek hedef       ]
[                 | 24>32  4,8  2/sn]
[                 | YÜKSELT75 Sat+52 ]
```

### 3.13 Uzmanlaşma modalı
**İyi (K):** Oyun duraklıyor; iki dal ve fiyat karşılaştırılabiliyor, açılma/gold koşulları denetleniyor (`P/DefenderHudController.Specializations.cs:41`).
**Kötü (K→Ç):** İki upgrade butonu klonlanıp ad/açıklama/fiyat/eksik-altın aynı etikete basılıyor; geri butonunda uzun “satıp yeniden kur” cümlesi. 440×260 sabit kart, tek 32 font; modalın PNG'si yok, küçük cihaz sığması **Doğrulanamadı**.
**Referans:** BTD'nin farklı geliştirme yolları; KR'nin seçimin taktik rolünü görünür kılan uzmanlaşmaları.
**Yeni (Ö):** İki kart: büyük ikon, rol, iki somut fark, ayrı fiyat/“Seç” butonu; ortak “Dal değiştirmek için satıp yeniden kurmalısın” dipnotu. Başta “Savaş duraklatıldı”; geri dönünce eski hız/faz geri gelsin. Uzun isimlerde düzen büyüsün, metin min sınırının altına inmesin.
```text
[ UZMANLAŞ · Savaş duraklatıldı         X ]
[ Hızlı Atış           | Keskin Nişancı   ]
[ Çok atış / düşük tek | Zırhlı hedef odağı]
[ SEÇ 220             | SEÇ 220           ]
[ Dal değiştirmek için satıp yeniden kur  ]
```

### 3.14 Pause
**İyi (K):** Dünya etkileşimini kesen modal blocker ve reduced-motion yolu var (`P/DefenderHudController.cs:1422,1478`).
**Kötü (K→Ç):** Yalnız Resume/MainMenu; restart ve ses kontrolü burada yok. 920×440 sabit kart ve 80 birim görsel düğmeler; hit alanı genişletilse de ekrandaki boyu ayrıca ölçülmeli (`:1452,1469`). Çıkışta gerçek kayıp/kaldığı yerden devam davranışı bu denetimde **Doğrulanamadı**.
**Referans:** KR/BTD'nin savaş içi yardımcı kontrol ayrımı; devam ana eylem, çıkış ikincil.
**Yeni (Ö):** Büyük Devam, altında yeniden başlat/harita; küçük ses tercihleri. Yeniden başlatma/çıkışın somut sonucu açıklansın, doğrulanmış checkpoint varsa ona göre metin üretin. Sağ başparmak açar; merkezde Devam kolay erişilir.
```text
[           DURAKLATILDI            ]
[              DEVAM               ]
[ Yeniden başlat | Haritaya dön     ]
[ Müzik o---    Efekt o---          ]
```

### 3.15 Zafer
**İyi (G/K):** Ana eylem sonraki bölüm; geri/tekrar var, kayıt durumu görünür (`S/victory.png`; `UI/AppFlowController.cs:646`).
**Kötü (G→Ç):** Kutuda iki dev başlık ve metinsel “0/3 STARS”; ödül yerine hesap eşitleme paragrafı. Sevincin görsel odağı eksik. Ancak 0 yıldız gerçek ödül hatası değil: çekim 0,25 sn, ilk yıldız 0,35 sn (`Acceptance.cs:139`; `AppFlowController.cs:1317`). Kabul modu gerçek savaşı kazanmaz (`Acceptance.cs:104`).
**Referans:** CR'deki kazanımı simgeyle ödüllendirme; KR'deki bölüm/yıldız ilerlemesi. Hesap bağlama ana kutlamayı bölmemeli.
**Yeni (Ö):** “Zafer” + bölüm adı daha küçük; üç gerçek yıldız simgesi, kazanılanlar sırayla dolsun; kaydedildi işareti küçük. Somut yeni açılan içerik varsa bir satır. Sonraki bölüme tek CTA, harita/tekrar ikincil. Hesap önerisini kutlama sonrası isteğe bağlı kart yapın.
```text
[          ZAFER!          ]
[         *  *  *          ]
[      İlk Nöbet tamamlandı]
[ SONRAKİ BÖLÜM            ]
[ Harita | Tekrar   Kayıt ✓]
```

### 3.16 Yenilgi
**İyi (G/K):** Hangi dalga/düşman ve uygulanabilir ipucu var; retry altın; sonraki bölüm kodda etkileşimsiz (`S/defeat.png`; `UI/AppFlowController.cs:668,670`).
**Kötü (G→Ç):** Büyük kırmızı uzun başlık, 0 yıldız ve kullanılmayan “Sonraki” alanı cezalandırıcı/kalabalık. İpucu kendi 52 birimlik kutusunu aşıyor; şu an ekranda okunuyor oluşu taşma olmadığını kanıtlamaz (`:359`).
**Referans:** KR/BTD retry döngüsü: oyuncuya bir sonraki denemede farklı ne yapacağını söyle. İlerleme olmayan kazanım göstergesi gerekmez.
**Yeni (Ö):** Kısa “Savunma düştü”; “Dalga 2/3 · Yağmacılar kaleye ulaştı”; düşman ikonu + tek ipucu. Retry en büyük, harita sade; yıldızlar ve geçersiz sonraki eylem yok. Gerçek savaşta oluşmuş leak verisine göre kişiselleştir.
```text
[        Savunma düştü       ]
[ Dalga 2/3 · Yağmacı        ]
[ Son kıvrıma bir okçu ekle. ]
[         YENİDEN DENE       ]
[         Haritaya dön      ]
```

### 3.17 Öğretici ipuçları
**İyi (K):** İlk bölümle sınırlı ve oyun durumuna göre sıralı; ilk nokta dünya halkasıyla gösteriliyor (`C/GameSessionController.Tutorial.cs:15`).
**Kötü (K/G):** İpuçları 8 sn mesaj; bir ipucunda kahraman adı + yetenek etkisi + basılı tutma jesti; her aşama tamamlanma yerine bazen sayı/faz üzerinden seçiliyor. “Sarı halkalı noktaya dokun” mesajı hedeften uzakta (`S/battle.png`; `Tutorial.cs:17,29,34`).
**Referans:** KR'nin yeni düşman/kuleyi gerektiğinde tanıtma ilkesi; CR'nin ilk eylemi yaptırarak öğretme örüntüsü.
**Yeni (Ö):** Tek görev/tek vurgu; 1: yuva, 2: okçu, 3: dalga, 4: gerçek düşman yakındayken yetenek. Tamamlanınca kısa tik; 8 sn sonra kaybolsa bile küçük “?” ile geri çağrı. Oyuncunun denemesini engelleyen sürekli karartma yok; “Biliyorum” atlama ve tercihten tekrar açma.
```text
[ oyun alanı        (hedef yuva) ]
[                    ^          ]
[          Buraya okçu kur.  [?] ]
[                  [Biliyorum]  ]
```

## 4. Akış ve bilgi mimarisi

**Dokunuş muhasebesi (K + belirtilen varsayım):** İlk zaferin tek bir kesin toplamı yok; strateji, sızıntı ve erken çağırma değişir. Aşağıdaki sayılar kodun önerdiği öğretici yolu izleyen, iptalsiz oyuncu senaryosudur; oynayarak doğrulanmış kazanma garantisi değildir.
- Yeni yerel kullanıcı: Boot otomatik → Welcome “Oyuna başla” **1** → yataya dönüş otomatik → yuva/tür/onay ×2 = **6** → ilk dalga **1**: iki kuleyle savaşa **8 dokunuş** (`UI/AppFlowController.Screens.cs:261`; C/GameSessionController.cs:1151; C/GameSessionController.Tutorial.cs:17).
- Ekran setindeki ana menüden ilk savunma: Hazırlan **1** → otomatik seçili haritada Başlat **1** → iki kule **6** → dalga **1** = **9**. İsteğe bağlı aynı düğümü yeniden seçme +1; zorunlu sayılmadı (`UI/AppFlowController.cs:410,440`).
- Öğretici önerisinin kalanında yetenek **1** + üçüncü kule **3** + kule seç/yükselt **2** = **6** daha: gerçek yeni kullanıcı yolu **14**, ana menü yolu **15**. Dalga 2/3 otomatik ara sayacını beklerse ek dokunuş 0; erken çağırırsa +2 (`C/GameSessionController.Battle.cs:100`). Zafer görünmesi otomatik; sonraki bölüm +1. Yenilgiler/ek strateji bu toplamın dışındadır.
- Önerilen 2 dokunuşlu inşa: yeni kullanıcı ilk dalgaya **6**, öğretici önerilerinin sonuna **11**; ana menüden de direkt başlayarak aynı hedef. Harita ve brifing isteğe bağlı kalsın. Mevcut üç dokunuşu koruma tercih edilirse ikinci tür dokunuşu ayrı “KUR 75” yüzeyi olsun; maliyet/tap kazanımı olmaz.
**Sürtünme:** Welcome direkt savaşa giderken yeniden açılmış sıfır-yıldız menüsü önce haritaya yönlendiriyor; iki farklı başlangıç zihinsel modeli var. Hesap zorunlu değil, bu korunmalı. İlk başarıdan önce kahraman adını ve ayrıntılı ansiklopediyi istemeyin. İlk zafer akışı ekran setinden kanıtlanamaz, çünkü kabul modu faz/yeniden yükleme çağrılarını otomatik yapıyor (`Acceptance.cs:53,81,104`).
**Yön kararı:** Portre menü / yatay savaş **MVP'de korunsun**: portrede tek elle gezinti, yatayda rota genişliği kazanımı makul. Savaş pause/sonuç/retry/sonraki bölüm yatay kalmalı; yalnız açık menü/harita talebinde portreye dönmeli. Mevcut politika buna uygun (`C/MobileOrientationController.cs:40`; `UI/AppFlowController.cs:98,646,724,789,840`).
**Yön riskleri:** Bekleme 3 sn ve ardışık kararlı boyut/safe-area kontrolü var (`UI/FrontendOrientationWait.cs:7,17`). Otomatik dönüşün kilit açık/kapalı cihazda, iki yatay yönde, arka plana geçip dönmede davranışı **Doğrulanamadı**. Tüm oyunu yataya almak +6–10 sa yeniden yerleşim/cihaz testi demektir; kullanıcı testi bunu gerekçelendirmeden onaylı portre yatırımını atmayın.
**Eksik ekranlar / MVP:** “Kahramanım” var ama yalnız ad formu (`UI/AppFlowController.Hero.cs:15`); tam kahraman seçimi/gelişimi varmış gibi yeni navigasyon koymayın. MVP için kule/düşman tam ansiklopedisi yerine brifing ve kule panelinden ortak bilgi kartı yeterli (rol, direnç, karşı hamle); +4–6 sa opsiyon. Yeni kule açılışı ilk karşılaşmada kısa kart; boş mağaza/envanter/görev/sezon ekranı yok. Kayıt hata/çakışma/hesap bağlama başarısı için mevcut hesap ekranında açık durumlar gereklidir; bu akışlar canlı servis çağrısıyla doğrulanmadı.

## 5. Teknik zemin ve riskler

### 5.1 Nasıl kuruluyor, ne yeniden kullanılabilir?
- **K:** Ön yüz `AppFlowController` kısmi sınıflarında runtime `new GameObject` ile; ekranlar ihtiyaçta kurulup saklanıyor (`UI/AppFlowController.cs:225,1138`). `CreateButton/CreatePanel/CreateText` fabrikaları var (`:1180,1274,1344`), fakat bunlar bağımsız durumlu bileşen kiti değil. AppFlow parçaları toplam **2.193 satır** (`wc -l Assets/Scripts/UI/AppFlowController*.cs`).
- **K:** HUD, `Assets/Art/GeneratedPresentation/DefenderHud.prefab` builder çıktısı; runtime referans arama, çerçeve ekleme, yeniden parent etme ve layout katmanı var (`E/DefenderPresentationAssetBuilder.cs:1303`; P/DefenderHudController.cs:494,647). HUD controller parçaları **1.911**, responsive layout **392** satır. Aynı görsel karar builder/runtime/style/layout arasında bölünüyor.
- **K:** Varsayılan builder mevcut prefabı koruyor; açık `RegenerateHudPrefab()` yolu artık mevcut (`E/DefenderPresentationAssetBuilder.cs:1287,1308`). “Builder değişikliği hiçbir şekilde uygulanamaz” eski iddiası yanlış. Yeni işi builder'a yazınca açık yeniden üretim ve GUID/bağlantı kontrolü teslim adımı olmalı.
- **K:** `UiDesignSystemAssets.asset` dokuz sprite/font rolünü içeriyor (`Assets/Art/GeneratedPresentation/UiDesignSystemAssets.asset:16`). Ancak runtime kaynakta bu sınıfın **tüketicisi yok**: `rg -n 'UiDesignSystemAssets|Rounded10|Rounded14|Rounded20|GoldGradient|CooldownRing' Assets/Scripts` yalnız sınıf tanımını döndürüyor. Menü hâlâ eski panel/button sprite'larını okuyor (`UI/AppFlowController.cs:1186,1278`; `Assets/Resources/Presentation/FrontendPresentationTheme.asset:35`).
- **K:** Font hazırlığı gerçek: Regular'ın 600/700 rolleri gerçek Bold assetine bağlı; SemiBold ayrı dosya değil, 700'e eşlenmiş (`E/UiDesignSystemAssetBuilder.cs:38`; `NotoSans-DC.asset:219274`). `FontStyles.Bold` bulunması artık otomatik olarak “sahte bold” kusuru değildir.

### 5.2 Güncel sayımlar — kapsamı belli sayılar

Sayım birimi eşleşme adedi, test adedi değil. C# dosyaları, mevcut çalışma ağacı; yorumdaki eşleşmeler otomatik sayımdan ayıklanmadı. `new Color[...]`, `Color.white`, `Hex(...)`, `Color.Lerp` bu yapıcı sayısına dahil değil.

| Kapsam | `new Color/Color32(...)` | `FontStyles.Bold` | `UiTokens.` |
|---|---:|---:|---:|
| `Assets/Scripts/UI/*.cs`, UiTokens dahil | 74 (tamamı UiTokens) | 19 | 41 |
| UI, UiTokens hariç | **0** | 19 | 41 |
| `P/DefenderHudController*.cs` | **1**, Specializations:54 | 16 | 68 |
| Tüm `P/*.cs` (HUD dahil) | 48; çoğu dünya/VFX | 16 | 98 |
| İki hedef Editor builder | 7; eski şekil pikseli/dünya materyali | 0 | 26 |

Komutlar: `rg -n 'new\s+Color(32)?\s*\(' Assets/Scripts/UI Assets/Scripts/Presentation Assets/Editor/{UiDesignSystemAssetBuilder,DefenderPresentationAssetBuilder}.cs -g '*.cs'`; `rg -o 'FontStyles\.Bold' Assets/Scripts/UI Assets/Scripts/Presentation -g '*.cs'`; `rg -o 'UiTokens\.' ...`. Sayılar Python `re.findall` ile kapsam başına toplandı; UI+HUD toplam Bold **35**. Dünya rengi 48'i UI borcu gibi raporlamayın. Açık renk yapıcısına ek hedef-tipli `Color x = new(...)` araması bu iki runtime dizininde eşleşmedi.
**Gerçek token tüketimi:** UI buton basış/giriş/yükleme; frontend theme palet/motion; HUD renk/style/overlay süreleri (`UI/FrontendButtonFeedback.cs:1`; P/FrontendPresentationTheme.cs:48; P/DefenderHudController.cs:1490). Ancak ekran ölçüleri çoğunlukla sabit sayılar; `FrontendNavigation.cs:20,47` hâlâ 0,15/0,06 sn kullanıyor. Tek palette geçmek, tüm yerleşim/animasyonların tokenlaşması değildir.
**Nesne adı bağımlılığı:** Açık string aramalarında tüm testlerde **133 kullanım / 90 ayrı literal**; dünya/ışık/shader/ses/model adları olan 33 kullanım çıkarılınca **100 UI kullanım / 70 ayrı UI ad/yolu**. Dinamik isimli yardımcılar ve reflection alan adları ayrıca vardır; bu rakam tam semantik bağımlılık sayısı değil, yeniden üretilebilir statik alt sınırdır.
Sayım desenleri: `\.Find\s*\(\s*"([^"]+)"`, `(FindButton|FindText|GetButtonInteractable)\s*\(\s*"([^"]+)"`, `\.name\s*==\s*"([^"]+)"`, `\bButton\(\s*"([^"]+)"`, `\bClick\(\s*\w+\s*,\s*"([^"]+)"`; `Assets/Tests/**/*.cs` üzerinde. UI dışı sınıflanan 20 literal: Greenwatch Moon Key, Moon Fill, Gate Ember Fill, Road Hand-Laid Stone Borders, Universal Render Pipeline/Lit, Scenic Battlefield Dressing, Road Edge, Ambient Wind, Ambient Detail, Greybox World/Active Entrance Indicators, Entrance Arrow 1/2, Greenwatch Environment, Licensed Nature 01 trees_A_medium, Free Placement 9, Stationary Defender Anchor 1, Greenwatch Valley, Road Organic Earth Surface, Road Surface, Head.
Örnek kırılgan test kanıtı: `T/PlayMode/GreyboxSceneTests.cs:947` doğrudan `SelectedPanel/BuildChoices`; `FrontendExperienceTests.cs:125` Settings Account adı; `AuditLayer2UxTests.cs:34` dinamik Button(name). Üretim HUD'u da yollara bağlı (`P/DefenderHudController.cs:494`); yalnız testleri düzeltmek yeterli olmaz.

### 5.3 Yerelleştirme ve yedi taşmanın gerçek kaynağı
**K:** 32 sabit TR/EN `FrontendCopy` üyesi, açık `LocalizedCopy` yapısı ve `LocalizedTmpText` binding var (`UI/FrontendCopy.cs:4,34`; `AppFlowController.cs:1358`). Türkçe metin sözlüğü hâlâ kampanya/eski HUD için mevcut (`GameLocalization.cs:21`), dinamik metinler açık çiftlerle hesaplanıyor. “Bütün İngilizce kırılgan Türkçe anahtarına bağlı” artık doğru değil; karma yapı var.
**K+G:** Bölge başlığı interpolated string olarak bir kez üretiliyor (`Screens.cs:199`), `HandleLanguageChanged` onu yenilemiyor (`AppFlowController.cs:1363`). `map-English.png` / `returned-map.png` farkı bununla uyumlu. Dil açılış vurgusu da sadece change eventinde boyanıyor; ilk `BuildSettingsModal` iki butonu aynı mavi kuruyor (`Screens.cs:337`).

| S/report.json kaydı | Gerçek alan/kök neden | Düzeltme |
|---|---|---|
| map-Turkish + settings-Turkish | Aynı `Briefing Reward`, 32 font, yaklaşık 112 Canvas yüksekliği; uzun hedef | Tam genişlikte içerik layout'u + kısa hedef |
| map-English + settings-English + returned-map | Aynı alanın daha uzun İngilizcesi | TR/EN ve %30 uzama testi; CTA ayrı sabit alan |
| defeat | `Result Reward`: 25 bold, 850×52; iki satır bile satır yüksekliğini aşabiliyor | En az içerik preferredHeight; metin/CTA aralığı kontrolü |
| victory | Aynı sonuç alanı; kayıt + uzun hesap bağlama cümlesi üç satıra yayılıyor | Kayıt çipi + ayrı isteğe bağlı hesap önerisi |

Kanıt: `UI/AppFlowController.Screens.cs:109,145`; `AppFlowController.cs:357,664`; `ClosedTest.cs:53`. Settings taşması, ayar label'ının taşması **değil**: modal altında harita aktif fakat CanvasGroup alpha=0 kaldığı için (`UI/AppFlowController.cs:876`) `GetComponentsInChildren<TMP_Text>()` kapatılmış görüş alanını ayırmadan geziyor (`Acceptance.cs:143`). Bu yine harita kusurudur; hata raporu kök element/path ve locale ile tekilleştirilsin. TMP taşma testine kardeş dikdörtgenlerin kesişmesi ve görünür/örtülü ayrımı eklenmeli.
**Güvenilirlik:** `S/report.json` failed; 7 hata mevcut. 226 UI nesnesi yedi örnekte sabit, bellek 195.077.310→195.114.366 bayt: bu kısa rota örneğinde belirgin nesne artışı yok; uzun oyun memory leak testi değildir. İlk savaş kullanılabilir ~580 ms, en uzun kare ~167 ms; retry ~380 ms, next ~414 ms, battle→map ~350 ms. Bunlar simülatör/otomatik akış ölçümü, gerçek cihaz 60 FPS kanıtı değildir.

### 5.4 Safe-area, ölçü ve en-boy oranı
**K:** Ön yüz güvenli alan kökü, her boyut değişiminde insets yenileniyor (`UI/SafeAreaFitter.cs:20`); HUD ayrı metriklerle güvenli alanı hesaplıyor (`P/DefenderHudResponsiveLayout.cs:207`). Kamera safe aspect'e göre ortografik ölçüyor (`P/AdaptiveBattlefieldCamera.cs:72`). Bunları kaldırıp sabit ekran koordinatına dönmeyin.
**Risk (K→Ç):** Ön yüz 1080×1920 genişliğe göre (`AppFlowController.cs:238`), savaş 1920×1080 ve 0,5 eşleme (`C/GameSessionController.cs:822`), loading ayrı 0,5 (`FrontendLoadingOverlay.cs:35`). `MobileTouchTarget.cs:10` minimum **112 Canvas birimi**, fiziksel pt değil. Örneğin 402 pt genişlikli portrede 112×402/1080 ≈ **41,7 pt**; 375 pt'de 38,9 pt. 140 birimlik ana CTA yeterince büyük olabilir, bu bütün kontrollerin ≥44 pt olduğu anlamına gelmez. Fiziksel ölçüm **Doğrulanamadı**.
**Ö:** Bir referans ölçek sözleşmesi seçin: mevcut canvasları koruyup `UiMetrics` ile pt→canvas dönüştürmek en düşük risk. 44 pt minimum, ana eylem 52–56 pt, yetenek 68–74 pt; hit alanları komşu hedefle kesişmesin. `Screen.dpi` tek doğruluk kaynağı olmasın; platform mantıksal ölçeği/test cihaz profili kullanılsın.
**Ö:** CSS'deki 14 px köşeyi `RadiusMedium=14` Canvas birimiyle eşit sanmayın: 402/1080 örneğinde 5,2 pt olur. `UiTokens.cs:5` “pt = Canvas reference units” diyor; `tokens.css` ekran CSS ölçüsü. Bu fark mockup'ı “benzemiyor” yapan gizli risk; yarıçap, yazı, ikon ve gölgede ortak dönüşüm gerekiyor.
**Ö:** 375×667 ve 402×874 portre; 16:9, 19,5:9 ve 4:3 yatay; iki yatay çentik yönü; kısa safe-area ve klavye açık ad formu. Column eşiği 0,70 (`FrontendResponsiveLayout.cs:20`), tablet brifingi ayrıca doğrulanmalı. Kahraman adı modalı doğrudan canvas çocuğu/sabit 900×720 (`Hero.cs:18,31`), klavye-safe yerleşim ayrıca gerekir.

## 6. Görsel dil ve uygulama mimarisi

**Temiz Fantastik + uGUI + yeni AI görseli yok kararı yeterli bir UI temeli.** Premium his için zorunlu araç değişimi yok; primitive'ler menü/HUD yüzeylerini oluşturabilir. Ancak yalnız yuvarlak köşe/gradient, `S/main-menu.png` ile `S/battle.png` arasındaki dünya sanat farkını kapatmaz. UI önce, ardından mevcut 3B varlıkların sanat yönetimi; bu değerlendirme görsel çıkarımdır.
**Dünya köprüsü (Ö):** Boyalı kaleyi koru; savaşta aynı ay mavisi taş/altın ışık vurgularını kullan. Neon mavi yuva diskini taş halka + seçildiğinde altın ile değiştir; koyu yeşil düz zemine 2–3 kontrollü ton/doku çeşitliliği; tekdüze parlak taş çitlerini daha seyrek/sessiz yap; kahraman/düşman silueti zeminden ayrışsın. Mevcut `P/DefenderSceneLook.cs:41` zaten ışık/fog/postprocess kuruyor; “ışık hiç yok” denemez. `P/DefenderBattlefieldDressing.cs` ve malzeme/biome verisi üzerinde küçük bir bölüm örneğiyle ilerle. Yeni model/AI art gerekmiyor; 10–14 sa ilk tutarlı 3B cila fazı. Tam boyalı 2B savaşa geçiş bu bütçenin dışında, MVP için önerilmiyor.
**İkon (Ö):** Kaynak/kule/yetenek mevcut boyalı atlas; sistem kontrolleri tek çizgi kalınlığında üretilmiş simge. 24–28 pt sistem ikonları, 36–48 pt kule simgeleri; fiyat simgesi yazının yanında. İkonların siyah kare zemini varsa kırpma/alpha doğrulanmalı (mockup A/C'de kare görünüyor; kaynak alpha burada ölçülmedi). Renk tek başına rol belirtmesin: yay/kılıç/kristal + metin.
**Tipografi/renk (Ö):** Cinzel yalnız kısa başlık; Noto gövde 15–17 pt, ikincil 12–14, sayı 20–24. Kritik eylem/istatistiği AutoSize ile 10 pt'ye düşürmeyin. GoldText/gold ana CTA; mavi seçili/odak, kırmızı hasar/tehlike, yeşil hazır/başarı. Gerçek transparan yüzey altında en parlak dünya karesinde kontrast ölçün; token birim testinin koyu backing varsayımı sahne garantisi değildir (`T/EditMode/UiDesignSystemTests.cs:71`).
**Hareket/ses (Ö):** Basış 80–100 ms, panel 180–220 ms, yıldız 3 kısa vuruş; hazır halkası yalnız yeni hazır olduğunda 2 nabız, sürekli yanıp sönme yok. Satın alma kısa taş/metal sesi, yetersiz para düşük “tık”, upgrade yükselen ton; ses tek bilgi kanalı olmasın. Haptik yalnız satın alma/başarı/kritik uyarıda ve tercihe bağlı. Mevcut `Handheld.Vibrate` tek kaba sinyal (`UI/AppFlowController.cs:1337`); ince iOS/Android haptik adaptörü isteğe bağlı, cihaz testi şart.
**Reduced-motion boşluğu (K):** HUD/frontend bazı hareketleri kontrol ediyor; sahne pulse'unun bloom/vignette/renk animasyonu `P/DefenderSceneLook.cs:200` içinde tercihi okumuyor (`rg -n 'ReducedMotion' .../DefenderSceneLook.cs` boş). Ortak motion policy kapsamına dünya efektleri de alınmalı; bugünkü fiziksel rahatsızlık ölçülmedi.

**Mimari öneri (Ö):** Büyük framework/migrasyon yerine küçük, tipli görünüm katmanı:
1. `UiDesignSystemAssets` catalog/theme üstünden açık referansla runtime'a verilsin; sprite/font üretimi builder'da tek kaynak. İlk kit: Surface/Card, Button varyantları, IconButton, StatPill, Chip, Slider/Switch, Modal. Ardından ProgressSegments, RadialCooldown ve edge-aware BuildPicker. Her biri disabled/pressed/selected/ready/error durumlarını veriden alsın.
2. Statik düzeni prefabda görünür tutun; dinamik veriyi C# presenter bağlasın. Builder kontrollü prefab üretimi burada uygun: bileşen builder'ı tek layout kaynağı; runtime tekrar aynı hiyerarşiyi onarmasın. Elle prefab düzenlenecekse builder'ın onun sahibi olmadığı açıkça belirlensin. İki sahipli düzen bırakmayın.
3. `BattleHudView` tipli serialize edilmiş referanslar + `BattleHudState` (can, altın, gerçek boss, faz, seçim); `AppFlowController` routing/account/screen state yönetmeye devam etsin. `SelectedTowerPresenter` genel/spec/uzmanlaşma metnini tek yerden üretsin. Oyun ekonomisi ve kayıt formatı değiştirilmesin.
4. `UiId` ya da view erişim sözleşmesiyle test ve runtime string-path bağımlılığı kademeli azaltılsın. Eski adları geçişte tutun; sırf görünüm değişti diye bütün davranış testlerini silmeyin. Testler “Button var” değil “yetersiz altın satın almaz”, “modal dünya girdisini keser”, “kapatınca önceki hız geri gelir” desin.
5. Mevcut coroutine yaklaşımı yeterli: tek `UiTransition` unscaled zaman + iptal/yeniden açılma + reduced-motion + kapanış tamamlanma callback'i. Yeni tween paketi şart değil. `FrontendNavigation.cs:29` eski ekranı hemen kapatıyor; gerçek çıkış animasyonu yok. Giriş/çıkış aynı politika içinde çözülsün.
6. Statik yüzey, sık değişen sayaç/cooldown, modal katmanı için az sayıda canvas; her düğmeye canvas koymayın. Dirty-state ile sadece değişen sayıyı yazın; profiler ile rebuild/overdraw ölçün. `P/DefenderHudController.cs:288` refresh'te metinleri tekrar atıyor; maliyetin büyük olduğu ölçülmüş değil.

## 7. Sıralı uygulama planı ve kabul kapıları

Aşağıdaki süreler tek deneyimli Unity geliştiricisinin odaklı mühendislik saatleri; tasarım/iterasyon ve hedefli test dahil, yeni sanat/servis işi hariç. Faz 0–1 tekrar yapılmaz. Q satırlarının süreleri tekrar sayılmadı. “Yeni” yazan dosya adı öneridir, mevcut olduğu iddia edilmez.
**Ortak görsel kapı G:** TR/EN, 375×667/402×874 portre, 16:9/19,5:9/4:3 savaş; çentik iki yön; readable minimum font, ≥44 pt hit, sibling overlap 0; normal/reduced-motion. **Ortak işlev kapısı F:** ilgili EditMode/PlayMode testleri; en sonda tüm mevcut suite ve gerçek cihaz. Mevcut Editor kilidi nedeniyle bu denetimde hiçbiri koşulmadı.

| Sıra | Teslim ve dosyalar | Efor | Test + görüntü kabulü |
|---|---|---:|---|
| Q | Yukarıdaki 5 hızlı kazanım | 3 sa | Q tablosundaki 5 hedefli doğrulama |
| 1 | Ölçü, görünürlük ve kabul altyapısı; UI/UiTokens.cs, MobileTouchTarget.cs, AppFlowController.Acceptance.cs; yeni UiMetrics.cs; T/PlayMode/AuditLayer2UxTests.cs | 4–6 sa | Pt dönüşümü/komşu hit çakışması; gerçek sonuç bekleme; boot/welcome/build/selected/boss/pause/spec çekim kapsamı; G örnekleri |
| 2 | Bileşen kitinin prefab üretimi ve referansları; E/UiDesignSystemAssetBuilder.cs, E/DefenderPresentationAssetBuilder.cs, UI/UiDesignSystemAssets.cs; Resources/Presentation catalog/theme; yeni UI/Components/* | 10–14 sa | Varyant/state, font/glif, sprite sınırı/kontrast; test sahnesinde bütün bileşen atlası; yeniden üretim GUID kontrolü |
| 3 | Hazırlık/aktif/boss HUD; P/DefenderHudController.cs, DefenderHudResponsiveLayout.cs, AdaptiveBattlefieldCamera.cs; C/GameSessionController.Battle.cs; generated DefenderHud.prefab | 10–14 sa | Gerçek boss/normal son dalga; cooldown ve hız; rota/slot/kale görünür; üç savaş oranında prep/wave/boss G; oyun alanı örtülmesi önce/sonra ölçümü |
| 4 | 2 dokunuşlu inşa, seçili kule ve uzmanlaşma; C/GameSessionController.cs; P/DefenderHudController.Specializations.cs, .Sales.cs; yeni BuildPicker/SelectedTowerPresenter | 10–14 sa | Tek satın alma, hold-release, yetersiz altın, kenar yuva, satış onayı, modal eski faz/hız; her durum screenshot |
| 5 | Menü/harita/brifing; UI/AppFlowController.Screens.cs, .cs, FrontendNavigation.cs, FrontendCopy.cs; yeni ekran prefabları | 8–12 sa | Welcome→savaş ve Continue akışı; harita scroll korunması, anlık dil, uzun hedef; B-menu karşılaştırması + G |
| 6 | Ayarlar/hesap/kahraman adı; UI/AppFlowController.Screens.cs, .Hero.cs, .ClosedTest.cs; yeni ortak Settings/Account view | 5–7 sa | Ses tercihi, toggle, otomatik kayıt, klavye ve uzun ad; TR/EN/bağlantı hata/iptal screenshots; servis stub ile test |
| 7 | Pause/zafer/yenilgi/öğretici; UI/AppFlowController.cs; P/DefenderHudController.cs; C/GameSessionController.Tutorial.cs; FrontendCopy.cs | 5–7 sa | Son bölümde next yok; gerçek 1/2/3 yıldız; retry; öğretici sıra/atlama; pause/reduced-motion dahil G |
| 8 | Dünya ile görsel uyum; P/DefenderSceneLook.cs, DefenderBattlefieldDressing.cs; ilgili biome/malzeme assetleri | 10–14 sa | İlk bölüm + yoğun dalga/boss önce-sonra; rota/tehdit ayırt edilebilir; fiziksel cihaz GPU/kare bütçesi |
| 9 | Ortak motion/ses, son regresyon; UI/FrontendNavigation.cs, FrontendButtonFeedback.cs; P/DefenderSceneLook.cs, DefenderAudioController.cs, DefenderPresentationFeedbackController.cs; yeni UiTransition | 6–12 sa | Kesilen/açılan modal, reduced-motion, 30 rota turu; EditMode+PlayMode; cihazda 60 FPS hedefi veya ölçülmüş kalite kademesi; tam G seti |

**Toplam:** 71–103 sa; %20 belirsizlik payıyla **85–124 sa** (~11–16 adet 8 saatlik gün). Dünya fazı hariç işlevsel modern UI **61–89 sa**; bütün eforu “birkaç saatte premium” diye tahmin etmeyin. Opsiyonel bilgi kartı +4–6 sa, tümü yatay alternatif +6–10 sa; ana toplama dahil değil.
**İlk gözle görünür teslim:** Q + ölçü/kit + temel HUD sonunda 27–37 sa; kullanıcı A/C ile gerçek cihazda karşılaştırıp sonraki ekranlarda aynı dili uygulayabilir. Mockup'ın font/köşe/alan ölçüleri ölçülmeden toplu yaymayın.
**Bitiş ölçütü:** Kabul raporunda kök taşma 0, bütün belirtilen gerçek oyun durumları çekilmiş; iki dilde 2 dokunuşlu inşa ve ilk zafer yolu gözlenmiş; testler başarılı; cihaz performansı ölçülmüş. Bugünkü 11 PNG bu kapanış için yeterli değil; aktif dalga, boss, seçili kule, uzmanlaşma, pause, boot/welcome ve gerçek oynanmış sonuç hâlâ görsel doğrulama bekliyor.
