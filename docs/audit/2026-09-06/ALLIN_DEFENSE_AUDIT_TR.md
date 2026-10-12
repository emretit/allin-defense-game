# ALLIN: Defense — uçtan uca oyun auditi

**Tarih:** 6 Eylül 2026 · **Kapsam:** mevcut Unity projesi, aktif oyun akışı, 50 bölümün verisi, yerel testler, sunucu kuralları ve depodaki cihaz kanıtları.

## 1. Karar: önce daha iyi kararlar üreten bir savaş sistemi

Projenin korunmaya değer çekirdeği açık: oyuncu sabit noktalara savunma kuruyor, kışlayla düşmanı tutuyor ve kalenin önündeki Knight’ın alan yeteneğini zamanlıyor. Bu, küçük bir ekip için anlaşılır ve yönetilebilir bir ürün vaadi. Kahramanı haritada gezdirme zorunluluğu eklemek gerekmiyor; mevcut “son savunma hattını komuta etme” fikri geliştirilebilir.

Ancak mevcut içerik miktarı, stratejik derinliğin önünde. Üç kule, tek yükseltme basamağı ve tekrar eden dalga yapısı 50 bölümü taşımakta zorlanıyor. İlk bölümün oyuncu hiçbir savunma kurmadan kazanılması bunu somutlaştırıyor. Menü sunumu, hesap sistemi ve teknik kabul altyapısına ciddi emek verilmiş; bir sonraki yatırımın ağırlığı savaş içindeki karar kalitesine kaymalı.

**Önerim:** yeni büyük sistemleri kısa süreliğine durdurup ilk 10 bölümü, üç savunma rolünü, bir ayırt edici boss’u ve güvenilir kayıt akışını tamamlamak. Daha sonra aynı kaliteyi kampanyaya yaymak. Mevcut kanıtlarla projeye geniş oyuncu kitlesine çıkış onayı vermiyorum; kontrollü oyun testine hazırlanabilir.

Bu değerlendirme bir kullanıcı araştırması sonucu veya mağaza başarısı tahmini değildir. Aşağıdaki maddelerde deneyle doğrulanan sonuç, kaynak kodundan çıkarım ve tasarım önerisi ayrı belirtilmiştir.

## 2. Neler incelendi, ne kadar doğrulandı?

Aktif başlangıç zinciri `GameBootstrap → GameSessionController → GreyboxCombatants`. Dosya adındaki “Greybox” ifadesine rağmen gerçek oyun bu savaş sınıflarını kullanıyor. Depodaki eski `GameManager`, `WaveManager`, `Towers`, `Enemies`, `Heroes` ve `DamageResolver` altyapısını mevcut oyunda çalışan özellikler olarak saymadım. Bu ayrım özellikle zırh, yavaşlatma ve kahraman gelişimi için önemli.

İnceleme; ürün notları, başlangıç/menü/hesap/bölüm/sonuç akışları, kampanya üretimi, kule ve düşman davranışları, ödül ekonomisi, yerel kayıt, Cloud Code, arayüz yerleşimi, sanat/ses altyapısı, testler ve geçmiş fiziksel cihaz raporlarını kapsıyor. Güncel Unity hazırlık ekranı ile depodaki tarihli menü ve savaş görüntüleri görsel olarak incelendi.

| Doğrulama | Sonuç ve sınır |
|---|---|
| İzole kopya EditMode | **78/78 geçti.** Bu sayı test keşfinin toplamıdır; ürün kalitesinin yüzdesi değildir. |
| İlk canlı PlayMode paketi | **13/13 geçti.** |
| İlk izole genişletilmiş PlayMode | **19 test: 18 geçti, 1 kaldı.** Mevcut 13 teste, çalışma sırasında projeye eklenen 3 strateji testi ve yalnızca audit kopyasına eklediğim 3 gözlem testi dahil. Başarısızlık: kışla ağırlıklı bölüm 10 stratejisi. |
| Cloud Code birim testleri | **4/4 geçti.** Proje .NET 9 hedefli; yerelde yalnızca .NET 10 bulunduğu için `DOTNET_ROLL_FORWARD=Major` ile çalıştırıldı. Dağıtılmış servis doğrulaması değildir. |
| Kampanya analizi | 50 bölüm, dalga grupları, rotalar, 400 kule noktası ve gelir/sağlık toplamları dışarı aktarıldı. 50 bölümün tamamı elle oynanmadı. |
| Yeni normal kaynaklı deneyler | Bölüm 1: kulesiz ve tek okçulu senaryolar; bölüm 10: sersemletilmiş kuşatma düşmanı. Gerçek sunuma ait katalog kullanıldı. |
| Yeni mobil dağıtım paketi | Başarılı bir yeni paket sonucu alınmadı. Ortak Editor’daki eşzamanlı testler derleme girişimiyle çakıştı; sonuç “Unknown”. Bunu derleme başarısı veya bağımsız ürün derleme hatası saymıyorum. |
| Fiziksel cihaz | Bu audit sırasında yeni telefon testi yapılmadı. 26 Ağustos tarihli fiziksel iPhone sonuçları tarihsel kanıt olarak incelendi. |

**Sürüm notu:** çalışma ağacı zaten değiştirilmişti ve audit sırasında başka geliştirmeler devam etti. Deneyler ayrı `/tmp/defender-audit-20260906` kopyasında yürütüldü. İlk kaynak özeti [source-hashes.json](evidence/source-hashes.json), sonradan gelen kışla değişiklikleri [followup-hashes.json](evidence/followup-hashes.json) ile kayıtlı. Oyun kaynaklarına düzeltme uygulamadım. Yeni gözlem kodu yalnızca geçici kopyaya eklendi; okunabilir örneği [burada](evidence/AuditObservationTests.cs.txt).

## 3. Öncelik sırası

P1, oyuncu güvenini, temel stratejiyi veya yayın hazırlığını doğrudan etkileyen iş; P2, kalite ve sürdürülebilirlik; P3, çekirdek doğrulandıktan sonraki genişleme anlamına geliyor. Sıralama yalnızca teknik güvenlik derecesi değildir.

| Kimlik | Öncelik | Bulgu / iş | Durum |
|---|---|---|---|
| G01 | P1 | İlk bölüm savunma kurmadan kazanılıyor; tek okçu üç yıldız eşiğini geçiyor | Deneyle doğrulandı, açık |
| G02 | P1 | Kışla askerlerinin yol dışındaki yerleşimi temel rolü bozuyordu | Audit sırasında dışarıdan düzeltme geldi; takip sonucu §4.2 |
| G03 | P1 | Sersemletilmiş kuşatma düşmanı kuleyi susturabiliyor | Deneyle doğrulandı, açık |
| G04 | P1 | Geç bölümlerde gelir, sekiz kulenin harcama kapasitesinin çok üstünde | Veri/kod analizi, açık |
| G05 | P1 | Zırh/yavaşlatma/rol vaatleri aktif savaşta tamamlanmamış | Kod analizi, açık |
| G06 | P1 | Aynı temel içerik 50 bölüme yayılıyor; boss fazları sınırlı | Kod + içerik analizi, açık |
| S01 | P1 | Uygulama/sahne yeniden açılışında bulut eşitleme başlatma yolu eksik | Kod akışından doğrulandı; uçtan uca servis testi gerekli |
| S02 | P1 | Kayıt değişimi sırasında eski dosya siliniyor; kurtarma yolu yok | Kod analizi, açık |
| S03 | P1 | İmkânsız yıldız talebi ve güvenilmeyen eski kayıt göçü kabul ediliyor | Yerel sunucu kuralı deneyleriyle doğrulandı |
| S04 | P1 | Hesap değişimi ve reddedilen ödül taleplerinin kullanıcıya yansıması eksik | Kod analizi, açık |
| U01 | P1 | Mobil dokunma hedefi kontrolü canvas birimini fiziksel ölçüyle karıştırıyor | Yerleşim hesabı; cihazda ölçüm gerekli |
| R01 | P1 | “ALLIN: Defense” adı aynı türde daha önce kullanılmış | Birincil kaynakla doğrulandı; ad kararı gerekli |
| R02 | P1 / yayın öncesi | Gizlilik bağlantıları boş; uygulama içi hesap silme akışı bulunamadı | Kaynak/asset taraması; mağaza hazırlığı eksik |
| P01 | P1 / yayın öncesi | Eski cihaz raporunda ciddi termal durum; kabul ölçümü bunu dışlamıyor | Tarihsel ölçüm + güncel kod analizi |
| U02 | P2 | Başlangıç öğretimi, yenilgi açıklaması ve kaldığı yerden devam zayıf | Akış değerlendirmesi |
| V01 | P2 | Menü ile savaş görüntüsü arasında belirgin görsel kalite farkı | Görsel değerlendirme |
| T01 | P2 | Üretim mantığı, otomasyon ve eski sistemler iç içe | Kaynak analizi |
| M01 | P2 | Valor ve kahraman XP’si anlamlı bir seçim döngüsüne bağlanmıyor | Aktif kod analizi |
| A01 | P2 | Oynanış olayları ve oyuncu davranışı ölçümü yok | İncelenen kaynaklarda bulunamadı |

## 4. Savaş ve denge

### 4.1 İlk bölüm oyuncudan savunma istemiyor

Normal başlangıç kaynakları, sunum kataloğu, hız düğmesindeki **1×**, otomatik normal dalga araları ve hiç yetenek kullanmadan:

| Senaryo | Sonuç | Kalan can | Süre | Yıldız kuralına karşılığı |
|---|---|---:|---:|---|
| Hiç kule kurma | Zafer | 4 / 20 | 54,0 sn | 1 yıldız |
| İlk noktaya tek okçu kur; sonra müdahale etme | Zafer | 16 / 20 | 56,5 sn | 3 yıldız |

Kaynak: [kulesiz gözlem](evidence/level1-idle-1x.json), [tek okçu gözlemi](evidence/level1-one-archer-1x.json). Yıldızlar deneyde açılan sonuç ekranından okunmadı; [AppFlowController.cs](/Users/emreaydin/AllinDefense/Assets/Scripts/UI/AppFlowController.cs:884) içindeki `%75 / %40` eşiklerinden hesaplandı. Sınırlı sayıda deterministik senaryodur; oyuncu başarı oranını ölçmez.

İlk bölümün kolay olması doğru olabilir. Fakat hiçbir savunma kararı gerektirmemesi öğretim hedefini boşa çıkarıyor. Ayrıca katalogdaki yaklaşık **4 dakika** beklentisi bu iki senaryodaki gerçek süreyle uyuşmuyor. Sırf süreyi uzatmak için bekleme eklemek çözüm olmaz.

**Değişiklik:** birinci dalga ilk kuleyi, ikinci dalga yükseltme/konum ilişkisini, üçüncü dalga zamanında Kalkan Darbesi kullanımını öğretsin. Kulesiz oyun doğal biçimde başarısız olsun veya açık bir öğretim adımında beklesin. Üç yıldız, gösterilen stratejiyi uygulamayı gerektirsin. Kale muhafızının tek başına işi çözmesini ilk bölüm düşman sayısı, sızma hasarı ve dalga aralığı birlikte ayarlayarak engelleyin.

**Kabul:** sıfır eylem senaryosu tamamlanamasın; önerilen öğretim adımlarını izleyen oyuncu kazanabilsin; tek kuleyle otomatik üç yıldız alınamasın. Ardından 5–8 yeni oyuncuyla yardım almadan ilk kuleyi kurma ve yeteneğin amacını anlama gözlemlensin.

### 4.2 Kışla: ölçülen hata ve çalışma sırasında gelen düzeltme

İlk alınan kaynak kopyasında askerler kulenin sağına/soluna `0,6`, önüne `0,4` birim yerleşiyor; yolu hedeflemiyordu. Asker saldırı menzili `1,45`, sıradan düşmanın engelleyici arama menzili `1,2` idi. Askerler düşmana yürümeyip aynı toplanma noktasını koruyordu.

50 bölümün rotalarına karşı yapılan geometrik analizde **400 yerleşimin 125’inde iki asker de normal düşmanı durduramayacak kadar uzaktaydı**. Bölüm 10’da bu durum sekiz noktanın tamamında vardı; 1, 3, 4, 5 ve 8 numaralı noktalarda iki asker de normal rota üzerindeki düşmana saldırı menzili dışındaydı. Bu, 400 tam oyun deneyi değil; gerçek rota ve yerleşim verilerinin mesafe hesabıdır. İlk izole testte kışla ağırlıklı strateji de bölüm 10’da yenildi.

Audit sırasında ana projeye `FindBarracksRallyPoint` eklendi: askerler en yakın yol parçasına taşınıyor. Asker sağlık/hasar değerleri de artırıldı ve strateji testinin satın alma sırası değişti. Bu değişiklikler audit tarafından yapılmadı. Aynı veriyle yeni formülün analizinde **menzil dışı yerleşim 125’ten 0’a indi**. [Önce/sonra analizi](evidence/rally-followup-analysis.json), [ilk sürüm kodu](evidence/GreyboxCombatants.baseline.cs.txt), [güncel kaynak](/Users/emreaydin/AllinDefense/Assets/Scripts/Core/GreyboxCombatants.cs:410).

**Takip testi: üç strateji de geçti.** Okçu ağırlıklı kurulum 13, kışla ağırlıklı kurulum 2, büyücü ağırlıklı kurulum 20 canla kazandı. [Sonuç XML’i](evidence/strategy-followup.xml). Bu, belirli satın alma sıralarını, hızlandırılmış oyunu, otomatik dalga çağırmayı ve otomatik yeteneği kullanan bir testtir; gerçek oyuncu başarı oranı değildir. Strateji testi sunum kataloğunu ayrıca yüklemiyor. Geometri hatası düzeltilmiş durumda; daha geniş denge doğrulaması açık. Hem geometri hem istatistikler hem satın alma sırası değiştiğinden tek başına neden-sonuç ayrıştırılamaz.

**Kalan iş:** kışla için “kaç saniye düşman tuttu, kaç asker kaybetti, kaç düşmanı durdurdu” ölçümleri; dar köşe ve kesişen rotalarda gerçek engelleme testi; seviye 1/2 arayüz değerlerini gerçek asker verilerinden türetme. İleride oyuncunun yakın yol üzerinde toplanma noktası seçmesi, mevcut ürün vaadine uygun bir derinlik sağlar.

### 4.3 Kule ekonomisi: gelir var, karar alanı erken bitiyor

| Kule | Kurulum | Tek yükseltme | Tam maliyet | Temel görev |
|---|---:|---:|---:|---|
| Okçu | 75 | 55 | 130 | Hızlı tek hedef hasarı |
| Kışla | 100 | 70 | 170 | İki askerle yolu tutma |
| Büyücü | 125 | 85 | 210 | Alan hasarı |

Kaynak: [GameSessionController.cs](/Users/emreaydin/AllinDefense/Assets/Scripts/Core/GameSessionController.cs:2282). Her kule yalnızca ikinci seviyeye çıkabiliyor. Sekiz okçu tam yükseltmeyle **1.040**, üç okçu + iki kışla + üç büyücü **1.360**, sekiz büyücü **1.680** altına mal oluyor.

| Bölüm | Dalga | Düşman | Toplam düşman canı | Başlangıç altını | Olası toplam altın |
|---:|---:|---:|---:|---:|---:|
| 1 | 3 | 21 | 1.575 | 230 | 515 |
| 5 | 5 | 77 | 9.533 | 225 | 1.405 |
| 9 | 7 | 166 | 24.668 | 220 | 3.009 |
| 10 | 7 | 119 | 28.668 | 200 | 3.156 |
| 11 | 4 | 52 | 9.173 | 215 | 1.252 |
| 20 | 9 | 267 | 54.161 | 200 | 5.327 |
| 30 | 10 | 331 | 78.764 | 185 | 6.726 |
| 40 | 11 | 408 | 113.061 | 170 | 8.810 |
| 50 | 12 | 485 | 150.705 | 155 | 11.041 |

“Olası toplam altın”, başlangıç + bütün düşmanların ödülü + dalga bonuslarıdır. Gerçek kazanılmış para değildir; son dalganın savaş bitince gelen bonusunu da içerir. Bölümün kazanılabilir olduğunu kanıtlamaz. Buna rağmen geç bölümlerdeki gelir ile kalıcı harcama tavanı arasındaki fark belirgindir. [50 bölümlük CSV](campaign-balance.csv), [hesaplanan veri](evidence/balance-analysis.json).

**Değişiklik:** önce dalga başına gelir/harcama eğrisi çıkarın. İlk alışveriş, ilk yükseltme ve son anlamlı satın alma anı görünür olsun. Sonra her temel kuleye iki uzmanlaşma ekleyin: okçuda hızlı atış/öncelikli hedef; büyücüde patlama/alan kontrolü; kışlada dayanıklı savunma/destek. Seçimler farklı tehditlere cevap versin. Sadece daha fazla pahalı seviye eklemek sayısal şişme yaratır.

**Kabul:** seçilmiş 10 bölümün her birinde en az iki makul kurulum kazanabilsin; tek baskın sıra bütün bölümleri çözmesin; son anlamlı harcama sürekli bölümün ilk yarısında bitmesin. Bunlar önerilen tasarım kapılarıdır, sektör standardı değildir.

### 4.4 Düşmanların vaat ettiği karşı oyun tamamlanmalı

Aktif oyunda Raider, Vanguard, Regenerator, Support, Siege ve Boss var. Bu altılı yeterli bir başlangıç kadrosu. Ancak Vanguard’ın “zırhlı” kimliği aktif hasar çözümünde farklı zırh/hasar türü davranışına bağlanmıyor; yüksek can tek başına bir karşı hamle öğretmiyor. `ApplySlow` parametreleri etkili bir yavaşlatma uygulamıyor; eski `DamageResolver` dosyasının varlığı üretim savaşının bunu kullandığını göstermiyor.

**Önerilen rol matrisi:** Raider kalabalık/zaman baskısı; Vanguard okçu hasarına dayanıklılık; Regenerator odak ateşi ihtiyacı; Support öldürme önceliği; Siege yaklaşmadan kesme veya yetenekle saldırısını durdurma; Boss daha önce öğrenilen iki mekaniğin birleşimi. Zırh/direnç eklenirse ikon, kısa açıklama ve hasar tepkisiyle görünür kılınmalı; görünmez hesap cezasına dönüşmemeli.

**Doğrulanmış durum etkisi hatası:** sersemletilmiş bir Siege, kule menziline yerleştirildikten sonra kuleyi yine susturdu. [Deney](evidence/stunned-siege.json). `RunSpecialBehaviour()` sersemletme kontrolünden önce çalışıyor; yenilenme ve destek davranışları da aynı sırada. [Kaynak](/Users/emreaydin/AllinDefense/Assets/Scripts/Core/GreyboxCombatants.cs:57).

Sersemletmenin hangi eylemleri durdurduğu tek sözleşmede tanımlansın. Pasif yenilenme sürsün isteniyorsa bu bilinçli kural olabilir; kuşatma saldırısının devam etmesi ise oyuncunun “doğru anda durdurdum” beklentisini kırar. Kabul testi hareket, normal saldırı ve özel saldırıyı ayrı doğrulasın.

### 4.5 Dalga düzeni, boss ve zamanlama

`SpawnWave`, grupları sırayla tamamen üretip sonra bir sonraki grubun gecikmesini bekliyor. `startDelay`, dalganın başlangıcına göre ortak zaman çizelgesi gibi çalışmıyor. Tank, destek ve hızlı birliğin tasarlanmış eşzamanlı baskısı yerine birbirini takip eden paketler oluşabiliyor. “İki koldan” önizlemesi de tek aktif rota/spawn düzeniyle örtüşmüyor. [Kaynak](/Users/emreaydin/AllinDefense/Assets/Scripts/Core/GameSessionController.cs:685).

Dalga verisinde gecikmenin anlamını açıkça belirleyin. Karma baskı isteniyorsa grupları ortak dalga saatinden planlayın; amaç ardışık paketlerse önizleme bunu söylesin. İlk bölgede her dalganın tek bir öğretim amacı olsun, sonra roller birleştirilsin.

Boss fazları yaklaşık %66 ve %33 canda değişiyor; temel fark görsel değişim ve hız artışı. Aynı boss davranışı bölgelere yayılıyor. Kule susturma etkisi uyarıdan sonra gerçekleşen bir saldırı yerine etkisini hemen uyguluyor. Daha fazla can yerine: önce kısa uyarı, ardından hedefli susturma, ikinci fazda yol baskısı, son fazda kısa ama okunabilir bir son hamle öneriyorum. Oyuncu hangi eylemi hangi anda engelleyebileceğini görebilmeli.

`BaselineGameplayPace = 2`: arayüzdeki 1×, yazılı savaş sürelerinin iki katı simülasyon hızı; 8× ise bu tabanın sekiz katı. Kendi başına hata olmak zorunda değil, fakat saldırı, bekleme, cooldown ve animasyon saatleri aynı anlamda kullanılmıyor. Kare başına tek saldırı yapan sayaçların düşük FPS/yüksek hızda hasar ritmi sapabilir. 1×/2×/4×/8× koşuları 30 ve 60 FPS sınırlarında aynı olay sırası ve benzer simülasyon sonucu bakımından karşılaştırılmalı.

Hasar bazı saldırılarda animasyondan önce uygulanıyor. `PresentationActionTimingContract` ve `PresentationAnimationEventRelay` var; `FeedbackSignaled` olayının üretim tarafında tüketildiği bir bağlantı bulunamadı. Hasar, ses, temas parlaması ve ok/mermi aynı vuruş anına bağlansın. Oyun mantığını animasyon dosyasına bağımlı kılmadan ortak bir saldırı zaman çizelgesi kullanılabilir.

## 5. Kampanya, gelişim ve tekrar oynama

### 5.1 50 kayıt, 50 ayrı tasarım deneyimi anlamına gelmiyor

Kampanya beş temel rota ve bunların aynalanmış biçimleri üzerine kurulmuş; dışa aktarılan veride **10 farklı rota geometrisi** bulunuyor. Bölüm 10 özel dalga düzenini koruyor; diğerlerinin büyük kısmı formüllerle üretiliyor. Bölge adları değişse de temel savaş sunumu ve boss davranışı benzer kalıyor. [CampaignLevelFactory.cs](/Users/emreaydin/AllinDefense/Assets/Scripts/Data/CampaignLevelFactory.cs:68).

Formül kullanmak yanlış değil. Ancak el yapımı yerleşim, dalga kompozisyonu ve çevresel mekanikle desteklenmediğinde bölüm sayısı ilerleme hissinin yerine geçiyor. Bölüm 10’dan 11’e toplam düşman canının 28.668’den 9.173’e düşmesi dinlenme noktası olarak bilinçli olabilir; buna rağmen “zorluk” alanının ikisinde de aynı olması, etikete bakarak gerçek baskının anlaşılamadığını gösteriyor. Bu alan aktif düşman istatistiklerini doğrudan yöneten tek kaynak değil.

**İlk 10 bölüm için önerilen öğretim dizisi:**

| Bölüm | Yeni karar | Tasarım amacı |
|---:|---|---|
| 1 | İlk okçu, yükseltme, yetenek | Oyuncunun eylemini zaferle ilişkilendirmek |
| 2 | Yol tutan kışla | Engellemenin menzil içindeki ateşi uzattığını göstermek |
| 3 | Alan hasarlı büyücü | Kalabalığa farklı çözüm öğretmek |
| 4 | Gerçek zırhlı düşman | Hasar türü seçimi |
| 5 | Küçük ara boss | İlk üç rolü birlikte kullanmak |
| 6 | Yenilenme | Odak ateşi ve birikmiş hasar |
| 7 | Destek düşmanı | Hedef önceliği |
| 8 | Kuşatma | Uyarıyı okuyup zamanında müdahale |
| 9 | İki baskının birleşimi | Kaynak ayırma; uygulanıyorsa iki rota |
| 10 | Bölge boss’u | Öğrenilen iki mekaniği fazlar halinde sınamak |

Bunlar yeni 10 bölüm ekleme önerisi değil; mevcut ilk bölümleri yeniden düzenleme planı. Her bölüme tek cümlelik “burada oyuncu ne öğreniyor?” hedefi yazın. Bölge geçişlerinde görünüşün yanında bir mekanik değişsin: örneğin dar geçit, görüş kısıtı veya zamanlı çevre tehdidi. Hepsini birden eklemeyin.

### 5.2 Ödül kazanmak, anlamlı gelişim yaşamakla aynı şey değil

Valor ve kahraman XP’si birikiyor; fakat aktif Knight savaş değerlerine veya oyuncunun harcama/seçim ekranına bağlanan tamamlanmış bir döngü bulunamadı. Eski kahraman sınıflarındaki XP kodu aktif Knight’ın geliştiğini kanıtlamıyor. Oyuncuya gelişim ödülü verilip oyunda etkisinin görülmemesi uzun vadeli hedefi zayıflatır.

İlk metagame küçük olsun: bölüm kilometre taşlarında açılan kule uzmanlaşmaları ve aynı anda yalnızca biri seçilen iki komutan pasifi. Örneğin daha güvenli son hat ile daha güçlü erken savunma arasında yatay bir tercih. İstatistik yükseltmek için eski bölümleri zorunlu tekrar ettirmeyin; ana kampanyayı temel araçlarla bitirilebilir tutun.

Üç yıldızı yalnızca kalan cana bağlamak anlaşılır bir başlangıçtır. Sonrasında isteğe bağlı “kayıpsız savunma”, “sınırlı kule türü” gibi iki meydan okuma, az içerikle tekrar oynama sağlayabilir. Günlük görev, sezon bileti, ekipman envanteri, çok sayıda kahraman ve sonsuz mod bu döngü doğrulandıktan sonra değerlendirilmeli.

Karşılaştırma ilkesi olarak Kingdom Rush Frontiers’ın uzmanlaşmış kule seçenekleri, her yükseltmenin farklı bir savunma rolüne hizmet etmesinin iyi bir örneği. Aynı içerik sayısını hedeflemek veya görsel kimliğini taklit etmek gerekmiyor. [Resmî oyun sayfası](https://play.kingdomrush.com/kingdom-rush-frontiers).

## 6. Kullanıcı deneyimi, mobil arayüz ve erişilebilirlik

### 6.1 İlk savaşa daha doğrudan giriş

Mevcut akış açılış, hoş geldin/hesap, ana menü, bölüm özeti, yön değişimi, hazırlık ve dalga aşamalarına sahip. İlk kez oynayan kişi için henüz değerini bilmediği oyuna ulaşmadan çok sayıda kavramsal adım var. Öneri: belirgin “Oyuna başla”, hesap gerektirmeyen ilk savaş, ilk zaferden sonra ilerlemeyi koruma teklifi. Misafir modunun kaydı daha sonra hesaba taşıma kuralı net olmalı.

Hazırlık ekranındaki genel “platforma dokun” mesajı, kulelerin neden farklı olduğunu öğretmiyor. İlk kurulum için kısa görsel işaret, menzil önizlemesi, maliyet ve satın alma sonrası görünür etkisi gerekli. Kule düğmelerinin ilk durumda “SLOT SEÇ” göstermesi maliyeti ve rolü öğrenmeyi geciktiriyor. Seçili yer olmasa da ad/maliyet görünür tutulabilir; basınca uygun yerler vurgulanabilir.

Knight’ın hareket ettirilemediği açık olsun: “Kale muhafızı son hattı korur; sen Kalkan Darbesi’ni yönetirsin.” Yetenek alanını önceden gösterin. Boş alana kullanım bilinçli olarak serbestse kısa “menzilde düşman yok” geri bildirimi verin; sessizce cooldown tüketmek yanlış dokunuşu anlaşılmaz kılar.

### 6.2 Mobil dokunma boyutu yeniden ölçülmeli

Alt eylem düğmeleri 75 canvas birimi, bazı yükseltme düğmeleri 58 birim yükseklikte. Önceki erişilebilirlik raporunun “44’ten büyük” karşılaştırması fiziksel dokunma boyutunu kanıtlamıyor; canvas birimi, piksel ve iOS point aynı birim değil. [Yerleşim](/Users/emreaydin/AllinDefense/Assets/Scripts/Presentation/DefenderHudResponsiveLayout.cs:213).

1920×1080 referans ve eşit genişlik/yükseklik ölçekleme varsayımıyla, 75 birimlik düğmenin yüksekliği iPhone SE sınıfı 1334×750 çözünürlükte yaklaşık **26 point**, 2532×1170 ve 3× ölçekli ekranda yaklaşık **30 point** olur. Bunlar cihaz üzerinde okunmuş hit rectangle ölçümleri değil; mevcut ölçekleme düzeninin hesaplanan sonucudur. Görünmez genişletilmiş dokunma alanı varsa ayrıca ölçülmelidir.

Apple güncel kılavuzunda iOS için varsayılan kontrol boyutu 44×44 point; minimum boyut 28×28 point olarak ayrılıyor. Oyundaki sık kullanılan eylemlerde **44×44 point hedefleyin**, düğmeler arası boşluk bırakın ve gerçek hit alanını doğrulayın. Web WCAG ölçülerini doğrudan Unity canvas birimine uygulamayın. [Apple erişilebilirlik kılavuzu](https://developer.apple.com/design/human-interface-guidelines/accessibility), [Apple oyun arayüzü oturumu](https://developer.apple.com/videos/play/wwdc2024/10085/).

HUD’da altı şeyi aynı anda okunur tutmak yeterli: kale canı, altın, mevcut dalga, sıradaki tehdit, seçili savunma, yetenek durumu. Seçili kule kartının savaş alanını kapatma oranı küçük ekranda ölçülsün. Tam yükselmiş kulede “MAKS / SV” yerine kısa ve tutarlı ifade, satışta geri alınacak tutar, yetersiz altında eksik miktar gösterilsin.

### 6.3 Devam, yenilgi ve kesintiler

Ana menüdeki devam eylemi `lastSelectedLevel` değerini kullanıyor. Sonuç ekranında ilerleme açılırken bu alanın bir sonraki oynanmamış bölüme kalıcı taşındığı bir yol bulunmadı. Oyuncu “devam” dediğinde bitirdiği bölüme dönme riski var. “Son oynadığın bölüm” ile “sıradaki bölüm” ayrı anlamlar olarak modellenmeli. [Kaynak](/Users/emreaydin/AllinDefense/Assets/Scripts/UI/AppFlowController.cs:694).

Yenilgi ekranı sadece savunmayı güçlendirmeyi söylemesin. En azından hangi dalgada, hangi düşmanlardan kaç can kaybedildiğini; bir sonraki denemede değiştirilebilecek tek somut öneriyi göstersin. İleri aşamada kule hasarı ve kışla engelleme süresi eklenebilir. Bu, oyuncunun kendi stratejisini öğrenmesini sağlar.

Aktif kaynaklarda uygulama odağı/kesintisi için tamamlanmış `OnApplicationPause/Focus` kurtarma yolu bulunamadı. Kampanya kaydı, savaş içi dalga kontrol noktası değildir. Telefon kilidi, arama, uygulama değişimi, işletim sistemi tarafından kapanma için otomatik duraklatma ve “dalgayı yeniden başlat / bölüme devam” davranışı tasarlanmalı. Bunlar bu audit sırasında telefonda yeniden üretilmiş hata değil, açık doğrulama alanlarıdır. Yön değiştirme bekleyişine de zaman aşımı ve kurtarma ekranı eklenmesi değerlendirilmeli.

### 6.4 Dil, görsel erişilebilirlik ve ses

TR/EN altyapısı ve yerelleştirme var; fakat dinamik savaş metinleri parçalı kalıyor. “SLOT SEÇ”, “SV”, “sn”, “KALKAN” gibi ifadeler ve bazı dalga önizlemeleri Türkçe kalabiliyor. `GameLocalization` eşleşmeyen metni olduğu gibi döndürüyor. Düşman sınıflandırmasını ad içindeki “muhafız” gibi sözcüklerden çıkarmak yerine archetype üzerinden yapın. [GameLocalization.cs](/Users/emreaydin/AllinDefense/Assets/Scripts/UI/GameLocalization.cs).

Dil kontrolü ana menüyle sınırlı kalmasın: hazırlanma, dalga, boss, yetersiz para, cooldown, duraklatma, zafer, yenilgi ve çevrimdışı/hata durumları iki dilde gezilsin. Uzun metinler ve Türkçe karakterler küçük ekran/safe area koşullarında test edilsin.

Azaltılmış hareket, sarsıntı/haptik ve ses tercihleri olumlu bir temel. Bunları koruyun. Tehdit türünü yalnız renkle anlatmayın; siluet/ikon/efekt ritmi ekleyin. VoiceOver veya native erişilebilirlik köprüsünün uçtan uca çalıştığına dair kanıt yok. Erişilebilirlik için otomatik rect testi ve kontrast kontrolünün yanında insan testi gerekli.

Ses tarafında katman/politika altyapısı var; bu audit yeni cihaz üzerinde dinleme testi içermiyor. Oyun içi öncelik net olmalı: kaleye sızma, kuşatma uyarısı, boss saldırısı, yetenek hazır, sonra sıradan vuruşlar. Yoğun dalgada benzer vuruş seslerini sınırlandırın; kritik uyarı müzik ve kalabalık efekt altında kaybolmasın. Sessiz modda da tüm kritik olayların görsel karşılığı olmalı.

## 7. Sanat yönü ve savaşın okunabilirliği

Menülerdeki resimli ortaçağ atmosferi, lacivert/altın renk düzeni ve arma tutarlı bir başlangıç kimliği veriyor. İncelenen savaş görüntüsü ise daha düz/koyu zemin, köşeli rota, tekrar eden çevre öğeleri ve küçük birlik siluetleriyle daha erken geliştirme aşamasında görünüyor. Menüyle savaş arasındaki bu fark, oyuncunun ilk beklentisini zayıflatabilir.

Görsel kanıtlar: [6 Eylül güncel Editor hazırlık ekranı](evidence/live-preparation-initial.png), [27 Ağustos menü görüntüsü](/Users/emreaydin/AllinDefense/Builds/Beautification-Final/welcome-9x19_5.png), [25 Ağustos yoğun dalga görüntüsü](/Users/emreaydin/AllinDefense/docs/evidence/runtime/defender-simulator-vis812-final-20260825-205459-dense-landscape-readable.png). Eski ekranlarda görülen taşma veya yerleşim sorunları yeni telefonda tekrar görülmeden “güncel cihaz hatası” olarak sayılmıyor.

**Önce tek bir referans savaş sahnesi tamamlayın:**

1. Yolu zeminden parlaklık ve doku ile ayırın; çevre detaylarını oyun alanından daha sakin tutun.
2. Okçuda ahşap/metal ve uzun siluet, kışlada taş/kalkan ve geniş kütle, büyücüde mor kristal ve dikey vurgu kullanın. Renk kadar şekil de çalışsın.
3. Dost ve düşman takım dilini ayırın. Mevcut mavi vurguların kule, kahraman ve boss üzerinde benzer kullanımı tehdidi zayıflatabiliyor.
4. Birliklerin ayağına kontrollü temas gölgesi, okunur saldırı hazırlığı ve kısa temas efekti ekleyin. Görsel kalabalığı artıran sürekli parıltı yerine olay anını güçlendirin.
5. Boss’u yalnız ölçekle büyütmeyin; özgün siluet ve açık saldırı hedef işareti verin.
6. Üç bölgesel malzeme/ışık/çevre varyantını bu sahne üzerinden üretin; her bölüme ayrı rastgele varlık yığmayın.

Mevcut basit iskelet/animasyon varlıkları bu amaç için kullanılabilir. Baştan tüm karakterleri değiştirme veya pahalı bir varlık paketi satın alma gereği kanıtlanmış değil. Önce oyunun telefon boyutundaki okunabilirliğine göre bir sanat standardı belirleyin.

## 8. Kayıt, hesap ve sunucu kuralları

### 8.1 Bulut eşitlemenin yaşam döngüsü tamamlanmalı

`UnityGameServicesController.Configure` olaylara bağlanıyor; başlangıçta servisi başlatmıyor. `EnsureInitializedAsync` oturum açma talebinden çağrılıyor. Yerel kayıtta oyuncu kimliği varsa frontend doğrudan ana menüye yöneliyor. Bölüm değiştirme/yeniden deneme sahneyi yeniden yüklüyor; yeni controller’ın `initialized` alanı yine false. `HandleProgressChanged` bu durumda hemen dönüyor. [Servis kaynağı](/Users/emreaydin/AllinDefense/Assets/Scripts/Core/UnityGameServicesController.cs:24), [sahne yükleme](/Users/emreaydin/AllinDefense/Assets/Scripts/UI/AppFlowController.cs:1395).

Bu akışta SDK’nın statik oturumu açık kalsa dahi controller’ın yerel başlangıç koşulu eşitlemeyi durdurabiliyor. İnternetin geri gelmesini izleyen düzenli tekrar deneme yolu da bulunmadı. Kaynakta görülen sorun budur; gerçek hesaplar arasında uzaktan veri kaybı deneyi yapılmadı.

**Çözüm:** uygulama ömrü boyunca yaşayan servis katmanı, açılışta oturum geri yükleme, sıraya alınmış talepler, tek eşitleme işlemi, artan beklemeli tekrar deneme ve görünür kayıt durumu. Kabul senaryoları: soğuk açılış, yeniden dene, sonraki bölüm, çevrimdışı zafer → bağlantı geri gelişi, arka plan → dönüş.

### 8.2 Yerel kayıt güvenliği

Kayıt önce geçici dosyaya yazılıyor, sonra eski dosya siliniyor ve geçici dosya taşınıyor. Eski dosyanın silinmesi ile taşıma arasındaki kesinti kaydı erişilemez bırakabilir. Yükleme yalnız ana dosyayı okuyor; bozuk dosyada temiz kayıt açılıyor, `.tmp/.bak` kurtarma yolu yok. [CampaignProgressStore.cs](/Users/emreaydin/AllinDefense/Assets/Scripts/Core/CampaignProgressStore.cs:193).

Platforma uygun atomik dosya değiştirme, bir önceki geçerli yedek, sürüm/kayıt doğrulaması ve açılışta kurtarma sırası ekleyin. Kayıt bozulduğunda sessiz sıfırlama yerine kurtarma durumu gösterin. Test, yalnız normal yaz/oku değil; her yazma aşamasında kesinti ve bozuk JSON içermeli.

Tek ortak `campaign-save.json` kullanılması hesap sınırlarını da belirsizleştiriyor. `SignIn` oyuncu kimliğini mevcut veri üstünde değiştiriyor. Hesap A, misafir ve hesap B için ayrı yerel alan; misafir ilerlemesini hangi koşulda birleştireceğine dair açık kural gerekli. Eski hesabın bekleyen ödül talebi yeni hesaba taşınmamalı.

### 8.3 Sunucuda hesaplamak, savaş sonucunu doğrulamak değildir

Cloud Code’da korumalı veri, kilit/çakışma kontrolü ve tekrar eden talep kimliği kontrolü olumlu. Ancak kabul edilen talebin oynanmış bir savaşa ait olduğunu doğrulayan bağlam sınırlı.

Yerel kurallar motoruna yapılan iki deney:

- **Bölüm 1, kalan can 0, yıldız 3:** kabul edildi; **27 Valor** verildi, kayıt 3 yıldız/0 en iyi can oldu.
- **Eski kayıt göçü:** yalnızca bölüm 50 için başarı kaydı içeren istemci JSON’u, ilerlemeyi **50. bölüme** açabildi.

[Deney çıktısı](evidence/cloud-rules-probes.json), [CampaignProgressionModule.cs](/Users/emreaydin/AllinDefense/CampaignProgressionCCM/CampaignProgression/CampaignProgressionModule.cs:137). Bunlar yerel kurallar motoru deneyleridir; üretim servisine talep gönderilmedi, gerçek oyuncu verisi değiştirilmedi.

En azından zaferde can > 0 olmalı; yıldız sayısı sunucuda kalan can ve bölüm kuralından türetilmeli; eski kayıt göçü güvenilir kaynağa veya sınırlı bir geçiş politikasına bağlanmalı. Tek başına bunlar sahte tamamlamayı bütünüyle önlemez. Çevrimiçi değeri olan ödüller için başlangıç/bitiş ilişkisi, oturum kimliği, kural sürümü ve makul süre gibi bağlamlar gerekir. Unity de istemci kaynaklı durumun güven sınırını ve sunucuda doğrulamayı vurguluyor. [Unity Cloud Code güvenlik rehberi](https://docs.unity.com/ugs/en-us/manual/cloud-code/manual/cheat-prevention).

Çevrimdışı tek oyunculu tasarım korunabilir. Rekabet/satın alma yokken tam sunucu simülasyonu dayatmak gereksiz olabilir; fakat “cihazın beyanına güvenilen yerel ilerleme” ile “doğrulanmış çevrimiçi ödül” ürün politikasında ayrılmalı.

`ApplyServerSync`, karar verilen talepleri kabul/reddet ayrımı olmadan kuyruktan çıkarıyor. Geçersiz talebi sonsuz tekrar etmek de doğru olmaz; reddedilen talep için neden, inceleme/kurtarma kaydı ve kullanıcıya anlaşılır açıklama gerekli. “Senkronize” durumu, ödülün onaylandığı sanısını yaratmamalı.

## 9. Performans ve teknik yapı

### 9.1 Fiziksel cihaz kanıtı var; kapsamı doğru okunmalı

26 Ağustos tarihli fiziksel iPhone 13 sınıfı cihaz raporunda yaklaşık **60,1 FPS**, **16,9 ms p95** ve otomatik kabul adımları var. Bu değerler olumlu tarihsel kanıt; “hiç cihaz testi yapılmamış” demek yanlış olur. Aynı raporda hazırlık/normal/yoğun/boss aşamalarında yaklaşık **609 / 821 / 949 / 1.015 MB** bellek, 90 saniyelik dokuz döngülü soak sırasında yaklaşık **1.089 MB**, **%7,1 büyüme**, **21,1 ms p95** ve **serious** termal durum da yer alıyor. [Fiziksel kabul raporu](/Users/emreaydin/AllinDefense/docs/IOS_PHYSICAL_DEVICE_ACCEPTANCE_PROGRESS_2026-08-26.md).

Bu bellek değerleri probe’un Unity ayrılmış bellek ölçümüne dayanıyor; işletim sistemi süreç belleğiyle aynı şey değil. 90 saniyelik döngü, normal oynanışta 20 dakika testinin yerine geçmez. Ayrıca bu veriler audit günündeki sürümün yeni ölçümü değildir.

Güncel `DeviceAcceptanceProbe.MarkScenario` etiketi değiştiriyor ama kare örneklerini temizlemiyor. “Boss p95” gibi etiketli değerler önceki aşamaları da içeren kümülatif dağılımdan gelebiliyor. `MeetsPerformanceGate` FPS/p95/düşük bellek uyarısını kontrol ediyor; termal durumu kabul koşuluna katmıyor. [DeviceAcceptanceProbe.cs](/Users/emreaydin/AllinDefense/Assets/Scripts/Core/DeviceAcceptanceProbe.cs:90).

**Önce ölçümü düzeltin:** ayrı aşama pencereleri, tüm oturum özeti, düşük FPS süreleri, termal durum ve ayrı bellek ölçüm adları. Sonra en düşük hedef cihazda 20–30 dakika normal savaş/menü/yeniden başlat döngüsü çalıştırın. iPhone’daki geçmiş sonuç Android’i temsil etmez; düşük ve orta seviye Android cihazı da gerekli. Hedef donanım listesi ve desteklenen kalite düzeyi açıkça yazılmalı.

### 9.2 Ölçülmesi gereken maliyetler

Aşağıdakiler kaynakta görülen maliyet adaylarıdır; bu audit sırasında Profiler’da sıralanmış gerçek darboğazlar olarak sunulmuyor:

- Düşmanların sürekli oluşturulup yok edilmesi; her düşmana world-space sağlık göstergesi/canvas eklenmesi.
- Ölüm efektinde `StylizedActorView.PlayDefeatEcho` ile ikinci model örneği oluşturulması.
- Bazı materyallere `.material` üzerinden erişilmesi ve örnek materyal üretimi.
- `RefreshUi` ile her kare güncellenen HUD, seçili kule metinleri ve tekrar üretilen yazılar.
- Alan hasarı/arama yollarındaki koleksiyon kopyaları ve hedef seçerken rota uzunluğunun tekrar hesaplanması.
- Frontend ekranlarının açılışta birlikte kurulması, savaş masası için ek kamera/RenderTexture kullanımı.

VFX havuzu zaten var; bunu koruyun. Düşman/sağlık göstergesi havuzu, değişen değer odaklı HUD güncellemesi, önceden hesaplanan rota segmentleri ve materyal özellik blokları profiler bulgularına göre sıraya alınabilir. Unity’nin UI kılavuzu da sık değişen öğeleri ayırmayı, gereksiz canvas yeniden kurulumlarını ve raycast maliyetini azaltmayı öneriyor. [Unity UI optimizasyon rehberi](https://unity.com/how-to/unity-ui-optimization-tips).

Her mobil cihazı tek kalite ayarında 60 FPS’e zorlamak yerine, 30 FPS pil modu ve düşük cihaz profili değerlendirilmeli. Termal duruma göre efekt/gölge kalitesini azaltmak, ana savaş okunabilirliğini korumalı. Görsel optimizasyon, yanlış bellek ölçümünü veya strateji dengesini çözmez; bunlar ayrı kabul kapılarıdır.

### 9.3 Kod organizasyonu gelişim hızını yavaşlatıyor

`GameSessionController` yaklaşık 2.500, `AppFlowController` 1.900, `DefenderHudController` 1.600 satır. Boyut tek başına kusur değildir; burada savaş, sahne oluşturma, arayüz, otomatik smoke, performans senaryosu ve kampanya sorumluluklarının birleşmesi değişiklik riskini artırıyor. Üstelik eski oyun sınıflarıyla aktif sınıflar arasında iki uygulama yolu bulunuyor.

Önerilen sınırlar: savaş simülasyonu, dalga zamanlayıcı, ekonomi/kurulum, sunum olayları, HUD sunumu, uygulama gezinmesi ve kayıt servisleri. Test/otomasyon rutinlerini geliştirme derlemesine ait ayrı assembly’ye taşıyın. Eski sistemleri önce referans haritasıyla işaretleyin; gerçekten kullanılmayanları kontrollü biçimde kaldırın. Büyük bir yeniden yazım yerine her hata düzeltmesinde bir sınırı netleştirin.

Kule ve düşman değerleri tek veri kaynağından gelsin; açıklama/istatistik kartı aynı tanımdan türesin. Özellikle kışla yükseltmesinde HUD’un genel kule çarpanları ile gerçek asker sağlık/hasar/saldırı aralığı farklı kaynaklarda. Oyuncuya menzil/saldırı hızı artışı gösteriliyorsa aktif davranış gerçekten bunu sağlamalı.

### 9.4 Testlerin geçtiği iddia ile oyunun kaliteli olduğu iddia ayrılmalı

Otomatik smoke’ın tam bölümü kazanması değerli bir entegrasyon kontrolü. Fakat `RunAutomatedSmoke` **2.200 ilave altın** verip savunmayı dolduruyor; performans akışlarında **2.600 ilave altın** ve bazı senaryolarda kale hasarını yok sayma yolu var. Bunlar normal kaynaklı denge testi değildir. [Smoke kaynağı](/Users/emreaydin/AllinDefense/Assets/Scripts/Core/GameSessionController.cs:1024).

Mevcut testler katalog, arayüz sözleşmeleri, yazı tipi/ses/presentation varlıkları ve temel kurallarda yararlı. Buna şu davranış testleri eklenmeli:

| Alan | Anlamlı doğrulama |
|---|---|
| İlk bölüm | Kulesiz kazanamama; rehberli kurulumla kazanma; üç yıldızın çaba istemesi |
| Kışla | Her harita tipinde yolu tutma, köşelerde yerleşim, asker ölümü/yeniden doğuş |
| Durum etkisi | Stun sırasında normal ve özel saldırı kuralları; slow’un gerçek hareket süresi |
| Dalga | Grup başlangıçlarının hedeflenen ortak zamanlara uyması |
| Ekonomi | Gerçek altınla farklı planlar; satın alma engeli; satış/yükseltme geri bildirimleri |
| Saat | Farklı hız ve FPS koşullarında saldırı/etki sürelerinin tutarlılığı |
| Kayıt | Yarım yazım, bozuk dosya, eski sürüm, hesap değişimi, reddedilen talep |
| Servis | Açılış/sonraki bölüm/yeniden dene/bağlantı dönüşünde eşitleme |
| Mobil | Safe area, gerçek dokunma alanı, iki dil, duraklatma/geri dönüş |

İncelenen depoda bu kontrolleri otomatik çalıştıran bir CI iş akışı bulunmadı; dışarıda CI olmadığı sonucunu çıkarmıyorum. Yayın için kaynak sürümü, test raporu, paket kimliği ve fiziksel cihaz raporu aynı sürüme bağlanmalı.

## 10. Yayın, ürün konumu ve ticari hazırlık

### 10.1 İsim kararı geciktirilmemeli

**“ALLIN: Defense” aynı türde daha önce kullanılmış bir oyun adı.** 2012 tarihli geliştirici/yayıncı duyurusu “ALLIN: Defense II”yi ve önceki “ALLIN: Defense – Legend of the Desert King” oyununu açıkça anıyor. Bu, genel bir isim benzerliği varsayımı değil. [Dönemin resmî basın duyurusu](https://pressreleases.triplepointpr.com/2012/05/10/man-the-battlements-allin-defense-ii-announced-for-ios-rallies-onto-app-store-may-24/).

Bu audit marka hakkı ihlali kararı vermez. Ancak arama bulunabilirliği, oyuncunun karıştırması ve mağaza sunumu açısından adı yeniden değerlendirmek güçlü bir ürün gereği. Mağaza görsellerine, alan adına ve büyük tanıtıma yatırım yapmadan önce farklı ad seçenekleri ve uygunluk araştırması tamamlanmalı. Mevcut ürün notu da adı çalışma başlığı olarak ele alıyor.

### 10.2 Hesap özelliği, yayın sorumluluğu doğuruyor

`FrontendPresentationTheme.asset` içinde gizlilik politikası ve kullanım koşulu bağlantıları boş. Uygulamada butonları bulunuyor; boş bağlantıda yapılandırma mesajı gösteriliyor. İncelenen kaynaklarda uygulama içi hesap silme akışı bulunamadı. [Theme asset](/Users/emreaydin/AllinDefense/Assets/Resources/Presentation/FrontendPresentationTheme.asset:41).

Apple, uygulama üzerinden hesap oluşturmayı destekleyen uygulamalarda uygulama içinden hesap silmeyi başlatma imkânı istiyor. Google Play’in kapsamına giren uygulamalarda uygulama içi yolun yanında web üzerinden talep yolu ve veri beyanları da gerekiyor. Kullanılan misafir/Player Account akışlarının kapsamı yayın öncesi netleştirilmeli. [Apple hesap silme rehberi](https://developer.apple.com/support/offering-account-deletion-in-your-app/), [Google Play hesap silme rehberi](https://support.google.com/googleplay/android-developer/answer/13327111?hl=en-EN).

Yapılacak iş: çalışan herkese açık politika sayfaları, hesap silme ve veri saklama kapsamı, destek yolu, kullanılan SDK/verilerle uyumlu mağaza beyanları. Bu bulgular hesap sisteminin gerçekten projeye eklenmiş olmasına dayanıyor; varsayımsal bir uyum listesi değil.

### 10.3 Gelir modeli için çekirdeği doğrulamak gerekiyor

Ürün notundaki çevrimdışı, kısa oturumlu ve baskısız gelişim vaadiyle; ücretli tam oyun veya ilk bölge ücretsiz/tam kampanya ücretli modelinin uyumu araştırılabilir. Bu aşamada fiyat, gelir veya dönüşüm tahmini yapmak için veri yok. Mevcut kitle, edinme kanalı ve oyuncu ödeme isteği ölçülmeden reklam/IAP altyapısını büyütmeyin.

Tanıtımın merkezine “50 bölüm” yerine gerçek savaş kararını koyun: düşmanı doğru yerde tut, destek birimini zamanında indir, son hatta Kalkan Darbesi’ni kullan. Mağaza videosu/görselleri gerçek savaş kalitesini temsil etmeli. Hikâye ve karakter kimliği bu çekirdeği desteklesin; ek menülerle kapatılmaya çalışılmasın.

## 11. Oyuncudan hangi verileri toplamalıyız?

İncelenen aktif kaynaklarda oyun tasarımını ölçen olay gönderimi bulunmadı. Unity Analytics paket/modülünün bulunması, bu olayların kaydedildiğini göstermez. İlk aşamada kişisel veri gerektirmeyen oyun davranışları yeterli:

| Olay grubu | Yanıtlayacağı soru |
|---|---|
| İlk açılış → ilk kule → ilk dalga → ilk zafer | Oyuncu hangi adımda takılıyor/ayrılıyor? |
| Bölüm başlangıç/sonuç/çıkış/yeniden deneme | Hangi bölüm yoruyor, hangi yenilgi tekrar denetiyor? |
| Kule kur/yükselt/sat; dalga ve kalan altın | Oyuncu hangi tercihi yapıyor, harcama ne zaman bitiyor? |
| Sızan düşman türü, kaybedilen can, dalga | Yenilginin gerçek nedeni ne? |
| Yetenek kullanımı ve etkilediği düşman sayısı | Oyuncu yeteneği anlıyor mu, boş kullanımlar ne kadar? |
| Kışla engelleme süresi ve kule katkısı | Savunma rollerinin gerçek değeri ne? |
| Kayıt bekliyor/onaylandı/reddedildi/hata | Oyuncunun ilerlemesi güvenli mi? |
| Cihaz/kalite sınıfı, FPS/termal aşama | Teknik sorun belirli donanıma mı bağlı? |

Olaylarda sürüm, bölüm, dalga, zorluk/kural sürümü ve test/gerçek oyun ayrımı bulunsun. Otomatik testleri gerçek oyuncu analizine katmayın. Veriler yayın beyanları ve gizlilik tasarımıyla uyumlu toplanmalı.

Önerilen öğrenme sırası:

1. **5–8 yeni oyuncu:** yanlarına açıklama yapmadan ilk üç bölümü izleyin. Kule rollerini, mağlubiyet nedenini ve yeteneği kendi kelimeleriyle anlatmalarını isteyin. Küçük örneklemde yüzdelerle başarı iddiası kurmayın.
2. **30–50 oyuncu:** ilk 10 bölümde kurulum çeşitliliği, tekrar deneme ve bırakma noktalarını inceleyin; görüşmeleri olay verisiyle eşleştirin.
3. **100–300 kişilik sınırlı test:** kararlılık, içerik tüketimi ve geri dönüş sinyallerini araştırın. D1/D7 için yeterli gözlem süresini bekleyin; bu örneklemin ince farklarda istatistiksel kesinlik sağlamadığını kabul edin.

Ana kalite sorusu “kazandılar mı?” ile sınırlı kalmasın: **oyuncu yaptığı seçimin neden işe yaradığını anlayabiliyor mu ve kaybettiğinde farklı bir plan denemek istiyor mu?**

## 12. Uygulanabilir çalışma planı

Takvim, iki geliştirici + bir görsel tasarımcı ve kısmi test desteği varsayımıyla önerilen kapsam sırasıdır; teslim taahhüdü değildir. Tek geliştiriciyle süre uzar. Birbirine bağımlı işlerin sırası korunmalı.

| Dönem | Odak | Somut çıktı | Bitiş ölçütü |
|---|---|---|---|
| Gün 1–7 | Temel savaş doğruluğu ve kayıt | Stun düzeltmesi, ilk bölüm dengesi, kışla regresyonları, servis açılışı, güvenli kayıt, yıldız doğrulama | G01/G03 ve S01–S03 için davranış testleri; normal kaynakla ilk üç bölüm |
| Gün 8–14 | İlk 10 bölümün tasarımı | Öğretim sırası, ortak dalga zaman çizelgesi, tehdit/karşı hamle matrisi, bir boss, gelir eğrisi | Farklı kurulumlarla kazanılabilirlik; boşta kazanma ve yanlış rol vaatleri yok |
| Gün 15–21 | Görsel ve mobil deneyim | Tek referans savaş sahnesi, gerçek hit alanları, iki dil, yenilgi açıklaması, devam akışı | Küçük/büyük telefonda okunabilirlik; 5–8 gözlemli oyun testi |
| Gün 22–30 | Cihaz ve sınırlı oyuncu testi | Doğru performans pencereleri, 20–30 dk cihaz testi, olay ölçümü, yayın hazırlığı | Aynı sürümde doğrulama dosyası; açık P1 işlerinin kapanması veya açık ürün kararı |

Kule uzmanlaşmalarının tüm içeriğini tek ayda bitirme zorunluluğu koymayın. Önce bir kulede iki dalı prototipleyip karar çeşitliliği yarattığını doğrulayın. Kalan kuleleri ve 50 bölümün yeniden ayarını sonuçlara göre planlayın.

**İlk başlanacak beş iş:**

1. Bölüm 1’in kulesiz kazanılmasını düzeltin ve öğretim hedeflerini teste bağlayın.
2. Stun/özel saldırı sözleşmesini düzeltin; yeni kışla yerleşimini rota köşelerinde doğrulayın.
3. Eşitlemenin açılış/sonraki bölümde sürmesini ve yerel kayıt kurtarmasını tamamlayın.
4. İlk 10 bölümün dalga/gelir tablosunu ve üç kuleye ait karşı hamle matrisini tasarlayın.
5. En küçük hedef telefonda HUD’u gerçek dokunma ölçüsüyle yeniden düzenleyip ilk oyuncu gözlemini yapın.

**Şimdilik büyütülmemesi gereken alanlar:** yeni hesap sağlayıcıları, PvP, klan, sezon, çok sayıda kahraman, büyük ekipman envanteri ve daha fazla formülle üretilmiş bölüm. Bu sistemler mevcut strateji ve güvenilirlik açıklarının çözülmesine bağımlı.

## 13. Son doğrulama ve kanıt dizini

| Sürüm / koşu | Sonuç |
|---|---|
| İlk izole kaynak kopyası, EditMode | 78/78 geçti |
| İlk izole kaynak kopyası, PlayMode | 18/19 geçti; kışla ağırlıklı strateji yenildi |
| Audit sırasında gelen kışla + satın alma sırası güncellemesi, yalnız strateji paketi | **3/3 geçti:** okçu 13, kışla 2, büyücü 20 can |
| Audit gözlemleri | Kulesiz bölüm 1 zafer; tek okçu 16 can; stun altında kuşatma susturması devam ediyor |
| Cloud Code | 4/4 birim testi; .NET 10 uyumluluk koşulu; iki ilave yerel kural açığı doğrulandı |

Son takip koşusu yalnız değişiklikle ilgili strateji paketidir; güncellenmiş bütün projede 19/19 veya yeni tam cihaz kabulü iddiası değildir. Gözlem testlerinin “geçmesi”, kaydedilen davranışın tasarım bakımından doğru olduğu anlamına gelmez; bu testler ölçümü tamamladıklarını doğrular.

- [Öncelikli uygulama listesi](audit-backlog.csv)
- [50 bölümün denge tablosu](campaign-balance.csv)
- [EditMode sonuçları](evidence/editmode-isolated.xml)
- [İlk PlayMode sonuçları](evidence/playmode-isolated.xml)
- [Kışla düzeltmesi sonrası strateji sonuçları](evidence/strategy-followup.xml)
- [Cloud Code test raporu](evidence/cloudcode-dotnet10.trx)
- [Yerel kurallar motoru deneyleri](evidence/cloud-rules-probes.json)
- [Kulesiz ilk bölüm](evidence/level1-idle-1x.json), [tek okçulu ilk bölüm](evidence/level1-one-archer-1x.json), [stun deneyi](evidence/stunned-siege.json)
- [Tüm kampanya tanımları](evidence/campaign-data.json), [denge hesabı](evidence/balance-analysis.json), [kışla önce/sonra mesafeleri](evidence/rally-followup-analysis.json)
- [İlk kaynak kimlikleri](evidence/source-hashes.json), [takip kaynak kimlikleri](evidence/followup-hashes.json)

`build-status.json`, `editmode.log`, `cloudcode.trx` ve `idle-level1.json` dosyaları sırasıyla sonuçsuz ortak Editor derleme girişimi, proje açıkken reddedilen ilk batch girişimi, eksik .NET 9 çalışma zamanı denemesi ve oturum bulunamayan ilk gözlemdir. Bunlar olumlu doğrulama kanıtı olarak kullanılmadı; başarılı veya tamamlanmış alternatif koşular yukarıda açıkça listelendi.

**Ürün yönü:** son savunma hattı fikrini koruyun; yolu tutma, tehdit seçme ve zamanında müdahale kararlarını anlaşılır hâle getirin. İlk 10 bölüm bu üç kararı tekrar tekrar farklı biçimde kullanabiliyorsa kampanyayı büyütmek anlam kazanır.
