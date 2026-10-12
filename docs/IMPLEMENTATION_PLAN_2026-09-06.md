# ALLIN: Defense — Uygulama Planı

Tarih: 2026-09-06

## Tamamlanan ilk sprint

- Unity Editor ve Unity CLI bağlantısı doğrulandı.
- 78 EditMode testi geçti.
- 16 PlayMode testi geçti.
- Project Auditor taraması 0 sorunla tamamlandı.
- Okçu, Kışla ve Büyü ağırlıklı üç gerçek-ekonomi stratejisi için tam bölüm kabul testleri eklendi.
- Kışla askerlerinin karakol çevresinde kalıp düşman yoluna ulaşamaması düzeltildi.
- Kışla askerlerinin dayanıklılık, hasar, saldırı aralığı ve yeniden katılma süresi dengelendi.

### Strateji kabul sonucu

Test bölümü: Level 10, 6 normal dalga + boss, gerçek başlangıç altını ve düşman ödülleri, 4× desteklenen oyun hızı.

| Strateji | Sonuç | Kalan kale canı | Son dizilim |
|---|---:|---:|---:|
| Okçu ağırlıklı | Zafer | 12 / 20 | 5 ana karakol, 8 / 8 slot |
| Kışla ağırlıklı | Zafer | 10 / 20 | 5 ana karakol, 8 / 8 slot |
| Büyü ağırlıklı | Zafer | 20 / 20 | 5 ana karakol, 8 / 8 slot |

Bu sonuç otomatik denge tabanını kapatır; gerçek oyuncu hissi ve normal hız kabulünün yerini almaz.

## Sonraki sprintler

### 1. Fiziksel cihaz kabulü — en yüksek öncelik

- 2026-09-06 hazırlık kontrolünde iPhone 13 eşleşmiş ve kullanılabilir bulundu; cihaz yalnız yerel ağ üzerinden bağlı olduğu için fiziksel kabul koşusu USB bağlantısı bekliyor.
- iPhone ve Android cihazlarda soğuk açılış, safe area ve iki yatay yön kontrolü.
- Sekiz slotta hızlı ardışık ve iki parmaklı dokunma stresi.
- Yoğun savaşta FPS, yüzde 95 kare süresi, tepe bellek ve termal durum ölçümü.
- Arka plana alma, geri dönme, pause, devam ve temiz restart doğrulaması.

Mevcut kurulu Simulator kabul yapısında yoğun savaş provası geçti: medyan 60 FPS, p95 kare süresi 17,3 ms ve 30 FPS altındaki en uzun dönem 0,15 saniye. Bu koşu son kışla düzeltmesinden önce kurulmuş yapıyı kullandığı için yalnız performans tabanıdır. Simulator tepe belleği 991,7 MB olduğu için bu değer fiziksel cihazdaki 750 MB kapısının yerine kullanılmayacak.

### 2. Dayanıklılık ve gerçek hız dengesi

- 30–60 dakikalık kesintisiz koşu.
- Üç stratejiyi 1× hızda manuel oynayıp bölüm süresi, kalan can ve karar noktalarını kaydetme.
- Büyü ağırlıklı planın 20 / 20 sonucu nedeniyle güç farkını oyuncu hissiyle tekrar değerlendirme.

### 3. Çevrim içi servisler

- Cloud Code dağıtımı.
- Gerçek Authentication ve Cloud Save hesabıyla kayıt birleştirme, yeniden giriş ve hata senaryoları.
- Çevrim dışı ilerleme ile sunucu ilerlemesinin güvenli birleşimini cihazda doğrulama.

### 4. Dış oyuncu ve mağaza hazırlığı

- En az beş yeni oyuncuyla yardımsız ilk bölüm testi.
- Zorluk, anlaşılabilirlik, tekrar oynama isteği ve 8–12 dakika hedef süresini ölçme.
- İmzalama, sürüm numarası, gizlilik politikası, yaş derecelendirmesi, mağaza metni ve görseller.

## Yayın kararı

Kod tabanı ve otomatik savaş kabulü sağlıklı. Bir sonraki gerçek yayın engeli fiziksel cihaz performansı ve dokunma kabulüdür; bunu geçmeden mağaza adımına geçilmemelidir.
