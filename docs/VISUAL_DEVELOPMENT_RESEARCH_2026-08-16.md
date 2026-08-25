# Defender Chronicles — Görsel Geliştirme Araştırması

Durum: Üretim öncesi araştırma ve karar kaydı
Tarih: 2026-08-16
Hedef: Mevcut tek haritayı, sekiz actor rolünü, altı karakol görünümünü, HUD'u, animasyonları ve VFX'i sanat vertical slice kalitesine taşımak.

Bu belge, `docs/VISUAL_DEVELOPMENT_TODO.md` dosyasının karar kaynağıdır. Tam ekran ve safe-area çalışmasının ayrıntıları için `docs/UI_FULL_SCREEN_RESEARCH_2026-08-16.md` kullanılmalıdır. Önceki araştırma ve mevcut kullanıcı değişiklikleri korunacaktır.

## 1. Yönetici Özeti

Defender Chronicles'ın mevcut sunumu işlevsel ve oynanabilir bir teknik temel oluşturuyor. Unity 6 URP, sabit izometrik kamera, responsive yatay HUD, üç karakol türü, sekiz actor rolü, ses durumları ve lisans kayıtları hazır. Buna karşın mevcut görsel dil henüz sanat vertical slice seviyesinde değil:

- Sekiz rol, katalogda yalnızca beş özgün actor prefabını paylaşıyor; bazı roller renk ve ölçek farkıyla ayrılıyor.
- Dokuz ortak animasyon klibi kemik/rig animasyonu yerine actor kökündeki konum ve ölçek eğrilerinden oluşuyor.
- Darbe, iyileştirme, büyü, inşa ve boss olayları için oynanış anlamı taşıyan ayrı VFX aileleri henüz yok.
- Altı karakol görünümü mevcut olsa da rol ve seviye ayrımı oyun kamerasından daha güçlü okunmalı.
- Greenwatch Valley'in yol ve oynanış rotası okunuyor; fakat zemin, prop dağılımı, landmark ve harita sınırı çeşitliliği sınırlı.
- Tam ekran HUD cihaz alanını kullanıyor; sonraki ihtiyaç layout'u değiştirmek değil, kontrastı, durum geri bildirimini ve fantasy materyal dilini geliştirmek.

Karar: Mevcut gece atmosferli, düşük/orta detaylı stylized low-poly fantasy temel korunacak. Yeni bir sanat yönüne geçilmeyecek; siluet, aksesuar, materyal, ışık, animasyon ve VFX yoluyla Defender Chronicles kimliği derinleştirilecek. İlk üretim kapsamı tek harita ve mevcut oynanış içeriğiyle sınırlı olacak.

## 2. İncelenen Proje Durumu

### 2.1 Teknik temel

- Unity sürümü: `6000.4.1f1`.
- Render pipeline: Universal Render Pipeline.
- UI sistemi: uGUI / Canvas / RectTransform.
- Hedef platform sırası: iPhone, Android telefon, tablet.
- Savaş ekranı: yatay, sabit izometrik kamera.
- Oyun süresi hedefi: 8–12 dakika.
- Normal savaş performans hedefi: 60 FPS.
- Yoğun boss sahnesi kabul alt sınırı: 30 FPS.
- İlk hedef cihaz sınıfı: iPhone 13; ekran oranı doğrulama cihazı: iPhone 17 Pro simülatörü.

### 2.2 Sunum kataloğu envanteri

`DefenderPresentationCatalog` şu üretim arayüzünü sağlıyor:

- 8 actor rolü: Knight, Guard, Raider, Vanguard, Regenerator, Support, Siege, Boss.
- 3 karakol türü × 2 seviye: Archer, Barracks, Mage için toplam 6 görünüm.
- Environment, outpost slot ve command point prefabları.
- 11 ortak palet materyali ve 5 lisanslı atlas materyali.
- 9 ortak actor klibi: Idle, Move, Attack, Hit, Defeat, Ability, Victory, Phase, BossAttack.
- 22 ses olayı ve 6 müzik durumu.
- Font, ses ve üçüncü taraf sanat kaynağı provenance alanları.

İlk baseline sırasında katalog bütün rolleri dolduruyor ancak özgünlük bire bir değildi:

| Roller | Mevcut paylaşım | Sonuç |
|---|---|---|
| Knight + Guard | Aynı prefab | İki savunmacı rol uzaktan birbirine fazla yakın |
| Raider + Boss | Aynı prefab | Boss, normal düşmanın büyütülmüş varyantı gibi algılanabilir |
| Regenerator + Support | Aynı prefab | İyileştirici ve yenilenen düşman silueti ayrışmıyor |
| Vanguard | Özgün prefab | Kütle ve silah dili güçlendirilmeli |
| Siege | Özgün prefab | Mekanik/ağır rol okunabilirliği güçlendirilmeli |

2026-08-16 M0 uygulama notu: Katalog ve `DefenderPresentationAssetBuilder`, sekiz actor rolü için artık `Assets/Art/Models/Generated` altındaki role özel proxy FBX'leri kullanacak şekilde güncellendi. Bu, final kalite kabulü değildir; yalnızca ortak prefab probleminden çıkıp Knight/Raider/Archer L1 stil kanıtına daha doğru bir taban hazırlar.

### 2.3 Kaynak dosya ve üretim altyapısı

`ArtSource/Blender` altında sekiz actor, altı karakol seviyesi, environment, slot ve command point için düzenlenebilir Blender kaynakları bulunuyor. Bu, dış assetleri doğrudan kullanmak yerine dönüştürülmüş özgün varyantlar üretmek için uygun bir temel sağlıyor.

Mevcut actor animasyonları `Assets/Art/GeneratedPresentation/DC_*.anim` altında tutuluyor. Kliplerde boş path'e bağlı root `Position` ve `Scale` eğrileri var; kemik bazlı hareket bulunmuyor. KayKit Adventurers upstream paketi rig ve animasyon içeriyor; ancak repoda saklanan seçilmiş `*Licensed.fbx` dosyaları Blender inspection'a göre tek meshli, armature/action içermeyen statik proxy dosyalarıdır. Üretim aşamasında iki çıktı ayrılmalı:

1. Rig ve gerekli klipleri koruyan runtime actor prefabı.
2. İkon, uzak LOD veya statik gösterim için animasyonsuz hafif proxy.

2026-08-16 VIS-202 notu: `docs/evidence/visual-development/actor-rig-source-inspection-2026-08-16.json` mevcut yerel KayKit actor FBX'lerinde `armature_count: 0` ve `action_count: 0` olduğunu gösterir. Runtime rig üretimi için full rigli CC0 kaynakların yeniden alınması veya özgün rigli kaynak üretimi gerekir.

2026-08-25 üretim kararı: vertical slice kapsamındaki sekiz özgün/generated actor artık yedi kemikli hafif Generic transform rig kullanır; görünür parçalar kemiklerin altında tutulur ve role-specific override controller'lar kemik eğrileri içerir. Bu karar, depodaki seçilmiş KayKit dosyalarının statik olması ve tek haritalık mobil slice için tekrarlanabilir/ölçülebilir bir kaynak hattı gerekmesi nedeniyle alındı. İlk plandaki Knight, Guard, Raider, Vanguard ve Support için Humanoid retarget hedefi bu kabul build'inin tamamlanmış bir özelliği olarak gösterilmeyecektir; gelecekte tam skinned kaynak kütüphanesi edinilirse ayrı bir kalite yükseltmesi olarak ele alınacaktır. Gameplay sonuçları animation event'lerine bağlanmadı; mevcut `Attack`, `Hit`, `Defeat`, `Ability`, `Victory`, `Phase` ve `BossAttack` trigger sözleşmeleri korundu.

### 2.4 Görsel baseline

Ana kanıt: `docs/evidence/defender-fullscreen-iphone17pro-2026-08-16.png`.

Görüntüden çıkarılanlar:

- Harita bütün yatay ekranı başarıyla dolduruyor; üst ve alt HUD fiziksel ekran kenarına uzanıyor.
- Yol, slotlar, düşman girişi ve savunma kalesi ilk bakışta seçilebiliyor.
- Soğuk mavi, koyu lacivert ve turkuaz gece paleti tutarlı bir başlangıç oluşturuyor.
- Büyük koyu zemin alanları ve aynı kümelerin tekrarı haritayı düz gösteriyor.
- Yol taşı ritmi ve çevre prop dağılımı mekanik tekrar hissi veriyor.
- Slotlar çevreden iyi ayrılıyor ancak boş/dolu/yükseltilebilir durumları görsel olarak daha açık olmalı.
- Yapılar kaliteli temel assetlere sahip; tür ve seviye farkının küçük mobil ölçekte kuvvetlendirilmesi gerekiyor.
- HUD metinleri okunuyor ancak koyu panel üzerindeki bazı durumlar ve ikincil metinler daha yüksek kontrast gerektiriyor.
- Hazırlık görüntüsünde actor kalabalığı olmadığı için unit silueti, yoğun savaş ve VFX okunabilirliği henüz kanıtlanmış değil.

Performans baseline'ı bu araştırmada ölçülmüş kabul edilmez. CPU/GPU frame time, draw call, triangle, overdraw, GC allocation ve bellek değerleri `VIS-000` aşamasında gerçek cihazdan kaydedilecektir.

## 3. Seçilen Sanat Yönü

### 3.1 Görsel ifade

Ana cümle: **Ay ışığı altındaki Greenwatch Valley'de, soğuk mavi savunma mimarisinin sıcak altın komuta işaretleriyle kırmızı/turuncu istilaya karşı durduğu okunaklı izometrik fantasy savunma sahnesi.**

Korunacak özellikler:

- Stylized low-poly geometri.
- Büyük ve sade mobil siluetler.
- Gece atmosferi ve soğuk savunmacı paleti.
- Taş, ahşap ve metal ağırlıklı savunma mimarisi.
- Altın kaynak/komuta; kırmızı/turuncu tehlike; cyan/mor büyü kodları.
- Sabit izometrik oynanış okunabilirliği.

Geliştirilecek özellikler:

- Rol bazlı beden oranları, aksesuarlar ve silah yönleri.
- Yapı türü ve upgrade seviyesinde kütle/hacim değişimi.
- Zemin katmanları, yol kenarı ve landmark çeşitliliği.
- Saldırı türlerini anlatan ayrı hareket ve VFX şekilleri.
- HUD'da etkileşim, cooldown, seçili, kilitli ve yetersiz kaynak durumları.

Kaçınılacak özellikler:

- Başka bir oyunun UI kompozisyonunu, ikonlarını, karakter oranlarını veya haritasını kopyalamak.
- Rol ayrımını yalnız renge bırakmak.
- Küçük ölçekte kaybolan ince detay ve gürültülü texture kullanmak.
- Bütün büyüleri aynı halka, bütün darbeleri aynı parıltı olarak göstermek.
- Boss'u normal düşmanın yalnızca büyütülmüş hali yapmak.

### 3.2 Palet ve kontrast görevleri

| İşlev | Ana yön | Kullanım kuralı |
|---|---|---|
| Savunmacı | Soğuk mavi + sınırlı altın | Büyük zırh yüzeyi mavi/çelik; komuta ve önemli kenarlar altın |
| Düşman | Koyu kırmızı + turuncu | Saldırı ve tehdit noktalarında sıcak vurgu; zeminden değer kontrastıyla ayrılmalı |
| Doğa | Koyu turkuaz/yeşil | Oynanabilir birimlerden daha düşük doygunluk |
| Yol | Açık sıcak taş/toprak | Ana rota, zemin ve prop kümelerinden değer olarak ayrılmalı |
| Büyü | Cyan, mor ve yeşil aileleri | Cyan saldırı/arcane, yeşil heal/regen, mor disrupt/phase |
| Kaynak/CTA | Altın | Yalnız ekonomi, ödül ve ana ilerleme eylemleri |
| Hata/tehlike | Kırmızı/turuncu | Kısa süreli vurgu; kalıcı metin rengi olarak aşırı kullanılmamalı |

Renk değerleri art bible aşamasında ekran görüntüsü üzerinden sabitlenecektir. Bilgi hiçbir zaman yalnız hue farkıyla kodlanmayacak; şekil, ikon, hareket veya outline desteği bulunacaktır.

## 4. Actor Görsel Hedefleri

| Rol | Siluet hedefi | Malzeme/renk | Hareket ve VFX dili |
|---|---|---|---|
| Knight | Geniş omuz, büyük kalkan, kısa kılıç, yere sağlam basan ağırlık merkezi | Soğuk çelik, mavi kumaş, altın komuta vurgusu | Kontrollü kılıç vuruşu, kalkan darbesi, kısa mavi-altın şok dalgası |
| Guard | Knight'tan daha ince ve uzun; öne uzanan mızrak ile yatay okuma | Mavi kumaş, açık çelik, küçük arma | Hızlı mızrak dürtüsü, blok/parry kıvılcımı |
| Raider | Öne eğik gövde, hafif zırh, ayrık silah/kollar | Koyu deri, kırmızı kumaş, turuncu vurgu | Hızlı kesme izi, kısa hız çizgisi, çevik ölüm |
| Vanguard | Geniş gövde, kalın zırh, büyük iki elli silah | Koyu çelik, bordo/kızıl plakalar | Ağır anticipation, yer darbesi, taş/toz parçaları |
| Regenerator | Asimetrik organik uzantı, sırt/totem öğesi, parlak çekirdek | Koyu gövde, yeşil emissive çatlaklar | Nabız gibi heal/regen halkası, filiz/spor parçaları |
| Support | İnce dik duruş, asa veya fener, açık negatif alan | Nötr kumaş, cyan/yeşil enerji | Belirgin cast pozu, hedefe akan parçacık, sakin aura |
| Siege | Düşük ve geniş mekanik/ağır profil; büyük saldırı aparatı | Demir, ahşap, koyu kırmızı bağlantılar | Yavaş kurulum, güçlü recoil, duman/enkaz ve yönlü mermi |
| Boss | Üç fazda değişen, diğerlerinden belirgin büyük ve benzersiz siluet | Siyah çelik, sıcak çekirdek, faza göre artan emissive | Faz kırılması, arena uyarısı, özel saldırı telegraph'ları; basit ölçek büyütme yok |

Her actor teslimatı birlikte şu parçaları içermelidir: model, materyal, portre/ikon, selection marker ilişkisi, blob/contact shadow, idle/move/attack/hit/defeat, rol özel animasyonu, rol VFX'i, katalog bağlantısı ve gameplay-zoom kanıtı.

## 5. Karakol Görsel Hedefleri

| Tür/seviye | Kütle ve landmark | Rol işareti | Seviye farkı |
|---|---|---|---|
| Archer L1 | İnce ahşap gözetleme kulesi | Yay, ok kılıfı, küçük hedef | Hafif ve açık platform |
| Archer L2 | Daha yüksek/kapalı okçu karakolu | İki yönlü okçuluk alanı, hedef, büyük banner | Yükseklik, çatı ve silah eki artar |
| Barracks L1 | Alçak muhafız noktası | Kalkan, mızrak rafı, küçük çadır | Yatay ve ağır temel |
| Barracks L2 | Güçlendirilmiş kışla | Silah rafı, sur parçası, daha büyük mavi banner | Taş kütle ve çevre eklentileri büyür |
| Mage L1 | Açık kristal tapınak | Tek cyan kristal ve küçük rune halkası | İnce, parlak, dikey odak |
| Mage L2 | Yüksek arcane sanctuary | Çoklu kristal, rune, enerji sütunu | Yükseklik ve emissive ritmi belirgin artar |

Kabul kuralı: 160–220 piksel genişlikte gri ton thumbnail'a bakıldığında tür ve seviye doğru tahmin edilebilmelidir. Upgrade yalnız prefab scale veya renk değişimiyle yapılamaz.

## 6. Greenwatch Valley Çevre Hedefi

Harita oynanış geometrisi korunacak; görsel geliştirme rota veya sekiz build slotunun işlevini değiştirmeyecek.

Katmanlar:

1. **Zemin tabanı:** koyu turkuaz ana yüzey, büyük ölçekli renk kırılması ve hafif yükseklik varyasyonu.
2. **Yol:** okunaklı sıcak yüzey, daha doğal kenar ritmi, seyrek çatlak/iz decal veya meshleri.
3. **Oynanış landmark'ları:** düşman kapısı, command point/keep, sekiz slot ve boss yaklaşım bölgesi.
4. **Orta alan dekoru:** kontrollü ağaç, kaya, çalı, ot, kristal ve küçük ışık kümeleri.
5. **Harita sınırı:** foreground taş/bitki parçaları, uzak dağ/orman silueti ve ay ışığı katmanı.
6. **Atmosfer:** sıcak-soğuk odak ışıkları, düşük maliyetli sis/partikül ve sınırlı emissive vurgular.

Dağılım kuralları:

- Aynı prop arka arkaya aynı rotation/scale ile tekrarlanmayacak.
- Kritik rota ve slot çevresinde görsel gürültü azaltılacak.
- Dekor yoğunluğu ekran kenarlarında artabilir; oynanış merkezinde düşürülür.
- En az üç landmark kümesi kullanılacak: giriş tehdidi, orta vadi odağı, savunmacı kale odağı.
- Dekor collider'ları dokunma ve pathing'i etkilemeyecek.

## 7. Animasyon ve VFX Araştırması

KayKit Adventurers upstream paketi dört rigli karakter, 75 animasyon ve 25'ten fazla aksesuar sunuyor. Bu nedenle ilk tercih, full rigli CC0 kaynaklar projeye tekrar alınabildiğinde aynı iskeleti kullanan rollerde paketin gerçek kliplerini seçip Unity import ayarlarıyla kırpmak/retarget etmek; sonrasında rol aksesuarları ve özel animasyonlarla ayrıştırmaktır. Mevcut repodaki seçilmiş KayKit actor FBX'leri yalnız statik proxy olduğundan VIS-202 kapanmadan önce full rigli kaynak edinimi veya özgün rig üretimi gerekir. Rig uyumsuz veya özgün mekanik assetlerde Blender kaynak animasyonu ya da kontrollü root fallback kullanılabilir.

Animasyon ilkeleri:

- Oynanış hasarı, anticipation–impact–recovery zamanlamasındaki impact anıyla senkron olmalı.
- Root motion gameplay pozisyonunu yönetmemeli; hareket sistemi otorite olarak kalmalı.
- Idle ve move loop'ları actor kökünü zaman içinde kaydırmamalı.
- Defeat animasyonu collider/pathing davranışını geciktirmemeli.
- Boss faz geçişi oynanış telegraph süresiyle bire bir eşleşmeli.
- Animasyon event'leri yalnız görsel/ses tetiklemeli; hasar kararını animation event vermemeli.

VFX aileleri:

| Aile | Şekil dili | Renk | Zamanlama hedefi |
|---|---|---|---|
| Fiziksel darbe | Kısa yönlü kama/kıvılcım | Beyaz, çelik, az turuncu | Çok kısa, tek impact |
| Ok | İnce yönlü iz + küçük yüzey darbesi | Açık ahşap/altın | Mermi hızıyla okunur |
| Arcane | Geometrik shard/rune | Cyan/mor | Cast, uçuş, impact ayrılır |
| Shield bash | Ön koni + kırılan halka | Mavi/altın | Telegraph sonrası tek güçlü pulse |
| Heal/regen | Yukarı akan parçacık + açık halka | Yeşil/cyan | Yumuşak, tekrarlı pulse |
| Slow | Ayak çevresinde kırık buz/rune | Açık mavi | Debuff boyunca düşük yoğunluk |
| Disrupt | Kesintili mor glitch/rune | Mor/magenta | Kısa giriş, durum işareti |
| Build/upgrade/sell | Yukarı/aşağı geometri ve toz | Mavi-altın / nötr | Ekonomi eylemiyle aynı anda |
| Spawn/death | Yön ve fraksiyona özel giriş/dağılma | Takım paleti | Pool dönüşünü gizleyecek kadar kısa |
| Boss phase | Geniş fakat sınırları belirgin telegraph | Kırmızı/turuncu/mor | Hasar alanını önceden açıklar |

VFX'ler pooled olacak. Yoğun savaşta aynı ailenin efektleri concurrency sınırı ve önem sırasına göre azaltılabilecek.

## 8. HUD ve Tam Ekran Kullanımı

Mevcut kararlar korunur:

- Battlefield ekranı doldurur.
- Dekoratif HUD yüzeyleri fiziksel kenara kadar uzanır.
- Dokunulabilir ve okunabilir içerik safe area içinde kalır.
- Kamera cihaz oranına göre kritik savaş alanını kadrajlar.

Sonraki görsel geliştirme alanları:

- Normal, hover/pressed, selected, disabled, cooldown ve unaffordable buton durumlarını aynı bileşen diliyle tanımlamak.
- Bir karakol seçildiğinde dünya selection marker'ı ile bağlamsal panelin aynı renk/ikon dilini kullanması.
- Dalga numarası, hazırlık/combat/boss durumu ve yaklaşan tehdit bilgisinin tek bakışta taranması.
- Cooldown'u metne bağımlı bırakmayan radial/linear fill ve hazır pulse'u.
- UI çerçevelerinde Kenney tabanını doğrudan çoğaltmak yerine mevcut mavi metal/taş kimliğiyle işlenmiş sınırlı 9-slice set kullanmak.
- Küçük metin ve ikincil durumlarda kontrastı gerçek cihazda doğrulamak.

Referans uygulama ve ekran oranı araştırması: `docs/UI_FULL_SCREEN_RESEARCH_2026-08-16.md`.

## 9. Açık Kaynak ve CC0 Kaynak Araştırması

### 9.1 Projede kullanılan kaynaklar

| Kaynak | Durum | Lisans | Projedeki kullanım | Kayıt |
|---|---|---|---|---|
| [KayKit Adventurers](https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0) | Kullanılıyor | CC0 1.0 | Dört karakter tabanı ve atlasları | `Assets/ThirdParty/ART_ASSET_SOURCES.md` |
| [KayKit Medieval Hexagon](https://github.com/KayKit-Game-Assets/KayKit-Medieval-Hexagon-Pack-1.0) | Kullanılıyor | CC0 1.0 | Yapı, ağaç, kaya ve ortak atlas | `Assets/ThirdParty/ART_ASSET_SOURCES.md` |
| [Kenney Fantasy UI Borders](https://kenney.nl/assets/fantasy-ui-borders) | Kullanılıyor | CC0 1.0 | HUD frame tabanı | `Assets/ThirdParty/ART_ASSET_SOURCES.md` |

KayKit Medieval Hexagon'ın resmi deposu 200'den fazla mobil uyumlu low-poly model ve tek 1024×1024 gradient atlas sunduğunu belirtiyor. Adventurers deposu dört rigli karakter, 75 animasyon ve 25'ten fazla aksesuar bildiriyor. Kenney paketi 140 UI dosyası içeriyor. Bu bilgiler paketlerin üretim tabanı olmaya uygun olduğunu gösteriyor; buna rağmen oyunda kullanılan her dosya yine seçilerek, özelleştirilerek ve provenance kaydıyla alınmalıdır.

### 9.2 Değerlendirilecek kaynak

| Kaynak | Durum | Lisans | Uygun kullanım | Karar kapısı |
|---|---|---|---|---|
| [Quaternius Medieval Village MegaKit](https://quaternius.com/packs/medievalvillagemegakit.html) | Henüz projeye alınmadı | CC0 | Modüler duvar, çatı, kapı, pencere, sarmaşık ve köy prop tabanları | Ölçek, stil, atlas ve draw-call karşılaştırması sonrası yalnız seçili parçalar |

Quaternius resmi sayfası 300'den fazla modüler parça, FBX/OBJ/glTF biçimleri ve CC0 lisans bildiriyor. Kaynak sürümde URP kurulumu bulunması ücretsiz paketin tamamının projeye eklenmesi gerektiği anlamına gelmez. İlk kullanım öncesinde seçilen dosya listesi, indirme hash'i, lisans kopyası ve işlenmiş çıktı yolları kaydedilecektir.

2026-08-25 kapanış kararı: Quaternius paketi bu vertical slice'a indirilmedi veya kopyalanmadı. Greenwatch Valley, provenance kaydı mevcut KayKit Medieval Hexagon parçaları ile özgün/generated modeller kullanılarak tamamlandı. Böylece kullanılmayan bir dış pakete yapay sürüm/hash kaydı üretilmedi; kabul edilen KayKit ve Kenney dosyalarının kaynak, lisans, revision, tarih, SHA-256 ve işlenmiş yolları `Assets/ThirdParty/ART_ASSET_SOURCES.md` ve `docs/evidence/visual-development/asset-inventory-2026-08-16.md` içinde izlenebilir durumda kaldı.

### 9.3 Kaynak kabul politikası

Her dış asset için şu alanlar zorunludur:

- Paket ve üretici adı.
- Resmi kaynak URL'si.
- Lisans türü ve repository içindeki lisans kopyası.
- İndirme/edinme tarihi.
- Git kaynağıysa commit revision; arşiv kaynağıysa sürüm.
- İndirilen arşiv veya kaynak dosyanın SHA-256 değeri.
- Kullanılan özgün dosyaların listesi.
- `Assets/ThirdParty` altındaki kaynak yolu.
- `ArtSource` altındaki işlenebilir kaynak yolu.
- `Assets/Art` altındaki runtime çıktı yolu.
- Yapılan değişikliklerin kısa dönüşüm notu.

Ücretli paket alınmayacak. Bir assetin lisansı belirsizse, dosya projeye eklenmeyecek. CC0 olması doğrudan görsel kullanım kararı değildir; stil, performans, ölçek ve özgünlük kapısından ayrıca geçmelidir.

## 10. Mobil URP Performans Bütçesi

Unity'nin resmi URP dokümanına göre gölge maliyetini görünür caster/receiver sayısı, shadow light sayısı, cascade sayısı, çözünürlük ve soft shadow belirler. Point light gölgesi altı yönden render edildiği için mobilde özellikle pahalıdır. Bu nedenle kalite profilleri şu şekilde ele alınacaktır:

| Alan | Low | Medium | High |
|---|---|---|---|
| Hedef | Düşük seviye telefonlarda okunabilir 30+ FPS | iPhone 13 sınıfında 60 FPS hedefi | Güçlü cihazda ek görsel kalite |
| Render scale | Profilleme sonrası 0.8–0.9 adayı | 1.0 başlangıç | 1.0 |
| Main light shadow | Kapalı veya düşük çözünürlük/kısa mesafe | Tek cascade, sınırlı mesafe | En fazla iki cascade; ölçüm şartı |
| Additional light shadow | Kapalı | Kapalı | Yalnız gerekçeli ve aynı anda çok sınırlı |
| Soft shadow | Kapalı | Kapalı veya Low | Low/Medium yalnız GPU bütçesi uygunsa |
| Point light shadow | Kullanılmaz | Kullanılmaz | Varsayılan olarak kullanılmaz |
| VFX | Düşük parçacık ve concurrency | Standart | Sınırlı ek parçacık/ışık |

İçerik bütçeleri ilk profiling turunda kesinleştirilecek başlangıç sınırlarıdır:

- Actor prefabı: mümkünse 1 atlas, en fazla 2 runtime materyal slotu.
- Karakol prefabı: mümkünse 1 atlas, en fazla 3 runtime materyal slotu; emissive ayrıysa gerekçeli.
- Aynı anda görünür normal actor: mevcut oyun hedefi olan 60'ın altında.
- Transparan VFX: kısa ömür, küçük ekran alanı, overdraw görünümünde kontrol.
- VFX instantiate/destroy: normal combat sırasında kullanılmaz; pool zorunlu.
- Gameplay sırasında sürekli managed allocation: Profiler'da sıfıra yakın; tekrar eden spike kabul edilmez.
- Texture import: mipmap, compression, max size ve atlas kararı hedef cihaz ölçümüne göre yapılır.
- Dekor collider'ı ve Update çalışan dekor scripti: varsayılan olarak yok.

Mevcut `UniversalRP.asset` render scale 1, dört additional light/object sınırı, 50 birim shadow distance ve tek cascade içeriyor. Bu değerler hedef değil baseline'dır; `VIS-800` aşamasında Low/Medium/High profilleri gerçek cihaz ölçümüyle ayrıştırılacaktır.

Kaynaklar:

- [Unity 6 — Optimize shadow rendering in URP](https://docs.unity3d.com/6000.0/Documentation/Manual/shadows-optimization.html)
- [Unity 6 — Adjust settings to improve URP performance](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/optimize-for-better-performance.html)

## 11. Mevcut Durum / Hedef Kalite Matrisi

| Alan | Mevcut durum | Hedef kalite | Ana eksiklik | Önerilen çözüm |
|---|---|---|---|---|
| Actor | 8 katalog rolü, 5 özgün prefab | 8 bağımsız okunabilir rol | Paylaşılan siluetler | Rol art sheet'i, aksesuar/model düzenleme, özel materyal |
| Boss | Raider tabanını paylaşıyor | Üç fazlı benzersiz ana tehdit | Ölçeklenmiş normal düşman hissi | Özgün gövde/zırh, faz parçaları, telegraph seti |
| Animasyon | 9 root-transform klibi | Rol bazlı rig animasyonları | Kemik hareketi ve silah senkronu yok | Kaynak rig kliplerini koruyan import pipeline |
| VFX | Genel shape/ring yaklaşımı | Olay bazlı VFX aileleri | Oynanış anlamı zayıf | Yön, şekil, renk ve timing sözlüğü |
| Karakol | 6 çalışan görünüm | Tür/seviye thumbnail'da okunur | Upgrade farkı küçük kalabilir | Kütle, yükseklik ve aksesuar değişimi |
| Zemin | Tekdüze koyu yüzey | Katmanlı gece vadisi | Büyük boş ve düz alanlar | Büyük ölçek renk kırılması, küçük prop/decal katmanı |
| Yol | Net fakat düzenli | Doğal fakat okunaklı rota | Mekanik kenar tekrarı | Kenar varyantı, seyrek kırık/iz, kontrollü prop |
| Çevre | Tekrarlanan ağaç/kaya kümeleri | Üç landmark'lı özgün biome | Varyasyon ve odak az | Set dressing kuralları, varyant/rotation/scale dağılımı |
| HUD | Responsive ve tam ekran | Fantasy, yüksek kontrast, durumları açık | Bazı durumlar metne/renge bağımlı | State sprite/fill/ikon/animasyon sistemi |
| Performans | Hedefler var, ölçüm baseline'ı yok | Profil bazlı kanıtlı bütçe | Gerçek cihaz ölçümü eksik | iPhone 13 profiler capture ve quality matrix |
| Erişilebilirlik | Temel okunabilirlik | Renkten bağımsız durum dili | Shape/outline standardı eksik | Gri ton, thumbnail, kontrast ve touch-target testleri |

## 12. Sanat Vertical Slice Kabul Standardı

Vertical slice aşağıdaki şartların tamamında kabul edilir:

- Sekiz actor rolü gameplay zoom'da, gri tonda ve küçük thumbnail'da ayırt edilir.
- Üç karakol türü ile L1/L2 seviyeleri yalnız renk/scale kullanılmadan ayırt edilir.
- Boss üç fazda model parçası, emissive, animasyon ve VFX ile görünür şekilde değişir.
- Hazırlık, normal dalga, yoğun dalga, boss faz 1/2/3, ability, victory ve defeat anlarının kanıt görüntüleri vardır.
- Fiziksel, ok, arcane, heal/regen, slow, disrupt, build/upgrade/sell, spawn/death ve boss VFX aileleri birbirinden ayrılır.
- HUD 16:9, 19.5:9–20:9 ve 4:3 oranlarında battlefield'ı gereksiz küçültmez; bütün etkileşimli içerik safe area içinde kalır.
- Sekiz build slotu, dokunma hedefleri, route, kamera ve oynanış davranışında regresyon yoktur.
- Normal savaş iPhone 13 sınıfında 60 FPS hedefini karşılar; yoğun boss sahnesi 30 FPS altına kalıcı olarak düşmez.
- Low ve Medium profillerde shadow-casting point light bulunmaz; VFX pooling doğrulanır.
- Dış asset provenance kayıtları eksiksizdir.
- Placeholder veya başka oyundan kopyalanmış görsel bulunmaz.

## 13. Kapsam Dışı

- Yeni oyun mekaniği veya denge değişikliği.
- Yeni harita, kampanya veya bölüm üretimi.
- Yeni hero veya mevcut sekiz rol dışında actor eklemek.
- Meta menüler, mağaza, ekipman, PvP, sosyal özellikler.
- Ücretli asset satın almak.
- Tam ekran HUD'u sıfırdan başka bir layout sistemine geçirmek.

## 14. Üretim Sırası Kararı

1. Baseline ve art bible tamamlanır.
2. Knight, Raider ve Archer L1 ile küçük bir stil/pipeline kanıtı hazırlanır.
3. Kanıt kabul edilince kalan actor ve karakollar aynı kurallarla üretilir.
4. Environment ve landmark pass yapılır.
5. Rig animasyon ve VFX aileleri bağlanır.
6. HUD state polish ve ses-görsel senkron tamamlanır.
7. Profiling, cihaz matrisi ve final ekran görüntüsü kabulü yapılır.

Bu sıra, bütün assetleri üretip en sonda stil veya performans uyumsuzluğu fark etme riskini azaltır. Ayrıntılı, bağımlılık sıralı görevler `docs/VISUAL_DEVELOPMENT_TODO.md` içindedir.
