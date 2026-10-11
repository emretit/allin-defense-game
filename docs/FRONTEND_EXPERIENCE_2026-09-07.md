# Menü deneyimi uygulama ve doğrulama kaydı

Dikey menüler ve yatay savaş korunarak ana menü, harita, hesap ve ayarlar yeniden düzenlendi. Bölüm brifingi haritadaki seçim paneline taşındı. Savaş dengesi, 50 bölümlük katalog ve kayıt şeması değiştirilmedi.

## Uygulama

- Ekranlar ilk kullanımda oluşturulup yeniden kullanılıyor. Harita ilk ziyarette yalnız seçili bölgenin 10 düğümünü oluşturuyor; diğer dört bölge ziyaret edildiğinde hazırlanıyor. `FrontendNavigation` geçişi 150 ms ile yönetiyor; azaltılmış harekette kaydırma yok. Kesilen geçişlerin konum, görünürlük ve etkileşim durumu temizleniyor.
- Harita doğrudan arka plan görseli kullanıyor; ek kamera ve RenderTexture kaldırıldı. Gizli ekranların hareketleri duruyor.
- Ana menüde kompakt profil, kale görseli, bölüm kartı, Devam Et ve Krallık Haritası var. Hesap erişimi profil/ayarlarda.
- Harita bölgeleri yatay sayfalanıyor, bölümlerin rotası dikey kaydırılıyor. Bölüm adı, dalga/süre, düşman ve ödül bilgisi seçimle güncelleniyor. Savaşı Başlat sabit alt alanda. Güvenli alan oranı 0,70 ve üstünde harita ile bölüm paneli yan yana.
- Bölge, bölüm, kaydırma konumu ve hesap dönüş hedefi geçici gezinme durumunda tutuluyor. Eski brifing çağrıları haritayı açıyor.
- Misafir girişinde yerel menü hemen açılıyor; ağ girişi ve bulut eşitlemesi arka planda sürüyor. Geç gelen başarı/hata yanıtı açık savaştan menüye dönmüyor. İlerleme birleştirme kuralları korunuyor.
- Tekrar deneme ve sonraki bölüm asenkron sahne yüklemesi kullanıyor. Kalıcı hafif yükleme yüzeyi yeni sahne ve savaş hazır olduğunda kapanıyor. Savaş kurulumu karelere bölünüyor.
- Ekran dönüşü boyut ve güvenli alanın kararlı olmasını bekliyor; üç saniye sonunda açıklama ve yeniden deneme gösteriyor.
- Zaferde sonraki bölüm, yenilgide tekrar deneme öne çıkıyor. Son bölümde sonraki bölüm devre dışı. Yıldız animasyonu butonların kullanılmasını bekletmiyor.

## Otomatik kontroller

- EditMode: **84/84 geçti** (`Builds/FrontendExperience/editmode-result.json`).
- Tam PlayMode: **22/22 geçti**, savaş dengesi senaryoları dahil (`playmode-result.json`).
- Son yerleşim, bölüm seçimi, geç gelen giriş yanıtı, son-bölüm ve yükleme geri bildirimi testleri sonrasında odaklı PlayMode: **7/7 geçti** (`loading-feedback-playmode-final.xml`).
- Bölge içeriklerinin ilk ziyarette hazırlanması sonrasında mevcut Greybox testleri **13/13 geçti** (`greybox-final-result.json`).
- Hızlı ve tekrarlı gezinme, aynı ekrana tekrar gitme, çift savaş başlatma, harita konumunun korunması, hesap/ayar dönüşü, kararlı güvenli alan ve üç saniyelik yön zaman aşımı testleri mevcut.
- 1080×1920, 1080×2340 ve 1536×2048 oranlarında Türkçe/İngilizce; beş ekranın toplam **30 görüntüsü** alındı. Metin taşması kontrolü temiz; ana menü, harita, hesap ve ayar yerleşimleri görsel olarak incelendi (`Builds/FrontendExperience/Review`). Bunlar Unity Game View sonuçlarıdır.

## Ölçüm yöntemi ve sınırlar

İlk ziyaret ile 30 sıcak menü döngüsü ayrı kaydedilir. Editör ölçümleri fiziksel iOS sonucu değildir. İlk geri bildirim tanısal çalıştırıcıda eylemden sonraki ilk kareye kadar ölçülür; gerçek parmak-ekran gecikmesi ölçümü değildir. Bellek için nesne sayısı ve toplam ayrılmış Unity belleği izlenir; editörün GC belleği tek başına sızıntı kanıtı sayılmaz.

Editörün 30 sıcak döngü ölçümünde p95 ana menü → harita önce **163,1 ms**, sonra **150,8 ms**; harita → ana menü önce **161,8 ms**, sonra **153,8 ms**. Ayarlar p95 önce **3,8 ms**, sonra **3,8 ms**. Ham ölçümler `before-timing.json` ve `after-timing.json` içindedir. ≤100 ms ilk geri bildirim ve ≤250 ms menü kullanılabilirliği hedefleri fiziksel cihazda ayrıca doğrulanmalıdır.

Savaş yüklemesinin değişiklik öncesi eşdeğer cihaz ölçümü alınmadığı için savaşta önce/sonra hızlanma iddiası yoktur.

## iOS

Mevcut iOS son işlem adımı, güncel SDK’nın ASWebAuthenticationSession kullanan `.m` dosyasını artık değiştirmiyor. Böylece native OAuth geri çağrı API’si korunuyor. Eski `.mm` sürümü için yön düzeltmesi ve SafariServices bağlantısı tutuluyor. Güncel kaynak dosyasının son işlemden değişmeden çıktığı ayrıca kontrol edildi. Unity iOS çıktısı ve Xcode simülatör derlemesi başarılı.

İlk simülatör koşusunda bütün akış tamamlandı; sıcak menüler p95 yaklaşık 168 ms, ayarlar 17 ms ölçüldü. 30 döngüde 394 UI nesnesi sabit kaldı ve bellek yaklaşık 205,3 MB seviyesinde plato yaptı. Haritanın ilk açılışı 1.186 ms ile hedefi kaçırdı. Bu bulgu üzerine bölge içerikleri de ilk ziyarette oluşturulacak şekilde değiştirildi; son ölçüm aşağıdadır. İlk kurulum ölçümü `iOS/first-install-report.json` içinde korunuyor. Aynı eski sürümün yeniden başlatılmasında ilk harita 596 ms idi (`before-region-laziness-report.json`); grafik önbelleği koşulları farklı olduğundan 1.186 → son değer farkı yalnız kod değişikliğine atfedilemez.

### Son simülatör ölçümü

iPhone 13 simülatörü, iOS 26.5, 1170×2532, Development IL2CPP/arm64. İşlevsel koşu tamamlandı; taşma/akış hatası yok. Bu durum performans hedeflerinin tamamının geçtiği anlamına gelmez.

| Akış | İlk kare geri bildirimi (ms) | İlk/tek kullanılabilirlik (ms) | Sıcak p95 (ms) | En uzun kare (ms) |
|---|---:|---:|---:|---:|
| Ana menü → harita | 114.2 | 148.2 | 166.8 | 116.7 |
| Harita → ana menü | 16.3 | 166.7 | 167.3 | 18.0 |
| Ayarlar | 16.2 | 16.2 | 16.4 | 16.9 |
| İlk savaş | 77.2 | 463.9 | — | 166.8 |
| Tekrar deneme | 80.4 | 297.6 | — | 83.2 |
| Sonraki bölüm | 75.3 | 293.0 | — | 83.2 |
| Savaş → harita | 16.5 | 383.0 | — | 166.9 |

Yükleme gerektirmeyen menüler ≤250 ms kullanılabilirlik hedefini karşıladı. İlk harita geri bildirimi **114,2 ms**, ≤100 ms hedefinin **14,2 ms üzerinde** kaldı. Bu metrik gezinme eyleminden sonraki ilk kareyi ölçer; butona ilk dokunma/PointerDown geri bildirimi ve fiziksel dokunma gecikmesi ayrıca ölçülmelidir. Diğer ölçülen ilk kare geri bildirimleri 100 ms altında.

30 sıcak döngüde UI nesne sayısı **179 → 179**. Toplam ayrılmış Unity belleği **190.077.035 → 190.077.163 bayt**; son beş örnek 190,08 MB civarında dalgalandı, sürekli büyüme görülmedi. Bu, simülatördeki bu koşuya ilişkin gözlemdir; fiziksel cihaz sızıntı kabulünün yerine geçmez.

Ham son ölçüm: `Builds/FrontendExperience/iOS/report.json`. 11 ekran görüntüsü aynı klasörde. Native derleme kaydı: `Builds/FrontendExperience/xcode-build.log`; Unity derleme sonucu: `unity-build-result.json`.

### Fiziksel cihaz ölçümü

Kablolu iPhone 13, iOS 26.6 beta, 1170×2532, Development IL2CPP/arm64. İmzalı uygulama cihaza kuruldu ve otomatik uçtan uca kabul akışı tamamlandı. İlk giriş, ana menü, harita, ayarlar, hesap, savaş, yenilgi, tekrar deneme, zafer, sonraki bölüm ve haritaya dönüş adımları geçti; taşma veya akış hatası kaydedilmedi.

| Akış | İlk kare geri bildirimi (ms) | İlk/tek kullanılabilirlik (ms) | Sıcak p95 (ms) | En uzun kare (ms) |
|---|---:|---:|---:|---:|
| Ana menü → harita | 312.5 | 679.6 | 152.0 | 365.9 |
| Harita → ana menü | 30.3 | 30.3 | 165.5 | 31.5 |
| Ayarlar | 52.9 | 52.9 | 32.8 | 53.8 |
| İlk savaş | 178.6 | 3520.9 | — | 2129.5 |
| Tekrar deneme | 186.3 | 570.1 | — | 232.8 |
| Sonraki bölüm | 178.5 | 541.4 | — | 199.5 |
| Savaş → harita | 20.7 | 402.9 | — | 166.7 |

Yükleme gerektirmeyen sıcak menü geçişleri ≤250 ms p95 hedefini karşıladı. İlk harita açılışı, savaş girişleri ve sahne hazırlığındaki uzun kareler hedefi karşılamadı; bunlar fiziksel cihazda kalan performans işleri olarak kaydedildi. 30 sıcak döngüde UI nesne sayısı **190 → 190** kaldı. Toplam ayrılmış Unity belleği **228.467.890 → 228.500.706 bayt** oldu ve son altı örnek yaklaşık 228,50 MB seviyesinde plato yaptı; sürekli artış görülmedi.

Ham ölçüm ve 11 ekran görüntüsü `Builds/FrontendExperience/iOS-Physical/` altındadır. Bu ölçümden sonra savaş yükleme yüzeyinin yön değiştirme ve sahne yükleme başlamadan önce çizilmesi sağlandı; EditMode **6/6**, odaklı PlayMode **7/7** geçti. Güncel sürüm fiziksel telefona yeniden kuruldu ve test ortam değişkeni olmadan açıldı. Otomatik kabul akışı tekrar çalıştırılmadığı için tablodaki fiziksel süreler bu son geri bildirim sıralaması değişikliğini içermez.

Simülatörde uygulama Ayarlar uygulaması açılarak arka plana gönderildi ve aynı süreç yeniden öne getirildi; seçili bölüm ve harita konumu görsel olarak korundu (`iOS/before-background.png`, `iOS/after-background.png`). Gerçek parmak hareketi ve fiziksel cihaz yaşam döngüsü ölçümü değildir.

## Yeniden çalıştırma

Unity PlayMode test filtresi: `FrontendExperienceTests`. EditMode filtresi: `FrontendNavigationTests`. Üretim sunumu içeren iOS Development simülatör derlemesi için `tools/run_frontend_experience_simulator.sh` kullanılır; tanılama ayrı geçici kampanya kaydı kullanır. Ekran görüntüsü paketi editörde `FrontendExperienceReview.CaptureSuite` ile üretilebilir.
