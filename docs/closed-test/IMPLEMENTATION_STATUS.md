# İlk 10 bölüm — uygulama ve doğrulama raporu

Sürüm: **ct-20260906-01**, uygulama: **0.4.0**. Tarih: 6 Eylül 2026.

Kod, otomatik savaş testleri, ayrı bulut ortamı ve Android paketi hazır. iOS cihaz kodu derlemesi geçti; imzalı kurulum, fiziksel telefon performansı ve gerçek oyuncu kabulü henüz tamamlanmadı. Bu rapor kapalı testin bütün kabul koşullarının geçtiği anlamına gelmez.

## Kaynak ve çalışma alanı

- Çalışma kopyası: `/Users/emreaydin/DefenderChronicles-first-region`
- Dal: `codex/first-region-closed-test`
- Kullanıcının mevcut değişikliklerinin başlangıç görüntüsü: `5d486a5`.
- Savaş ve kayıt temeli: `34cf710`; ilk 10 bölüm uygulaması: `823c4f0`. Son kaynak ve paket özetleri: `BUILD_MANIFEST.json`.
- Unity 6000.4.1f1; Editor işlemleri, testler ve Unity derlemeleri Unity CLI üzerinden bu kopyada yürütüldü. Ortak açık Unity oturumu kullanılmadı.

## Uygulanan değişiklikler

**Savaş:** 60 Hz tek savaş saati, gerçek 1× süre ve ortak dalga başlangıç saatleri. Stun hareketi, iyileştirmeyi ve saldırıları durduruyor; başlayan kuşatma uyarısını iptal edip saldırı beklemesini yeniden başlatıyor. Slow gerçek hareket hızına uygulanıyor; güçlü etki öncelikli. Fiziksel ve büyü hasarı ortak çözümden geçiyor. Vanguard fiziksel hasarı yarıya indiriyor. Hasar, ses ve temas işaretleri saldırı olayından çıkıyor; animasyon oynanış sonucunu belirlemiyor. Mevcut yol üzerindeki kışla toplanma düzeni korundu.

**İlerleme ve kayıt:** Uygulama boyunca yaşayan kayıt/servis yöneticisi; hesaplara ayrı kayıt alanları; geçici dosya, bütünlük kontrolü ve son geçerli yedek ile güvenli yazma/kurtarma. Yazma hatası ilerlemeyi bellekte tutup yeniden deniyor. Bütün kopyalar okunamazsa özgün baytlar ayrı kurtarma klasörüne korunuyor ve aktarım sorunu gösteriliyor; okunamayan ilerlemenin kurtarıldığı iddia edilmiyor. Misafirden hesaba aktarım ayrı işlem. Hesap değişimi başka sahibin kuyruğunu göndermez. Çevrimdışı talepler tek kuyrukta artan beklemeyle denenir. Zafer talebi ve son dalga kaydının temizlenmesi aynı yerel yazmada gerçekleşir. Eski 11–50 ilerlemesi korunur.

**Sunucu:** `closed-test` UGS ortamında kurallar ve erişim politikası dağıtıldı. Yıldızlar kalan can ve bölüm kurallarından türetiliyor; sıfır can, yanlış sahip ve kilitli bölüm reddediliyor. Aynı talep ikinci ödül üretmiyor. Eski talepler ve kayıt aktarımı sürümlü işleniyor; reddedilme nedenleri saklanıyor. Üretim ortamına dağıtım veya toplu veri dönüşümü yapılmadı. Çevrimdışı savaşın tamamı sunucuda doğrulanmıyor.

**Kuleler ve bölümler:** Üç kule için altı uzmanlaşma; ikinci seviyeden sonra tek dal; ortak tanımdan maliyet, istatistik, hedef önceliği ve satış değeri. Okçu 160, kışla 180, büyücü 220 altın; dallar bölüm başarısıyla açılır. İlk 10 bölüm açık JSON dalga verisi kullanıyor. Boss %66/%33 fazları, 1,2 saniye susturma uyarısı, ek düşmanlar ve son faz hareket baskısı içeriyor. Bölüm seçimi 10 ile sınırlı; kayıt sınırı 50. Valor/XP verisi saklanıyor, bitmemiş harcama sistemi gösterilmiyor.

**Oyuncu akışı ve sunum:** İlk açılışta hesap olmadan başlama, ilk zafer sonrası hesap önerisi, sıradaki tamamlanmamış bölüme devam, ilk savaşta iki okçu → Kalkan Darbesi → üçüncü okçu → yükseltme sırasıyla eylem yönlendirmesi. Kule adı/maliyeti, eksik altın, satış tutarı, menzil ve basılı tutulan yetenek önizlemesi. Boş alandaki yetenek bekleme süresini tüketmez. Yenilgide dalga, başlıca sızan düşman ve savunma önerisi. Arka planda duraklama; uyumlu dalga kaydından aynı kaynaklarla dönüş. TR/EN savaş ve servis metinleri, veri kimliğinden düşman isimleri, büyük dokunma kontrolleri, safe-area yerleşimi, dost/düşman renk ayrımı, zırh simgesi ve uzmanlaşma işaretleri. Sızma ve kuşatma sesleri öncelikli; görsel karşılıkları mevcut.

**Ölçüm:** Kurulum, yükseltme, satış, ilk kule, yetenek, dalga, sızma, çıkış ve kayıt hataları yerel olay dosyalarına yazılıyor. Otomatik koşular oyuncu klasöründen ayrılıyor. Hazırlık/normal/yoğun/boss için kare süresi, bellek ve termal durum ölçümü var. Geliştirme otomasyonu yalnız geliştirme derlemesinde çalışır. HUD periyodik güncellemesi sınırlı; ek havuzlama veya materyal optimizasyonunun gerekliliği fiziksel profiler ölçümünden sonra belirlenecek.

## Tamamlanan doğrulama

- EditMode: **102/102**; yerel kurtarma, hesap sahipliği, eski ilerleme, combat ve veri kuralları.
- Tam PlayMode koşusu: **27/27**; ilk 10 bölümün 17 gerçek kaynak senaryosu, üç kule ağırlıklı boss stratejisi, savaş/kayıt/arayüz kontrolleri. Son sunum ve öğretim değişiklikleri ayrıca **24/24** geçen `playmode-ui-final.xml` dosyasında doğrulandı. Bu ek koşu önceki testlerin bir kısmını tekrarlar; sayılar bağımsız test sayısı olarak toplanmamalı.
- İlk öğretimin gerçek kaynak testi: iki okçu, Kalkan Darbesi, üçüncü okçu ve yükseltme ile zafer; sonuç ekranı olmadan tek bekleyen zafer talebi, yeniden açılışta temiz dalga kaydı.
- Cloud Code .NET: **12/12**.
- Gerçek UGS SDK denemesi: geçerli zafer, sunucuda yıldız türetme, sıfır can ve yanlış sahip reddi, yinelenen talepte sıfır ek ödül. Kanıt: `evidence/cloud-integration-proof.txt` ve `cloud-integration.json`.
- 30/60 FPS × 1/2/4/8 hız simülasyonlarında ilk bölümün sonucu aynı: zafer, 20 can, 542 altın, 140,81 saniye. Bu, fiziksel cihaz FPS ölçümü değildir.
- Altı dalın kontrollü karşılaştırmasında beklenen avantaj ölçüldü: Rapid Fire 102→120 hasar, Sharpshooter 102→183, Blast 118→142, Assault 156→320; Shield askerin ayakta kalma süresini 1,87→3,73 saniyeye çıkardı; Frost boss hareketine %25 yavaşlatma uyguladı. Bu rol deneyleri kontrollü kaynak kullanır; kampanya koşularındaki altın bütçesi gerçek oyun bütçesidir.

### 1× savaş süresi ve alternatif kurulumlar

Duraklama ve oyuncunun hazırlıkta beklemesi hariç. İki sonuç bulunan bölümlerde iki farklı kurulum kazanmıştır. Ayrıntı: `campaign-balance.csv`; bu 17 senaryonun 97 dalga kaydı: `wave-balance.csv`; aynı senaryoların satın alma zamanları: `purchase-timing.csv`. Sonuç ekranını yapay açan arayüz testleri bu tablolara dahil edilmedi.

| Bölüm | Ölçülen saniye | Hedef saniye |
|---:|---:|---:|
| 1 | 136,84 | 120–180 |
| 2 | 137,69 | 120–180 |
| 3 | 139,86 | 120–180 |
| 4 | 307,69 / 309,67 | 240–360 |
| 5 | 309,69 / 305,77 | 240–360 |
| 6 | 314,08 / 305,29 | 240–360 |
| 7 | 318,93 / 316,13 | 240–360 |
| 8 | 307,75 / 323,12 | 240–360 |
| 9 | 322,88 / 321,67 | 240–360 |
| 10 | 411,69 / 394,63 | 360–480 |

Otomatik satın alma stratejisinde bölüm 6–9 dalları geç safhada, boss bölümünde orta ve geç safhada alınıyor. Bu strateji önce temel kuleleri ve ikinci seviyeleri tamamlıyor. İnsanların dal seçimini ne zaman yaptığı ve altı dalın bölüm içindeki tercih değeri oyuncu testinde ayrıca ölçülecek; kontrollü hasar karşılaştırması oyuncu tercihi kanıtı değildir.

## Paketler ve kurulum durumu

- **Android:** Unity IL2CPP ARM64 geliştirme APK derlemesi başarılı, sıfır hata. `Builds/ClosedTest-Android/DefenderChronicles-ct-20260906-01.apk`. Gerçek APK dosyası 156.734.517 bayt (yaklaşık 149,5 MiB); imzası doğrulandı. SHA-256 manifestte. Android cihaz çalıştırması ölçülmedi.
- **iOS:** Unity Xcode projesi dışa aktarımı ve Xcode cihaz kodu derlemesi başarılı. `Builds/ClosedTest-iOS/Unity-iPhone.xcodeproj`; imzasız `.app`, `Builds/ClosedTest-Xcode/Build/Products/Debug-iphoneos/DefenderChroniclesTest.app` içinde.
- Her iki test uygulaması ayrı `com.emreaydin.defenderchronicles.closedtest` kimliğini kullanır. Mevcut telefondaki uygulamanın üzerine yazmaz.
- Xcode imzalı derleme, geliştirme ekibi için hesap oturumu ve bu uygulamaya uygun profil bulunmadığından tamamlanamadı. İmzasız iOS uygulaması telefona kurulabilir teslim sayılmaz. Adımlar: `INSTALL_TR.md`.

Editor'de incelenen uzmanlaşma ekranı: `evidence/specialization-ui-tr.png`. Bu görüntü cihaz ölçümü değildir.

## Açık kabul koşulları

1. Xcode Apple Developer hesabı/profili tamamlanınca kilidi açık iPhone 13'e kurulum ve 30 dakika koşu: çökme/düşük bellek yok; dört savaş safhasının her birinde medyan ≥55 FPS, p95 ≤18 ms; sürekli ciddi termal durum yok; ısınma sonrası eşdeğer döngülerde bellek büyümesi ≤%10. Bunlar henüz ölçülmedi.
2. Küçük/büyük fiziksel ekranlar, iki yatay yön, TR/EN ve gerçek 44×44 point dokunma alanı kontrolü. Editor ekran yakalaması fiziksel dokunma ve safe-area kabulünün yerine geçmez.
3. Önce 5–8 yeni oyuncunun ilk üç bölümü yardımsız oynaması, ardından 30–50 kişilik ilk bölge testi. Protokol ve boş gözlem tablosu hazır; katılımcı bulgusu üretilmedi.
4. Fiziksel ölçümde en pahalı sistemler belirlendikten sonra gerekiyorsa düşman/sağlık göstergesi havuzu, ölüm efektleri ve materyal maliyetlerine müdahale.

Bu dış koşullar tamamlanmadan kapalı test kabulü, performans başarısı veya mağazaya hazır sürüm olarak işaretlenmeyecek.
