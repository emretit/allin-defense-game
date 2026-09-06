# Kapalı oyuncu testi — ct-20260906-01

Bu belge test yürütme kılavuzudur; tamamlanmış oyuncu araştırması sonucu değildir.

## İlk tur: 5–8 yeni oyuncu

Temiz misafir profili, 1× hız, ilk üç bölüm. Gözlemci çözüm anlatmaz. Katılımcı kimliği rastgele kod olur; hesap, e-posta ve doğum tarihi toplanmaz.

1. Oyunu aç. İlk kuleyi yardım almadan kur. İlk başarılı kurulumun süresini ve yanlış dokunmaları kaydet.
2. İlk üç bölümü oyna. Her başarısız denemede ulaşılan dalga, kalan can, kule yerleşimi, satın alma ve yetenek kullanımı olay kayıtlarıyla eşleştirilir.
3. Her bölüm sonunda sor: “Bir sonraki denemede neyi değiştirirdin?”
4. Üçüncü bölüm sonunda sor: “Okçu, kışla ve büyücü ne işe yarıyor? Kalkan Darbesini ne zaman kullanırsın?” Açıklamayı olduğu gibi not et; yönlendirici örnek verme.
5. Bir dalga sırasında uygulamayı arka plana al; tekrar aç. Uygulamayı kapatıp yeniden açarak dalga başı dönüşünü ve kaynakları karşılaştır.
6. Hesap bağlama teklifinin anlaşılıp anlaşılmadığını sor. Gerçek hesap kullanmak şart değildir.

Başarı kapısı: ilk kule yardım gerektirmeden kuruluyor; oyuncu üç rolü ve yeteneğin amacını kendi sözleriyle açıklayabiliyor. Hata sayısı, süre ve alıntılar kaydedilir; örneklemden istatistiksel kesinlik iddia edilmez.

## İkinci tur: 30–50 oyuncu

İlk turdaki engelleyici sorunlar çözüldükten sonra aynı sürümün ilk 10 bölümü dağıtılır. Bölüm bazında deneme, kazanma, yıldız, terk etme dalgası, kule/dal seçimi, satış ve kayıt hatası oranları çıkarılır. İlk kez oynama ve tekrar oynama ayrı hesaplanır. Otomasyon verisi `telemetry/automated`, oyuncu verisi `telemetry/players` klasöründedir.

## Fiziksel cihaz

İlk cihaz iPhone 13. Hazırlık, normal, yoğun ve boss örnekleri ayrı değerlendirilir. En az 30 dakika normal oyun/yeniden deneme; 60 FPS profilinde medyan ≥55 FPS ve p95 ≤18 ms. Isınma sonrası aynı bölüm döngülerinde bellek artışı ≤%10; düşük bellek uyarısı/çökme ve sürekli ciddi termal durum kabul edilmez. Simülatör veya editör bu kapıyı karşılamaz.

Küçük/büyük ekran ve iki yatay yön için TR/EN düğme metinleri, safe area, fiziksel point boyutları ve çakışmalar kontrol edilir. Arayüz testinin geçmesi fiziksel dokunma kullanılabilirliğinin kanıtı değildir.

## Birleştirme

Kaynak commit ve dosya hash’leri, test XML dosyaları, uygulama paketi hash’i, cihaz modeli/OS, ölçüm başlangıç-bitişi, olay kaydı ve gözlem notu aynı build kimliğine bağlanır. Eksik ölçüm “geçti” sayılmaz. Üretim hesabı kayıtları test verisine katılmaz.
