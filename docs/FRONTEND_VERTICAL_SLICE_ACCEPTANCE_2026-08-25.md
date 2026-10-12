# ALLIN: Defense — Premium Medieval Frontend Dikey Dilim Kabul Özeti

Tarih: 25 Ağustos 2026

## Sonuç

Greenwatch için `Hoş Geldin → Ana Menü → Krallık Haritası → Brifing → Bölüm 1 → Sonuç`
zinciri premium medieval tema, ortak 9-slice bileşen ailesi, responsive yerleşim ve erişilebilir
durum geri bildirimleriyle çalışır durumdadır. Savaş dengesi, 50 bölümlük katalog ve kayıt şeması
değiştirilmemiştir.

## iOS mobil görsel kanıtı

iPhone 17 Pro Simulator üzerinde Unity iOS IL2CPP development build kuruldu. Dynamic Island
safe-area ile aşağıdaki ekranlar 2622×1206 yatay çözünürlükte doğrulandı:

- Ana Menü
- Krallık Haritası
- Bölüm Brifingi
- Bölüm 1 savaş HUD'ı

İlk kurulum deneyimi de iOS üzerinde güncellendi:

- Varsayılan Unity splash kaldırıldı.
- iOS yerel açılış yüzeyi Greenwatch kale görselini gösteriyor.
- Kısa arma açılışı doğrudan aynı sanat dilindeki Hoş Geldin ekranına geçiyor.
- Hoş Geldin paneli Dynamic Island safe-area içinde kalıyor; birincil eylem 460×92 referans piksel.
- Entegre PlayMode akışı 11/11 geçti ve runtime error/exception üretmedi.

İlk açılış kanıtları:
`docs/evidence/runtime/ios-first-impression-native-launch-20260825.png` ve
`docs/evidence/runtime/ios-first-impression-welcome-20260825-final.png`.

Mobil kanıt klasörü: `docs/evidence/runtime/`

Masaüstünde alınan aşağıdaki sekiz durumun 16:9, 20:9 ve 4:3 görüntüleri yalnızca çoklu oran
yerleşim referansıdır; mobil kabul veya performans kanıtı olarak değerlendirilmez:

- Hoş Geldin
- Hesap
- Ana Menü
- Krallık Haritası
- Bölüm Brifingi
- Bölüm 1 savaş HUD'ı
- Zafer
- Yenilgi

Oran referans klasörü: `Builds/FrontendEvidence/Captures/`

Kontrol edilen başlıca kusurlar: safe-area dışına taşma, başlık/sekme çakışması, seçim kartı/CTA
çakışması, hesap penceresi kapatma hedefi, eksik Türkçe glyph, harita kadrajı, sonuç animasyonu ve
eski tiled HUD çerçevesi. Son yakalamalarda bu kusurlar görülmedi.

## Otomatik testler

- EditMode: 62/62 geçti.
- PlayMode: 11/11 geçti.
- Derleme: Unity 6000.4.1f1 iOS Simulator IL2CPP ve Xcode Debug build başarılı
- iOS açılış günlüğü: hata/exception yok
- Mobil başlangıç profili: Medium kalite, 60 FPS hedefi ve ekran uyku kilidi etkin
- iPhone 17 Pro safe-area: `(x:186, y:60, width:2250, height:1146)`
- Kaynak denetimi: değiştirilen C# dosyalarında whitespace/diff hatası yok

## Performans ve kararlılık

iPhone 17 Pro Simulator iOS IL2CPP build üzerinde hazırlık, normal, yoğun ve boss senaryoları:

- Medyan: 60,0 FPS
- Boss son ölçüm p95 kare süresi: 17,1 ms
- 30 FPS altındaki en uzun kesintisiz süre: 0,10 sn
- Düşük bellek uyarısı: yok
- Sonuç: geçti

90 saniyelik yoğun savaş koşusu:

- 9 yoğun düşman/boss döngüsü
- Medyan: 60,0 FPS
- p95 kare süresi: 17,1 ms
- Isınma sonrası bellek artışı: %8,9
- Düşük bellek uyarısı: yok
- Sonuç: geçti

Ham kayıtlar: `Builds/ios-performance-acceptance.log` ve
`Builds/ios-soak-acceptance.log`.

Güncel mobil başlangıç görseli:
`docs/evidence/runtime/defender-simulator-mobile-runtime-20260825-landscape-readable.png`.

Bu sonuçlar iOS kod yolu, IL2CPP, safe-area, yaşam döngüsü ve otomasyon doğrulamasıdır.
Simulator kare süresi fiziksel mobil GPU veya termal performans kabulünün yerine geçmez.

## Kaynak ve lisans kaydı

Cinzel SemiBold, Greenwatch hoş geldin/harita görselleri ve projeye özgü medieval panel/buton
ailesi `DefenderPresentationCatalog` provenance listesine kaydedildi. Noto Sans mevcut gövde fontu
olarak korunmuştur.

## Kalan fiziksel cihaz kapısı

Bağlı iPhone 13 iOS 26.6 beta kullanırken kurulu Xcode'un iOS DeviceSupport/DDI sürümü 26.5'te kaldığı için developer disk image bağlanamadı. Fiziksel termal, titreşim, Profiler ve 60 FPS kabulü bu araç zinciri eşleşmesi düzelince ayrıca çalıştırılmalıdır.
Bu makinede Android Unity modülü ve bağlı Android cihaz bulunmadığı için Android APK/AAB ve cihaz
kabulü henüz çalıştırılmamıştır. Masaüstü ölçümleri mobil kalite kapısına dahil değildir.

Serialized ekran prefablarına tam geçiş henüz tamamlanmamıştır. Tema varlığı ve fallback sistemi
hazırdır; mevcut çalışma anı ekran üretimi, görsel dikey dilimi riske atmamak için korunmuştur.
